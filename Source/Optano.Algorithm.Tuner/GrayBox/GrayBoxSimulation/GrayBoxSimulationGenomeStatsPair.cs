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

namespace Optano.Algorithm.Tuner.GrayBox.GrayBoxSimulation
{
    using System;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// A pair of <see cref="ImmutableGenomeStats{TInstance,TResult}"/>, used in the <see cref="GrayBoxSimulation{TTargetAlgorithm, TInstance, TResult}"/>.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class GrayBoxSimulationGenomeStatsPair<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()

    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GrayBoxSimulationGenomeStatsPair{TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="blackBoxGenomeStats">The black box genome stats.</param>
        /// <param name="grayBoxGenomeStats">The gray box genome stats.</param>
        public GrayBoxSimulationGenomeStatsPair(
            ImmutableGenomeStats<TInstance, TResult> blackBoxGenomeStats,
            ImmutableGenomeStats<TInstance, TResult> grayBoxGenomeStats)
        {
            if (!ImmutableGenome.GenomeComparer.Equals(blackBoxGenomeStats.Genome, grayBoxGenomeStats.Genome))
            {
                throw new ArgumentException(
                    "The genome of the black box genome stats needs to be the same genome than the one of the gray box genome stats.");
            }

            this.Genome = blackBoxGenomeStats.Genome;
            this.BlackBoxGenomeStats = blackBoxGenomeStats;
            this.GrayBoxGenomeStats = grayBoxGenomeStats;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome.
        /// </summary>
        public ImmutableGenome Genome { get; }

        /// <summary>
        /// Gets the black box genome stats.
        /// </summary>
        public ImmutableGenomeStats<TInstance, TResult> BlackBoxGenomeStats { get; }

        /// <summary>
        /// Gets the gray box genome stats.
        /// </summary>
        public ImmutableGenomeStats<TInstance, TResult> GrayBoxGenomeStats { get; }

        #endregion
    }
}