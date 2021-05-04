#region Copyright (c) OPTANO GmbH

// ////////////////////////////////////////////////////////////////////////////////
// 
//        OPTANO GmbH Source Code
//        Copyright (c) 2010-2021 OPTANO GmbH
//        ALL RIGHTS RESERVED.
// 
//    The entire contents of this file is protected by German and
//    International Copyright Laws. Unauthorized reproduction,
//    reverse-engineering, and distribution of all or any portion of
//    the code contained in this file is strictly prohibited and may
//    result in severe civil and criminal penalties and will be
//    prosecuted to the maximum extent possible under the law.
// 
//    RESTRICTIONS
// 
//    THIS SOURCE CODE AND ALL RESULTING INTERMEDIATE FILES
//    ARE CONFIDENTIAL AND PROPRIETARY TRADE SECRETS OF
//    OPTANO GMBH.
// 
//    THE SOURCE CODE CONTAINED WITHIN THIS FILE AND ALL RELATED
//    FILES OR ANY PORTION OF ITS CONTENTS SHALL AT NO TIME BE
//    COPIED, TRANSFERRED, SOLD, DISTRIBUTED, OR OTHERWISE MADE
//    AVAILABLE TO OTHER INDIVIDUALS WITHOUT WRITTEN CONSENT
//    AND PERMISSION FROM OPTANO GMBH.
// 
// ////////////////////////////////////////////////////////////////////////////////

