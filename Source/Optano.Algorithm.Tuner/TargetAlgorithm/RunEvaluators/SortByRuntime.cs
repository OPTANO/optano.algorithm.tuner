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
    /// An implementation of <see cref="IRunEvaluator{I,R}"/> that sorts genomes by (penalized) average runtime on target algorithm runs, lower runtime first.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    public class SortByRuntime<TInstance> : IMetricRunEvaluator<TInstance, RuntimeResult>
        where TInstance : InstanceBase
    {
        #region Fields

        /// <summary>
        /// The penalization factor for timed out runs' runtime.
        /// </summary>
        private readonly int _factorPar;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortByRuntime{TInstance}"/> class.
        /// </summary>
        /// <param name="factorPar">
        /// The penalization factor for timed out runs' runtime.
        /// </param>
        public SortByRuntime(int factorPar)
        {
            this._factorPar = factorPar;
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public IEnumerable<ImmutableGenomeStats<TInstance, RuntimeResult>> Sort(
            IEnumerable<ImmutableGenomeStats<TInstance, RuntimeResult>> allGenomeStatsOfMiniTournament)
        {
            // The lower the (penalized) average runtime, the better.
            return allGenomeStatsOfMiniTournament
                .OrderByDescending(gs => gs.FinishedInstances.Count)
                .ThenBy(
                    gs => gs.FinishedInstances.Values
                        .Select(this.GetMetricRepresentation)
                        .DefaultIfEmpty(double.PositiveInfinity)
                        .Average());
        }

        /// <inheritdoc />
        public IEnumerable<ImmutableGenome> GetGenomesThatCanBeCancelledByRacing(
            IReadOnlyList<ImmutableGenomeStats<TInstance, RuntimeResult>> allGenomeStatsOfMiniTournament,
            int numberOfMiniTournamentWinners)
        {
            var canBeCancelledByRacing = new List<ImmutableGenome>();

            var racingCandidate = this.Sort(allGenomeStatsOfMiniTournament.Where(g => g.AllInstancesFinishedWithoutCancelledResult))
                .Skip(numberOfMiniTournamentWinners - 1).FirstOrDefault();

            if (racingCandidate == null)
            {
                return canBeCancelledByRacing;
            }

            foreach (var genomeStats in allGenomeStatsOfMiniTournament.Where(g => !g.IsCancelledByRacing && g.HasOpenOrRunningInstances))
            {
                if (genomeStats.RuntimeOfFinishedInstances > racingCandidate.RuntimeOfFinishedInstances)
                {
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
    }
}