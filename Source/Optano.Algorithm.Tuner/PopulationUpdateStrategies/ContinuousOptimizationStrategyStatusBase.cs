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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies
{
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// An object wrapping the current status of a
    /// <see cref="ContinuousOptimizationStrategyBase{TSearchPoint, TInstance,TResult}"/>.
    /// Can be serialized to a file and deserialized from one.
    /// </summary>
    /// <typeparam name="TSearchPoint">
    /// Type of <see cref="SearchPoint"/> handled by the
    /// <see cref="ContinuousOptimizationStrategyBase{TSearchPoint, TInstance,TResult}"/>.
    /// </typeparam>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    public abstract class ContinuousOptimizationStrategyStatusBase<TSearchPoint, TInstance> : StatusBase
        where TSearchPoint : SearchPoint, IDeserializationRestorer<TSearchPoint>
        where TInstance : InstanceBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuousOptimizationStrategyStatusBase{TSearchPoint, TInstance}"/> class.
        /// </summary>
        /// <param name="originalIncumbent">
        /// The incumbent from before the start of the continuous optimization phase. Might be <c>null</c>.
        /// </param>
        /// <param name="currentEvaluationInstances">
        /// The most recent set of <typeparamref name="TInstance"/>s used for evaluation.
        /// </param>
        /// <param name="mostRecentSorting">
        /// The most recent sorting returned by
        /// <see cref="IEvolutionBasedContinuousOptimizer{TSearchPoint}.NextGeneration"/>.
        /// </param>
        protected ContinuousOptimizationStrategyStatusBase(
            Genome originalIncumbent,
            List<TInstance> currentEvaluationInstances,
            List<TSearchPoint> mostRecentSorting)
        {
            this.OriginalIncumbent = originalIncumbent;
            this.CurrentEvaluationInstances = currentEvaluationInstances;
            this.MostRecentSorting = mostRecentSorting;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the incumbent from before the start of the CMA-ES phase. Might be <c>null</c>.
        /// </summary>
        public Genome OriginalIncumbent { get; }

        /// <summary>
        /// Gets the most recent sorting.
        /// </summary>
        public List<TSearchPoint> MostRecentSorting { get; }

        /// <summary>
        /// Gets the most recent set of <typeparamref name="TInstance"/>s used for evaluation.
        /// </summary>
        public List<TInstance> CurrentEvaluationInstances { get; }

        #endregion
    }
}