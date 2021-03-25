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

namespace Optano.Algorithm.Tuner.Tests.Configuration.ArgumentParsers
{
    using System;
    using System.IO;

    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.DistributedExecution;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="ArgumentParserUtils"/> class.
    /// </summary>
    public class ArgumentParserUtilsTest : IDisposable
    {
        #region Fields

        /// <summary>
        /// The <see cref="HelpSupportingArgumentParserBaseTest"/> used in tests. Must be initialized.
        /// </summary>
        private readonly HelpSupportingArgumentParserBase _parser;

        /// <summary>
        /// Listener for console output.
        /// </summary>
        private readonly StringWriter _consoleOutput;

        /// <summary>
        /// Stores the reference to original <see cref="Console.Out"/>.
        /// </summary>
        private readonly TextWriter _originalConsoleOut;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentParserUtilsTest"/> class.
        /// </summary>
        public ArgumentParserUtilsTest()
        {
            TestUtils.InitializeLogger();

            this._originalConsoleOut = Console.Out;
            this._consoleOutput = new StringWriter();
            Console.SetOut(this._consoleOutput);
            this._parser = new WorkerArgumentParser();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Cleans up after each test.
        /// </summary>
        public void Dispose()
        {
            Console.SetOut(this._originalConsoleOut);
            this._consoleOutput?.Dispose();
        }

        /// <summary>
        /// Checks that
        /// <see cref="ArgumentParserUtils.ParseArguments(HelpSupportingArgumentParserBase, string[])"/> returns 
        /// true if valid arguments without the --help one are provided.
        /// </summary>
        [Fact]
        public void ParseArgumentsReturnsTrueForValidOptionsWithoutHelpOption()
        {
            string[] args = new string[]
                                {
                                    "--seedHostName=testHost",
                                    "--port=42",
                                };
            Assert.True(ArgumentParserUtils.ParseArguments(this._parser, args));
        }

        /// <summary>
        /// Checks that
        /// <see cref="ArgumentParserUtils.ParseArguments(HelpSupportingArgumentParserBase, string[])"/> returns 
        /// false if required options are missing.
        /// </summary>
        [Fact]
        public void ParseArgumentsReturnsFalseForMissingOptions()
        {
            Assert.False(
                ArgumentParserUtils.ParseArguments(this._parser, args: new string[0]));
        }

        /// <summary>
        /// Checks that
        /// <see cref="ArgumentParserUtils.ParseArguments(HelpSupportingArgumentParserBase, string[])"/> prints 
        /// information if required options are missing.
        /// </summary>
        [Fact]
        public void ParseArgumentsPrintsInformationAboutMissingOptions()
        {
            // Parse with missing options.
            ArgumentParserUtils.ParseArguments(this._parser, args: new string[0]);

            // Check that information about it is written to console.
            var reader = new StringReader(this._consoleOutput.ToString());
            var line = reader.ReadLine();
            Assert.Equal(
                "Invalid options: ",
                line);
            line = reader.ReadLine();
            Assert.Equal(
                "Seed host name must be provided. Where is the master located?",
                line);
            line = reader.ReadLine();
            Assert.Equal(
                "Try adding '--help' for more information.",
                line);
        }

        /// <summary>
        /// Checks that
        /// <see cref="ArgumentParserUtils.ParseArguments(HelpSupportingArgumentParserBase, string[])"/> returns 
        /// false for additional unknown options.
        /// </summary>
        [Fact]
        public void ParseArgumentsReturnsFalseForUnknownOptions()
        {
            string[] args = new string[]
                                {
                                    "--seedHostName=testHost",
                                    "--port=42",
                                    "--foo",
                                    "--bar",
                                };
            Assert.False(ArgumentParserUtils.ParseArguments(this._parser, args));
        }

        /// <summary>
        /// Checks that
        /// <see cref="ArgumentParserUtils.ParseArguments(HelpSupportingArgumentParserBase, string[])"/> prints 
        /// information if unknown options are provided.
        /// </summary>
        [Fact]
        public void ParseArgumentsPrintsInformationAboutUnknownOptions()
        {
            string[] args = new string[]
                                {
                                    "--seedHostName=testHost",
                                    "--port=42",
                                    "--foo",
                                    "--bar",
                                };

            // Parse with unknwon options.
            ArgumentParserUtils.ParseArguments(this._parser, args);

            var writtenOutput = this._consoleOutput.ToString();
            var reader = new StringReader(writtenOutput);

            Assert.Equal(
                "One or more arguments could not be interpreted:",
                reader.ReadLine());
            Assert.Equal(
                "Could not resolve '--foo'.",
                reader.ReadLine());
            Assert.Equal(
                "Could not resolve '--bar'.",
                reader.ReadLine());
            Assert.Equal(
                "Try adding '--help' for more information.",
                reader.ReadLine());
        }

        /// <summary>
        /// Checks that
        /// <see cref="ArgumentParserUtils.ParseArguments(HelpSupportingArgumentParserBase, string[])"/> returns 
        /// false if help is requested.
        /// </summary>
        [Fact]
        public void ParseArgumentsReturnsFalseIfHelpIsRequested()
        {
            var args = new string[] { "--help" };
            Assert.False(ArgumentParserUtils.ParseArguments(this._parser, args));
        }

        /// <summary>
        /// Checks that
        /// <see cref="ArgumentParserUtils.ParseArguments(HelpSupportingArgumentParserBase, string[])"/> prints 
        /// information about options if --help is provided as a parameter.
        /// </summary>
        [Fact]
        public void ParseArgumentsPrintsHelpIfHelpIsRequested()
        {
            // Parse with help option.
            var args = new string[] { "--help" };
            ArgumentParserUtils.ParseArguments(this._parser, args);

            // Check that information about options is written to console.
            var consoleText = this._consoleOutput.ToString();
            Assert.True(
                consoleText.Contains("Information about usage will be printed."),
                "No information about --help parameter was given on --help.");
        }

        #endregion
    }
}