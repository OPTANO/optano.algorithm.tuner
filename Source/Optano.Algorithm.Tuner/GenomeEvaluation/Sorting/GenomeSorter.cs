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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Sorting
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Akka.Actor;
    using Akka.Routing;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Communication;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// Responsible for sorting a number of <see cref="ImmutableGenome"/>s.
    /// </summary>
    /// <typeparam name="TInstance">
    /// Type of instance the target algorithm is able to process.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// Type of result of a target algorithm run.
    /// </typeparam>
    public class GenomeSorter<TInstance, TResult> : GenomeEvaluationDelegatorBase<TInstance, TResult>, IWithUnboundedStash
        where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// The <see cref="SortCommand{TInstance}"/> that currently gets executed.
        /// </summary>
        private SortCommand<TInstance> _sortCommand;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeSorter{TInstance,TResult}"/> class.
        /// </summary>
        /// <param name="runEvaluator">Object for evaluating target algorithm runs.</param>
        public GenomeSorter(IRunEvaluator<TResult> runEvaluator)
            : base(runEvaluator)
        {
            this.WaitingForEvaluators();

            // Directly check if we have enough evaluators to switch to ready state.
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

        #region Properties

        /// <summary>
        /// Gets all instances that have to be utilized in an evaluation.
        /// </summary>
        protected override ImmutableList<TInstance> InstancesForEvaluation => this._sortCommand.Instances;

        #endregion

        #region Methods

        /// <summary>
        /// Actor is ready to process <see cref="SortCommand{TInstance}"/>s.
        /// </summary>
        protected override void Ready()
        {
            // Switch to working state if a genome sorting command was sent.
            this.Receive<SortCommand<TInstance>>(
                command =>
                    {
                        this._sortCommand = command;
                        this.CommandIssuer = this.Sender;
                        this.BecomeWorking();
                    });
        }

        /// <summary>
        /// Additional preparations after <see cref="GenomeEvaluationDelegatorBase{TInstance, TResult}.OpenGenomeEvaluations" /> are set, but no evaluators have been polled
        /// yet.
        /// </summary>
        protected override void PrepareWork()
        {
            foreach (var message in UpdateInstances.CreateInstanceUpdateMessages(this._sortCommand.Instances))
            {
                this.EvaluationActorRouter.Tell(message);
            }

            base.PrepareWork();
        }

        /// <summary>
        /// Extracts all <see cref="ImmutableGenome"/>s to evaluate from a command.
        /// </summary>
        /// <returns>The extracted <see cref="ImmutableGenome"/>s.</returns>
        protected override IList<ImmutableGenome> ExtractGenomesFromCommand()
        {
            return this._sortCommand.Items;
        }

        /// <summary>
        /// Actor is working on a specific command.
        /// </summary>
        protected override void Working()
        {
            // A watched evaluator terminated:
            this.Receive<Terminated>(
                terminationMessage =>
                    {
                        // Remove evaluation command from terminated evaluation actor
                        var cancelledEvaluation = this.AssignedEvaluations[terminationMessage.ActorRef];
                        this.CurrentRunResults.Remove(cancelledEvaluation.EvaluationId);
                        this.OpenGenomeEvaluations.Add(cancelledEvaluation);
                        this.AssignedEvaluations.Remove(terminationMessage.ActorRef);
                        LoggingHelper.WriteLine(
                            VerbosityLevel.Warn,
                            $"Withdrew evaluation from {terminationMessage.ActorRef}, because Akka marked the actor as terminated.");

                        // See if any of the other evaluators is free to handle the rolled back evaluation.
                        this.AskForEvaluators();
                    });

            // Correctly handle 'terminated' evaluators that turn out to be alive after all.
            // As a message always activates the first fitting receive handler, these handlers catch those issues.
            this.Receive<PartialGenomeEvaluationResults<TResult>>(
                results => GenomeSorter<TInstance, TResult>.HandleMessageFromSupposedlyTerminated(this.Sender),
                results => !this.AssignedEvaluations.ContainsKey(this.Sender));
            this.Receive<GenomeEvaluationFinished>(
                message => GenomeSorter<TInstance, TResult>.HandleMessageFromSupposedlyTerminated(this.Sender),
                message => !this.AssignedEvaluations.ContainsKey(this.Sender));

            base.Working();
        }

        /// <summary>
        /// Prepares <paramref name="evaluator"/> for the evaluation <paramref name="nextGenomeEvaluation"/>.
        /// </summary>
        /// <param name="evaluator">The actor who will evaluate the <see cref="Genome"/>.</param>
        /// <param name="nextGenomeEvaluation">The <see cref="GenomeEvaluation"/>.</param>
        protected override void PrepareForEvaluation(IActorRef evaluator, GenomeEvaluation nextGenomeEvaluation)
        {
            Context.Watch(evaluator);
            base.PrepareForEvaluation(evaluator, nextGenomeEvaluation);
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
            Context.Unwatch(sender);
            base.ReceivedGenomeEvaluationFinishedMessage(sender, message);
        }

        /// <summary>
        /// Sends evaluation information to <see cref="GenomeEvaluationDelegatorBase{TInstance, TResult}.CommandIssuer"/>.
        /// </summary>
        /// <param name="orderedGenomes"><see cref="ImmutableGenome"/>s ordered by run results.</param>
        protected override void SendInformationToCommandIssuer(ImmutableList<ImmutableGenome> orderedGenomes)
        {
            this.CommandIssuer.Tell(new SortResult(orderedGenomes));
        }

        /// <summary>
        /// Resets all fields that change between two different commands.
        /// </summary>
        protected override void ResetCommandSpecificFields()
        {
            this._sortCommand = null;
            base.ResetCommandSpecificFields();
        }

        /// <summary>
        /// Creates a router managing actors responsible for evaluating single genomes.
        /// </summary>
        /// <returns>The created router.</returns>
        protected override IActorRef CreateEvaluationActorRouter()
        {
            return Context.ActorOf(
                Props.Empty.WithRouter(FromConfig.Instance),
                AkkaNames.SortingRouter);
        }

        /// <summary>
        /// Handles messages from 'terminated' evaluators that turn out to be alive after all.
        /// </summary>
        /// <param name="sender">The evaluator.</param>
        private static void HandleMessageFromSupposedlyTerminated(IActorRef sender)
        {
            LoggingHelper.WriteLine(
                VerbosityLevel.Debug,
                $"Received results from actor with path {sender}. Evaluation was withdrawn from actor because it was assumed to be terminated. Result is ignored.");

            // Reuse the actor.
            sender.Tell(new Poll());
        }

        /// <summary>
        /// Actor is waiting for workers to join the cluster s.t. a sufficient number of
        /// <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}" />s is available.
        /// </summary>
        private void WaitingForEvaluators()
        {
            // Sort commands cannot be handled while we are still waiting for evaluators, so stash them for later.
            this.Receive<SortCommand<TInstance>>(command => this.Stash.Stash());

            // The availability of evaluators can be checked.
            this.Receive<CheckWorkers>(
                checkEvaluators =>
                    {
                        var self = this.Self;
                        this.EvaluationActorRouter.Ask(new Poll(), TimeSpan.FromSeconds(1))
                            .ContinueWith<object>(
                                tr =>
                                    {
                                        if (tr.IsCanceled)
                                        {
                                            // No evaluation actors exist yet => check again.
                                            return new CheckWorkers();
                                        }
                                        else
                                        {
                                            return new WorkersExist();
                                        }
                                    }).PipeTo(self, sender: self);
                    });

            // If an evaluator is available, change to Ready state.
            this.Receive<WorkersExist>(confirmation => this.LeaveWaitingState());
        }

        /// <summary>
        /// Actor has access to a sufficient number of
        /// <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>s and migrates to the <see cref="Ready" />
        /// state.
        /// </summary>
        private void LeaveWaitingState()
        {
            this.Become(this.Ready);

            // Unstash all select commands that have been stashed while in waiting for evaluators state.
            this.Stash.UnstashAll();
        }

        #endregion
    }
}