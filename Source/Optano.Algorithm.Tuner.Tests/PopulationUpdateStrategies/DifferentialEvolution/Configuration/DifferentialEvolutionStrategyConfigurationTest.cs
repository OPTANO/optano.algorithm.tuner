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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.DifferentialEvolution.Configuration
{
    using System;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration;
    using Optano.Algorithm.Tuner.Tests.Configuration;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="DifferentialEvolutionStrategyConfiguration"/> and
    /// <see cref="DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder"/>.
    /// </summary>
    public class DifferentialEvolutionStrategyConfigurationTest : ConfigurationBaseTest
    {
        #region Fields

        /// <summary>
        /// A default <see cref="DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder"/>.
        /// </summary>
        private readonly DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder
            _defaultDifferentialEvolutionConfigurationBuilder
                = new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder();

        /// <summary>
        /// Builder used for tests.
        /// </summary>
        private readonly DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder _builder;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentialEvolutionStrategyConfigurationTest"/> class.
        /// </summary>
        public DifferentialEvolutionStrategyConfigurationTest()
        {
            this._builder =
                new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that all values get transferred from builder to configuration.
        /// </summary>
        [Fact]
        public override void AllValuesGetTransferred()
        {
            var detailedBuilder =
                this._defaultDifferentialEvolutionConfigurationBuilder
                    .SetInitialMeanMutationFactor(0.01);
            var config = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(detailedBuilder)
                .SetMinimumDomainSize(23)
                .SetMaximumNumberGenerations(12)
                .SetReplacementRate(0.02)
                .SetFixInstances(true)
                .SetFocusOnIncumbent(true)
                .BuildWithFallback(null);

            Assert.Equal(
                0.01,
                config.DifferentialEvolutionConfiguration.InitialMeanMutationFactor);
            Assert.Equal(
                23,
                config.MinimumDomainSize);
            Assert.Equal(
                12,
                config.MaximumNumberGenerations);
            Assert.Equal(
                0.02,
                config.ReplacementRate);
            Assert.True(config.FixInstances, "Fix instances flag was not transferred from builder.");
            Assert.True(config.FocusOnIncumbent, "Focus on incumbent option was not transferred from builder.");
        }

        /// <summary>
        /// Checks that the configuration has correct default values.
        /// </summary>
        [Fact]
        public override void DefaultsAreSetCorrectly()
        {
            var config = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .BuildWithFallback(null);

            Assert.Equal(
                DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder.DefaultMaximumNumberGenerations,
                config.MaximumNumberGenerations);
            Assert.Equal(
                DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder.DefaultMinimumDomainSize,
                config.MinimumDomainSize);
            Assert.Equal(
                DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder
                    .DefaultReplacementRate,
                config.ReplacementRate);
            Assert.False(config.FixInstances, "Fix instances flag should be false by default.");
            Assert.Equal(
                DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder.DefaultFocusOnIncumbent,
                config.FocusOnIncumbent);
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
            var detailedBuilder =
                this._defaultDifferentialEvolutionConfigurationBuilder.SetInitialMeanMutationFactor(0.01);
            var fallback = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(detailedBuilder)
                .SetMinimumDomainSize(23)
                .SetMaximumNumberGenerations(12)
                .SetReplacementRate(0.49)
                .SetFixInstances(true)
                .SetFocusOnIncumbent(true)
                .BuildWithFallback(null);

            // Create a new builder based on it and let it build a configuration.
            var config = new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                .BuildWithFallback(fallback);

            // Check all values.
            Assert.Equal(
                fallback.DifferentialEvolutionConfiguration.InitialMeanMutationFactor,
                config.DifferentialEvolutionConfiguration.InitialMeanMutationFactor);
            Assert.Equal(
                fallback.MinimumDomainSize,
                config.MinimumDomainSize);
            Assert.Equal(
                fallback.MaximumNumberGenerations,
                config.MaximumNumberGenerations);
            Assert.Equal(
                fallback.ReplacementRate,
                config.ReplacementRate);
            Assert.True(config.FixInstances, "Fix instances flag was not copied over.");
            Assert.Equal(
                fallback.FocusOnIncumbent,
                config.FocusOnIncumbent);
        }

        /// <summary>
        /// Checks that 
        /// <see cref="DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder.BuildWithFallback"/>
        /// also uses fallbacks for <see cref="DifferentialEvolutionConfiguration"/>.
        /// </summary>
        [Fact]
        public void BuildWithFallbackUsesFallbacksForDetailedConfiguration()
        {
            var differentialEvolutionFallback =
                new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder()
                    .SetInitialMeanMutationFactor(0.87);
            var fallback =
                new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                    .SetDifferentialEvolutionConfigurationBuilder(differentialEvolutionFallback)
                    .BuildWithFallback(null);

            var differentialEvolution = this._defaultDifferentialEvolutionConfigurationBuilder.SetBestPercentage(0.12);
            var config = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(differentialEvolution)
                .BuildWithFallback(fallback);

            Assert.Equal(
                0.87,
                config.DifferentialEvolutionConfiguration.InitialMeanMutationFactor);
            Assert.Equal(
                0.12,
                config.DifferentialEvolutionConfiguration.BestPercentage);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStrategyConfiguration.IsCompatible"/> returns false if the
        /// minimum domain size is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMinimumDomainSize()
        {
            var defaultConfig = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .BuildWithFallback(null);
            var otherConfig = new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .SetMinimumDomainSize(defaultConfig.MinimumDomainSize + 1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStrategyConfiguration.IsCompatible"/> returns false if the 
        /// maximum number of generations is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMaximumNumberGenerations()
        {
            var defaultConfig = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .BuildWithFallback(null);
            var otherConfig = new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .SetMaximumNumberGenerations(defaultConfig.MaximumNumberGenerations - 1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStrategyConfiguration.IsCompatible"/> returns false if the 
        /// focus on incumbent flag is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentFocusOnIncumbentFlags()
        {
            var defaultConfig = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .BuildWithFallback(null);
            var otherConfig = new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .SetFocusOnIncumbent(!defaultConfig.FocusOnIncumbent)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStrategyConfiguration.IsCompatible"/> returns false if the 
        /// replacement rate is different and incumbent focus is turned on.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForIncumbentFocusWithDifferentReplacementRate()
        {
            var localConfig = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .SetFocusOnIncumbent(true)
                .BuildWithFallback(null);
            var otherConfig = new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .SetReplacementRate(localConfig.ReplacementRate + 0.1)
                .BuildWithFallback(localConfig);
            ConfigurationBaseTest.CheckIncompatibility(localConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStrategyConfiguration.IsCompatible"/> returns false if the 
        /// fix instances flag is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentFixInstancesFlags()
        {
            var defaultConfig = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .BuildWithFallback(null);
            var otherConfig = new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .SetFixInstances(!defaultConfig.FixInstances)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStrategyConfiguration.IsCompatible"/> returns false if the 
        /// differential evolution configuration builder is incompatible.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForIncompatibleDifferentialEvolutionConfigurationBuilders()
        {
            var defaultConfig = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .BuildWithFallback(null);

            var incompatibleDifferentialEvolutionConfigurationBuilder = this._defaultDifferentialEvolutionConfigurationBuilder
                .SetBestPercentage(this._defaultDifferentialEvolutionConfigurationBuilder.BuildWithFallback(null).BestPercentage + 0.01);
            var otherConfig = new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                .SetDifferentialEvolutionConfigurationBuilder(incompatibleDifferentialEvolutionConfigurationBuilder)
                .BuildWithFallback(defaultConfig);

            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStrategyConfiguration.IsCompatible"/> returns true if the parameters
        /// only change in initial values.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsTrueForChangesInInitialParameters()
        {
            var defaultConfig = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .BuildWithFallback(null);

            var differentialEvolutionConfiguration = this._defaultDifferentialEvolutionConfigurationBuilder.BuildWithFallback(null);
            var compatibleDifferentialEvolutionConfigurationBuilder = this._defaultDifferentialEvolutionConfigurationBuilder
                .SetInitialMeanMutationFactor(differentialEvolutionConfiguration.InitialMeanMutationFactor + 0.01)
                .SetInitialMeanCrossoverRate(differentialEvolutionConfiguration.InitialMeanCrossoverRate + 0.02);
            var otherConfig = new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                .SetDifferentialEvolutionConfigurationBuilder(compatibleDifferentialEvolutionConfigurationBuilder)
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
        /// Checks that <see cref="DifferentialEvolutionStrategyConfiguration.IsCompatible"/> returns true if only
        /// inactive conditional parameters are changing.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsTrueForIrrelevantParameterChanges()
        {
            var globalConfig = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .SetFocusOnIncumbent(false)
                .BuildWithFallback(null);
            var otherConfig = new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                .SetReplacementRate(globalConfig.ReplacementRate + 0.1)
                .BuildWithFallback(globalConfig);

            Assert.True(
                globalConfig.IsCompatible(otherConfig),
                "Configurations should be compatible.");
            Assert.Equal(
                globalConfig.IsCompatible(otherConfig),
                otherConfig.IsCompatible(globalConfig));
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStrategyConfiguration.IsTechnicallyCompatible"/> returns false if the
        /// minimum domain size is different.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForDifferentMinimumDomainSize()
        {
            var defaultConfig = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .BuildWithFallback(null);
            var otherConfig = new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .SetMinimumDomainSize(defaultConfig.MinimumDomainSize + 1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckTechnicalIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStrategyConfiguration.IsTechnicallyCompatible"/> returns true for any
        /// <see cref="DifferentialEvolutionStrategyConfiguration"/> with same minimum domain size.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsTrueForSameMinimumDomainSize()
        {
            var defaultConfig = this._builder
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .BuildWithFallback(null);

            var differentialEvolutionConfiguration = this._defaultDifferentialEvolutionConfigurationBuilder.BuildWithFallback(null);
            var incompatibleDifferentialEvolutionConfigurationBuilder = this._defaultDifferentialEvolutionConfigurationBuilder
                .SetInitialMeanMutationFactor(differentialEvolutionConfiguration.InitialMeanMutationFactor + 0.01)
                .SetInitialMeanCrossoverRate(differentialEvolutionConfiguration.InitialMeanCrossoverRate + 0.02)
                .SetBestPercentage(differentialEvolutionConfiguration.BestPercentage + 0.03)
                .SetLearningRate(differentialEvolutionConfiguration.LearningRate + 0.04);

            var otherConfig = new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                .SetDifferentialEvolutionConfigurationBuilder(incompatibleDifferentialEvolutionConfigurationBuilder)
                .SetMaximumNumberGenerations(defaultConfig.MaximumNumberGenerations - 1)
                .SetReplacementRate(defaultConfig.ReplacementRate + 0.1)
                .SetFixInstances(!defaultConfig.FixInstances)
                .SetFocusOnIncumbent(!defaultConfig.FocusOnIncumbent)
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
        /// Verifies that setting the maximum number of generations to 0 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ZeroMaximumNumberGenerationsThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetMaximumNumberGenerations(0));
        }

        /// <summary>
        /// Verifies that setting the minimum domain size to 0 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ZeroMinimumDomainSizeThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetMinimumDomainSize(0));
        }

        /// <summary>
        /// Verifies that setting the replacement rate to a negative value results in a
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void NegativeReplacementRateThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetReplacementRate(-0.01));
        }

        /// <summary>
        /// Verifies that setting the replacement rate to a value above 0.5 results in a
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ReplacementRateAboveOneHalfThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetReplacementRate(0.51));
        }

        /// <summary>
        /// Verifies that calling
        /// <see cref="DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder.BuildWithFallback"/>
        /// without fallback and without
        /// <see cref="DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder"/>
        /// set throws a <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void MissingDifferentialEvolutionConfigurationBuilderThrowsError()
        {
            Assert.Throws<InvalidOperationException>(() => this._builder.BuildWithFallback(null));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a valid configuration object.
        /// </summary>
        /// <returns>The created object.</returns>
        protected override ConfigurationBase CreateTestConfiguration()
        {
            return this._builder
                .SetDifferentialEvolutionConfigurationBuilder(this._defaultDifferentialEvolutionConfigurationBuilder)
                .BuildWithFallback(null);
        }

        /// <summary>
        /// Creates a valid configuration builder object.
        /// </summary>
        /// <returns>The created object.</returns>
        protected override IConfigBuilder<ConfigurationBase> CreateTestConfigurationBuilder()
        {
            return new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder();
        }

        #endregion
    }
}