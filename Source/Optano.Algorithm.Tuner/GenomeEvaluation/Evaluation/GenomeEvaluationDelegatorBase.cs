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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Akka.Actor;
    using Akka.Routing;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Communication;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// An actor who accepts evaluation commands and delegates <see cref="GenomeEvaluation"/>s to
    /// <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>s.
    /// </summary>
    /// <typeparam name="TInstance">
    /// Type of instance the target algorithm is able to process.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// Type of result of a target algorithm run.
    /// </typeparam>
    public abstract class GenomeEvaluationDelegatorBase<TInstance, TResult> : ReceiveActor
        where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// Evaluator for target algorithm runs.
        /// </summary>
        private readonly IRunEvaluator<TResult> _runEvaluator;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeEvaluationDelegatorBase{TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="runEvaluator">Object for evaluating target algorithm runs.</param>
        protected GenomeEvaluationDelegatorBase(IRunEvaluator<TResult> runEvaluator)
        {
            this._runEvaluator = runEvaluator ?? throw new ArgumentNullException(nameof(runEvaluator));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets all instances that have to be utilized in an evaluation.
        /// </summary>
        protected abstract ImmutableList<TInstance> InstancesForEvaluation { get; }

        /// <summary>
        /// Gets evaluations that have to be executed for the current command
        /// and have not been sent to be processed yet.
        /// </summary>
        protected List<GenomeEvaluation> OpenGenomeEvaluations { get; } = new List<GenomeEvaluation>();

        /// <summary>
        /// Gets a mapping of actors to the evaluation that has been assigned to them
        /// and hasn't been returned with a result yet.
        /// </summary>
        protected Dictionary<IActorRef, GenomeEvaluation> AssignedEvaluations { get; } =
            new Dictionary<IActorRef, GenomeEvaluation>();

        /// <summary>
        /// Gets results of target algorithm runs that have been executed for the current command.
        /// The key is the evaluation ID as fixed by <see cref="CurrentGenomesToEvaluationIds"/>.
        /// <remarks>
        /// It is not possible to directly use <see cref="ImmutableGenome"/>s as keys,
        /// because references change in serialization required for TCP messaging, but we somehow need to group
        /// <see cref="PartialGenomeEvaluationResults{TResult}"/> messages.
        /// Just using a <see cref="ImmutableGenome.GeneValueComparer"/> is not feasible as several individuals may
        /// have the same values.</remarks>
        /// </summary>
        protected Dictionary<int, List<TResult>> CurrentRunResults { get; } =
            new Dictionary<int, List<TResult>>();

        /// <summary>
        /// Gets a router managing actors responsible for evaluating single genomes.
        /// </summary>
        protected IActorRef EvaluationActorRouter { get; private set; }

        /// <summary>
        /// Gets or sets the current command's sender. null if no command is currently executed.
        /// </summary>
        protected IActorRef CommandIssuer { get; set; }

        /// <summary>
        /// Gets a mapping of currently evaluated genomes to evaluation IDs.
        /// </summary>
        private Dictionary<ImmutableGenome, int> CurrentGenomesToEvaluationIds { get; } =
            new Dictionary<ImmutableGenome, int>();

        #endregion

        #region Methods

        /// <summary>
        /// Actor is ready to process commands.
        /// </summary>
        protected abstract void Ready();

        /// <summary>
        /// Actor has accepted a command and migrates to the <see cref="Working" /> state.
        /// </summary>
        protected void BecomeWorking()
        {
            this.OpenGenomeEvaluations.AddRange(this.ExtractEvaluationsFromCommand());
            this.PrepareWork();
            this.AskForEvaluators();
            this.Become(this.Working);
        }

        /// <summary>
        /// Additional preparations after <see cref="OpenGenomeEvaluations"/> are set, but no evaluators have been polled
        /// yet.
        /// </summary>
        protected virtual void PrepareWork()
        {
        }

        /// <summary>
        /// Actor asks for workers for genome evaluation.
        /// </summary>
        protected void AskForEvaluators()
        {
            this.EvaluationActorRouter.Tell(new Broadcast(new Poll()));
        }

        /// <summary>
        /// Extracts all <see cref="ImmutableGenome"/>s to evaluate from a command.
        /// </summary>
        /// <returns>The extracted <see cref="ImmutableGenome"/>s.</returns>
        protected abstract IList<ImmutableGenome> ExtractGenomesFromCommand();

        /// <summary>
        /// Actor is working on a specific command.
        /// </summary>
        protected virtual void Working()
        {
            // If an actor is interested in the current instances, tell him and check whether he is available for 
            // working now.
            this.Receive<InstancesRequest>(
                request =>
                    {
                        foreach (var message in UpdateInstances.CreateInstanceUpdateMessages(this.InstancesForEvaluation))
                        {
                            this.Sender.Tell(message);
                        }

                        this.Sender.Tell(new Poll());
                    });

            // If poll was accepted and open list is not empty, assign a genome evaluation.
            this.Receive<Accept>(
                accepted =>
                    {
                        var nextGenomeEvaluation = this.OpenGenomeEvaluations[0];
                        this.OpenGenomeEvaluations.RemoveAt(0);

                        this.PrepareForEvaluation(this.Sender, nextGenomeEvaluation);
                        this.Sender.Tell(nextGenomeEvaluation);

                        this.AssignedEvaluations.Add(this.Sender, nextGenomeEvaluation);
                    },
                accepted => this.OpenGenomeEvaluations.Any()
                            && !this.AssignedEvaluations.ContainsKey(this.Sender));

            // New results are added to dictionary.
            this.Receive<PartialGenomeEvaluationResults<TResult>>(
                results =>
                    {
                        if (!this.CurrentRunResults.TryGetValue(results.EvaluationId, out var resultsSoFar))
                        {
                            resultsSoFar = new List<TResult>();
                            this.CurrentRunResults.Add(results.EvaluationId, resultsSoFar);
                        }

                        resultsSoFar.AddRange(results.RunResults);
                    });

            // Handle a finished evaluation by modifying fields accordingly
            // and either polling for evaluators again or leaving the working state.  
            this.Receive<GenomeEvaluationFinished>(
                finishedMessage =>
                    {
                        var receivedResultNumber = this.CurrentRunResults.ContainsKey(finishedMessage.EvaluationId)
                                                       ? this.CurrentRunResults[finishedMessage.EvaluationId].Count
                                                       : 0;
                        if (receivedResultNumber != finishedMessage.ExpectedResultCount)
                        {
                            this.CurrentRunResults.Remove(finishedMessage.EvaluationId);
                            this.OpenGenomeEvaluations.Add(this.AssignedEvaluations[this.Sender]);
                            LoggingHelper.WriteLine(
                                VerbosityLevel.Warn,
                                $"Redo evaluation because we received {receivedResultNumber} instead of {finishedMessage.ExpectedResultCount} evaluation results from {this.Sender}.");
                        }

                        this.AssignedEvaluations.Remove(this.Sender);
                        this.ReceivedGenomeEvaluationFinishedMessage(this.Sender, finishedMessage);

                        var done = !this.OpenGenomeEvaluations.Any() && !this.AssignedEvaluations.Any();
                        if (done)
                        {
                            this.BecomeReady();
                        }
                        else
                        {
                            this.AskForEvaluators();
                        }
                    });
        }

        /// <summary>
        /// Prepares <paramref name="evaluator"/> for the evaluation <paramref name="nextGenomeEvaluation"/>.
        /// </summary>
        /// <param name="evaluator">The actor who will evaluate the <see cref="Genome"/>.</param>
        /// <param name="nextGenomeEvaluation">The <see cref="GenomeEvaluation"/>.</param>
        protected virtual void PrepareForEvaluation(IActorRef evaluator, GenomeEvaluation nextGenomeEvaluation)
        {
        }

        /// <summary>
        /// Post processes the fact that <paramref name="sender"/> sent a <see cref="GenomeEvaluationFinished"/>
        /// message and was removed from <see cref="AssignedEvaluations"/>.
        /// </summary>
        /// <param name="sender">The sender of the <see cref="GenomeEvaluationFinished"/> message.</param>
        /// <param name="message">The <see cref="GenomeEvaluationFinished"/> message.</param>
        protected virtual void ReceivedGenomeEvaluationFinishedMessage(
            IActorRef sender,
            GenomeEvaluationFinished message)
        {
        }

        /// <summary>
        /// Finds the current run results for the provided <see cref="ImmutableGenome"/>.
        /// <remarks>Convenience method using <see cref="CurrentRunResults"/>.</remarks>
        /// </summary>
        /// <param name="genome">The genome to find results for.</param>
        /// <returns>The current run results.</returns>
        protected IEnumerable<TResult> FindCurrentRunResults(ImmutableGenome genome)
        {
            return this.CurrentRunResults[this.CurrentGenomesToEvaluationIds[genome]];
        }

        /// <summary>
        /// Sends evaluation information to <see cref="CommandIssuer"/>.
        /// </summary>
        /// <param name="orderedGenomes"><see cref="ImmutableGenome"/>s ordered by run results.</param>
        protected abstract void SendInformationToCommandIssuer(ImmutableList<ImmutableGenome> orderedGenomes);

        /// <summary>
        /// Resets all fields that change between two different commands.
        /// </summary>
        protected virtual void ResetCommandSpecificFields()
        {
            this.CommandIssuer = null;
            this.AssignedEvaluations.Clear();
            this.OpenGenomeEvaluations.Clear();
            this.CurrentRunResults.Clear();
            this.CurrentGenomesToEvaluationIds.Clear();
        }

        /// <summary>
        /// Method gets called when the actor gets started.
        /// It initializes <see cref="EvaluationActorRouter" />.
        /// </summary>
        protected override void PreStart()
        {
            this.EvaluationActorRouter = this.CreateEvaluationActorRouter();
            base.PreStart();
        }

        /// <summary>
        /// Creates a router managing actors responsible for evaluating single genomes.
        /// </summary>
        /// <returns>The created router.</returns>
        protected abstract IActorRef CreateEvaluationActorRouter();

        /// <summary>
        /// Extracts single <see cref="GenomeEvaluation"/>s from a command.
        /// </summary>
        /// <returns>The extracted <see cref="GenomeEvaluation"/>s.</returns>
        private IEnumerable<GenomeEvaluation> ExtractEvaluationsFromCommand()
        {
            var currentGenomes = this.ExtractGenomesFromCommand();
            for (int i = 0; i < currentGenomes.Count; i++)
            {
                this.CurrentGenomesToEvaluationIds.Add(currentGenomes[i], i);
                yield return new GenomeEvaluation(currentGenomes[i], evaluationId: i);
            }
        }

        /// <summary>
        /// All genomes have been evaluated and the actor migrates to the <see cref="Ready" /> state.
        /// </summary>
        private void BecomeReady()
        {
            var genomesToResults = new Dictionary<ImmutableGenome, IEnumerable<TResult>>();
            foreach (var genomeToId in this.CurrentGenomesToEvaluationIds)
            {
                genomesToResults.Add(genomeToId.Key, this.CurrentRunResults[genomeToId.Value]);
            }

            var orderedGenomes = this._runEvaluator
                .Sort(genomesToResults)
                .ToImmutableList();
            this.SendInformationToCommandIssuer(orderedGenomes);

            this.ResetCommandSpecificFields();
            this.Become(this.Ready);
        }

        #endregion
    }
}