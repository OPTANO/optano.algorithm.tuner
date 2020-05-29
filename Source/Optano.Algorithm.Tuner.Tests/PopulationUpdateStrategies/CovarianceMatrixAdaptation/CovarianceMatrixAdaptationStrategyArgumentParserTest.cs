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
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.Tests.Configuration.ArgumentParsers;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="CovarianceMatrixAdaptationStrategyArgumentParser"/> class.
    /// </summary>
    public class CovarianceMatrixAdaptationStrategyArgumentParserTest : HelpSupportingArgumentParserBaseTest
    {
        #region Fields

        /// <summary>
        /// The <see cref="CovarianceMatrixAdaptationStrategyArgumentParser"/> used in tests.
        /// </summary>
        private readonly CovarianceMatrixAdaptationStrategyArgumentParser _parser = new CovarianceMatrixAdaptationStrategyArgumentParser();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="CovarianceMatrixAdaptationStrategyArgumentParser"/> used in tests as a
        /// <see cref="HelpSupportingArgumentParserBase"/> to use in base class tests.
        /// </summary>
        protected override HelpSupportingArgumentParserBase Parser => this._parser;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="HelpSupportingArgumentParserBase.ParseArguments(string[])"/> correctly interprets
        /// arguments given in the --longoption=value format.
        /// </summary>
        [Fact]
        public void LongOptionsAreParsedCorrectly()
        {
            string[] args =
                {
                    "--help",
                    "--maxGenerationsPerCmaEsPhase=13",
                    "--minDomainSize=4",
                    "--initialStepSize=0.35",
                    "--fixInstances",
                    "--replacementRate=0.89",
                    "--focusOnIncumbent=true",
                };

            this._parser.ParseArguments(args);
            var config = this._parser.ConfigurationBuilder.BuildWithFallback(null);
            Assert.Equal(
                13,
                config.MaximumNumberGenerations);
            Assert.Equal(4, config.MinimumDomainSize);
            Assert.Equal(
                0.89,
                config.ReplacementRate);
            Assert.True(config.FixInstances, "Fix instances flag was not parsed correctly.");
            Assert.Equal(0.35, config.InitialStepSize);
            Assert.True(config.FocusOnIncumbent, "Focus on incumbent option was not parsed correctly.");
            Assert.True(this._parser.HelpTextRequested, "Help option was not parsed correctly.");
        }

        /// <summary>
        /// Checks that <see cref="HelpSupportingArgumentParserBase.ParseArguments(string[])"/> leads to correct
        /// default values if only required arguments are provided.
        /// </summary>
        [Fact]
        public void CorrectDefaultValues()
        {
            var args = new string[0];

            this._parser.ParseArguments(args);
            var config = this._parser.ConfigurationBuilder.BuildWithFallback(null);

            Assert.Equal(
                CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder
                    .DefaultMaximumNumberGenerations,
                config.MaximumNumberGenerations);
            Assert.Equal(
                CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder.DefaultMinimumDomainSize,
                config.MinimumDomainSize);
            Assert.Equal(
                CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder.DefaultFixInstances,
                config.FixInstances);
            Assert.Equal(
                CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder.DefaultInitialStepSize,
                config.InitialStepSize);
            Assert.Equal(
                CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder.DefaultReplacementRate,
                config.ReplacementRate);
            Assert.Equal(
                CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder.DefaultFocusOnIncumbent,
                config.FocusOnIncumbent);
            Assert.False(this._parser.HelpTextRequested, "Help option should be disabled by default.");
        }

        /// <summary>
        /// Checks that <see cref="CovarianceMatrixAdaptationStrategyArgumentParser"/> collects unknown arguments.
        /// </summary>
        [Fact]
        public void UnknownArgumentsAreCollected()
        {
            string[] args = { "--halp" };
            this._parser.ParseArguments(args);
            Assert.Equal(
                "--halp",
                this._parser.AdditionalArguments.Single());
        }

        #endregion
    }
}