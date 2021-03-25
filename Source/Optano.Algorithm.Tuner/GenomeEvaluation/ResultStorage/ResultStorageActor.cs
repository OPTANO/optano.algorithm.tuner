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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage
{
    using System.Collections.Generic;
    using System.Linq;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Responsible for providing already computed results.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    public class ResultStorageActor<TInstance, TResult> : ReceiveActor
        where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// All run results of target algorithm runs grouped by the <see cref="Genome" /> they were configured
        /// with and instance they were run on.
        /// </summary>
        private readonly Dictionary<ImmutableGenome, IDictionary<TInstance, TResult>> _runResults =
            new Dictionary<ImmutableGenome, IDictionary<TInstance, TResult>>(ImmutableGenome.GenomeComparer);

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultStorageActor{TInstance, TResult}" /> class.
        /// </summary>
        public ResultStorageActor()
        {
            // Definition of message handlers.
            this.Receive<EvaluationResult<TInstance, TResult>>(result => this.HandleResult(result));
            this.Receive<AllResultsRequest>(request => this.HandleAllResultsRequest(this.Sender));
            this.Receive<EvaluationStatisticRequest>(request => this.HandleEvaluationStatisticRequest(this.Sender));
            this.Receive<GenomeResultsRequest>(request => this.HandleGenomeResultsRequest(request.Genome, this.Sender));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles a <see cref="EvaluationResult{TInstance,TResult}" /> message.
        /// </summary>
        /// <param name="evaluationResult">The <see cref="EvaluationResult{TInstance,TResult}" /> message.</param>
        private void HandleResult(EvaluationResult<TInstance, TResult> evaluationResult)
        {
            this.AddResultToStorageIfUnknown(
                evaluationResult.GenomeInstancePair.Genome,
                evaluationResult.GenomeInstancePair.Instance,
                evaluationResult.RunResult);
        }

        /// <summary>
        /// If it there is no corresponding result in storage yet, this method stores the given result using the
        /// provided <see cref="ImmutableGenome" /> - instance combination as key.
        /// </summary>
        /// <param name="genome">The <see cref="ImmutableGenome" />.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="result">The result.</param>
        private void AddResultToStorageIfUnknown(ImmutableGenome genome, TInstance instance, TResult result)
        {
            // Check if the genome - instance combination is already present in run results.
            IDictionary<TInstance, TResult> knownRunResults = null;
            var genomeRunResultsAreKnown = this._runResults.TryGetValue(genome, out knownRunResults);
            var runAlreadyInStorage = genomeRunResultsAreKnown && knownRunResults.ContainsKey(instance);
            if (runAlreadyInStorage)
            {
                return;
            }

            // If it is not, the genome - instance combination is unknown and has to be added.
            if (!genomeRunResultsAreKnown)
            {
                knownRunResults = new Dictionary<TInstance, TResult>();
                this._runResults.Add(genome, knownRunResults);
            }

            knownRunResults.Add(instance, result);
        }

        /// <summary>
        /// Handles a <see cref="AllResultsRequest"/> sent by a certain actor.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void HandleAllResultsRequest(IActorRef sender)
        {
            var allResults = new AllResults<TInstance, TResult>(this._runResults);
            sender.Tell(allResults);
        }

        /// <summary>
        /// Handles a <see cref="EvaluationStatisticRequest"/> sent by a certain actor.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void HandleEvaluationStatisticRequest(IActorRef sender)
        {
            var statistic = new EvaluationStatistic(
                configurationCount: this._runResults.Count,
                totalEvaluationCount: this._runResults.Sum(configurationResults => configurationResults.Value.Count));
            sender.Tell(statistic);
        }

        /// <summary>
        /// Handles a <see cref="GenomeResultsRequest"/> sent by a certain actor.
        /// </summary>
        /// <param name="genome">The genome the results are requested for.</param>
        /// <param name="sender">The sender.</param>
        private void HandleGenomeResultsRequest(ImmutableGenome genome, IActorRef sender)
        {
            IDictionary<TInstance, TResult> results;
            if (!this._runResults.TryGetValue(genome, out results))
            {
                results = new Dictionary<TInstance, TResult>(0);
            }

            sender.Tell(new GenomeResults<TInstance, TResult>(genome, results));
        }

        #endregion
    }
}