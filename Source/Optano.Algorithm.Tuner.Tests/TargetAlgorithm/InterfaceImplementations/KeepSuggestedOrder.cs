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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// An implementation of <see cref="IRunEvaluator{TInstance,TResult}"/> that doesn't reorder the genomes at all.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    internal class KeepSuggestedOrder<TInstance, TResult> : IRunEvaluator<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Public Methods and Operators

        /// <inheritdoc />
        public IEnumerable<ImmutableGenomeStats<TInstance, TResult>> Sort(
            IEnumerable<ImmutableGenomeStats<TInstance, TResult>> allGenomeStatsOfMiniTournament)
        {
            return allGenomeStatsOfMiniTournament;
        }

        /// <inheritdoc />
        public IEnumerable<ImmutableGenome> GetGenomesThatCanBeCancelledByRacing(
            IReadOnlyList<ImmutableGenomeStats<TInstance, TResult>> allGenomeStatsOfMiniTournament,
            int numberOfMiniTournamentWinners)
        {
            return Enumerable.Empty<ImmutableGenome>();
        }

        /// <summary>
        /// Uses the <paramref name="genomeStats"/>.Genome.Age - OpenInstances.Count as priority.
        /// </summary>
        /// <param name="genomeStats">The genome stats.</param>
        /// <param name="cpuTimeout">The timeout.</param>
        /// <returns><see cref="ImmutableGenomeStats{TInstance,TResult}.Genome"/>.Age.</returns>
        public double ComputeEvaluationPriorityOfGenome(ImmutableGenomeStats<TInstance, TResult> genomeStats, TimeSpan cpuTimeout)
        {
            return genomeStats.Genome.Age - genomeStats.OpenInstances.Count;
        }

        #endregion
    }
}