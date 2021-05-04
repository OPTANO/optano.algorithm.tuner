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
    /// An implementation of <see cref="IRunEvaluator{I,R}"/> that sorts genomes by the higher number of uncancelled runs first and the lower average runtime second.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    public class SortByUnpenalizedRuntime<TInstance> : IRunEvaluator<TInstance, RuntimeResult>
        where TInstance : InstanceBase
    {
        #region Fields

        /// <summary>
        /// The cpu timeout.
        /// </summary>
        private readonly TimeSpan _cpuTimeout;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortByUnpenalizedRuntime{TInstance}"/> class.
        /// </summary>
        /// <param name="cpuTimeout">The cpu timeout.</param>
        public SortByUnpenalizedRuntime(TimeSpan cpuTimeout)
        {
            this._cpuTimeout = cpuTimeout;
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public IEnumerable<ImmutableGenomeStats<TInstance, RuntimeResult>> Sort(
            IEnumerable<ImmutableGenomeStats<TInstance, RuntimeResult>> allGenomeStatsOfMiniTournament)
        {
            /* This implementation uses the following sorting criteria:

            1.) The higher the number of uncancelled runs, the better.
            2.) The lower the averaged runtime, the better.

            NOTE: No need to penalize the average runtime, since the number of uncancelled runs is a superior sorting criterion.*/

            return allGenomeStatsOfMiniTournament
                .OrderByDescending(gs => gs.FinishedInstances.Values.Count(result => !result.IsCancelled))
                .ThenBy(
                    gs => gs.FinishedInstances.Values
                        .Select(result => result.Runtime.TotalSeconds)
                        .DefaultIfEmpty(double.PositiveInfinity)
                        .Average());
        }

        /// <inheritdoc />
        public IEnumerable<ImmutableGenome> GetGenomesThatCanBeCancelledByRacing(
            IReadOnlyList<ImmutableGenomeStats<TInstance, RuntimeResult>> allGenomeStatsOfMiniTournament,
            int numberOfMiniTournamentWinners)
        {
            var canBeCancelledByRacing = new List<ImmutableGenome>();

            var racingIncumbent = this.Sort(allGenomeStatsOfMiniTournament).Skip(numberOfMiniTournamentWinners - 1).First();
            var minimumNumberOfUncancelledResultsOfRacingIncumbent = racingIncumbent.FinishedInstances.Values.Count(result => !result.IsCancelled);
            var maximumTotalRuntimeOfRacingIncumbent = racingIncumbent.RuntimeOfFinishedInstances
                                                       + ((racingIncumbent.OpenInstances.Count + racingIncumbent.RunningInstances.Count)
                                                          * this._cpuTimeout);

            foreach (var genomeStats in allGenomeStatsOfMiniTournament.Where(g => !g.IsCancelledByRacing && g.HasOpenOrRunningInstances))
            {
                var maximumNumberOfUncancelledResults = genomeStats.FinishedInstances.Values.Count(result => !result.IsCancelled)
                                                        + genomeStats.OpenInstances.Count + genomeStats.RunningInstances.Count;
                var minimumTotalRuntime = genomeStats.RuntimeOfFinishedInstances;

                if (maximumNumberOfUncancelledResults < minimumNumberOfUncancelledResultsOfRacingIncumbent)
                {
                    // Cancel by racing, because the current genome cannot have more uncancelled results than the racing incumbent.
                    canBeCancelledByRacing.Add(genomeStats.Genome);
                }

                if (maximumNumberOfUncancelledResults == minimumNumberOfUncancelledResultsOfRacingIncumbent
                    && minimumTotalRuntime > maximumTotalRuntimeOfRacingIncumbent)
                {
                    // Cancel by racing, because the current genome cannot have a lower total run time than the racing incumbent.
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

        #endregion
    }
}