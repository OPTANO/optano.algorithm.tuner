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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// An implementation of <see cref="IRunEvaluator{TInstance,TResult}"/> that sorts genomes by the descending average integer value.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    public class SortByDescendingIntegerValue<TInstance> : IMetricRunEvaluator<TInstance, IntegerResult>
        where TInstance : InstanceBase
    {
        #region Public Methods and Operators

        /// <inheritdoc />
        public IEnumerable<ImmutableGenomeStats<TInstance, IntegerResult>> Sort(
            IEnumerable<ImmutableGenomeStats<TInstance, IntegerResult>> allGenomeStatsOfMiniTournament)
        {
            return allGenomeStatsOfMiniTournament
                .OrderByDescending(gs => gs.FinishedInstances.Values.Count(SortByDescendingIntegerValue<TInstance>.HasValidResultValue))
                .ThenByDescending(
                    gs => gs.FinishedInstances.Values.Where(SortByDescendingIntegerValue<TInstance>.HasValidResultValue)
                        .Select(this.GetMetricRepresentation)
                        .DefaultIfEmpty().Average());
        }

        /// <inheritdoc />
        public IEnumerable<ImmutableGenome> GetGenomesThatCanBeCancelledByRacing(
            IReadOnlyList<ImmutableGenomeStats<TInstance, IntegerResult>> allGenomeStatsOfMiniTournament,
            int numberOfMiniTournamentWinners)
        {
            return Enumerable.Empty<ImmutableGenome>();
        }

        /// <inheritdoc />
        public double ComputeEvaluationPriorityOfGenome(ImmutableGenomeStats<TInstance, IntegerResult> genomeStats)
        {
            return 42;
        }

        /// <inheritdoc />
        public double GetMetricRepresentation(IntegerResult result)
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
        private static bool HasValidResultValue(IntegerResult result)
        {
            return !result.IsCancelled && !double.IsNaN(result.Value) && !double.IsInfinity(result.Value);
        }

        #endregion
    }
}