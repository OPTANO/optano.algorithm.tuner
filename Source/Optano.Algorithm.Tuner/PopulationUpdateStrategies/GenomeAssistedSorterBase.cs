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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Abstract base class for all <see cref="ISearchPointSorter{TSearchPoint}"/> which are based on
    /// <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>s.
    /// </summary>
    /// <typeparam name="TSearchPoint">
    /// The kind of <typeparamref name="TSearchPoint"/> which gets sorted.
    /// </typeparam>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    public abstract class GenomeAssistedSorterBase<TSearchPoint, TInstance, TResult> : SearchPointSorterBase<TSearchPoint>
        where TSearchPoint : SearchPoint
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// Gets an <see cref="IActorRef" /> to a <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </summary>
        private readonly IActorRef _generationEvaluationActor;

        /// <summary>
        /// The <typeparamref name="TInstance"/>s to base the sorting on.
        /// </summary>
        private readonly List<TInstance> _instances = new List<TInstance>();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeAssistedSorterBase{TSearchPoint, TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="generationEvaluationActor">
        /// An <see cref="IActorRef" /> to a <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </param>
        protected GenomeAssistedSorterBase(IActorRef generationEvaluationActor)
        {
            this._generationEvaluationActor = generationEvaluationActor ?? throw new ArgumentNullException(nameof(generationEvaluationActor));
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Updates the <typeparamref name="TInstance"/>s to base the sorting on.
        /// </summary>
        /// <param name="instances">The <typeparamref name="TInstance"/>s to use.</param>
        public void UpdateInstances(IEnumerable<TInstance> instances)
        {
            this._instances.Clear();
            this._instances.AddRange(instances);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Assigns ranks to <see cref="ImmutableGenome"/>s based on a <see cref="SortResult"/> on these points.
        /// </summary>
        /// <param name="sortResult">
        /// <see cref="SortResult"/> on <paramref name="genomes"/>.
        /// </param>
        /// <param name="genomes">
        /// The <see cref="ImmutableGenome"/>s to assign ranks to.
        /// </param>
        /// <returns>The rank assignments.</returns>
        protected static Dictionary<ImmutableGenome, int> AssignRanksToGenomes(
            SortResult sortResult,
            ImmutableList<ImmutableGenome> genomes)
        {
            var ranks = new Dictionary<ImmutableGenome, int>(genomes.Count);
            for (int rank = 0; rank < sortResult.Ranking.Count; rank++)
            {
                // There might be several points describing the same genome, so make sure to choose one which has
                // no rank assigned as of yet.
                var fittingGenome = genomes.First(
                    genome =>
                        ImmutableGenome.GenomeComparer.Equals(sortResult.Ranking[rank], genome) && !ranks.ContainsKey(genome));
                ranks.Add(fittingGenome, rank);
            }

            return ranks;
        }

        /// <summary>
        /// Sorts genomes using the <see cref="_generationEvaluationActor"/>.
        /// </summary>
        /// <param name="genomesToSort">The <see cref="ImmutableGenome"/>s to sort.</param>
        /// <returns>The sorting result.</returns>
        protected SortResult SortGenomes(ImmutableList<ImmutableGenome> genomesToSort)
        {
            var generationEvaluationTask = this._generationEvaluationActor.Ask<SortResult>(
                new GenerationEvaluation<TInstance, TResult>(
                    genomesToSort,
                    this._instances,
                    (runEvaluator, participantsOfGeneration, instancesOfGeneration) =>
                        new SortingGenerationEvaluationStrategy<TInstance, TResult>(runEvaluator, participantsOfGeneration, instancesOfGeneration)));

            generationEvaluationTask.Wait();

            return generationEvaluationTask.Result;
        }

        #endregion
    }
}