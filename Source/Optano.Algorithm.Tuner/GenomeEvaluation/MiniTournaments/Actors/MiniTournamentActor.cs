#region Copyright (c) OPTANO GmbH

// ////////////////////////////////////////////////////////////////////////////////
// 
//        OPTANO GmbH Source Code
//        Copyright (c) 2010-2020 OPTANO GmbH
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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Akka.Actor;
    using Akka.Routing;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Communication;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// Responsible for executing mini tournaments.
    /// </summary>
    /// <typeparam name="TTargetAlgorithm">
    /// Type of the target algorithm.
    /// Must implement <see cref="ITargetAlgorithm{TInstance,TResult}"/>.
    /// </typeparam>
    /// <typeparam name="TInstance">
    /// Type of instance the target algorithm is able to process.
    /// Must be a subtype of <see cref="InstanceBase"/>.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// Type of result of a target algorithm run.
    /// Must be a subtype of <see cref="ResultBase{TResultType}"/>.
    /// </typeparam>
    public class MiniTournamentActor<TTargetAlgorithm, TInstance, TResult> : GenomeEvaluationDelegatorBase<TInstance, TResult>
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult> where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// Algorithm tuner configuration parameters.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _configuration;

        /// <summary>
        /// The parameter tree, needed to identify active genes that have to be passed to the target algorithm.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// Reference to the actor which is responsible for storing all evaluation results that have been observed so
        /// far.
        /// </summary>
        private readonly IActorRef _resultStorageActor;

        /// <summary>
        /// An object producing configured target algorithms to run with the genomes.
        /// </summary>
        private readonly ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> _targetAlgorithmFactory;

        /// <summary>
        /// Evaluation IDs of all evaluations finished for this command.
        /// </summary>
        private readonly HashSet<int> _currentlyFinishedEvaluations = new HashSet<int>();

        /// <summary>
        /// All instances that have to be utilized in an evaluation.
        /// </summary>
        private readonly List<TInstance> _instancesForEvaluation = new List<TInstance>();

        /// <summary>
        /// The <see cref="MiniTournament" /> that currently gets executed.
        /// </summary>
        private MiniTournament _miniTournament;

        /// <summary>
        /// Number of winners that should emerge from the current <see cref="MiniTournament" />.
        /// </summary>
        private int _numberOfWinners;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniTournamentActor{TTargetAlgorithm, TInstance, TResult}" /> class.
        /// </summary>
        /// <param name="targetAlgorithmFactory">Produces configured target algorithms to run with the genomes.</param>
        /// <param name="runEvaluator">Object for evaluating target algorithm runs.</param>
        /// <param name="configuration">Algorithm tuner configuration parameters.</param>
        /// <param name="resultStorageActor">
        /// Actor which is responsible for storing all evaluation results that have
        /// been observed so far.
        /// </param>
        /// <param name="parameterTree">Specifies parameters and their relationships.</param>
        /// <param name="tournamentSelector">Actor which provides this actor with evaluation tasks.</param>
        public MiniTournamentActor(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            IRunEvaluator<TResult> runEvaluator,
            AlgorithmTunerConfiguration configuration,
            IActorRef resultStorageActor,
            ParameterTree parameterTree,
            IActorRef tournamentSelector)
            : base(runEvaluator)
        {
            if (tournamentSelector == null)
            {
                throw new ArgumentNullException(nameof(tournamentSelector));
            }

            this._targetAlgorithmFactory =
                targetAlgorithmFactory ?? throw new ArgumentNullException(nameof(targetAlgorithmFactory));
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._resultStorageActor =
                resultStorageActor ?? throw new ArgumentNullException(nameof(resultStorageActor));
            this._parameterTree = parameterTree ?? throw new ArgumentNullException(nameof(parameterTree));

            // Start in waiting for instances state.
            this.WaitForInstances();

            // Finally, find out if there is something to do already.
            tournamentSelector.Tell(new InstancesRequest());
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets all instances that have to be utilized in an evaluation.
        /// </summary>
        protected override ImmutableList<TInstance> InstancesForEvaluation => this._instancesForEvaluation.ToImmutableList();

        #endregion

        #region Methods

        /// <summary>
        /// Actor is ready to process mini tournaments.
        /// </summary>
        protected override void Ready()
        {
            // Polls are accepted.
            this.Receive<Poll>(poll => this.Sender.Tell(new Accept()));

            // Switch to working state if a mini tournament was sent.
            this.Receive<MiniTournament>(
                tournament =>
                    {
                        this._miniTournament = tournament;
                        this.CommandIssuer = this.Sender;
                        this.BecomeWorking();
                    });

            // Switch to waiting for configuration state if instances are reset.
            this.Receive<ClearInstances>(
                update =>
                    {
                        this._instancesForEvaluation.Clear();
                        this.EvaluationActorRouter.Tell(new Broadcast(update));
                        this.Become(this.WaitForInstances);
                    });

            // Instance updates without earlier reset indicate missing messages.
            this.Receive<AddInstances<TInstance>>(
                update =>
                    {
                        this.Sender.Tell(new InstancesRequest());
                        this.Become(this.WaitForInstances);
                    });
            this.Receive<InstanceUpdateFinished>(
                update =>
                    {
                        this.Sender.Tell(new InstancesRequest());
                        this.Become(this.WaitForInstances);
                    });

            // Evaluation actors might ask for instances.
            this.Receive<InstancesRequest>(
                request =>
                    {
                        foreach (var message in UpdateInstances.CreateInstanceUpdateMessages(this.InstancesForEvaluation))
                        {
                            this.Sender.Tell(message);
                        }
                    });
        }

        /// <summary>
        /// Extracts all <see cref="ImmutableGenome"/>s to evaluate from a command.
        /// </summary>
        /// <returns>The extracted <see cref="ImmutableGenome"/>s.</returns>
        protected override IList<ImmutableGenome> ExtractGenomesFromCommand()
        {
            return this._miniTournament.Participants.ToList();
        }

        /// <summary>
        /// Additional preparations after <see cref="GenomeEvaluationDelegatorBase{TInstance, TResult}.OpenGenomeEvaluations"/>
        /// are set, but no evaluators have been polled yet.
        /// </summary>
        protected override void PrepareWork()
        {
            var numberOfParticipants = this.OpenGenomeEvaluations.Count;
            this._numberOfWinners = (int)Math.Ceiling(numberOfParticipants * this._configuration.TournamentWinnerPercentage);

            base.PrepareWork();
        }

        /// <summary>
        /// Post processes the fact that <paramref name="sender" /> sent a <see cref="GenomeEvaluationFinished" />
        /// message and was removed from <see cref="GenomeEvaluationDelegatorBase{TInstance, TResult}.AssignedEvaluations" />.
        /// </summary>
        /// <param name="sender">The sender of the <see cref="GenomeEvaluationFinished" /> message.</param>
        /// <param name="message">The <see cref="GenomeEvaluationFinished"/> message.</param>
        protected override void ReceivedGenomeEvaluationFinishedMessage(
            IActorRef sender,
            GenomeEvaluationFinished message)
        {
            if (this.CurrentRunResults.ContainsKey(message.EvaluationId) &&
                this.CurrentRunResults[message.EvaluationId].Count == message.ExpectedResultCount)
            {
                this._currentlyFinishedEvaluations.Add(message.EvaluationId);
                // If racing is enabled and a sufficient number of participants already was evaluated:
                if (this._configuration.EnableRacing && (this._currentlyFinishedEvaluations.Count >= this._numberOfWinners))
                {
                    // Determine & send newest timeout.
                    var timeout = this.DetermineCurrentTimeout();
                    this.EvaluationActorRouter.Tell(new Broadcast(new UpdateTimeout(timeout)));
                }
            }

            base.ReceivedGenomeEvaluationFinishedMessage(sender, message);
        }

        /// <summary>
        /// Sends evaluation information to <see cref="GenomeEvaluationDelegatorBase{TInstance, TResult}.CommandIssuer"/>.
        /// </summary>
        /// <param name="orderedGenomes"><see cref="ImmutableGenome"/>s ordered by run results.</param>
        protected override void SendInformationToCommandIssuer(ImmutableList<ImmutableGenome> orderedGenomes)
        {
            var winners = orderedGenomes.Take(this._numberOfWinners);

            // Send information to issuer.
            var miniTournamentWinners = new MiniTournamentResult<TResult>(
                this._miniTournament.MiniTournamentId,
                orderedGenomes,
                winners.ToDictionary(winner => winner, winner => this.FindCurrentRunResults(winner).ToImmutableList()));
            this.CommandIssuer.Tell(miniTournamentWinners);
        }

        /// <summary>
        /// Resets all fields that change between two different commands.
        /// </summary>
        protected override void ResetCommandSpecificFields()
        {
            LoggingHelper.WriteLine(VerbosityLevel.Info, $"{this.Self.Path}: Finished a tournament.");

            // Reset timeout.
            this.EvaluationActorRouter.Tell(new Broadcast(new ResetTimeout()));

            this._miniTournament = null;
            this._currentlyFinishedEvaluations.Clear();
            base.ResetCommandSpecificFields();
        }

        /// <summary>
        /// Method gets called when the actor gets started.
        /// It initializes <see cref="GenomeEvaluationDelegatorBase{TInstance, TResult}.EvaluationActorRouter" /> by creating
        /// the correct number of <see cref="EvaluationActor{A, I, R}" />s according to
        /// <see cref="AlgorithmTunerConfiguration.MaximumNumberParallelEvaluations" />.
        /// </summary>
        protected override void PreStart()
        {
            // Write a warning if not enough processors are available.
            // This check can not be done at the start of the program because workers will only know about the
            // number of desired processes as soon as MiniTournamentActors are deployed onto them.
            this.CheckNumberOfAvailableProcessors();

            base.PreStart();
        }

        /// <summary>
        /// Creates number of evaluation actors equal to the maximum number of parallel evaluations
        /// and wraps a router around them.
        /// </summary>
        /// <returns>The created router.</returns>
        protected override IActorRef CreateEvaluationActorRouter()
        {
            return Context.ActorOf(
                Props.Create(
                        () => new EvaluationActor<TTargetAlgorithm, TInstance, TResult>(
                            this._targetAlgorithmFactory,
                            this._resultStorageActor,
                            this._configuration,
                            this._parameterTree))
                    .WithRouter(
                        new RoundRobinPool(this._configuration.MaximumNumberParallelEvaluations)
                            .WithSupervisorStrategy(
                                new OneForOneStrategy(
                                    e =>
                                        {
                                            // Evaluation apparently does not work on this worker.
                                            // Killing it such that other workers have a chance to try this, too.
                                            Console.WriteLine("Evaluation failed. Killing this process.");
                                            Context.System.Terminate();
                                            return Directive.Escalate;
                                        }))),
                AkkaNames.EvaluationActorRouter);
        }

        /// <summary>
        /// Actor is ignorant about which instances to evaluate genomes on.
        /// </summary>
        private void WaitForInstances()
        {
            // If a poll comes in, decline and ask for configuration first.
            this.Receive<Poll>(poll => this.DeclinePollWhenWaitingForConfiguration());

            // Instance updates are forwarded.
            this.Receive<ClearInstances>(
                update =>
                    {
                        this._instancesForEvaluation.Clear();
                        this.EvaluationActorRouter.Tell(new Broadcast(update));
                    });
            this.Receive<AddInstances<TInstance>>(
                update =>
                    {
                        this._instancesForEvaluation.AddRange(update.Instances);
                        this.EvaluationActorRouter.Tell(new Broadcast(update));
                    });

            // If an instance update finishes successfully, change to ready state.
            this.Receive<InstanceUpdateFinished>(
                update =>
                    {
                        this.Become(this.Ready);
                        this.EvaluationActorRouter.Tell(new Broadcast(update));
                    },
                update => this._instancesForEvaluation.Count == update.ExpectedInstanceCount);

            // Else request a new instance specification.
            this.Receive<InstanceUpdateFinished>(
                update =>
                    {
                        this.Sender.Tell(new InstancesRequest());
                        LoggingHelper.WriteLine(
                            VerbosityLevel.Warn,
                            $"Request instances again because we received {this._instancesForEvaluation.Count} instead of {update.ExpectedInstanceCount} instances from {this.Sender}.");
                    },
                update => this._instancesForEvaluation.Count != update.ExpectedInstanceCount);
        }

        /// <summary>
        /// Decline a <see cref="Poll"/> when the configuration was not send.
        /// Request the instances to work on instead.
        /// </summary>
        private void DeclinePollWhenWaitingForConfiguration()
        {
            this.Sender.Tell(new Decline());
            this.Sender.Tell(new InstancesRequest());
        }

        /// <summary>
        /// Determines the current timeout for the <see cref="MiniTournament" />:
        /// A genome only has a chance to end up among the <see cref="MiniTournament" />s winners if the target
        /// algorithm configured with that genome needs at most this number of milliseconds to solve all instances.
        /// <para>
        /// Therefore, genome evaluations already running longer don't have to be continued as they won't change
        /// the result anyway.
        /// </para>
        /// </summary>
        /// <returns>
        /// The current timeout.
        /// Might be <see cref="TimeSpan.MaxValue" /> if not enough winner candidates can be found.
        /// </returns>
        private TimeSpan DetermineCurrentTimeout()
        {
            // Only look at evaluations that are finished and did not need cancellations.
            var evaluatedGenomesWithoutCancellations = this.CurrentRunResults
                .Where(
                    evaluationToResults =>
                        this._currentlyFinishedEvaluations.Contains(evaluationToResults.Key)
                        && !evaluationToResults.Value.Any(result => result.IsCancelled))
                .Select(keyPair => keyPair.Key)
                .ToList();

            // If that are less than the number of winners that have to be returned, no timeout can be specified.
            if (evaluatedGenomesWithoutCancellations.Count < this._numberOfWinners)
            {
                return TimeSpan.MaxValue;
            }

            // Else we sort the genomes by increasing total runtime and return the (this.numberOfWinners)th one.
            return evaluatedGenomesWithoutCancellations
                .Select(
                    genome =>
                        new
                            {
                                genome,
                                TotalTime = this.CurrentRunResults[genome].Sum(result => result.Runtime),
                            })
                .OrderBy(genomeRuntimePair => genomeRuntimePair.TotalTime)
                .Skip(this._numberOfWinners - 1).First().TotalTime;
        }

        /// <summary>
        /// Writes a warning if the number of available processors is less than the desired number of parallel
        /// evaluations.
        /// </summary>
        private void CheckNumberOfAvailableProcessors()
        {
            if (this._configuration.MaximumNumberParallelEvaluations > Environment.ProcessorCount)
            {
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Warning: You specified {this._configuration.MaximumNumberParallelEvaluations} parallel evaluations, but only have {Environment.ProcessorCount} processors. Processes may fight for resources.");
            }
        }

        #endregion
    }
}