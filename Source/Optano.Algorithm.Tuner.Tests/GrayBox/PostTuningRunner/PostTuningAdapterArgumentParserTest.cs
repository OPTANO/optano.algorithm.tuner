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

namespace Optano.Algorithm.Tuner.Tests.GrayBox.PostTuningRunner
{
    using System;
    using System.IO;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.GrayBox.PostTuningRunner;
    using Optano.Algorithm.Tuner.Tests.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.Tests.DistributedExecution.DummyImplementations;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="PostTuningAdapterArgumentParser{TConfigBuilder}"/> class.
    /// </summary>
    public class PostTuningAdapterArgumentParserTest : HelpSupportingArgumentParserBaseTest
    {
        #region Fields

        /// <summary>
        /// The <see cref="DummyPostTuningAdapterArgumentParser"/> used in tests. Must be initialized.
        /// </summary>
        private readonly DummyPostTuningAdapterArgumentParser _parser;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PostTuningAdapterArgumentParserTest"/> class.
        /// </summary>
        public PostTuningAdapterArgumentParserTest()
        {
            this._parser = new DummyPostTuningAdapterArgumentParser();
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        protected override HelpSupportingArgumentParserBase Parser => this._parser;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that accessing the configuration builder before parsing throws an exception.
        /// </summary>
        [Fact]
        public void AccessingConfigurationBuilderBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.ConfigurationBuilder);
        }

        /// <summary>
        /// Checks that accessing the IsMaster-Boolean before parsing throws an exception.
        /// </summary>
        [Fact]
        public void AccessingIsMasterBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.IsMaster);
        }

        /// <summary>
        /// Checks that accessing the IsPostTuningRunner-Boolean before parsing throws an exception.
        /// </summary>
        [Fact]
        public void AccessingIsPostTuningRunnerBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.IsPostTuningRunner);
        }

        /// <summary>
        /// Checks that accessing the <see cref="PostTuningConfiguration"/> before parsing throws an exception.
        /// </summary>
        [Fact]
        public void AccessingPostTuningConfigurationRunnerBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.PostTuningConfiguration);
        }

        /// <summary>
        /// Checks that master arguments get parsed correctly.
        /// </summary>
        [Fact]
        public void MasterArgumentsGetParsedCorrectly()
        {
            var args = new[]
                           {
                               "--master",
                               "--value=1",
                           };

            this._parser.ParseArguments(args);
            this._parser.IsMaster.ShouldBeTrue();

            var config = this._parser.ConfigurationBuilder.Build();
            config.Value.ShouldBe(1);
        }

        /// <summary>
        /// Checks that worker arguments get parsed correctly.
        /// </summary>
        [Fact]
        public void WorkerArgumentsGetParsedCorrectly()
        {
            var args = new[] { "--test" };

            this._parser.ParseArguments(args);
            this._parser.IsMaster.ShouldBeFalse();

            this._parser.AdditionalArguments.Count().ShouldBe(1);
            this._parser.AdditionalArguments.First().ShouldBe("--test");
        }

        /// <summary>
        /// Checks that post tuning arguments get parsed correctly.
        /// </summary>
        [Fact]
        public void PostTuningArgumentsGetParsedCorrectly()
        {
            var args = new[]
                           {
                               "--postTuning",
                               "--value=1",
                               "--pathToPostTuningFile=dummy.csv",
                               "--indexOfFirstPostTuningRun=5",
                               "--numberOfPostTuningRuns=7",
                           };

            this._parser.ParseArguments(args);
            this._parser.IsPostTuningRunner.ShouldBeTrue();
            this._parser.PostTuningConfiguration.PathToPostTuningFile.ShouldBe("dummy.csv");
            this._parser.PostTuningConfiguration.IndexOfFirstPostTuningRun.ShouldBe(5);
            this._parser.PostTuningConfiguration.NumberOfPostTuningRuns.ShouldBe(7);

            var config = this._parser.ConfigurationBuilder.Build();
            config.Value.ShouldBe(1);
        }

        /// <summary>
        /// Checks that the post tuning configuration default values are set correctly.
        /// </summary>
        [Fact]
        public void PostTuningConfigurationDefaultValuesAreSetCorrectly()
        {
            // Do not provide any post tuning configuration values.
            var args = new[]
                           {
                               "--postTuning",
                               "--value=1",
                           };

            this._parser.ParseArguments(args);
            this._parser.IsPostTuningRunner.ShouldBeTrue();
            this._parser.PostTuningConfiguration.PathToPostTuningFile.ShouldBe(PathUtils.GetAbsolutePathFromCurrentDirectory("postTuningRuns.csv"));
            this._parser.PostTuningConfiguration.IndexOfFirstPostTuningRun.ShouldBe(0);
            this._parser.PostTuningConfiguration.NumberOfPostTuningRuns.ShouldBe(1);

            var config = this._parser.ConfigurationBuilder.Build();
            config.Value.ShouldBe(1);
        }

        /// <summary>
        /// Checks that providing --master and --postTuning throws an exception.
        /// </summary>
        [Fact]
        public void MasterAndPostTuningThrowsException()
        {
            var args = new[]
                           {
                               "--master",
                               "--postTuning",
                           };

            Assert.Throws<InvalidOperationException>(() => this._parser.ParseArguments(args));
        }

        /// <summary>
        /// Checks that <see cref="PostTuningAdapterArgumentParser{TConfigBuilder}.PrintHelp()"/> prints all help arguments.
        /// </summary>
        [Fact]
        public void PrintHelpPrintsAllHelpArguments()
        {
            TestUtils.CheckOutput(
                action: () => this._parser.PrintHelp(),
                check: consoleOutput =>
                    {
                        var reader = new StringReader(consoleOutput.ToString());
                        var text = reader.ReadToEnd();
                        text.ShouldContain("General arguments for the application:");
                        text.ShouldContain("Additional arguments if this instance acts as master (i.e. --master provided):");
                        text.ShouldContain("Adapter arguments for master:");
                        text.ShouldContain("OAT arguments for master:");
                        text.ShouldContain("Additional arguments if this instance acts as worker (i.e. nothing provided):");
                        text.ShouldContain("OAT arguments for worker:");

                        text.ShouldContain("Additional arguments if this instance acts as post tuning runner (i.e. --postTuning provided):");
                        text.ShouldContain("Adapter arguments for post tuning runner:");
                        text.ShouldContain("OAT arguments for post tuning runner:");
                        text.ShouldContain(
                            "In addition, the post tuning runner makes use of several OAT master arguments. In particular, you might want to set the following parameters.");
                        text.ShouldContain("Please see 'OAT arguments for master' for details on these parameters.");
                    });
        }

        #endregion
    }
}