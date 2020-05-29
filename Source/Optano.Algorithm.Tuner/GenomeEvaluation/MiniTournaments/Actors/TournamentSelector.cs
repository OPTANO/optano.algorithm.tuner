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
    using System.Linq;

    using Akka.Actor;
    using Akka.Cluster;
    using Akka.Routing;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Communication;
    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Messages;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// Responsible for selecting best target algorithm parameters. Kicks off tournaments and returns their results.
    /// </summary>
    /// <typeparam name="TTargetAlgorithm">
    /// Type of the target algorithm.
    /// Must implement <see cref="ITargetAlgorithm{TInstance,TResult}" />.
    /// </typeparam>
    /// <typeparam name="TInstance">
    /// Type of instance the target algorithm is able to process.
    /// Must be a subtype of <see cref="InstanceBase" />.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// Type of result of a target algorithm run.
    /// Must be a subtype of <see cref="ResultBase{TResultType}" />.
    /// </typeparam>
    public class TournamentSelector<TTargetAlgorithm, TInstance, TResult> : ReceiveActor, IWithUnboundedStash
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
        /// Evaluator for target algorithm runs.
        /// </summary>
        private readonly IRunEvaluator<TResult> _runEvaluator;

        /// <summary>
        /// An object producing configured target algorithms to run with the genomes.
        /// </summary>
        private readonly ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> _targetAlgorithmFactory;

        /// <summary>
        /// The progress of the current tournament selection.
        /// </summary>
        private readonly TournamentSelectionProgress<TInstance, TResult> _selectionProgress;

        /// <summary>
        /// Router managing workers responsible for executing mini tournaments.
        /// </summary>
        private IActorRef _miniTournamentWorkerRouter;

        /// <summary>
        /// The current <see cref="SelectCommand{TInstance}" />'s sender. null if no such command is currently executed.
        /// </summary>
        private IActorRef _selectCommandIssuer;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TournamentSelector{TTargetAlgorithm, TInstance, TResult}" /> class.
        /// </summary>
        /// <param name="targetAlgorithmFactory">Produces configured target algorithms to run with the genomes.</param>
        /// <param name="runEvaluator">Object for evaluating target algorithm runs.</param>
        /// <param name="configuration">Algorithm tuner configuration parameters.</param>
        /// <param name="resultStorageActor">
        /// Actor which is responsible for storing all evaluation results that have
        /// been observed so far.
        /// </param>
        /// <param name="parameterTree">Specifies parameters and their relationships.</param>
        public TournamentSelector(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            IRunEvaluator<TResult> runEvaluator,
            AlgorithmTunerConfiguration configuration,
            IActorRef resultStorageActor,
            ParameterTree parameterTree)
        {
            // Verify parameters.
            if (targetAlgorithmFactory == null)
            {
                throw new ArgumentNullException("targetAlgorithmFactory");
            }

            if (runEvaluator == null)
            {
                throw new ArgumentNullException("runEvaluator");
            }

            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (resultStorageActor == null)
            {
                throw new ArgumentNullException("resultStorageActor");
            }

            if (parameterTree == null)
            {
                throw new ArgumentNullException("parameterTree");
            }

            // Use them to set fields.
            this._targetAlgorithmFactory = targetAlgorithmFactory;
            this._runEvaluator = runEvaluator;
            this._configuration = configuration;
            this._resultStorageActor = resultStorageActor;
            this._parameterTree = parameterTree;

            this._selectionProgress = new TournamentSelectionProgress<TInstance, TResult>(this._configuration);

            // If Akka.Cluster gets used, watch for disconnecting cluster members
            // to make tournament rollbacks possible.
            if (Context.System.HasExtension<Cluster>())
            {
                Cluster.Get(Context.System).Subscribe(this.Self, typeof(ClusterEvent.UnreachableMember));
            }

            // Start in waiting for workers state.
            this.WaitingForWorkers();
            // And directly check if we have enough workers to switch to ready state.
            this.Self.Tell(new CheckWorkers());
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets a message stash.
        /// Automatically initialized due to this class implementing <see cref="IWithUnboundedStash" />.
        /// </summary>
        public IStash Stash { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Called when the actor is started. Creates <see cref="MiniTournamentActor{TTargetAlgorithm, TInstance, TResult}" />s.
        /// </summary>
        protected override void PreStart()
        {
            // Create a number of mini tournament actors that are managed by a router.
            this._miniTournamentWorkerRouter = Context.ActorOf(
                Props.Create(
                        () => new MiniTournamentActor<TTargetAlgorithm, TInstance, TResult>(
                            this._targetAlgorithmFactory,
                            this._runEvaluator,
                            this._configuration,
                            this._resultStorageActor,
                            this._parameterTree,
                            this.Self))
                    .WithRouter(FromConfig.Instance),
                AkkaNames.MiniTournamentWorkers);

            base.PreStart();
        }

        /// <summary>
        /// Actor is waiting for workers to join the cluster s.t. a sufficient number of
        /// <see cref="_miniTournamentWorkerRouter" /> are available.
        /// </summary>
        private void WaitingForWorkers()
        {
            // Select commands cannot be handled while we are still waiting for workers, so stash them for later.
            this.Receive<SelectCommand<TInstance>>(command => this.Stash.Stash());

            // The current number of workers can be checked.
            this.Receive<CheckWorkers>(
                checkWorkers =>
                    {
                        var self = this.Self;
                        this._miniTournamentWorkerRouter.Ask<Routees>(new GetRoutees())
                            .ContinueWith(tr => new WorkerCount(tr.Result.Members.Count())).PipeTo(self);
                    });

            // If it is still too small, it should be checked again soon.
            this.Receive<WorkerCount>(
                workerCount => Context.System.Scheduler.ScheduleTellOnce(
                    TimeSpan.FromSeconds(1),
                    this.Self,
                    new CheckWorkers(),
                    this.Self),
                workerCount => workerCount.Count == 0);

            // If a sufficient number of workers is available, change to Ready state.
            this.Receive<WorkerCount>(
                workerCount => this.LeaveWaitingState(),
                workerCount => workerCount.Count > 0);

            // Unreachable member events should always be handled to keep the Akka.NET cluster clean.
            this.Receive<ClusterEvent.UnreachableMember>(
                unreachableMessage => this.HandleUnreachableMessage(unreachableMessage));
        }

        /// <summary>
        /// Actor has access to a sufficient number of <see cref="MiniTournamentActor{TTargetAlgorithm, TInstance, TResult}" />s and migrates to the
        /// <see cref="Ready" /> state.
        /// </summary>
        private void LeaveWaitingState()
        {
            this.Become(this.Ready);

            // Unstash all select commands that have been stashed while in waiting for workers state.
            this.Stash.UnstashAll();
        }

        /// <summary>
        /// Actor is ready to process messages sent to it from external senders.
        /// </summary>
        private void Ready()
        {
            this.Receive<SelectCommand<TInstance>>(
                selectCommand =>
                    {
                        this._selectCommandIssuer = this.Sender;
                        this.BecomeWorking(selectCommand);
                    });

            // Unreachable member events should always be handled to keep the Akka.NET cluster clean.
            this.Receive<ClusterEvent.UnreachableMember>(
                unreachableMessage => this.HandleUnreachableMessage(unreachableMessage));
        }

        /// <summary>
        /// Actor has accepted a <see cref="SelectCommand{TInstance}" /> and migrates to the <see cref="Working" /> state.
        /// </summary>
        /// <param name="command">The accepted <see cref="SelectCommand{TInstance}" />.</param>
        private void BecomeWorking(SelectCommand<TInstance> command)
        {
            this._selectionProgress.Initialize(command);

            // Update workers on instances they have to handle.
            foreach (var message in this._selectionProgress.CreateInstanceUpdateMessages())
            {
                this._miniTournamentWorkerRouter.Tell(new Broadcast(message));
            }

            // Ask for workers.
            this.AskForWorkers();
            // Change state.
            this.Become(this.Working);
        }

        /// <summary>
        /// Actor asks for workers to execute mini tournaments.
        /// </summary>
        private void AskForWorkers()
        {
            var poll = new Poll();
            try
            {
                this._miniTournamentWorkerRouter.Tell(new Broadcast(poll));
            }
            catch (Exception e)
            {
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Broadcasting of Poll to miniTournamentWorkers failed in {this.GetType().FullName}: {poll.ToString()}");
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Error in {this.GetType().FullName} when trying to send a Broadcast:\r\n{e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Actor is working on a specific <see cref="SelectCommand{TInstance}" />.
        /// </summary>
        private void Working()
        {
            // If an actor is interested in the current instances, tell him and check whether he is available for 
            // working now.
            this.Receive<InstancesRequest>(
                request =>
                    {
                        foreach (var message in this._selectionProgress.CreateInstanceUpdateMessages())
                        {
                            this.Sender.Tell(message);
                        }

                        this.Sender.Tell(new Poll());
                    });

            // If poll was accepted, the sender is not handling another mini tournament and open list is not empty,
            // assign a mini tournament:
            this.Receive<Accept>(
                accepted =>
                    {
                        // Pop a mini tournament from the open list.
                        var nextMiniTournament = this._selectionProgress.PopOpenMiniTournament();

                        // Send the mini tournament.
                        this.Sender.Tell(nextMiniTournament);

                        // Add the worker to the assignment list.
                        this._selectionProgress.AddAssignment(this.Sender, nextMiniTournament);
                    },
                accept => this._selectionProgress.HasOpenMiniTournaments &&
                          !this._selectionProgress.HasAssignment(this.Sender));

            // If poll was declined, ignore.
            this.Receive<Decline>(decline => { });

            // Handle a result by modifying fields accordingly
            // and either poll for workers again or leave the working state.  
            this.Receive<MiniTournamentResult<TResult>>(
                results =>
                    {
                        // Remove from assignments.
                        var senderWasResponsibleForTournament = this._selectionProgress.RemoveAssignment(this.Sender);
                        if (senderWasResponsibleForTournament)
                        {
                            // Remember results.
                            this._selectionProgress.AddResult(results);
                        }
                        else
                        {
                            LoggingHelper.WriteLine(
                                VerbosityLevel.Debug,
                                $"Received tournament results for TournamentId {results.MiniTournamentId} from Worker with Address {this.Sender.Path.Address}. Tournament was withdrawn from worker because it was assumed to be unreachable. Result is ignored.\r\nWorker should be available for assignment of new tournaments.");
                        }

                        // If all are done, becomeReady. If not, poll for workers.
                        if (this._selectionProgress.IsFinished)
                        {
                            this.BecomeReady();
                        }
                        else
                        {
                            this.AskForWorkers();
                        }
                    });

            // Unreachable member events should always be handled to keep the Akka.NET cluster clean.
            this.Receive<ClusterEvent.UnreachableMember>(
                unreachableMessage => this.HandleUnreachableMessage(unreachableMessage));
        }

        /// <summary>
        /// Actor has finished a <see cref="SelectCommand{TInstance}" /> and migrates to the <see cref="Ready" /> state.
        /// </summary>
        private void BecomeReady()
        {
            // Answer the original selection command.
            this._selectCommandIssuer.Tell(this._selectionProgress.CreateSelectionResultMessage(this._runEvaluator));

            // Reset all selection command specific fields.
            this._selectCommandIssuer = null;
            this._selectionProgress.Reset();

            // Change internal state to Ready.
            this.Become(this.Ready);
        }

        /// <summary>
        /// Handles a <see cref="ClusterEvent.UnreachableMember" /> message by rolling back all tournaments that had
        /// been assigned to that member and removing the member from the cluster.
        /// </summary>
        /// <param name="unreachableMember">The message.</param>
        private void HandleUnreachableMessage(ClusterEvent.UnreachableMember unreachableMember)
        {
            var addressOfUnreachableMember = unreachableMember.Member.Address;

            // Roll back all tournaments assigned to actors placed on unreachable node.
            this._selectionProgress.RollBackOnAddress(addressOfUnreachableMember);

            // Remove node from cluster.
            Cluster.Get(Context.System).Down(addressOfUnreachableMember);

            // See if any of the other workers is free to handle the rolled back tournaments.
            this.AskForWorkers();
        }

        #endregion
    }
}