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
    /// An implementation of <see cref="IRunEvaluator{I,R}"/> that sorts genomes by the (penalized) average runtime of all target algorithm runs.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    public class SortByPenalizedRuntime<TInstance> : IMetricRunEvaluator<TInstance, RuntimeResult>
        where TInstance : InstanceBase
    {
        #region Fields

        /// <summary>
        /// The penalization factor for timed out runs' runtime.
        /// </summary>
        private readonly int _factorPar;

        /// <summary>
        /// The cpu timeout.
        /// </summary>
        private readonly TimeSpan _cpuTimeout;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortByPenalizedRuntime{TInstance}"/> class.
        /// </summary>
        /// <param name="factorPar">
        /// The penalization factor for timed out runs' runtime.
        /// </param>
        /// <param name="cpuTimeout">The cpu timeout.</param>
        public SortByPenalizedRuntime(int factorPar, TimeSpan cpuTimeout)
        {
            if (factorPar < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(factorPar), $"{nameof(factorPar)} needs to be greater than 0.");
            }

            this._factorPar = factorPar;
            this._cpuTimeout = cpuTimeout;
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public IEnumerable<ImmutableGenomeStats<TInstance, RuntimeResult>> Sort(
            IEnumerable<ImmutableGenomeStats<TInstance, RuntimeResult>> allGenomeStatsOfMiniTournament)
        {
            return allGenomeStatsOfMiniTournament
                .OrderBy(gs => this.GetUpperBoundForPenalizedTotalRuntime(gs) / gs.TotalInstanceCount);
        }

        /// <inheritdoc />
        public IEnumerable<ImmutableGenome> GetGenomesThatCanBeCancelledByRacing(
            IReadOnlyList<ImmutableGenomeStats<TInstance, RuntimeResult>> allGenomeStatsOfMiniTournament,
            int numberOfMiniTournamentWinners)
        {
            var canBeCancelledByRacing = new List<ImmutableGenome>();

            var racingIncumbent = this.Sort(allGenomeStatsOfMiniTournament).Skip(numberOfMiniTournamentWinners - 1).First();
            var maximumPenalizedTotalRuntimeOfRacingIncumbent = this.GetUpperBoundForPenalizedTotalRuntime(racingIncumbent);

            foreach (var genomeStats in allGenomeStatsOfMiniTournament.Where(g => !g.IsCancelledByRacing && g.HasOpenOrRunningInstances))
            {
                var minimumPenalizedTotalRuntime = genomeStats.FinishedInstances.Values.Sum(this.GetMetricRepresentation);

                if (minimumPenalizedTotalRuntime > maximumPenalizedTotalRuntimeOfRacingIncumbent)
                {
                    // Cancel by racing, because the current genome cannot have a lower (penalized) total run time than the racing incumbent.
                    canBeCancelledByRacing.Add(genomeStats.Genome);
                }
            }

            return canBeCancelledByRacing;
        }

        /// <inheritdoc />
        public double ComputeEvaluationPriorityOfGenome(ImmutableGenomeStats<TInstance, RuntimeResult> genomeStats, TimeSpan cpuTimeout)
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
                                   / (genomeStats.TotalInstanceCount * cpuTimeout.TotalMilliseconds);

            RunEvaluatorUtils.CheckIfRateIsOutOfBounds(cancelledInstanceRate, nameof(cancelledInstanceRate));
            RunEvaluatorUtils.CheckIfRateIsOutOfBounds(runningInstanceRate, nameof(runningInstanceRate));
            RunEvaluatorUtils.CheckIfRateIsOutOfBounds(totalRuntimeRate, nameof(totalRuntimeRate));

            var priority = (100 * cancelledInstanceRate) + (10 * runningInstanceRate) + (1 * totalRuntimeRate);

            // The lower the priority, the earlier the genome will start.
            return priority;
        }

        /// <inheritdoc />
        public double GetMetricRepresentation(RuntimeResult result)
        {
            var factor = result.IsCancelled ? this._factorPar : 1;
            return factor * result.Runtime.TotalSeconds;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets an upper bound for the penalized total runtime by adding NumberOfNotFinishedInstances * CpuTimeout * PARK to the current penalized total run time.
        /// </summary>
        /// <param name="genomeStats">The genome stats.</param>
        /// <returns>The upper bound for the penalized total runtime.</returns>
        private double GetUpperBoundForPenalizedTotalRuntime(ImmutableGenomeStats<TInstance, RuntimeResult> genomeStats)
        {
            return genomeStats.FinishedInstances.Values.Sum(this.GetMetricRepresentation)
                   + ((genomeStats.TotalInstanceCount - genomeStats.FinishedInstances.Count)
                      * this._cpuTimeout.TotalSeconds * this._factorPar);
        }

        #endregion
    }
}