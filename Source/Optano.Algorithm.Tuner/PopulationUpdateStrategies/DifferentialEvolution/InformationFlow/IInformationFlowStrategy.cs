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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.InformationFlow
{
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.Genomes;

    /// <summary>
    /// Defines the information flow between the main tuning and a
    /// <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}"/> instance.
    /// </summary>
    /// <typeparam name="TSearchPoint">
    /// The type of <see cref="SearchPoint"/>s handled by the strategy instance.
    /// </typeparam>
    public interface IInformationFlowStrategy<TSearchPoint>
        where TSearchPoint : SearchPoint
    {
        #region Public Methods and Operators

        /// <summary>
        /// Determines the initial points to use for the continuous optimizer at the start of a phase.
        /// </summary>
        /// <param name="basePopulation">Population to start with.</param>
        /// <param name="currentIncumbent">Most recent incumbent genome. Might be <c>null</c>.</param>
        /// <returns>The initial points to use for the continuous optimizer.</returns>
        IEnumerable<TSearchPoint> DetermineInitialPoints(Population basePopulation, Genome currentIncumbent);

        /// <summary>
        /// Defines the competitive genomes to use when defining a new population at the end of a phase.
        /// </summary>
        /// <param name="originalCompetitives">
        /// The competitive genomes from the population the new one is based on.
        /// </param>
        /// <param name="originalIncumbent">
        /// The original incumbent from the start of the phase. Might be <c>null</c>.
        /// </param>
        /// <param name="mostRecentSorting">
        /// Most recent sorting as found by the
        /// <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}"/> instance.
        /// </param>
        /// <returns>The competitive genomes to add to the new population.</returns>
        IEnumerable<Genome> DefineCompetitivePopulation(
            IReadOnlyList<Genome> originalCompetitives,
            Genome originalIncumbent,
            IList<TSearchPoint> mostRecentSorting);

        #endregion
    }
}