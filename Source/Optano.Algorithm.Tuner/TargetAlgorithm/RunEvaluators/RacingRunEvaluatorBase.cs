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

namespace Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Abstract <see cref="IRunEvaluator{I,R}"/> base class, which supports the implementation of custom racing strategies.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public abstract class RacingRunEvaluatorBase<TInstance, TResult> : IRunEvaluator<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// The GetPlaceholderInstance func, used in <see cref="RacingRunEvaluatorBase{TInstance, TResult}.GetExtendedGenomeStats"/>: Gets a placeholder instance from a unique ID integer.
        /// </summary>
        private readonly Func<int, TInstance> _getPlaceholderInstance;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RacingRunEvaluatorBase{TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="cpuTimeout">The cpu timeout.</param>
        /// <param name="getPlaceholderInstance">
        /// The GetPlaceholderInstance func, used in <see cref="RacingRunEvaluatorBase{TInstance, TResult}.GetExtendedGenomeStats"/>: Gets a placeholder instance from a unique ID integer.
        /// If null, a fallback func, implemented for <see cref="InstanceFile"/> and <see cref="InstanceSeedFile"/>, is used.
        /// </param>
        protected RacingRunEvaluatorBase(TimeSpan cpuTimeout, Func<int, TInstance> getPlaceholderInstance)
        {
            this.CpuTimeout = cpuTimeout;
            this._getPlaceholderInstance = getPlaceholderInstance ?? this.GetPlaceholderInstanceFallback;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the cpu timeout.
        /// </summary>
        protected TimeSpan CpuTimeout { get; }

        /// <summary>
        /// Gets the best possible result. This result is used to extend the racing candidate genome stats.
        /// </summary>
        protected abstract TResult BestPossibleResult { get; }

        /// <summary>
        /// Gets the worst possible result. This result is used to extend the racing incumbent genome stats.
        /// </summary>
        protected abstract TResult WorstPossibleResult { get; }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public abstract IEnumerable<ImmutableGenomeStats<TInstance, TResult>> Sort(
            IEnumerable<ImmutableGenomeStats<TInstance, TResult>> allGenomeStatsOfMiniTournament);

        /// <inheritdoc />
        public virtual IEnumerable<ImmutableGenome> GetGenomesThatCanBeCancelledByRacing(
            IReadOnlyList<ImmutableGenomeStats<TInstance, TResult>> allGenomeStatsOfMiniTournament,
            int numberOfMiniTournamentWinners)
        {
            var racingIncumbentStats = this.Sort(allGenomeStatsOfMiniTournament).Skip(numberOfMiniTournamentWinners - 1).First();
            var extendedWorstPossibleRacingIncumbentStats = this.GetExtendedGenomeStats(racingIncumbentStats, this.WorstPossibleResult);

            var racingCandidateStats = allGenomeStatsOfMiniTournament.Where(g => !g.IsCancelledByRacing && g.HasOpenOrRunningInstances);
            var extendedBestPossibleRacingCandidateStats =
                racingCandidateStats.Select(g => this.GetExtendedGenomeStats(g, this.BestPossibleResult)).ToList();

            var extendedGenomeStats =
                Enumerable.Repeat(extendedWorstPossibleRacingIncumbentStats, 1).Concat(extendedBestPossibleRacingCandidateStats);

            return this.Sort(extendedGenomeStats)
                .Reverse()
                .TakeWhile(genomeStats => !ImmutableGenome.GenomeComparer.Equals(genomeStats.Genome, racingIncumbentStats.Genome))
                .Select(genomeStats => genomeStats.Genome);
        }

        /// <inheritdoc />
        public virtual double ComputeEvaluationPriorityOfGenome(ImmutableGenomeStats<TInstance, TResult> genomeStats)
        {
            if (genomeStats.IsCancelledByRacing)
            {
                return 1000;
            }

            // First decision criterion: The higher the cancelled instance rate, the later the genome will start.
            var cancelledCount = genomeStats.FinishedInstances.Values.Count(r => r.IsCancelled);
            var cancelledInstanceRate = (double)cancelledCount / genomeStats.TotalInstanceCount;

            // Second decision criterion: The higher the running instance rate, the later the genome will start.
            var runningInstanceRate = (double)genomeStats.RunningInstances.Count / genomeStats.TotalInstanceCount;

            // Third decision criterion: The higher the total runtime rate, the later the genome will start.
            var totalRuntimeRate = genomeStats.RuntimeOfFinishedInstances.TotalMilliseconds
                                   / (genomeStats.TotalInstanceCount * this.CpuTimeout.TotalMilliseconds);

            // The lower the priority, the earlier the genome will start.
            return (100 * cancelledInstanceRate) + (10 * runningInstanceRate) + (1 * totalRuntimeRate);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Extends the results of the finished instances by the given result for every not finished instance.
        /// </summary>
        /// <param name="genomeStats">The genome stats.</param>
        /// <param name="result">The result.</param>
        /// <returns>The extended genome stats.</returns>
        protected virtual ImmutableGenomeStats<TInstance, TResult> GetExtendedGenomeStats(
            ImmutableGenomeStats<TInstance, TResult> genomeStats,
            TResult result)
        {
            var results = genomeStats.FinishedInstances.Values.Concat(
                Enumerable.Repeat(result, genomeStats.TotalInstanceCount - genomeStats.FinishedInstances.Count));

            var counter = 0;
            var resultDictionary = results.ToDictionary(x => this._getPlaceholderInstance(counter++));

            return new ImmutableGenomeStats<TInstance, TResult>(
                new GenomeStats<TInstance, TResult>(genomeStats.Genome, resultDictionary));
        }

        /// <summary>
        /// The fallback func for <see cref="RacingRunEvaluatorBase{TInstance, TResult}._getPlaceholderInstance"/>: Gets a placeholder instance from a unique ID integer.
        /// </summary>
        /// <param name="id">The unique ID integer.</param>
        /// <returns>The placeholder instance.</returns>
        private TInstance GetPlaceholderInstanceFallback(int id)
        {
            if (typeof(TInstance) == typeof(InstanceFile))
            {
                return new InstanceFile($"Instance_{id}") as TInstance;
            }

            if (typeof(TInstance) == typeof(InstanceSeedFile))
            {
                return new InstanceSeedFile($"Instance_{id}", id) as TInstance;
            }

            throw new NotImplementedException(
                $"The fallback func for {nameof(this._getPlaceholderInstance)} is only implemented for {nameof(InstanceFile)} and {nameof(InstanceSeedFile)}. If you want to use another instance type, you need to provide your own GetPlaceholderInstance func in the constructor.");
        }

        #endregion
    }
}