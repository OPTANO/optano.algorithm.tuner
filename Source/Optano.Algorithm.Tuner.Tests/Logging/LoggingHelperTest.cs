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

namespace Optano.Algorithm.Tuner.Tests.Logging
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using NLog;

    using Optano.Algorithm.Tuner.Logging;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="LoggingHelper"/> class.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class LoggingHelperTest : IDisposable
    {
        #region Static Fields

        /// <summary>
        /// File in which <see cref="LoggingHelper"/> will write into.
        /// </summary>
        private static readonly string LogFile = PathUtils.GetAbsolutePathFromExecutableFolderRelative("test_logger.txt");

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingHelperTest"/> class.
        /// </summary>
        public LoggingHelperTest()
        {
            LoggingHelper.Configure(LoggingHelperTest.LogFile);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Called after every test.
        /// </summary>
        public void Dispose()
        {
            if (File.Exists(LoggingHelperTest.LogFile))
            {
                File.Delete(LoggingHelperTest.LogFile);
            }

            TestUtils.InitializeLogger();
        }

        /// <summary>
        /// Checks that after calling <see cref="LoggingHelper.ChangeConsoleLoggingLevel"/> with
        /// <see cref="VerbosityLevel.Warn"/>, only logs of type <see cref="NLog.LogLevel.Warn"/> are 
        /// written to console.
        /// </summary>
        [Fact]
        public void TestChangeConsoleLoggingLevelToWarn()
        {
            TestUtils.CheckOutput(
                action: () =>
                    {
                        LoggingHelper.ChangeConsoleLoggingLevel(VerbosityLevel.Warn);
                        LoggingHelperTest.WriteLinesForAllLogLevels();
                    },
                check: consoleOutput =>
                    {
                        using (var reader = new StringReader(consoleOutput.ToString()))
                        {
                            var output = reader.ReadToEnd();
                            this.CheckLinesForAllLogLevels(output, new List<LogLevel> { LogLevel.Warn });
                        }
                    });
        }

        /// <summary>
        /// Checks that after calling <see cref="LoggingHelper.ChangeConsoleLoggingLevel"/> with
        /// <see cref="VerbosityLevel.Info"/>, only logs of type <see cref="NLog.LogLevel.Warn"/>  or
        /// <see cref="NLog.LogLevel.Info"/> are written to console.
        /// </summary>
        [Fact]
        public void TestChangeConsoleLoggingLevelToInfo()
        {
            TestUtils.CheckOutput(
                action: () =>
                    {
                        LoggingHelper.ChangeConsoleLoggingLevel(VerbosityLevel.Info);
                        LoggingHelperTest.WriteLinesForAllLogLevels();
                    },
                check: consoleOutput =>
                    {
                        using (var reader = new StringReader(consoleOutput.ToString()))
                        {
                            this.CheckLinesForAllLogLevels(
                                reader.ReadToEnd(),
                                new List<LogLevel> { LogLevel.Warn, LogLevel.Info });
                        }
                    });
        }

        /// <summary>
        /// Checks that after calling <see cref="LoggingHelper.ChangeConsoleLoggingLevel"/> with
        /// <see cref="VerbosityLevel.Debug"/>, only logs of type <see cref="NLog.LogLevel.Warn"/>,
        /// <see cref="NLog.LogLevel.Info"/> or <see cref="NLog.LogLevel.Debug"/> are written to console.
        /// </summary>
        [Fact]
        public void TestChangeConsoleLoggingLevelToDebug()
        {
            TestUtils.CheckOutput(
                action: () =>
                    {
                        LoggingHelper.ChangeConsoleLoggingLevel(VerbosityLevel.Debug);
                        LoggingHelperTest.WriteLinesForAllLogLevels();
                    },
                check: consoleOutput =>
                    {
                        using (var reader = new StringReader(consoleOutput.ToString()))
                        {
                            this.CheckLinesForAllLogLevels(
                                reader.ReadToEnd(),
                                new List<LogLevel> { LogLevel.Warn, LogLevel.Info, LogLevel.Debug });
                        }
                    });
        }

        /// <summary>
        /// Checks that after calling <see cref="LoggingHelper.ChangeConsoleLoggingLevel"/> with
        /// <see cref="VerbosityLevel.Trace"/>, logs of type <see cref="NLog.LogLevel.Warn"/>,
        /// <see cref="NLog.LogLevel.Info"/>, <see cref="NLog.LogLevel.Debug"/> and <see cref="NLog.LogLevel.Trace"/>
        /// are written to console.
        /// </summary>
        [Fact]
        public void TestChangeConsoleLoggingLevelToTrace()
        {
            TestUtils.CheckOutput(
                action: () =>
                    {
                        LoggingHelper.ChangeConsoleLoggingLevel(VerbosityLevel.Trace);
                        LoggingHelperTest.WriteLinesForAllLogLevels();
                    },
                check: consoleOutput =>
                    {
                        using (var reader = new StringReader(consoleOutput.ToString()))
                        {
                            this.CheckLinesForAllLogLevels(
                                reader.ReadToEnd(),
                                new List<LogLevel> { LogLevel.Warn, LogLevel.Info, LogLevel.Debug, LogLevel.Trace });
                        }
                    });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes an empty line for every <see cref="VerbosityLevel"/>.
        /// </summary>
        private static void WriteLinesForAllLogLevels()
        {
            foreach (var level in Enum.GetValues(typeof(VerbosityLevel)))
            {
                LoggingHelper.WriteLine((VerbosityLevel)level, "");
            }
        }

        /// <summary>
        /// Checks that the provided lines contain exactly the provided levels.
        /// </summary>
        /// <param name="lines">Lines to check.</param>
        /// <param name="expectedLevels">Expected log levels.</param>
        private void CheckLinesForAllLogLevels(string lines, IList<LogLevel> expectedLevels)
        {
            foreach (var logLevel in LogLevel.AllLoggingLevels)
            {
                if (expectedLevels.Contains(logLevel))
                {
                    Assert.True(
                        lines.Contains($"[{logLevel}]"),
                        $"Output should contain {logLevel}, but doesn't. It is: {lines}.");
                }
                else
                {
                    Assert.False(
                        lines.Contains($"[{logLevel}]"),
                        $"Output should not contain {logLevel}, but does. It is: {lines}.");
                }
            }
        }

        #endregion
    }
}