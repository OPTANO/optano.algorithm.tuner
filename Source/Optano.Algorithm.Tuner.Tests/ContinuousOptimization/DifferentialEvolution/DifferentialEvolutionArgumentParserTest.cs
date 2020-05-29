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
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;
    using Optano.Algorithm.Tuner.Tests.Configuration.ArgumentParsers;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="DifferentialEvolutionArgumentParser"/> class.
    /// </summary>
    public class DifferentialEvolutionArgumentParserTest : HelpSupportingArgumentParserBaseTest
    {
        #region Fields

        /// <summary>
        /// The <see cref="DifferentialEvolutionArgumentParser"/> used in tests.
        /// </summary>
        private readonly DifferentialEvolutionArgumentParser _parser = new DifferentialEvolutionArgumentParser();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="DifferentialEvolutionArgumentParser"/> used in tests as a
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
                    "--bestPercentage=0.13",
                    "--learningRate=0.4",
                    "--meanMutationFactor=0.29",
                    "--meanCrossoverRate=0.31",
                };

            this._parser.ParseArguments(args);
            var config = this._parser.ConfigurationBuilder.BuildWithFallback(null);
            Assert.Equal(0.13, config.BestPercentage);
            Assert.Equal(0.4, config.LearningRate);
            Assert.Equal(
                0.29,
                config.InitialMeanMutationFactor);
            Assert.Equal(
                0.31,
                config.InitialMeanCrossoverRate);
            Assert.True(this._parser.HelpTextRequested);
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
                DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder.DefaultBestPercentage,
                config.BestPercentage);
            Assert.Equal(
                DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder.DefaultLearningRate,
                config.LearningRate);
            Assert.Equal(
                DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder.DefaultInitialMeanCrossoverRate,
                config.InitialMeanCrossoverRate);
            Assert.Equal(
                DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder.DefaultInitialMeanMutationFactor,
                config.InitialMeanMutationFactor);
            Assert.False(this._parser.HelpTextRequested, "Help option should be disabled by default.");
        }

        /// <summary>
        /// Verifies that unknown arguments, caused by e.g. typos, provoke an <see cref="AggregateException"/>.
        /// </summary>
        [Fact]
        public void TyposProvokeException()
        {
            string[] args = { "--halp" };
            Assert.Throws<AggregateException>(() => this._parser.ParseArguments(args));
        }

        /// <summary>
        /// Checks that, in contrast to <see cref="TyposProvokeException"/>, it is possible to instantiate
        /// <see cref="DifferentialEvolutionArgumentParser"/> such that unknown arguments are collected and no error
        /// is thrown.
        /// </summary>
        [Fact]
        public void UnknownArgumentsCanBeIgnored()
        {
            var tolerantParser = new DifferentialEvolutionArgumentParser(allowAdditionalArguments: true);
            string[] args = { "--halp" };
            tolerantParser.ParseArguments(args);
            Assert.Equal(
                "--halp",
                tolerantParser.AdditionalArguments.Single());
        }

        #endregion
    }
}