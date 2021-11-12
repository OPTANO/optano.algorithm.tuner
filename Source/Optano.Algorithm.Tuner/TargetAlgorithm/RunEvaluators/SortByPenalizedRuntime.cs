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
    /// An implementation of <see cref="IRunEvaluator{I,R}"/> that sorts genomes by the (penalized) average runtime of all target algorithm runs.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    public class SortByPenalizedRuntime<TInstance> : RacingRunEvaluatorBase<TInstance, RuntimeResult>,
                                                             IMetricRunEvaluator<TInstance, RuntimeResult>
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
        /// Initializes a new instance of the <see cref="SortByPenalizedRuntime{TInstance}"/> class.
        /// </summary>
        /// <param name="factorPar">
        /// The penalization factor for timed out runs' runtime.
        /// </param>
        /// <param name="cpuTimeout">The cpu timeout.</param>
        /// <param name="getPlaceholderInstance">
        /// The GetPlaceholderInstance func, used in <see cref="RacingRunEvaluatorBase{TInstance, TResult}.GetExtendedGenomeStats"/>: Gets a placeholder instance from a unique ID integer.
        /// If null, a fallback func, implemented for <see cref="InstanceFile"/> and <see cref="InstanceSeedFile"/>, is used.
        /// </param>
        public SortByPenalizedRuntime(int factorPar, TimeSpan cpuTimeout, Func<int, TInstance> getPlaceholderInstance = null)
            : base(cpuTimeout, getPlaceholderInstance)
        {
            if (factorPar < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(factorPar), $"{nameof(factorPar)} needs to be greater than 0.");
            }

            this._factorPar = factorPar;
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
            return allGenomeStatsOfMiniTournament
                .OrderBy(gs => this.GetUpperBoundForPenalizedTotalRuntime(gs) / gs.TotalInstanceCount);
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
                      * this.CpuTimeout.TotalSeconds * this._factorPar);
        }

        #endregion
    }
}