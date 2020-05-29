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

namespace Optano.Algorithm.Tuner.Tests.MachineLearning.RandomForest.Configuration
{
    using System;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.Tests.Configuration;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="GenomePredictionRandomForestConfig"/> and
    /// <see cref="GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder"/>.
    /// </summary>
    public class GenomePredictionRandomForestConfigTest : ConfigurationBaseTest
    {
        #region Fields

        /// <summary>
        /// Builder used for tests.
        /// </summary>
        private readonly GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder _builder;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomePredictionRandomForestConfigTest"/> class.
        /// </summary>
        public GenomePredictionRandomForestConfigTest()
        {
            this._builder =
                new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder();
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
                .SetMaximumTreeDepth(13)
                .SetFeaturesPerSplitRatio(0.37)
                .SetMinimumInformationGain(0.04)
                .SetMinimumSplitSize(56)
                .SetRunParallel(false)
                .SetSubSampleRatio(0.144)
                .SetTreeCount(333)
                .BuildWithFallback(null);

            Assert.Equal(13, config.MaximumTreeDepth);
            Assert.Equal(
                0.37,
                config.FeaturesPerSplitRatio);
            Assert.Equal(
                0.04,
                config.MinimumInformationGain);
            Assert.Equal(56, config.MinimumSplitSize);
            Assert.False(config.RunParallel);
            Assert.Equal(0.144, config.SubSampleRatio);
            Assert.Equal(333, config.TreeCount);
        }

        /// <summary>
        /// Checks that the configuration has correct default values.
        /// </summary>
        [Fact]
        public override void DefaultsAreSetCorrectly()
        {
            // Build the configuration without explicitely setting values.
            var config = this._builder.BuildWithFallback(null);

            // Check the values in the configuration. 
            Assert.Equal(GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.DefaultTreeCount, config.TreeCount);
            Assert.Equal(2, config.MinimumSplitSize);
            Assert.Equal(10, config.MaximumTreeDepth);
            Assert.Equal(0.3, config.FeaturesPerSplitRatio);
            Assert.Equal(
                1E-06,
                config.MinimumInformationGain);
            Assert.Equal(0.7, config.SubSampleRatio);
            Assert.True(config.RunParallel);
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
                .SetMaximumTreeDepth(13)
                .SetFeaturesPerSplitRatio(0.37)
                .SetMinimumInformationGain(0.04)
                .SetMinimumSplitSize(56)
                .SetRunParallel(false)
                .SetSubSampleRatio(0.144)
                .SetTreeCount(333)
                .BuildWithFallback(null);

            // Create a new builder based on it and let it build a configuration.
            var config = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .BuildWithFallback(fallback);

            // Check all values.
            Assert.Equal(
                fallback.MaximumTreeDepth,
                config.MaximumTreeDepth);
            Assert.Equal(
                fallback.FeaturesPerSplitRatio,
                config.FeaturesPerSplitRatio);
            Assert.Equal(
                fallback.MinimumInformationGain,
                config.MinimumInformationGain);
            Assert.Equal(fallback.RunParallel, config.RunParallel);
            Assert.Equal(fallback.SubSampleRatio, config.SubSampleRatio);
            Assert.Equal(fallback.TreeCount, config.TreeCount);
            Assert.Equal(
                fallback.MinimumSplitSize,
                config.MinimumSplitSize);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsCompatible"/> returns false if the maximum
        /// tree depth is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMaximumTreeDepth()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetMaximumTreeDepth(defaultConfig.MaximumTreeDepth + 1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsCompatible"/> returns false if the features per
        /// split ratio is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentFeaturesPerSplitRatio()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetFeaturesPerSplitRatio(defaultConfig.FeaturesPerSplitRatio + 0.1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsCompatible"/> returns false if the minimum
        /// information gain is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMinimumInformationGain()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetMinimumInformationGain(defaultConfig.MinimumInformationGain + 0.1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsCompatible"/> returns false if the minimum
        /// split size is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMinimumSplitSize()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetMinimumSplitSize(defaultConfig.MinimumSplitSize + 1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsCompatible"/> returns false if the run
        /// parallel flag is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentRunParallelFlag()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetRunParallel(!defaultConfig.RunParallel)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsCompatible"/> returns false if the sub
        /// sample ratio is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentSubSampleRatio()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetSubSampleRatio(defaultConfig.SubSampleRatio + 0.1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsCompatible"/> returns false if the tree
        /// count is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentTreeCount()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetTreeCount(defaultConfig.TreeCount + 1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsCompatible"/> returns true if the parameters
        /// are the same.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsTrueForSameParameters()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
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
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsTechnicallyCompatible"/> returns false if the maximum
        /// tree depth is different.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForDifferentMaximumTreeDepth()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetMaximumTreeDepth(defaultConfig.MaximumTreeDepth + 1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckTechnicalIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsTechnicallyCompatible"/> returns false if the features per
        /// split ratio is different.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForDifferentFeaturesPerSplitRatio()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetFeaturesPerSplitRatio(defaultConfig.FeaturesPerSplitRatio + 0.1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckTechnicalIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsTechnicallyCompatible"/> returns false if the minimum
        /// information gain is different.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForDifferentMinimumInformationGain()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetMinimumInformationGain(defaultConfig.MinimumInformationGain + 0.1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckTechnicalIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsTechnicallyCompatible"/> returns false if the minimum
        /// split size is different.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForDifferentMinimumSplitSize()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetMinimumSplitSize(defaultConfig.MinimumSplitSize + 1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckTechnicalIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsTechnicallyCompatible"/> returns false if the run
        /// parallel flag is different.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForDifferentRunParallelFlag()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetRunParallel(!defaultConfig.RunParallel)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckTechnicalIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsTechnicallyCompatible"/> returns false if the sub
        /// sample ratio is different.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForDifferentSubSampleRatio()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetSubSampleRatio(defaultConfig.SubSampleRatio + 0.1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckTechnicalIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsTechnicallyCompatible"/> returns false if the tree
        /// count is different.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForDifferentTreeCount()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                .SetTreeCount(defaultConfig.TreeCount + 1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckTechnicalIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForestConfig.IsTechnicallyCompatible"/> returns true if the parameters
        /// are the same.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsTrueForSameParameters()
        {
            var defaultConfig = this._builder.BuildWithFallback(null);
            var otherConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
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
        /// Checks that
        /// <see cref="GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.BuildWithFallback"/>
        /// throws a <see cref="ArgumentOutOfRangeException"/> if called on a builder with a maximum tree depth of 0.
        /// </summary>
        [Fact]
        public void MaximumTreeDepthOfZeroThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetMaximumTreeDepth(0).BuildWithFallback(null));
        }

