#region Copyright (c) OPTANO GmbH

// ////////////////////////////////////////////////////////////////////////////////
// 
//        OPTANO GmbH Source Code
//        Copyright (c) 2010-2020 OPTANO GmbH
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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution
{
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.SearchPoint;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// An object wrapping the current status of <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/>.
    /// Can be serialized to a file and deserialized from one.
    /// </summary>
    /// <typeparam name="TInstance">
    /// Type of <see cref="InstanceBase"/> handled by the
    /// <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/>.
    /// </typeparam>
    public class DifferentialEvolutionStrategyStatus<TInstance>
        : ContinuousOptimizationStrategyStatusBase<GenomeSearchPoint, TInstance>
        where TInstance : InstanceBase
    {
        #region Constants

        /// <summary>
        /// File name to use for serialized data.
        /// </summary>
        public const string FileName = "deStatus.oatstat";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentialEvolutionStrategyStatus{TInstance}"/> class.
        /// </summary>
        /// <param name="originalIncumbent">
        /// The incumbent from before the start of the JADE phase. Might be <c>null</c>.
        /// </param>
        /// <param name="currentEvaluationInstances">
        /// The most recent set of <typeparamref name="TInstance"/>s used for evaluation.
        /// </param>
        /// <param name="mostRecentSorting">
        /// The most recent sorting returned by differential evolution.
        /// </param>
        public DifferentialEvolutionStrategyStatus(
            Genome originalIncumbent,
            List<TInstance> currentEvaluationInstances,
            List<GenomeSearchPoint> mostRecentSorting)
            : base(originalIncumbent, currentEvaluationInstances, mostRecentSorting)
        {
        }

        #endregion
    }
}