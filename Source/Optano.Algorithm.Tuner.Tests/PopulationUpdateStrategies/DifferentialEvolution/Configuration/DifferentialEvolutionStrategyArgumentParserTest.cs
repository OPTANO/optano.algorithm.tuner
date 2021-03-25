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
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration;
    using Optano.Algorithm.Tuner.Tests.Configuration.ArgumentParsers;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="DifferentialEvolutionStrategyArgumentParser"/> class.
    /// </summary>
    public class DifferentialEvolutionStrategyArgumentParserTest : HelpSupportingArgumentParserBaseTest
    {
        #region Fields

        /// <summary>
        /// The <see cref="DifferentialEvolutionStrategyArgumentParser"/> used in tests.
        /// </summary>
        private readonly DifferentialEvolutionStrategyArgumentParser _parser = new DifferentialEvolutionStrategyArgumentParser();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="DifferentialEvolutionStrategyArgumentParser"/> used in tests as a
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
                    "--replacementRate=0.09",
                    "--maxGenerationsPerDePhase=13",
                    "--minDomainSize=4",
                    "--meanMutationFactor=0.29",
                    "--fixInstances",
                    "--focusOnIncumbent=true",
                };

            this._parser.ParseArguments(args);
            var config = this._parser.ConfigurationBuilder.BuildWithFallback(null);
            Assert.Equal(
                13,
                config.MaximumNumberGenerations);
            Assert.Equal(4, config.MinimumDomainSize);
            Assert.Equal(
                0.29,
                config.DifferentialEvolutionConfiguration.InitialMeanMutationFactor);
            Assert.True(config.FixInstances, "Fix instances flag was not parsed correctly.");
            Assert.True(config.FocusOnIncumbent, "Focus on incumbent option was not parsed correctly.");
            Assert.Equal(
                0.09,
                config.ReplacementRate);
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
                DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder.DefaultMaximumNumberGenerations,
                config.MaximumNumberGenerations);
            Assert.Equal(
                DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder.DefaultMinimumDomainSize,
                config.MinimumDomainSize);
            Assert.Equal(
                DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder.DefaultFocusOnIncumbent,
                config.FocusOnIncumbent);
            Assert.Equal(
                DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder.DefaultReplacementRate,
                config.ReplacementRate);
            Assert.Equal(
                DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder.DefaultInitialMeanMutationFactor,
                config.DifferentialEvolutionConfiguration.InitialMeanMutationFactor);
            Assert.False(config.FixInstances, "Default was not used for fix instances flag.");
            Assert.False(this._parser.HelpTextRequested, "Help option should be disabled by default.");
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStrategyArgumentParser"/> collects unknown arguments.
        /// </summary>
        [Fact]
        public void UnknownArgumentsAreCollected()
        {
            string[] args = { "--halp", "--meanCrossoverRate=0.8" };
            this._parser.ParseArguments(args);
            Assert.Equal(
                "--halp",
                this._parser.AdditionalArguments.Single());
        }

        #endregion
    }
}