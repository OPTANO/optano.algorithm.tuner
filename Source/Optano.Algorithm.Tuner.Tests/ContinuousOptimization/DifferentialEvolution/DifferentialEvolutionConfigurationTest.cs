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

namespace Optano.Algorithm.Tuner.Tests.ContinuousOptimization.DifferentialEvolution
{
    using System;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;
    using Optano.Algorithm.Tuner.Tests.Configuration;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="DifferentialEvolutionConfiguration"/> and
    /// <see cref="DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder"/>.
    /// </summary>
    public class DifferentialEvolutionConfigurationTest : ConfigurationBaseTest
    {
        #region Fields

        /// <summary>
        /// Builder used for tests.
        /// </summary>
        private readonly DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder _builder;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentialEvolutionConfigurationTest"/> class.
        /// </summary>
        public DifferentialEvolutionConfigurationTest()
        {
            this._builder =
                new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that all values get transferred from builder to configuration.
        /// </summary>
        [Fact]
        public override void AllValuesGetTransferred()
        {
            var config = this._builder
                .SetBestPercentage(0.11)
                .SetInitialMeanMutationFactor(0.69)
                .SetInitialMeanCrossoverRate(0.37)
                .SetLearningRate(0.81)
                .BuildWithFallback(null);

            Assert.Equal(0.11, config.BestPercentage);
            Assert.Equal(
                0.69,
                config.InitialMeanMutationFactor);
            Assert.Equal(
                0.37,
                config.InitialMeanCrossoverRate);
            Assert.Equal(0.81, config.LearningRate);
        }

        /// <summary>
        /// Checks that the configuration has correct default values.
        /// </summary>
        [Fact]
        public override void DefaultsAreSetCorrectly()
        {
            // Build the configuration without explicitly setting values.
            var config = this._builder.BuildWithFallback(null);

            // Check the values in the configuration. 
            Assert.Equal(0.1, config.BestPercentage);
            Assert.Equal(0.5, config.InitialMeanMutationFactor);
            Assert.Equal(0.5, config.InitialMeanCrossoverRate);
            Assert.Equal(0.1, config.LearningRate);
        }

        /// <summary>
        /// Checks that all values are copied if
        /// <see cref="IConfigBuilder{TConfiguration}.BuildWithFallback"/>
        /// is called on a builder without anything set.
        /// </summary>
        [Fact]
        public override void BuildWithFallbackUsesFallbacks()
        {
            // Set all values in builder and build a configuration.
            var fallback = this._builder
                .SetBestPercentage(0.11)
                .SetInitialMeanMutationFactor(0.69)
                .SetInitialMeanCrossoverRate(0.37)
                .SetLearningRate(0.81)
                .BuildWithFallback(null);

            // Create a new builder based on it and let it build a configuration.
            var config = new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder()
                .BuildWithFallback(fallback);

            // Check all values.
            Assert.Equal(fallback.BestPercentage, config.BestPercentage);
            Assert.Equal(
                fallback.InitialMeanMutationFactor,
                config.InitialMeanMutationFactor);
            Assert.Equal(
                fallback.InitialMeanCrossoverRate,
                config.InitialMeanCrossoverRate);
            Assert.Equal(fallback.LearningRate, config.LearningRate);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionConfiguration.IsCompatible"/> returns false if the best
        /// percentage is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentBestPercentage()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder()
                .SetBestPercentage(defaultConfig.BestPercentage + 0.01)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionConfiguration.IsCompatible"/> returns false if the learning
        /// rate is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentLearningRate()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder()
                .SetLearningRate(defaultConfig.LearningRate + 0.01)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionConfiguration.IsCompatible"/> returns true if the parameters
        /// only change in initial values.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsTrueForChangesInInitialParameters()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder()
                .SetInitialMeanMutationFactor(defaultConfig.InitialMeanMutationFactor + 0.01)
                .SetInitialMeanCrossoverRate(defaultConfig.InitialMeanCrossoverRate + 0.02)
                .BuildWithFallback(defaultConfig);

            // Check those two configuration are compatible.
            Assert.True(
                defaultConfig.IsCompatible(otherConfig),
                "Configurations should be compatible.");
            Assert.Equal(
                defaultConfig.IsCompatible(otherConfig),
                otherConfig.IsCompatible(defaultConfig));
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionConfiguration.IsTechnicallyCompatible"/> return true for any
        /// <see cref="DifferentialEvolutionConfiguration"/>.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsTrueForDifferentParameters()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder()
                .SetBestPercentage(defaultConfig.BestPercentage + 0.01)
                .SetInitialMeanMutationFactor(defaultConfig.InitialMeanMutationFactor + 0.02)
                .SetInitialMeanCrossoverRate(defaultConfig.InitialMeanCrossoverRate + 0.03)
                .SetLearningRate(defaultConfig.LearningRate + 0.04)
                .BuildWithFallback(defaultConfig);

            // Check those two configuration are compatible.
            Assert.True(
                defaultConfig.IsTechnicallyCompatible(otherConfig),
                "Configurations should be technically compatible.");
            Assert.Equal(
                defaultConfig.IsTechnicallyCompatible(otherConfig),
                otherConfig.IsTechnicallyCompatible(defaultConfig));
        }

        /// <summary>
        /// Verifies that setting the best percentage to 0 results in an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ZeroBestPercentageThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetBestPercentage(0));
        }

        /// <summary>
        /// Verifies that setting the best percentage to a value larger than 100% results in a
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void Above1BestPercentageThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetBestPercentage(1.1));
        }

        /// <summary>
        /// Verifies that setting the initial mean mutation factor to -0.1 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void NegativeInitialMeanMutationFactorThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetInitialMeanMutationFactor(-0.1));
        }

        /// <summary>
        /// Verifies that setting the initial mean mutation factor to a value greater than 1 results in a
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void InitialMeanMutationFactorGreaterThan1ThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetInitialMeanMutationFactor(1.1));
        }

        /// <summary>
        /// Verifies that setting the initial mean crossover rate to -0.1 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void NegativeInitialMeanCrossoverRateThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetInitialMeanCrossoverRate(-0.1));
        }

        /// <summary>
        /// Verifies that setting the initial mean crossover rate to a value greater than 1 results in a
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void InitialMeanCrossoverRateGreaterThan1ThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetInitialMeanCrossoverRate(1.1));
        }

        /// <summary>
        /// Verifies that setting the learning rate to -0.1 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void NegativeLearningRateThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetLearningRate(-0.1));
        }

        /// <summary>
        /// Verifies that setting the learning rate to a value greater than 1 results in a
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void LearningRateGreaterThan1ThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetLearningRate(1.1));
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStatus{TSearchPoint}"/>'s builder does not throw for a
        /// percentage of population members which may be used as best members of 1, an initial mean mutation factor of
        /// 1, an initial mean crossover rate of 0, and a learning rate of 0.
        /// </summary>
        [Fact]
        public void BuilderCanHandleEdgeValues()
        {
            var error = this._builder
                .SetBestPercentage(1)
                .SetInitialMeanMutationFactor(1)
                .SetInitialMeanCrossoverRate(0)
                .SetLearningRate(0)
                .BuildWithFallback(null);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a valid configuration object.
        /// </summary>
        /// <returns>The created object.</returns>
        protected override ConfigurationBase CreateTestConfiguration()
        {
            return this._builder.BuildWithFallback(null);
        }

        /// <summary>
        /// Creates a valid configuration builder object.
        /// </summary>
        /// <returns>The created object.</returns>
        protected override IConfigBuilder<ConfigurationBase> CreateTestConfigurationBuilder()
        {
            return new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder();
        }

        #endregion
    }
}