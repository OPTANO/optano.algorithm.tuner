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

namespace Optano.Algorithm.Tuner.Tests.ContinuousOptimization.CovarianceMatrixAdaptation
{
    using System;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="CmaEsConfiguration"/> class.
    /// </summary>
    public class CmaEsConfigurationTest
    {
        #region Fields

        /// <summary>
        /// The search space dimension used in tests.
        /// </summary>
        private readonly int _searchSpaceDimension = 3;

        /// <summary>
        /// The initial step size used in tests.
        /// </summary>
        private readonly double _initialStepSize = 0.1;

        /// <summary>
        /// The population size used in tests.
        /// </summary>
        private readonly int _populationSize;

        /// <summary>
        /// Initial distribution mean used in tests.
        /// </summary>
        private readonly Vector<double> _initialDistributionMean;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CmaEsConfigurationTest"/> class.
        /// </summary>
        public CmaEsConfigurationTest()
        {
            this._populationSize = 4 + (int)Math.Floor(3 * Math.Log(this._searchSpaceDimension));
            this._initialDistributionMean = Vector<double>.Build.Random(this._searchSpaceDimension);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="CmaEsConfiguration"/>'s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a population size of 1.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForSinglePopulationMember()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CmaEsConfiguration(
                    populationSize: 1,
                    initialDistributionMean: this._initialDistributionMean,
                    initialStepSize: this._initialStepSize));
        }

        /// <summary>
        /// Checks that <see cref="CmaEsConfiguration"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without an initial distribution mean.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingDistributionMean()
        {
            Assert.Throws<ArgumentNullException>(
                () => new CmaEsConfiguration(
                    this._populationSize,
                    initialDistributionMean: null,
                    initialStepSize: this._initialStepSize));
        }

        /// <summary>
        /// Checks that <see cref="CmaEsConfiguration"/>'s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a step size of 0.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForStepSizeZero()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CmaEsConfiguration(
                    this._populationSize,
                    this._initialDistributionMean,
                    initialStepSize: 0));
        }

        /// <summary>
        /// Checks that all properties get initialized correctly.
        /// </summary>
        /// <remarks>Expected values were computed independently from this program.</remarks>
        [Fact]
        public void PropertiesGetInitializedCorrectly()
        {
            var config = new CmaEsConfiguration(
                this._populationSize,
                this._initialDistributionMean,
                this._initialStepSize);

            Assert.Equal(this._searchSpaceDimension, config.SearchSpaceDimension);
            Assert.Equal(this._populationSize, config.PopulationSize);
            Assert.True(3 == config.ParentNumber, "Parent number is not as expected (half of population).");
            Assert.Equal(
                this._initialDistributionMean,
                config.InitialDistributionMean);
            Assert.Equal(this._initialStepSize, config.InitialStepSize);

            var doubleComparisonExactness = 1e-04;
            Assert.Equal(
                this._populationSize,
                config.Weights.Count);
            TestUtils.Equals(config.Weights[0], 0.5856, doubleComparisonExactness);
            TestUtils.Equals(config.Weights[1], 0.2928, doubleComparisonExactness);
            TestUtils.Equals(config.Weights[2], 0.1215, doubleComparisonExactness);
            TestUtils.Equals(config.Weights[3], 0, doubleComparisonExactness);
            TestUtils.Equals(config.Weights[4], -0.4241, doubleComparisonExactness);
            TestUtils.Equals(config.Weights[5], -0.7706, doubleComparisonExactness);
            TestUtils.Equals(
                config.Weights[6],
                -1.0636,
                doubleComparisonExactness);

            TestUtils.Equals(
                config.VarianceEffectiveSelectionMass,
                2.2548,
                doubleComparisonExactness);

            TestUtils.Equals(
                config.CumulationLearningRate,
                0.5588,
                doubleComparisonExactness);
            TestUtils.Equals(
                config.RankOneUpdateLearningRate,
                0.0964,
                doubleComparisonExactness);
            TestUtils.Equals(
                config.RankMuUpdateLearningRate,
                0.0512,
                doubleComparisonExactness);

            TestUtils.Equals(
                config.StepSizeControlLearningRate,
                0.4149,
                doubleComparisonExactness);
            TestUtils.Equals(
                config.StepSizeControlDamping,
                1.4149,
                doubleComparisonExactness);
        }

        /// <summary>
        /// Checks that <see cref="CmaEsConfiguration.ComputeExpectedConjugateEvolutionPathLength"/> returns the
        /// expected norm of a standard normally distributed vector.
        /// </summary>
        /// <remarks>Expected value was computed independently from this program.</remarks>
        [Fact]
        public void ExpectedConjugateEvolutionPathLengthIsComputedCorrectly()
        {
            var config = new CmaEsConfiguration(
                this._populationSize,
                this._initialDistributionMean,
                this._initialStepSize);
            TestUtils.Equals(
                config.ComputeExpectedConjugateEvolutionPathLength(),
                1.5957,
                1e-4);
        }

        /// <summary>
        /// Checks that neither changes to object returned by <see cref="CmaEsConfiguration.InitialDistributionMean"/>
        /// nor to data the object was initalized with change subsequent return values.
        /// </summary>
        [Fact]
        public void DistributionMeanIsImmutable()
        {
            var config = new CmaEsConfiguration(
                this._populationSize,
                this._initialDistributionMean,
                this._initialStepSize);

            var returnedMean = config.InitialDistributionMean;
            returnedMean.Clear();
            Assert.NotEqual(returnedMean, this._initialDistributionMean);
            Assert.Equal(
                this._initialDistributionMean,
                config.InitialDistributionMean);

            this._initialDistributionMean.Clear();
            Assert.NotEqual(
                this._initialDistributionMean,
                config.InitialDistributionMean);
        }

        #endregion
    }
}