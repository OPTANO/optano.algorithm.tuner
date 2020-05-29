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

namespace Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;

    /// <summary>
    /// Fixed parameters for CMA-ES.
    /// <para>
    /// Based on Hansen, N.: The CMA Evolution Strategy: A Tutorial. In: CoRR abs/1604.00772 (2016) http://arxiv.org/abs/1604.00772.
    /// </para>
    /// </summary>
    public class CmaEsConfiguration
    {
        #region Fields

        /// <summary>
        /// Backing field for <see cref="InitialDistributionMean"/>.
        /// </summary>
        private readonly Vector<double> _initialDistributionMean;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CmaEsConfiguration"/> class.
        /// </summary>
        /// <param name="populationSize">The population size, at least 2.</param>
        /// <param name="initialDistributionMean">The initial distribution mean.</param>
        /// <param name="initialStepSize">The positive initial step size.</param>
        public CmaEsConfiguration(
            int populationSize,
            Vector<double> initialDistributionMean,
            double initialStepSize)
        {
            if (populationSize < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(populationSize),
                    $"Population needs to consist of at least 2 search points, but size was {populationSize}.");
            }

            if (initialDistributionMean == null)
            {
                throw new ArgumentNullException(nameof(initialDistributionMean));
            }

            if (initialStepSize <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialStepSize),
                    $"Step size must be positive, but was {initialStepSize}.");
            }

            this.SearchSpaceDimension = initialDistributionMean.Count;
            this.PopulationSize = populationSize;
            this.ParentNumber = this.PopulationSize / 2;
            this._initialDistributionMean = initialDistributionMean.Clone();
            this.InitialStepSize = initialStepSize;

            this.InitializeDefaultStrategyParameters();
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the dimension of the search space, often denoted n in literature.
        /// </summary>
        public int SearchSpaceDimension { get; }

        /// <summary>
        /// Gets the population size, often denoted lambda in literature.
        /// </summary>
        public int PopulationSize { get; }

        /// <summary>
        /// Gets the parent number, often denoted mu in literature.
        /// </summary>
        public int ParentNumber { get; }

        /// <summary>
        /// Gets the recombination weights, often denoted w_i in literature.
        /// </summary>
        public ImmutableList<double> Weights { get; private set; }

        /// <summary>
        /// Gets the variance effective selection mass, often denoted mu_eff in literature.
        /// </summary>
        public double VarianceEffectiveSelectionMass { get; private set; }

        /// <summary>
        /// Gets the learning rate for cumulation for the rank-one update of the covariance matrix.
        /// Often denoted c_c in literature.
        /// </summary>
        public double CumulationLearningRate { get; private set; }

        /// <summary>
        /// Gets the learning rate for the rank-one update of the covariance matrix update.
        /// Often denoted c_1 in literature.
        /// </summary>
        public double RankOneUpdateLearningRate { get; private set; }

        /// <summary>
        /// Gets the learning rate for the rank-mu update of the covariance matrix update.
        /// Often denoted c_mu in literature.
        /// </summary>
        public double RankMuUpdateLearningRate { get; private set; }

        /// <summary>
        /// Gets the learning rate for the cumulation for the step-size control.
        /// Often denoted c_sigma in literature.
        /// </summary>
        public double StepSizeControlLearningRate { get; private set; }

        /// <summary>
        /// Gets the damping parameter for step-size update.
        /// Often denoted d_sigma in literature.
        /// </summary>
        public double StepSizeControlDamping { get; private set; }

        /// <summary>
        /// Gets the initial distribution mean, often denoted m in literature.
        /// </summary>
        public Vector<double> InitialDistributionMean => this._initialDistributionMean.Clone();

        /// <summary>
        /// Gets the initial step size, often denoted sigma in literature.
        /// </summary>
        public double InitialStepSize { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Computes the expected length of the conjugate evolution path, i.e. E||N(0, I)||.
        /// </summary>
        /// <returns>The computed expectation.</returns>
        public double ComputeExpectedConjugateEvolutionPathLength()
        {
            return
                Constants.Sqrt2 *
                (SpecialFunctions.Gamma((this.SearchSpaceDimension + 1) / 2d) / SpecialFunctions.Gamma(this.SearchSpaceDimension / 2d));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes default strategy parameters as defined by Hansen, N.:
        /// The CMA Evolution Strategy: A Tutorial. In: CoRR abs/1604.00772 (2016) http://arxiv.org/abs/1604.00772.
        /// <para>The author states that in general, it is not recommended to change these settings.</para>
        /// </summary>
        private void InitializeDefaultStrategyParameters()
        {
            var unnormalizedWeights = this.CreateConvexWeightShape();
            this.VarianceEffectiveSelectionMass = this.ComputeVarianceEffectiveSelectionMass(unnormalizedWeights);

            this.InitializeStepSizeControlParameters();
            this.InitializeCovarianceMatrixAdaptationParameters();
            this.InitializeWeights(unnormalizedWeights);
        }

        /// <summary>
        /// Creates convex weight shape s.t. the first half of the weights is positive and the latter half is negative.
        /// </summary>
        /// <returns>The created weights.</returns>
        private List<double> CreateConvexWeightShape()
        {
            var unnormalizedWeights = new List<double>(this.PopulationSize);
            for (int i = 1; i <= this.PopulationSize; i++)
            {
                var weight = Math.Log((this.PopulationSize + 1) / 2d) - Math.Log(i);
                unnormalizedWeights.Add(weight);
            }

            return unnormalizedWeights;
        }

        /// <summary>
        /// Computes the variance effective selection mass (often denoted mu_eff), i.e. the fraction between the
        /// squared sum of the parent weights and the sum of the squared parents weights.
        /// </summary>
        /// <param name="unnormalizedWeights">The unnormalized weights.</param>
        /// <returns>The variance effective selection mass.</returns>
        private double ComputeVarianceEffectiveSelectionMass(List<double> unnormalizedWeights)
        {
            var parentWeightSum = 0d;
            var squaredParentWeightSum = 0d;
            for (int i = 0; i < this.ParentNumber; i++)
            {
                parentWeightSum += unnormalizedWeights[i];
                squaredParentWeightSum += Math.Pow(unnormalizedWeights[i], 2);
            }

            return Math.Pow(parentWeightSum, 2) / squaredParentWeightSum;
        }

        /// <summary>
        /// Initializes strategy parameters for step size control, i.e. <see cref="StepSizeControlLearningRate"/>
        /// and <see cref="StepSizeControlDamping"/>.
        /// </summary>
        private void InitializeStepSizeControlParameters()
        {
            this.StepSizeControlLearningRate =
                (this.VarianceEffectiveSelectionMass + 2) / (this.SearchSpaceDimension + this.VarianceEffectiveSelectionMass + 5);
            this.StepSizeControlDamping =
                1
                + (2 * Math.Max(0, Math.Sqrt((this.VarianceEffectiveSelectionMass - 1) / (this.SearchSpaceDimension + 1)) - 1))
                + this.StepSizeControlLearningRate;
        }

        /// <summary>
        /// Initializes covariance matrix adaptation parameters, i.e. 
        /// <see cref="CumulationLearningRate"/>, <see cref="RankOneUpdateLearningRate"/> and
        /// <see cref="RankMuUpdateLearningRate"/>.
        /// </summary>
        private void InitializeCovarianceMatrixAdaptationParameters()
        {
            this.CumulationLearningRate = (4 + (this.VarianceEffectiveSelectionMass / this.SearchSpaceDimension))
                                          / (this.SearchSpaceDimension + 4 + (2 * (this.VarianceEffectiveSelectionMass / this.SearchSpaceDimension)));

            var alpha = 2;
            this.RankOneUpdateLearningRate = alpha / (Math.Pow(this.SearchSpaceDimension + 1.3, 2) + this.VarianceEffectiveSelectionMass);
            var unboundRankMuUpdateLearningRate = alpha * ((this.VarianceEffectiveSelectionMass - 2 + (1 / this.VarianceEffectiveSelectionMass))
                                                           / (Math.Pow(this.SearchSpaceDimension + 2, 2)
                                                              + ((alpha * this.VarianceEffectiveSelectionMass) / 2)));
            this.RankMuUpdateLearningRate = Math.Min(1 - this.RankOneUpdateLearningRate, unboundRankMuUpdateLearningRate);
        }

        /// <summary>
        /// Initializes <see cref="Weights"/>.
        /// </summary>
        /// <param name="unnormalizedWeights">The unscaled weights.</param>
        private void InitializeWeights(List<double> unnormalizedWeights)
        {
            // Positive weights sum to one.
            var positiveWeightScalingConstant = 1 / unnormalizedWeights.Where(w => w > 0).Sum();
            var negativeWeightScalingConstant = this.ComputeNegativeWeightScalingConstant(unnormalizedWeights);

            var weights = new List<double>(this.PopulationSize);
            foreach (var unnormalizedWeight in unnormalizedWeights)
            {
                if (unnormalizedWeight >= 0)
                {
                    weights.Add(positiveWeightScalingConstant * unnormalizedWeight);
                }
                else
                {
                    weights.Add(negativeWeightScalingConstant * unnormalizedWeight);
                }
            }

            this.Weights = weights.ToImmutableList();
        }

        /// <summary>
        /// Computes the constant to scale negative weights with.
        /// </summary>
        /// <param name="unnormalizedWeights">The unscaled weights.</param>
        /// <returns>The computed constant.</returns>
        private double ComputeNegativeWeightScalingConstant(List<double> unnormalizedWeights)
        {
            // Compute something like the variance effective selection mass for the weights not incorporated into
            // that one.
            var notSelectedWeightSum = 0d;
            var squaredNotSelectedWeightSum = 0d;
            for (int i = this.ParentNumber; i < this.PopulationSize; i++)
            {
                notSelectedWeightSum += unnormalizedWeights[i];
                squaredNotSelectedWeightSum += Math.Pow(unnormalizedWeights[i], 2);
            }

            var varianceNotSelectedMass = Math.Pow(notSelectedWeightSum, 2) / squaredNotSelectedWeightSum;

            // Goal of constant: No decay on C.
            var preventDecayAlpha = 1 + (this.RankOneUpdateLearningRate / this.RankMuUpdateLearningRate);

            // Goal of constant: Adapt negative weights to positive weights.
            var adaptWeightsAlpha = 1 + ((2 * varianceNotSelectedMass) / (this.VarianceEffectiveSelectionMass + 2));

            // Bound to guarantee positive definiteness of C. Might reintroduce decay.
            var alphaBound = (1 - this.RankOneUpdateLearningRate - this.RankMuUpdateLearningRate)
                             / (this.SearchSpaceDimension * this.RankMuUpdateLearningRate);

            // Use smallest of the constants + normalization as final scaling constant.
            var smallestAlpha = Math.Min(Math.Min(preventDecayAlpha, adaptWeightsAlpha), alphaBound);
            return smallestAlpha / (-unnormalizedWeights.Where(w => w < 0).Sum());
        }

        #endregion
    }
}