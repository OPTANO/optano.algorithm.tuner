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

namespace Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;

    /// <summary>
    /// The <see cref="GenomePredictionRandomForest{TSamplingStrategy}"/> config.
    /// </summary>
    public class GenomePredictionRandomForestConfig : ConfigurationBase
    {
        #region Public properties

        /// <summary>
        /// Gets the number of trees in the random forest.
        /// </summary>
        public int TreeCount { get; private set; }

        /// <summary>
        /// Gets the minimum size of a split in the trees.
        /// </summary>
        public int MinimumSplitSize { get; private set; }

        /// <summary>
        /// Gets the maximum depth of a tree.
        /// </summary>
        public int MaximumTreeDepth { get; private set; }

        /// <summary>
        /// Gets the percentage of features to use per split in each tree.
        /// Commonly used values in literature are ~1/3, or sqrt(#features).
        /// Keep in mind that #features may be larger than the
        /// number of parameters in your tree (see <see cref="CategoricalEncodingBase"/>)!.
        /// </summary>
        public double FeaturesPerSplitRatio { get; private set; }

        /// <summary>
        /// Gets the minimum information gain for a split in the trees.
        /// </summary>
        public double MinimumInformationGain { get; private set; }

        /// <summary>
        /// Gets the proportion of the observation subset that is passed to each tree during training.
        /// </summary>
        public double SubSampleRatio { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to train the trees in multiple threads.
        /// </summary>
        public bool RunParallel { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks if this config is compatible to the <paramref name="other"/> config.
        /// </summary>
        /// <param name="other">
        /// The other cofiguration.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool IsCompatible(ConfigurationBase other)
        {
            return this.IsTechnicallyCompatible(other);
        }

        /// <summary>
        /// Checks whether two <see cref="ConfigurationBase"/>s are compatible in a technical sense for one
        /// to be used in a continued run based on a run using the other.
        /// <para>In this case, this is the same as <see cref="IsCompatible"/>,
        /// because the random forest is not re-initialized on continue.</para>
        /// </summary>
        /// <param name="other">Configuration used for the start of run.</param>
        /// <returns>True iff this configuration can be used for continued run.</returns>
        public override bool IsTechnicallyCompatible(ConfigurationBase other)
        {
            if (!(other is GenomePredictionRandomForestConfig cast))
            {
                return false;
            }

            return this.TreeCount == cast.TreeCount
                   && this.MinimumSplitSize == cast.MinimumSplitSize
                   && this.MaximumTreeDepth == cast.MaximumTreeDepth
                   // ReSharper disable once CompareOfFloatsByEqualityOperator
                   && this.FeaturesPerSplitRatio == cast.FeaturesPerSplitRatio
                   // ReSharper disable once CompareOfFloatsByEqualityOperator
                   && this.MinimumInformationGain == cast.MinimumInformationGain
                   // ReSharper disable once CompareOfFloatsByEqualityOperator
                   && this.SubSampleRatio == cast.SubSampleRatio
                   && this.RunParallel == cast.RunParallel;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var descriptionBuilder = new StringBuilder();

            descriptionBuilder.AppendLine($"forestRunParallel : {this.RunParallel}");
            descriptionBuilder.AppendLine($"forestTreeCount : {this.TreeCount}");
            descriptionBuilder.AppendLine($"forestSubSampleRatio : {this.SubSampleRatio}");
            descriptionBuilder.AppendLine($"forestFeaturesPerSplitRatio : {this.FeaturesPerSplitRatio}");
            descriptionBuilder.AppendLine($"forestMaxTreeDepth : {this.MaximumTreeDepth}");
            descriptionBuilder.AppendLine($"forestMinSplitSize : {this.MinimumSplitSize}");
            descriptionBuilder.AppendLine($"forestMinInformationGain : {this.MinimumInformationGain}");

            return descriptionBuilder.ToString();
        }

        #endregion

        /// <summary>
        /// The <see cref="GenomePredictionRandomForestConfig"/> builder.
        /// </summary>
        [SuppressMessage(
            "NDepend",
            "ND1305:NestedTypesShouldNotBeVisible",
            Justification = "This type is part of the Builder Pattern.")]
        public class GenomePredictionRandomForestConfigBuilder : IConfigBuilder<GenomePredictionRandomForestConfig>
        {
            #region Static Fields

            /// <summary>
            /// The default tree count.
            /// </summary>
            internal static readonly int DefaultTreeCount = 75;

            /// <summary>
            /// The default minimum split size.
            /// </summary>
            internal static readonly int DefaultMinimumSplitSize = 2;

            /// <summary>
            /// The default maximum tree depth.
            /// </summary>
            internal static readonly int DefaultMaximumTreeDepth = 10;

            /// <summary>
            /// The default features per split ratio.
            /// </summary>
            internal static readonly double DefaultFeaturesPerSplitRatio = 0.3;

            /// <summary>
            /// The default minimum information gain.
            /// </summary>
            internal static readonly double DefaultMinimumInformationGain = 1E-06;

            /// <summary>
            /// The default sub sample ratio.
            /// </summary>
            internal static readonly double DefaultSubSampleRatio = 0.7;

            /// <summary>
            /// The default run parallel.
            /// </summary>
            internal static readonly bool DefaultRunParallel = true;

            #endregion

            #region Properties

            /// <summary>
            /// Gets or sets the tree count.
            /// </summary>
            protected int? TreeCount { get; set; }

            /// <summary>
            /// Gets or sets the minimum split size.
            /// </summary>
            protected int? MinimumSplitSize { get; set; }

            /// <summary>
            /// Gets or sets the maximum tree depth.
            /// </summary>
            protected int? MaximumTreeDepth { get; set; }

            /// <summary>
            /// Gets or sets the features per split.
            /// </summary>
            protected double? FeaturesPerSplitRatio { get; set; }

            /// <summary>
            /// Gets or sets the minimum information gain.
            /// </summary>
            protected double? MinimumInformationGain { get; set; }

            /// <summary>
            /// Gets or sets the sub sample ratio.
            /// </summary>
            protected double? SubSampleRatio { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether run parallel.
            /// </summary>
            protected bool? RunParallel { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// Builds a <see cref="GenomePredictionRandomForestConfig"/> using the provided
            /// <see cref="ConfigurationBase"/> as fallback.
            /// </summary>
            /// <param name="fallback">Used if a property is not set for this
            /// <see cref="GenomePredictionRandomForestConfig"/>.
            /// May be null. In that case, defaults are used as fallback.
            /// Needs to be of type <see cref="GenomePredictionRandomForestConfig"/> if it is not null.</param>
            /// <returns>The built <see cref="GenomePredictionRandomForestConfig"/>.</returns>
            public GenomePredictionRandomForestConfig BuildWithFallback(ConfigurationBase fallback)
            {
                return this.BuildWithFallback(CastToConfigurationType<GenomePredictionRandomForestConfig>(fallback));
            }

            /// <summary>
            /// Sets the <see cref="GenomePredictionRandomForestConfig.TreeCount"/>.
            /// </summary>
            /// <param name="treeCount">
            /// The tree count.
            /// </param>
            /// <returns>
            /// The <see cref="GenomePredictionRandomForestConfigBuilder"/>.
            /// </returns>
            public GenomePredictionRandomForestConfigBuilder SetTreeCount(int treeCount)
            {
                this.TreeCount = treeCount;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="GenomePredictionRandomForestConfig.FeaturesPerSplitRatio"/>.
            /// </summary>
            /// <param name="featurePerSplitRatio">
            /// The ratio of features to use during the training of a tree.
            /// </param>
            /// <returns>
            /// The <see cref="GenomePredictionRandomForestConfigBuilder"/>.
            /// </returns>
            public GenomePredictionRandomForestConfigBuilder SetFeaturesPerSplitRatio(double featurePerSplitRatio)
            {
                this.FeaturesPerSplitRatio = featurePerSplitRatio;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="GenomePredictionRandomForestConfig.MaximumTreeDepth"/>.
            /// </summary>
            /// <param name="depth">
            /// The depth.
            /// </param>
            /// <returns>
            /// The <see cref="GenomePredictionRandomForestConfigBuilder"/>.
            /// </returns>
            public GenomePredictionRandomForestConfigBuilder SetMaximumTreeDepth(int depth)
            {
                this.MaximumTreeDepth = depth;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="GenomePredictionRandomForestConfig.MinimumInformationGain"/>.
            /// </summary>
            /// <param name="gain">
            /// The gain.
            /// </param>
            /// <returns>
            /// The <see cref="GenomePredictionRandomForestConfigBuilder"/>.
            /// </returns>
            public GenomePredictionRandomForestConfigBuilder SetMinimumInformationGain(double gain)
            {
                this.MinimumInformationGain = gain;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="GenomePredictionRandomForestConfig.MinimumSplitSize"/>.
            /// </summary>
            /// <param name="split">
            /// The split size.
            /// </param>
            /// <returns>
            /// The <see cref="GenomePredictionRandomForestConfigBuilder"/>.
            /// </returns>
            public GenomePredictionRandomForestConfigBuilder SetMinimumSplitSize(int split)
            {
                this.MinimumSplitSize = split;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="GenomePredictionRandomForestConfig.RunParallel"/>.
            /// </summary>
            /// <param name="parallel">
            /// The parallel.
            /// </param>
            /// <returns>
            /// The <see cref="GenomePredictionRandomForestConfigBuilder"/>.
            /// </returns>
            public GenomePredictionRandomForestConfigBuilder SetRunParallel(bool parallel)
            {
                this.RunParallel = parallel;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="GenomePredictionRandomForestConfig.SubSampleRatio"/>.
            /// </summary>
            /// <param name="subSampleRatio">
            /// The sub sample ratio.
            /// </param>
            /// <returns>
            /// The <see cref="GenomePredictionRandomForestConfigBuilder"/>.
            /// </returns>
            public GenomePredictionRandomForestConfigBuilder SetSubSampleRatio(double subSampleRatio)
            {
                this.SubSampleRatio = subSampleRatio;
                return this;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Builds a <see cref="GenomePredictionRandomForestConfig"/> using the provided
            /// <see cref="GenomePredictionRandomForestConfig"/> as fallback.
            /// </summary>
            /// <param name="fallback">Used if a property is not set for this
            /// <see cref="GenomePredictionRandomForestConfig"/>.
            /// May be null. In that case, defaults are used as fallback.</param>
            /// <returns>The build <see cref="GenomePredictionRandomForestConfig"/>.</returns>
            private GenomePredictionRandomForestConfig BuildWithFallback(GenomePredictionRandomForestConfig fallback)
            {
                // Create new configuration.
                var configuration = new GenomePredictionRandomForestConfig();

                // Set all properties.
                // If the builder does not specify a property, use fallback. If fallback is null, use default.
                configuration.TreeCount = this.TreeCount ?? fallback?.TreeCount ?? DefaultTreeCount;
                configuration.FeaturesPerSplitRatio =
                    this.FeaturesPerSplitRatio ?? fallback?.FeaturesPerSplitRatio ?? DefaultFeaturesPerSplitRatio;
                configuration.MinimumSplitSize = this.MinimumSplitSize ?? fallback?.MinimumSplitSize ?? DefaultMinimumSplitSize;
                configuration.MaximumTreeDepth = this.MaximumTreeDepth ?? fallback?.MaximumTreeDepth ?? DefaultMaximumTreeDepth;
                configuration.MinimumInformationGain =
                    this.MinimumInformationGain ?? fallback?.MinimumInformationGain ?? DefaultMinimumInformationGain;
                configuration.RunParallel =
                    this.RunParallel ?? fallback?.RunParallel ?? DefaultRunParallel;
                configuration.SubSampleRatio =
                    this.SubSampleRatio ?? fallback?.SubSampleRatio ?? DefaultSubSampleRatio;

                this.ValidateParameters();

                return configuration;
            }

            /// <summary>
            /// Validates the current parameter settings.
            /// </summary>
            private void ValidateParameters()
            {
                if (this.TreeCount <= 0)
                {
                    throw new ArgumentOutOfRangeException($"{RegressionForestArgumentParser.TreeCountName} must be larger than 0.");
                }

                if (this.FeaturesPerSplitRatio <= 0 || this.FeaturesPerSplitRatio > 1)
                {
                    throw new ArgumentOutOfRangeException(
                        $"{RegressionForestArgumentParser.FeaturesPerSplitRatioName} must be in the range of (0, 1].");
                }

                if (this.MinimumSplitSize <= 0)
                {
                    throw new ArgumentOutOfRangeException($"{RegressionForestArgumentParser.MinimumSplitSizeName} must be larger than 0.");
                }

                if (this.MaximumTreeDepth <= 0)
                {
                    throw new ArgumentOutOfRangeException($"{RegressionForestArgumentParser.MaximumTreeDepthName} must be larger than 0.");
                }

                if (this.MinimumInformationGain <= 0)
                {
                    throw new ArgumentOutOfRangeException($"{RegressionForestArgumentParser.MinimumInformationGainName} must be larger than 0.");
                }

                if (this.SubSampleRatio <= 0.0 || this.SubSampleRatio > 1.0)
                {
                    throw new ArgumentOutOfRangeException($"{RegressionForestArgumentParser.SubSampleRatioName} must be in the range of (0, 1].");
                }
            }

            #endregion
        }
    }
}