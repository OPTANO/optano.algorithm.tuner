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
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// An implementation of <see cref="IRunEvaluator{I,R}"/> that sorts genomes by the higher number of uncancelled runs first and the lower average runtime second.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    public class SortByUnpenalizedRuntime<TInstance> : RacingRunEvaluatorBase<TInstance, RuntimeResult>
        where TInstance : InstanceBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortByUnpenalizedRuntime{TInstance}"/> class.
        /// </summary>
        /// <param name="cpuTimeout">The cpu timeout.</param>
        /// <param name="getPlaceholderInstance">
        /// The GetPlaceholderInstance func, used in <see cref="RacingRunEvaluatorBase{TInstance, TResult}.GetExtendedGenomeStats"/>: Gets a placeholder instance from a unique ID integer.
        /// If null, a fallback func, implemented for <see cref="InstanceFile"/> and <see cref="InstanceSeedFile"/>, is used.
        /// </param>
        public SortByUnpenalizedRuntime(TimeSpan cpuTimeout, Func<int, TInstance> getPlaceholderInstance = null)
            : base(cpuTimeout, getPlaceholderInstance)
        {
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        protected override RuntimeResult BestPossibleResult => new RuntimeResult(TimeSpan.Zero);

        /// <inheritdoc />
        protected override RuntimeResult WorstPossibleResult => new RuntimeResult(this.CpuTimeout);

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public override IEnumerable<ImmutableGenomeStats<TInstance, RuntimeResult>> Sort(
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

        #endregion
    }
}