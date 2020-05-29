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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.CovarianceMatrixAdaptation
{
    using System;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.Tests.Configuration;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="CovarianceMatrixAdaptationStrategyConfiguration"/> and
    /// <see cref="CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder"/>.
    /// </summary>
    public class CovarianceMatrixAdaptationStrategyConfigurationTest : ConfigurationBaseTest
    {
        #region Fields

        /// <summary>
        /// Builder used for tests.
        /// </summary>
        private readonly CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder _builder;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CovarianceMatrixAdaptationStrategyConfigurationTest"/> class.
        /// </summary>
        public CovarianceMatrixAdaptationStrategyConfigurationTest()
        {
            this._builder =
                new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder();
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
                .SetMaximumNumberGenerations(12)
                .SetMinimumDomainSize(23)
                .SetFixInstances(true)
                .SetInitialStepSize(0.34)
                .SetReplacementRate(0.02)
                .SetFocusOnIncumbent(true)
                .BuildWithFallback(null);

            Assert.Equal(
                12,
                config.MaximumNumberGenerations);
            Assert.Equal(0.34, config.InitialStepSize);
            Assert.Equal(
                23,
                config.MinimumDomainSize);
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
            var config = this._builder.BuildWithFallback(null);

            Assert.Equal(
                CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder
                    .DefaultMaximumNumberGenerations,
                config.MaximumNumberGenerations);
            Assert.Equal(
                CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder
                    .DefaultMinimumDomainSize,
                config.MinimumDomainSize);
            Assert.Equal(
                CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder
                    .DefaultInitialStepSize,
                config.InitialStepSize);
            Assert.Equal(
                CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder
                    .DefaultReplacementRate,
                config.ReplacementRate);
            Assert.False(config.FixInstances, "Fix instances flag should be false by default.");
            Assert.Equal(
                CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder
                    .DefaultFocusOnIncumbent,
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
            var fallback = this._builder
                .SetMaximumNumberGenerations(12)
                .SetMinimumDomainSize(23)
                .SetFixInstances(true)
                .SetInitialStepSize(0.34)
                .SetReplacementRate(0.96)
                .SetFocusOnIncumbent(true)
                .BuildWithFallback(null);

            // Create a new builder based on it and let it build a configuration.
            var config = new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                .BuildWithFallback(fallback);

            // Check all values.
            Assert.Equal(
                fallback.MaximumNumberGenerations,
                config.MaximumNumberGenerations);
            Assert.Equal(
                fallback.MinimumDomainSize,
                config.MinimumDomainSize);
            Assert.Equal(
                fallback.ReplacementRate,
                config.ReplacementRate);
            Assert.Equal(
                fallback.InitialStepSize,
                config.InitialStepSize);
            Assert.True(config.FixInstances, "Fix instances flag was not copied over.");
            Assert.Equal(
                fallback.FocusOnIncumbent,
                config.FocusOnIncumbent);
        }

        /// <summary>
        /// Checks that <see cref="CovarianceMatrixAdaptationStrategyConfiguration.IsCompatible"/> returns false if
        /// the focus on incumbent option is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentFocusOnIncumbentOption()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                .SetFocusOnIncumbent(!defaultConfig.FocusOnIncumbent)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="StrategyConfigurationBase{TConfiguration}.IsCompatible"/> returns false if the 
        /// maximum number of generations is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMaximumNumberGenerations()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                .SetMaximumNumberGenerations(defaultConfig.MaximumNumberGenerations - 1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="StrategyConfigurationBase{TConfiguration}.IsCompatible"/> returns false if the 
        /// replacement rate is different and the incumbent focus option is turned on.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForIncumbentFocusWithDifferentReplacementRate()
        {
            var focusedConfig = this._builder.SetFocusOnIncumbent(true).BuildWithFallback(null);
            var otherConfig = new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                .SetReplacementRate(focusedConfig.ReplacementRate + 0.1)
                .BuildWithFallback(focusedConfig);
            ConfigurationBaseTest.CheckIncompatibility(focusedConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="StrategyConfigurationBase{TConfiguration}.IsCompatible"/> returns false if the 
        /// fix instances flag is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentFixInstancesFlags()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                .SetFixInstances(!defaultConfig.FixInstances)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="StrategyConfigurationBase{TConfiguration}.IsCompatible"/> returns false if the
        /// minimum domain size is different and the incumbent focus option is turned on.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForIncumbentFocusWithForDifferentMinimumDomainSize()
        {
            var focusedConfig = this._builder.SetFocusOnIncumbent(true).BuildWithFallback(null);
            var otherConfig = new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                .SetMinimumDomainSize(focusedConfig.MinimumDomainSize + 1)
                .BuildWithFallback(focusedConfig);
            ConfigurationBaseTest.CheckIncompatibility(focusedConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="StrategyConfigurationBase{TConfiguration}.IsCompatible"/> returns false if the 
        /// initial step size is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentInitialStepSize()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                .SetInitialStepSize(defaultConfig.InitialStepSize + 0.02)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="StrategyConfigurationBase{TConfiguration}.IsCompatible"/> returns true if the parameters
        /// do not change.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsTrueForSameParameters()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                .BuildWithFallback(defaultConfig);

            Assert.True(
                defaultConfig.IsCompatible(otherConfig),
                "Configurations should be compatible.");
            Assert.Equal(
                defaultConfig.IsCompatible(otherConfig),
                otherConfig.IsCompatible(defaultConfig));
        }

        /// <summary>
        /// Checks that <see cref="StrategyConfigurationBase{TConfiguration}.IsCompatible"/> returns true if only
        /// inactive conditional parameters are changing.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsTrueForIrrelevantParameterChanges()
        {
            var globalConfig = this._builder.SetFocusOnIncumbent(false).BuildWithFallback(null);
            var otherConfig = new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                .SetReplacementRate(globalConfig.ReplacementRate + 0.01)
                .SetMinimumDomainSize(globalConfig.MinimumDomainSize + 1)
                .BuildWithFallback(globalConfig);

            Assert.True(
                globalConfig.IsCompatible(otherConfig),
                "Configurations should be compatible.");
            Assert.Equal(
                globalConfig.IsCompatible(otherConfig),
                otherConfig.IsCompatible(globalConfig));
        }

        /// <summary>
        /// Checks that <see cref="StrategyConfigurationBase{TConfiguration}.IsTechnicallyCompatible"/>
        /// returns false if the focus on incumbent option is different.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForDifferentIncumbentFocus()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                .SetFocusOnIncumbent(!defaultConfig.FocusOnIncumbent)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckTechnicalIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="StrategyConfigurationBase{TConfiguration}.IsTechnicallyCompatible"/>
        /// returns false if the focus on incumbent option is turned on and the minimum domain size changes.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForIncumbentFocusWithDifferentMinimumDomainSize()
        {
            var focusedConfig = this._builder.SetFocusOnIncumbent(true).BuildWithFallback(null);
            var otherConfig = new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                .SetMinimumDomainSize(focusedConfig.MinimumDomainSize + 1)
                .BuildWithFallback(focusedConfig);
            ConfigurationBaseTest.CheckTechnicalIncompatibility(focusedConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="StrategyConfigurationBase{TConfiguration}.IsTechnicallyCompatible"/> returns true for any
        /// <see cref="CovarianceMatrixAdaptationStrategyConfiguration"/> which does not change the incumbent focus or
        /// minimum domain size.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsTrueForOtherChanges()
        {
            var defaultConfig = this._builder.SetFocusOnIncumbent(true).BuildWithFallback(null);
            var otherConfig = new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                .SetMaximumNumberGenerations(defaultConfig.MaximumNumberGenerations - 1)
                .SetInitialStepSize(defaultConfig.InitialStepSize + 0.01)
                .SetFixInstances(!defaultConfig.FixInstances)
                .SetReplacementRate(defaultConfig.ReplacementRate + 0.1)
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
        /// Checks that <see cref="StrategyConfigurationBase{TConfiguration}.IsTechnicallyCompatible"/> returns true for any
        /// <see cref="CovarianceMatrixAdaptationStrategyConfiguration"/> if the incumbent focus option is turned off.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsTrueForTurnedOffIncumentFocus()
        {
            var defaultConfig = this._builder.SetFocusOnIncumbent(false).BuildWithFallback(null);

            var otherConfig = new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                .SetMaximumNumberGenerations(defaultConfig.MaximumNumberGenerations - 1)
                .SetInitialStepSize(defaultConfig.InitialStepSize + 0.01)
                .SetFixInstances(!defaultConfig.FixInstances)
                .SetReplacementRate(defaultConfig.ReplacementRate + 0.1)
                .SetMinimumDomainSize(defaultConfig.MinimumDomainSize + 1)
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
        /// Verifies that setting the initial step size to 0 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ZeroInitialStepSizeThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetInitialStepSize(0));
        }

        /// <summary>
        /// Verifies that setting the replacement rate to zero results in a
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ZeroReplacementRateThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetReplacementRate(0));
        }

        /// <summary>
        /// Verifies that setting the replacement rate to a value above 1 results in a
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ReplacementRateAboveOneThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetReplacementRate(1.01));
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
            return new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder();
        }

        #endregion
    }
}