#endregion

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Akka.Actor;
    using Akka.Cluster;
    using Akka.Routing;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.GrayBox;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// The generation evaluation actor is responsible of evaluating a whole generation, given by a <see cref="GenerationEvaluation{TInstance,TResult}"/> message.
    /// </summary>
    /// <typeparam name="TTargetAlgorithm">The target algorithm type.</typeparam>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class GenerationEvaluationActor<TTargetAlgorithm, TInstance, TResult> : ReceiveActor, IWithUnboundedStash, IDisposable
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult>
    {
        #region Fields

        /// <summary>
        /// The <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </summary>
        private readonly ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> _targetAlgorithmFactory;

        /// <summary>
        /// The <see cref="IRunEvaluator{TInstance,TResult}"/>.
        /// </summary>
        private readonly IRunEvaluator<TInstance, TResult> _runEvaluator;

        /// <summary>
        /// The configuration.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _configuration;

        /// <summary>
        /// The <see cref="IActorRef"/> to the result storage actor.
        /// </summary>
        private readonly IActorRef _resultStorageActor;

        /// <summary>
        /// The parameter tree.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// The <see cref="ICustomGrayBoxMethods{TResult}"/>.
        /// </summary>
        private readonly ICustomGrayBoxMethods<TResult> _customGrayBoxMethods;

        /// <summary>
        /// The current <see cref="GenerationEvaluation{TInstance,TResult}" />'s sender. It is null if no generation evaluation is currently executed.
        /// </summary>
        private IActorRef _generationEvaluationIssuer;

        /// <summary>
        /// The current <see cref="IGenerationEvaluationStrategy{TInstance,TResult}"/>.
        /// </summary>
        private IGenerationEvaluationStrategy<TInstance, TResult> _evaluationStrategy;

        /// <summary>
        /// The evaluations by assigned actor dictionary.
        /// </summary>
        private Dictionary<IActorRef, GenomeInstancePair<TInstance>> _evaluationsByAssignedActor;

        /// <summary>
        /// The list of genomes waiting for results from storage.
        /// </summary>
        private HashSet<ImmutableGenome> _genomesWaitingForResultsFromStorage;

        /// <summary>
        /// The current generation evaluation message.
        /// </summary>
        private GenerationEvaluation<TInstance, TResult> _currentGenerationEvaluation;

        /// <summary>
        /// The <see cref="IActorRef"/> to the evaluation actor router.
        /// </summary>
        private IActorRef _evaluationActorRouter;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationEvaluationActor{TTargetAlgorithm, TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="targetAlgorithmFactory">The <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/>.</param>
        /// <param name="runEvaluator">The <see cref="IRunEvaluator{TInstance,TResult}"/>.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="resultStorageActor">The <see cref="IActorRef"/> to the result storage actor.</param>
        /// <param name="parameterTree">The parameter tree.</param>
        /// <param name="customGrayBoxMethods">The <see cref="ICustomGrayBoxMethods{TResult}"/>.</param>
        public GenerationEvaluationActor(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            IRunEvaluator<TInstance, TResult> runEvaluator,
            AlgorithmTunerConfiguration configuration,
            IActorRef resultStorageActor,
            ParameterTree parameterTree,
            ICustomGrayBoxMethods<TResult> customGrayBoxMethods)
        {
            this._targetAlgorithmFactory = targetAlgorithmFactory ?? throw new ArgumentNullException(nameof(targetAlgorithmFactory));
            this._runEvaluator = runEvaluator ?? throw new ArgumentNullException(nameof(runEvaluator));
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._resultStorageActor = resultStorageActor ?? throw new ArgumentNullException(nameof(resultStorageActor));
            this._parameterTree = parameterTree ?? throw new ArgumentNullException(nameof(parameterTree));

            // No need to check for null. Might be null by purpose.
            this._customGrayBoxMethods = customGrayBoxMethods;

            // If Akka.Cluster gets used, watch for disconnecting cluster members to make evaluation rollbacks possible.
            if (Context.System.HasExtension<Cluster>())
            {
                Cluster.Get(Context.System).Subscribe(this.Self, typeof(ClusterEvent.UnreachableMember));
            }

            this.Become(this.WaitingForWorkers);
            this.Self.Tell(new CheckWorkers());
        }

        #endregion

        #region Public properties

        /// <inheritdoc />
        public IStash Stash { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public void Dispose()
        {
            // Kill the evaluation actors
            try
            {
                this._evaluationActorRouter?.Tell(new Broadcast(PoisonPill.Instance));
            }
            catch (Exception)
            {
                // ignored
            }

            // Kill the evaluation actor router
            try
            {
                this._evaluationActorRouter?.Tell(PoisonPill.Instance);
            }
            catch (Exception)
            {
                // ignored
            }

            // Send status failure to issuer, if available.
            this._generationEvaluationIssuer?.Tell(
                new Status.Failure(new InvalidOperationException("Critical error occurred in the generation evaluation actor.")));
            this.SetGenerationEvaluationIssuer(null);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called when the actor is started. Creates the desired number of evaluation actors, managed by a router. This router is responsible for starting new evaluation actors, whenever a node joins the cluster.
        /// </summary>
        protected override void PreStart()
        {
            this._evaluationActorRouter = Context.ActorOf(
                Props.Create(
                        () => new EvaluationActor<TTargetAlgorithm, TInstance, TResult>(
                            this._targetAlgorithmFactory,
                            this._configuration,
                            this._parameterTree,
                            this._customGrayBoxMethods,
                            this.Self))
                    .WithRouter(FromConfig.Instance),
                AkkaNames.EvaluationActorRouter);

            base.PreStart();
        }

        /// <summary>
        /// The supervisor strategy for the evaluation actors. Stops the tuning, whenever an exception is thrown on these actors.
        /// </summary>
        /// <returns>The supervisor strategy.</returns>
        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new AllForOneStrategy(
                0,
                Timeout.InfiniteTimeSpan,
                exception =>
                    {
                        this.HandleErrorInEvaluationActor(exception, null);
                        return Directive.Stop;
                    });
        }

        /// <summary>
        /// Sets the generation evaluation issuer.
        /// </summary>
        /// <param name="actorRef">The <see cref="IActorRef"/>.</param>
        private void SetGenerationEvaluationIssuer(IActorRef actorRef)
        {
            this._generationEvaluationIssuer = actorRef;
        }

        /// <summary>
        /// Actor is waiting for workers to join the cluster s.t. a sufficient number of <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>s are available.
        /// </summary>
        private void WaitingForWorkers()
        {
            // While we are still waiting for workers, generation evaluation messages cannot be handled. Therefore stash them for later.
            this.Receive<GenerationEvaluation<TInstance, TResult>>(message => this.Stash.Stash());

            this.Receive<CheckWorkers>(
                checkWorkers =>
                    {
                        var self = this.Self;
                        this._evaluationActorRouter.Ask<Routees>(new GetRoutees())
                            .ContinueWith(tr => new WorkerCount(tr.Result.Members.Count())).PipeTo(self);
                    });

            // If the worker count is too small, it should be checked again soon.
            this.Receive<WorkerCount>(
                workerCount => Context.System.Scheduler.ScheduleTellOnce(
                    TimeSpan.FromSeconds(1),
                    this.Self,
                    new CheckWorkers(),
                    this.Self),
                workerCount => workerCount.Count == 0);

            // If a sufficient number of workers is available, change to ready state.
            this.Receive<WorkerCount>(
                workerCount => this.BecomeReady(),
                workerCount => workerCount.Count > 0);

            // Unreachable member events should always be handled to keep the Akka.NET cluster clean.
            this.Receive<ClusterEvent.UnreachableMember>(
                this.HandleUnreachableMessage);
        }

        /// <summary>
        /// Move to <see cref="Ready" /> state.
        /// </summary>
        private void BecomeReady()
        {
            this.SetGenerationEvaluationIssuer(null);
            this._evaluationsByAssignedActor = new Dictionary<IActorRef, GenomeInstancePair<TInstance>>();
            this.Become(this.Ready);

            // Unstash possible generation evaluation messages.
            this.Stash.UnstashAll();
        }

        /// <summary>
        /// The <see cref="Ready"/> state is responsible for handling generation evaluation messages.
        /// </summary>
        private void Ready()
        {
            this.Receive<GenerationEvaluation<TInstance, TResult>>(
                generationEvaluation =>
                    {
                        this.SetGenerationEvaluationIssuer(this.Sender);
                        this._currentGenerationEvaluation = generationEvaluation;
                        this._evaluationStrategy = generationEvaluation.EvaluationStrategyFactory(
                            this._runEvaluator,
                            generationEvaluation.Participants,
                            generationEvaluation.Instances);
                        this._genomesWaitingForResultsFromStorage = new HashSet<ImmutableGenome>(
                            generationEvaluation.Participants,
                            ImmutableGenome.GenomeComparer);

                        this.Become(this.FetchingResultsFromStorage);
                        foreach (var genome in this._genomesWaitingForResultsFromStorage.ToList())
                        {
                            this._resultStorageActor.Tell(new GenomeResultsRequest(genome));
                        }
                    });

            // Unreachable member events should always be handled to keep the Akka.NET cluster clean.
            this.Receive<ClusterEvent.UnreachableMember>(
                this.HandleUnreachableMessage);
        }

        /// <summary>
        /// The <see cref="FetchingResultsFromStorage"/> state is responsible for handling genome result messages.
        /// </summary>
        private void FetchingResultsFromStorage()
        {
            this.Receive<GenomeResults<TInstance, TResult>>(
                genomeResults =>
                    {
                        if (this._genomesWaitingForResultsFromStorage.Remove(genomeResults.Genome))
                        {
                            foreach (var instance in this._currentGenerationEvaluation.Instances)
                            {
                                var genomeInstancePair = new GenomeInstancePair<TInstance>(genomeResults.Genome, instance);
                                if (genomeResults.RunResults.TryGetValue(instance, out var result))
                                {
                                    this._evaluationStrategy.GenomeInstanceEvaluationFinished(
                                        genomeInstancePair,
                                        result);
                                }
                                else
                                {
                                    this._evaluationStrategy.RequeueEvaluation(genomeInstancePair);
                                }
                            }
                        }

                        if (this._evaluationStrategy.IsGenerationFinished)
                        {
                            this._generationEvaluationIssuer?.Tell(this._evaluationStrategy.CreateResultMessageForPopulationStrategy());
                            this.BecomeReady();
                            return;
                        }

                        if (!this._genomesWaitingForResultsFromStorage.Any())
                        {
                            this.BecomeWorking();
                        }
                    });

            // Unreachable member events should always be handled to keep the Akka.NET cluster clean.
            this.Receive<ClusterEvent.UnreachableMember>(
                this.HandleUnreachableMessage);
        }

        /// <summary>
        /// Move to <see cref="Working"/> state.
        /// </summary>
        private void BecomeWorking()
        {
            this._evaluationStrategy.BecomeWorking();
            this.Become(this.Working);
            this.AskForWorkers();
        }

        /// <summary>
        /// The <see cref="Working"/> state is responsible for handling hello world, accept and evaluation result messages.
        /// </summary>
        private void Working()
        {
            this.Receive<HelloWorld>(
                message =>
                    {
                        // If the evaluation actor was already assigned to an evaluation, requeue its last assigned evaluation.
                        if (this._evaluationsByAssignedActor.Remove(this.Sender, out var lastEvaluation))
                        {
                            LoggingHelper.WriteLine(
                                VerbosityLevel.Warn,
                                $"The generation evaluation actor assumed that the evaluation actor {this.Sender} was working on an evaluation, while receiving a hello world message from it. Therefore requeue sender's last assigned evaluation and reschedule sender.");
                            this._evaluationStrategy.RequeueEvaluation(lastEvaluation);
                        }

                        this.Sender.Tell(new Poll());
                    });

            this.Receive<Accept>(
                message =>
                    {
                        // If the evaluation actor was already assigned to an evaluation, requeue its last assigned evaluation.
                        if (this._evaluationsByAssignedActor.Remove(this.Sender, out var lastEvaluation))
                        {
                            LoggingHelper.WriteLine(
                                VerbosityLevel.Warn,
                                $"The generation evaluation actor assumed that the evaluation actor {this.Sender} was working on an evaluation, while receiving an accept message from it. Therefore requeue sender's last assigned evaluation and reschedule sender.");
                            this._evaluationStrategy.RequeueEvaluation(lastEvaluation);
                        }

                        this.StartNextNotAlreadyStartedEvaluation(this.Sender);
                    });

            this.Receive<EvaluationResult<TInstance, TResult>>(
                resultMessage =>
                    {
                        // If the evaluation actor was not assigned to this evaluation or the genome instance pairs do not match, ignore the evaluation result.
                        if (!this._evaluationsByAssignedActor.Remove(this.Sender, out var expectedGip)
                            || resultMessage.GenomeInstancePair != expectedGip)
                        {
                            LoggingHelper.WriteLine(
                                VerbosityLevel.Warn,
                                $"The generation evaluation actor assumed that the evaluation actor {this.Sender} was not working on an evaluation, while receiving a result message from it. Therefore ignore sender's result and reschedule sender.");
                        }

                        // Else, store the evaluation result.
                        else
                        {
                            this._evaluationStrategy.GenomeInstanceEvaluationFinished(resultMessage.GenomeInstancePair, resultMessage.RunResult);
                            this._resultStorageActor.Tell(resultMessage);
                        }

                        // If the generation is not finished, start the next not already started evaluation ...
                        if (!this._evaluationStrategy.IsGenerationFinished)
                        {
                            this.Sender.Tell(new Poll());
                            return;
                        }

                        // ... else: If some tasks are still running, wait for them to terminate ...
                        if (this._evaluationsByAssignedActor.Any())
                        {
                            if (!this._configuration.EnableRacing)
                            {
                                var exception = new InvalidOperationException(
                                    "The generation evaluation strategy reports that the generation is finished, but the generation evaluation actor thinks that there are running evaluations, while racing is disabled!");
                                LoggingHelper.WriteLine(
                                    VerbosityLevel.Warn,
                                    $"Error: {exception.Message}");
                                throw exception;
                            }

                            LoggingHelper.WriteLine(
                                VerbosityLevel.Info,
                                "The generation is completed by racing, but the generation evaluation actor is waiting for running tasks to terminate.");
                            return;
                        }

                        // ... else, send generation result to issuer.
                        this._generationEvaluationIssuer?.Tell(this._evaluationStrategy.CreateResultMessageForPopulationStrategy());
                        this.BecomeReady();
                    });

            // Unreachable member events should always be handled to keep the Akka.NET cluster clean.
            this.Receive<ClusterEvent.UnreachableMember>(
                this.HandleUnreachableMessage);
        }

        /// <summary>
        /// Starts the next not already started evaluation, if any, by sending it to the given actor.
        /// </summary>
        /// <param name="actor">The actor.</param>
        private void StartNextNotAlreadyStartedEvaluation(IActorRef actor)
        {
            while (this._evaluationStrategy.TryPopEvaluation(out var nextEvaluation))
            {
                if (this._evaluationsByAssignedActor.Values.Contains(nextEvaluation.GenomeInstancePair))
                {
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Info,
                        $"The following genome instance pair is not started a second time, because it is already running.{Environment.NewLine}{nextEvaluation.GenomeInstancePair}");
                }
                else
                {
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Info,
                        $"Sending the following genome instance pair to evaluation actor {actor}.{Environment.NewLine}{nextEvaluation.GenomeInstancePair}");
                    this._evaluationsByAssignedActor.Add(actor, nextEvaluation.GenomeInstancePair);
                    actor.Ask<EvaluationResult<TInstance, TResult>>(nextEvaluation).ContinueWith(
                        (task) =>
                            {
                                if (task.IsFaulted)
                                {
                                    this.HandleErrorInEvaluationActor(task.Exception, this.Sender);
                                }

                                return task.Result;
                            }).PipeTo<EvaluationResult<TInstance, TResult>>(this.Self, this.Sender);
                    return;
                }
            }

            actor.Tell(new NoJob());
            LoggingHelper.WriteLine(VerbosityLevel.Info, "No open evaluations left. Waiting for running evaluations!");
        }

        /// <summary>
        /// Handles an error in an evaluation actor by logging the error and sending a <see cref="Status.Failure"/> message to the generation evaluation issuer.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="actor">The evaluation actor.</param>
        private void HandleErrorInEvaluationActor(Exception exception, IActorRef actor)
        {
            var actorString = actor == null ? "an evaluation actor" : $"evaluation actor {this.Sender}";
            LoggingHelper.WriteLine(
                VerbosityLevel.Warn,
                $"Critical error occurred in {actorString}.{Environment.NewLine}Message: {exception.Message}");
            this._generationEvaluationIssuer?.Tell(new Status.Failure(exception));
            this.SetGenerationEvaluationIssuer(null);
        }

        /// <summary>
        /// Handles a <see cref="ClusterEvent.UnreachableMember" /> message by rolling back all evaluations that had been assigned to that member and removing the member from the cluster.
        /// </summary>
        /// <param name="unreachableMember">The message.</param>
        private void HandleUnreachableMessage(ClusterEvent.UnreachableMember unreachableMember)
        {
            var addressOfUnreachableMember = unreachableMember.Member.Address;

            LoggingHelper.WriteLine(VerbosityLevel.Warn, $"Rollback unreachable member with address {addressOfUnreachableMember}!");

            // Roll back all evaluations assigned to actors placed on unreachable node.
            this.RollBackOnAddress(addressOfUnreachableMember);

            // Remove node from cluster.
            Cluster.Get(Context.System).Down(addressOfUnreachableMember);

            // See if any of the other workers is free to handle the rolled back evaluations.
            this.AskForWorkers();
        }

        /// <summary>
        /// Rolls back all assigned evaluations on the specified address.
        /// </summary>
        /// <param name="address">The address.</param>
        private void RollBackOnAddress(Address address)
        {
            IEnumerable<IActorRef> assigneesOnAddress = this._evaluationsByAssignedActor.Keys
                .Where(actor => actor.Path.Address == address)
                .ToList();

            foreach (var evaluationActor in assigneesOnAddress)
            {
                if (!this._evaluationsByAssignedActor.Remove(evaluationActor, out var withdrawnEvaluation))
                {
                    continue;
                }

                this._evaluationStrategy.RequeueEvaluation(withdrawnEvaluation);

                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Withdrew the following evaluation from evaluation actor {evaluationActor}, because Akka.NET marked the member as unreachable.{Environment.NewLine}{withdrawnEvaluation}");
            }
        }

        /// <summary>
        /// Sends a <see cref="Poll"/> to any evaluation actor in order to ask for availability.
        /// </summary>
        private void AskForWorkers()
        {
            this._evaluationActorRouter.Tell(new Poll());
        }

        #endregion
    }
}