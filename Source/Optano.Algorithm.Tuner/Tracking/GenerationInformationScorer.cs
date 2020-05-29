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

namespace Optano.Algorithm.Tuner.Tracking
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// Responsible for completing <see cref="GenerationInformation"/> objects by adding scores to them.
    /// </summary>
    /// <typeparam name="TInstance">
    /// The instance type to use.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The result of an individual evaluation.
    /// </typeparam>
    public class GenerationInformationScorer<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// A <see cref="IActorRef"/> to a <see cref="GenomeSorter{TInstance,TResult}"/> which can be used to provoke
        /// evaluations.
        /// </summary>
        private readonly IActorRef _genomeSorter;

        /// <summary>
        /// A <see cref="IActorRef" /> to a <see cref="ResultStorageActor{TInstance,TResult}" />
        /// which knows about all executed target algorithm runs and their results.
        /// </summary>
        private readonly IActorRef _resultStorageActor;

        /// <summary>
        /// The <see cref="IMetricRunEvaluator{TResult}"/> to score the target algorithm run results.
        /// </summary>
        private readonly IMetricRunEvaluator<TResult> _runEvaluator;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationInformationScorer{TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="genomeSorter">
        /// A <see cref="IActorRef"/> to a <see cref="GenomeSorter{TInstance, TResult}"/> which can be used to provoke
        /// evaluations.
        /// </param>
        /// <param name="resultStorageActor">
        /// A <see cref="IActorRef" /> to a <see cref="ResultStorageActor{TInstance,TResult}" />
        /// which knows about all executed target algorithm runs and their results.
        /// </param>
        /// <param name="runEvaluator">
        /// The <see cref="IMetricRunEvaluator{TResult}"/> to score the target algorithm run results.
        /// </param>
        /// <exception cref="NullReferenceException">
        /// Thrown if <paramref name="genomeSorter"/>, <paramref name="resultStorageActor"/> or
        /// <paramref name="runEvaluator"/> are <c>null</c>.
        /// </exception>
        public GenerationInformationScorer(
            IActorRef genomeSorter,
            IActorRef resultStorageActor,
            IMetricRunEvaluator<TResult> runEvaluator)
        {
            this._genomeSorter = genomeSorter ?? throw new ArgumentNullException(nameof(genomeSorter));
            this._resultStorageActor = resultStorageActor ??
                                      throw new ArgumentNullException(nameof(resultStorageActor));
            this._runEvaluator = runEvaluator ?? throw new ArgumentNullException(nameof(runEvaluator));
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Adds incumbent candidate scores to <see cref="GenerationInformation" /> objects.
        /// </summary>
        /// <param name="informationHistory">The <see cref="GenerationInformation" /> objects to score.</param>
        /// <param name="trainingInstances">The training instances.</param>
        /// <param name="testInstances">The test instances.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="informationHistory"/>, <paramref name="trainingInstances"/> or
        /// <paramref name="testInstances"/> are <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="trainingInstances"/> is empty.
        /// </exception>
        public void ScoreInformationHistory(
            IList<GenerationInformation> informationHistory,
            List<TInstance> trainingInstances,
            List<TInstance> testInstances)
        {
            if (informationHistory == null)
            {
                throw new ArgumentNullException(nameof(informationHistory));
            }

            if (trainingInstances == null)
            {
                throw new ArgumentNullException(nameof(trainingInstances));
            }

            if (testInstances == null)
            {
                throw new ArgumentNullException(nameof(testInstances));
            }

            if (!trainingInstances.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(trainingInstances), "No training instances provided.");
            }

            this.EvaluateAllIncumbentCandidates(informationHistory, trainingInstances.Concat(testInstances));
            foreach (var information in informationHistory)
            {
                information.IncumbentTrainingScore = this.AverageResultsOn(trainingInstances, information.Incumbent);
                if (testInstances.Any())
                {
                    information.IncumbentTestScore = this.AverageResultsOn(testInstances, information.Incumbent);
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Provokes evaluations on all incumbent candidates on the provided instances.
        /// </summary>
        /// <param name="informationHistory">
        /// <see cref="GenerationInformation"/> objects storing the incumbent candidates.
        /// </param>
        /// <param name="instances">The instances to evaluate the candidates on.</param>
        private void EvaluateAllIncumbentCandidates(
            IEnumerable<GenerationInformation> informationHistory,
            IEnumerable<TInstance> instances)
        {
            var incumbents = informationHistory.Select(information => information.Incumbent);

            var evaluationRequest = this._genomeSorter.Ask(
                new SortCommand<TInstance>(incumbents.ToImmutableList(), instances.ToImmutableList()));
            evaluationRequest.Wait();
        }

        /// <summary>
        /// Averages the compare values of a certain genome's results on the provided instances.
        /// </summary>
        /// <param name="instances">
        /// The instances to take the average on. All of them should already be evaluated for <paramref name="genome"/>.
        /// </param>
        /// <param name="genome">The genome.</param>
        /// <returns>The average compare value.</returns>
        private double AverageResultsOn(IEnumerable<TInstance> instances, ImmutableGenome genome)
        {
            var resultRequest = this._resultStorageActor.Ask<GenomeResults<TInstance, TResult>>(
                new GenomeResultsRequest(genome));
            resultRequest.Wait();
            var allResults = resultRequest.Result.RunResults;

            return instances.Average(instance => this._runEvaluator.GetMetricRepresentation(allResults[instance]));
        }

        #endregion
    }
}