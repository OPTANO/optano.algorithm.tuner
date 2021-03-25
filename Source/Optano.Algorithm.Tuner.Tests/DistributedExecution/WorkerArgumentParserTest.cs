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

namespace Optano.Algorithm.Tuner.Tests.DistributedExecution
{
    using System;

    using NDesk.Options;

    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.DistributedExecution;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Tests.Configuration.ArgumentParsers;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="WorkerArgumentParser"/> class.
    /// </summary>
    public class WorkerArgumentParserTest : HelpSupportingArgumentParserBaseTest
    {
        #region Fields

        /// <summary>
        /// The <see cref="WorkerArgumentParser"/> used in tests. Must be initialized.
        /// </summary>
        private readonly WorkerArgumentParser _parser;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerArgumentParserTest"/> class.
        /// </summary>
        public WorkerArgumentParserTest()
        {
            this._parser = new WorkerArgumentParser();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="WorkerArgumentParser"/> used in tests as a <see cref="HelpSupportingArgumentParserBase"/> to use
        /// in base class tests.
        /// </summary>
        protected override HelpSupportingArgumentParserBase Parser => this._parser;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that accessing <see cref="WorkerArgumentParser.Port"/> before calling
        /// <see cref="WorkerArgumentParser.ParseArguments(string[])"/> throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void AccessingPortBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.Port);
        }

        /// <summary>
        /// Verifies that accessing <see cref="WorkerArgumentParser.SeedHostName"/> before calling
        /// <see cref="WorkerArgumentParser.ParseArguments(string[])"/> throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void AccessingSeedHostNameBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.SeedHostName);
        }

        /// <summary>
        /// Verifies that accessing <see cref="WorkerArgumentParser.VerbosityLevel"/> before calling
        /// <see cref="WorkerArgumentParser.ParseArguments(string[])"/> thows an 
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void AccessingVerbosityLevelBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.VerbosityLevel);
        }

        /// <summary>
        /// Checks that <see cref="WorkerArgumentParser.ParseArguments(string[])"/> correctly interprets arguments
        /// given in the --longoption=value format.
        /// </summary>
        [Fact]
        public void LongOptionsAreParsedCorrectly()
        {
            string[] args = new string[]
                                {
                                    "--help",
                                    "--seedHostName=testHost",
                                    "--port=42",
                                    "--seed=2",
                                    "--verbose=3",
                                };

            this._parser.ParseArguments(args);
            Assert.Equal(42, this._parser.Port);
            Assert.True(this._parser.HelpTextRequested, "Help option was not parsed correctly.");
            Assert.Equal("testHost", this._parser.SeedHostName);
            Assert.Equal(VerbosityLevel.Trace, this._parser.VerbosityLevel);
        }

        /// <summary>
        /// Checks that <see cref="WorkerArgumentParser.ParseArguments(string[])"/> correctly interprets arguments
        /// given in the -shortoption value format.
        /// </summary>
        [Fact]
        public void ShortOptionsAreParsedCorreclty()
        {
            string[] args = new string[]
                                {
                                    "-p", "42",
                                    "-s", "testHost",
                                    "-h",
                                    "-v", "3",
                                };

            this._parser.ParseArguments(args);
            Assert.Equal(42, this._parser.Port);
            Assert.True(this._parser.HelpTextRequested, "Help option was not parsed correctly.");
            Assert.Equal("testHost", this._parser.SeedHostName);
            Assert.Equal(VerbosityLevel.Trace, this._parser.VerbosityLevel);
        }

        /// <summary>
        /// Checks that <see cref="WorkerArgumentParser.ParseArguments(string[])"/> leads to correct default values if
        /// only required arguments are provided.
        /// </summary>
        [Fact]
        public void CorrectDefaultValues()
        {
            var args = new string[] { "--seedHostName=testHost" };

            this._parser.ParseArguments(args);
            Assert.Equal(8081, this._parser.Port);
            Assert.False(this._parser.HelpTextRequested, "Help option should be disabled by default.");
            Assert.Equal(
                VerbosityLevel.Info,
                this._parser.VerbosityLevel);
        }

        /// <summary>
        /// Verifies that calling <see cref="WorkerArgumentParser.ParseArguments(string[])"/> without providing
        /// information about the seed host name thrown an <see cref="OptionException"/>.
        /// </summary>
        [Fact]
        public void NoSeedHostNameEvaluationInfoThrowsException()
        {
            Assert.Throws<OptionException>(() => this._parser.ParseArguments(new string[0]));
        }

        /// <summary>
        /// Verifies that unknown arguments, caused by e.g. typos, provoke an <see cref="AggregateException"/>.
        /// </summary>
        [Fact]
        public void TyposProvokeException()
        {
            string[] args = new string[]
                                {
                                    "--halp",
                                };
            Assert.Throws<AggregateException>(() => this._parser.ParseArguments(args));
        }

        #endregion
    }
}