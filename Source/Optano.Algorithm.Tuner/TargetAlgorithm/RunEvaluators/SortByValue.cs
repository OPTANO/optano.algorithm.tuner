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
    /// An implementation of <see cref="IRunEvaluator{I,R}" /> that sorts genomes by average value in target algorithm runs.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    public class SortByValue<TInstance> : IMetricRunEvaluator<TInstance, ContinuousResult>
        where TInstance : InstanceBase
    {
        #region Fields

        /// <summary>
        /// A value indicating whether values should be sorted ascendingly.
        /// </summary>
        private readonly bool _ascending;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortByValue{TInstance}"/> class.
        /// </summary>
        /// <param name="ascending">A value indicating whether values should be sorted ascendingly.</param>
        public SortByValue(bool ascending = true)
        {
            this._ascending = ascending;
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public IEnumerable<ImmutableGenomeStats<TInstance, ContinuousResult>> Sort(
            IEnumerable<ImmutableGenomeStats<TInstance, ContinuousResult>> allGenomeStatsOfMiniTournament)
        {
            var orderByNumberOfValidResults = allGenomeStatsOfMiniTournament
                .OrderByDescending(gs => gs.FinishedInstances.Values.Count(SortByValue<TInstance>.HasValidResultValue));

            if (this._ascending)
            {
                return orderByNumberOfValidResults
                    .ThenBy(
                        gs => gs.FinishedInstances.Values.Where(SortByValue<TInstance>.HasValidResultValue).Select(this.GetMetricRepresentation)
                            .DefaultIfEmpty().Average());
            }

            return orderByNumberOfValidResults
                .ThenByDescending(
                    gs => gs.FinishedInstances.Values.Where(SortByValue<TInstance>.HasValidResultValue).Select(this.GetMetricRepresentation)
                        .DefaultIfEmpty().Average());
        }

        /// <inheritdoc />
        public IEnumerable<ImmutableGenome> GetGenomesThatCanBeCancelledByRacing(
            IReadOnlyList<ImmutableGenomeStats<TInstance, ContinuousResult>> allGenomeStatsOfMiniTournament,
            int numberOfMiniTournamentWinners)
        {
            // Implementing a useful racing strategy is not possible without knowing the global minimum or maximum. Therefore no genome can be cancelled by racing.
            return Enumerable.Empty<ImmutableGenome>();
        }

        /// <inheritdoc />
        public double ComputeEvaluationPriorityOfGenome(ImmutableGenomeStats<TInstance, ContinuousResult> genomeStats, TimeSpan cpuTimeout)
        {
            // Implementing a useful racing strategy is not possible without knowing the global minimum or maximum. Therefore all genomes have the same priority.
            return 42;
        }

        /// <inheritdoc />
        public double GetMetricRepresentation(ContinuousResult result)
        {
            return result.Value;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks, if the result has a valid result value.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>True, if the result has a valid result value.</returns>
        private static bool HasValidResultValue(ContinuousResult result)
        {
            return !result.IsCancelled && !double.IsNaN(result.Value) && !double.IsInfinity(result.Value);
        }

        #endregion
    }
}