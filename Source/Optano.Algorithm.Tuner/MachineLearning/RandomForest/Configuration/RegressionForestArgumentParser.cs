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

    using Akka.Util.Internal;

    using NDesk.Options;

    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;

    /// <summary>
    /// The regression forest argument parser.
    /// </summary>
    public class RegressionForestArgumentParser : HelpSupportingArgumentParser<
        GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RegressionForestArgumentParser"/> class.
        /// </summary>
        public RegressionForestArgumentParser()
            : base(true)
        {
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the identifier for the parser class.
        /// </summary>
        public static string Identifier => typeof(GenomePredictionRandomForestConfig).FullName;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the parameter name for the <see cref="GenomePredictionRandomForestConfig.TreeCount"/>.
        /// </summary>
        internal static string TreeCountName { get; } = "forestTreeCount=";

        /// <summary>
        /// Gets the parameter name for the <see cref="GenomePredictionRandomForestConfig.MinimumSplitSize"/>.
        /// </summary>
        internal static string MinimumSplitSizeName { get; } = "forestMinSplitSize=";

        /// <summary>
        /// Gets the parameter name for the <see cref="GenomePredictionRandomForestConfig.MaximumTreeDepth"/>.
        /// </summary>
        internal static string MaximumTreeDepthName { get; } = "forestMaxTreeDepth=";

        /// <summary>
        /// Gets the parameter name for the <see cref="GenomePredictionRandomForestConfig.FeaturesPerSplitRatio"/>.
        /// </summary>
        internal static string FeaturesPerSplitRatioName { get; } = "forestFeaturesPerSplitRatio=";

        /// <summary>
        /// Gets the parameter name for the <see cref="GenomePredictionRandomForestConfig.MinimumInformationGain"/>.
        /// </summary>
        internal static string MinimumInformationGainName { get; } = "forestMinInformationGain=";

        /// <summary>
        /// Gets the parameter name for the <see cref="GenomePredictionRandomForestConfig.SubSampleRatio"/>.
        /// </summary>
        internal static string SubSampleRatioName { get; } = "forestSubSampleRatio=";

        /// <summary>
        /// Gets the parameter name for the <see cref="GenomePredictionRandomForestConfig.RunParallel"/>.
        /// </summary>
        internal static string RunParallelName { get; } = "forestRunParallel=";

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public override void PrintHelp(bool printHelpParameter)
        {
            Console.WriteLine("Arguments for random forest:");
            base.PrintHelp(printHelpParameter);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create the option set.
        /// </summary>
        /// <returns>
        /// The <see cref="OptionSet"/>.
        /// </returns>
        protected override OptionSet CreateOptionSet()
        {
            var optionSet = new OptionSet
                                {
                                    {
                                        RegressionForestArgumentParser.TreeCountName,
                                        $"The number of trees in the random forest.\nDefault is {GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.DefaultTreeCount}.\nThis must be an integer greater than 0.",
                                        (int tC) => this.InternalConfigurationBuilder.SetTreeCount(tC)
                                    },
                                    {
                                        RegressionForestArgumentParser.FeaturesPerSplitRatioName,
                                        $"The percentage of features to use per split in each tree. Commonly used values in literature are ~1/3, or sqrt(#features). Keep in mind that #features may be larger than the number of parameters in your tree. Refer to the \"Parameter Selection\" chapter in the user documentation.\nDefault is {GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.DefaultFeaturesPerSplitRatio}.\nThis must be a double in the range of (0, 1].",
                                        (double fps) => this.InternalConfigurationBuilder.SetFeaturesPerSplitRatio(fps)
                                    },
                                    {
                                        RegressionForestArgumentParser.MaximumTreeDepthName,
                                        $"The maximum depth of a tree.\nDefault is {GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.DefaultMaximumTreeDepth}.\nThis must be an integer greater than 0.",
                                        (int depth) => this.InternalConfigurationBuilder.SetMaximumTreeDepth(depth)
                                    },
                                    {
                                        RegressionForestArgumentParser.MinimumInformationGainName,
                                        $"The minimum information gain for a split in the trees.\nDefault is {GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.DefaultMinimumInformationGain}.\nThis must be a double greater than 0.",
                                        (double gain) => this.InternalConfigurationBuilder.SetMinimumInformationGain(gain)
                                    },
                                    {
                                        RegressionForestArgumentParser.MinimumSplitSizeName,
                                        $"The minimum size of a split in the trees.\nDefault is {GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.DefaultMinimumSplitSize}.\nThis must be an integer greater than 0.",
                                        (int split) => this.InternalConfigurationBuilder.SetMinimumSplitSize(split)
                                    },
                                    {
                                        RegressionForestArgumentParser.RunParallelName,
                                        $"Enables parallel learning for the Regression Forest.\nDefault is {GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.DefaultRunParallel}.\nThis must be a boolean",
                                        (bool parallel) => this.InternalConfigurationBuilder.SetRunParallel(parallel)
                                    },
                                    {
                                        RegressionForestArgumentParser.SubSampleRatioName,
                                        $"The proportion of the observation subset that is passed to each tree during training.\nDefault is {GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder.DefaultSubSampleRatio}.\nThis must be a double in the range of (0, 1].",
                                        (double subSampleRatio) => this.InternalConfigurationBuilder.SetSubSampleRatio(subSampleRatio)
                                    },
                                };

            var helpOptions = base.CreateOptionSet();
            helpOptions.ForEach(o => optionSet.Add(o));

            return optionSet;
        }

        #endregion
    }
}