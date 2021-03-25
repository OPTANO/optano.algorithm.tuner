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

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Customizable comparer, responsible for comparing multiple genome instance pair evaluations.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    public interface IRunEvaluator<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Public Methods and Operators

        /// <summary>
        /// Sorts the provided <see cref="ImmutableGenomeStats{TInstance,TResult}"/> by performance, best <see cref="ImmutableGenomeStats{TInstance,TResult}"/> first.
        /// </summary>
        /// <param name="allGenomeStatsOfMiniTournament">
        /// The <see cref="ImmutableGenomeStats{TInstance,TResult}"/> of the current mini tournament.
        /// <para>
        /// NOTE: If racing is enabled (i.e. --enableRacing=true), the number of finished instances per genome may vary.
        /// </para>
        /// <para>
        /// NOTE: The finished instances may include results cancelled due to CPU timeouts. For those the <see cref="ResultBase{TResult}.IsCancelled"/>-Boolean is true.
        /// </para>
        /// </param>
        /// <returns>The sorted <see cref="ImmutableGenomeStats{TInstance,TResult}"/>, best <see cref="ImmutableGenomeStats{TInstance,TResult}"/> first.</returns>
        IEnumerable<ImmutableGenomeStats<TInstance, TResult>> Sort(
            IEnumerable<ImmutableGenomeStats<TInstance, TResult>> allGenomeStatsOfMiniTournament);

        /// <summary>
        /// Gets all genomes that should not be evaluated further due to racing.
        /// IMPORTANT: We strongly recommend to not cancel any target algorithm evaluations of possible mini tournament winners by racing, even if all other genome evaluations have been finished, to ensure that all mini tournament winners have seen all instances. That's why the tuner will throw an exception, if you want to cancel more genomes by racing than "number of mini tournament participants - desired number of mini tournament winners".
        /// NOTE: If racing is enabled (i.e. --enableRacing=true), implementing this method in combination with <see cref="ComputeEvaluationPriorityOfGenome"/> can drastically reduce the run time of the whole tuning.
        /// That's why the implementation of this method should strongly depend on the implementations of <see cref="Sort"/> and <see cref="ComputeEvaluationPriorityOfGenome"/>.
        /// </summary>
        /// <param name="allGenomeStatsOfMiniTournament">The genome stats of the current mini tournament.</param>
        /// <param name="numberOfMiniTournamentWinners">The desired number of mini tournament winners.</param>
        /// <returns>The genomes that should not be evaluated further.</returns>
        IEnumerable<ImmutableGenome> GetGenomesThatCanBeCancelledByRacing(
            IReadOnlyList<ImmutableGenomeStats<TInstance, TResult>> allGenomeStatsOfMiniTournament,
            int numberOfMiniTournamentWinners);

        /// <summary>
        /// Computes the evaluation priority of the current genome, based on the given information.
        /// The lower the evaluation priority, the earlier the genome will be evaluated.
        /// NOTE: If racing is enabled (i.e. --enableRacing=true), implementing this method in combination with <see cref="GetGenomesThatCanBeCancelledByRacing"/> can drastically reduce the run time of the whole tuning.
        /// That's why the implementation of this method should strongly depend on the implementations of <see cref="Sort"/> and <see cref="GetGenomesThatCanBeCancelledByRacing"/>.
        /// </summary>
        /// <param name="genomeStats">The genome stats.</param>
        /// <param name="cpuTimeout">The CPU timeout.</param>
        /// <returns>The genome priority.</returns>
        double ComputeEvaluationPriorityOfGenome(ImmutableGenomeStats<TInstance, TResult> genomeStats, TimeSpan cpuTimeout);

        #endregion
    }
}