        /// <summary>
        /// Checks that
        /// <see cref="GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.BuildWithFallback"/>
        /// throws a <see cref="ArgumentOutOfRangeException"/> if called on a builder with features per split ratio set
        /// to 0.
        /// </summary>
        [Fact]
        public void ZeroFeaturesPerSplitRatioThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetFeaturesPerSplitRatio(0).BuildWithFallback(null));
        }

        /// <summary>
        /// Checks that
        /// <see cref="GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.BuildWithFallback"/>
        /// throws a <see cref="ArgumentOutOfRangeException"/> if called on a builder with a features per split ratio
        /// above 1.
        /// </summary>
        [Fact]
        public void Above1FeaturesPerSplitRatioThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetFeaturesPerSplitRatio(1.1).BuildWithFallback(null));
        }

        /// <summary>
        /// Checks that
        /// <see cref="GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.BuildWithFallback"/>
        /// throws a <see cref="ArgumentOutOfRangeException"/> if called on a builder with a sub sample ratio of 0.
        /// </summary>
        [Fact]
        public void ZeroSubSampleRatioThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetSubSampleRatio(0).BuildWithFallback(null));
        }

        /// <summary>
        /// Checks that
        /// <see cref="GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.BuildWithFallback"/>
        /// throws a <see cref="ArgumentOutOfRangeException"/> if called on a builder with a sub sample ratio
        /// above 1.
        /// </summary>
        [Fact]
        public void Above1SubSampleRatioThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetSubSampleRatio(1.1).BuildWithFallback(null));
        }

        /// <summary>
        /// Checks that
        /// <see cref="GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.BuildWithFallback"/>
        /// throws a <see cref="ArgumentOutOfRangeException"/> if called on a builder with negative tree count.
        /// </summary>
        [Fact]
        public void NegativeTreeCountThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetTreeCount(-1).BuildWithFallback(null));
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
            return new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder();
        }

        #endregion
    }
}