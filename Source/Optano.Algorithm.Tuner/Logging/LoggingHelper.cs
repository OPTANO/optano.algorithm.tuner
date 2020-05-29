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

namespace Optano.Algorithm.Tuner.Logging
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;

    using NLog;
    using NLog.Config;
    using NLog.Targets;

    /// <summary>
    /// Helper class to print log messages.
    /// Prints all text via the <see cref="NLog.Logger"/>.
    /// </summary>
    public static class LoggingHelper
    {
        #region Constants

        /// <summary>
        /// Identifier for console in <see cref="LoggingConfiguration"/>.
        /// Used in <see cref="Configure"/> and <see cref="ChangeConsoleLoggingLevel"/>.
        /// </summary>
        public const string ConsoleTargetName = "console";

        #endregion

        #region Static Fields

        /// <summary>
        /// Lock object to synchronize access.
        /// </summary>
        private static readonly object LoggingLock = new object();

        /// <summary>
        /// Reference to the NLog instance that is used by the <see cref="LoggingHelper"/>.
        /// </summary>
        private static Logger nlogInstance;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="nlogInstance"/>.
        /// If it is null, a new <see cref="Logger"/> is created.
        /// </summary>
        private static Logger NlogInstance
        {
            get
            {
                lock (LoggingLock)
                {
                    if (nlogInstance == null)
                    {
                        nlogInstance = LogManager.GetLogger("LoggingHelper");
                    }

                    return nlogInstance;
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Logs the given <paramref name="text"/> at the provided verbosity.
        /// </summary>
        /// <param name="minimumVerbosity">The minimum verbosity a logger must have in order to receive the message.
        /// </param>
        /// <param name="text">The line to log.</param>
        public static void WriteLine(VerbosityLevel minimumVerbosity, StringIfNotFormattableStringAdapter text)
        {
            lock (LoggingLock)
            {
                // write log line.
                var formattedText = (string)text;
                switch (minimumVerbosity)
                {
                    case VerbosityLevel.Warn:
                        NlogInstance.Warn(formattedText);
                        break;
                    case VerbosityLevel.Info:
                        NlogInstance.Info(formattedText);
                        break;
                    case VerbosityLevel.Debug:
                        NlogInstance.Debug(formattedText);
                        break;
                    case VerbosityLevel.Trace:
                        NlogInstance.Trace(formattedText);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(minimumVerbosity),
                            minimumVerbosity,
                            "Enum value not handled.");
                }
            }
        }

        /// <summary>
        /// Writes the given <paramref name="formattableText"/>, formatted with <see cref="CultureInfo.InvariantCulture"/> and the given <paramref name="arguments"/>.
        /// </summary>
        /// <param name="minimumVerbosity">The minimum verbosity a logger must have in order to receive the message.
        /// </param>
        /// <param name="formattableText">A text with using wildcards as {0}, to fill with <paramref name="arguments"/>.</param>
        /// <param name="arguments">The arguments to fill in the <paramref name="formattableText"/>.</param>
        public static void WriteLine(
            VerbosityLevel minimumVerbosity,
            StringIfNotFormattableStringAdapter formattableText,
            params object[] arguments)
        {
            lock (LoggingLock)
            {
                WriteLine(minimumVerbosity, CultureInfo.InvariantCulture, formattableText, arguments);
            }
        }

        /// <summary>
        /// Writes the given <paramref name="formattableText"/>, formatted with <paramref name="culture"/> and the given <paramref name="arguments"/>.
        /// </summary>
        /// <param name="minimumVerbosity">The minimum verbosity a logger must have in order to receive the message.
        /// </param>
        /// <param name="culture">The culture to use.</param>
        /// <param name="formattableText">A text with using wildcards as {0}, to fill with <paramref name="arguments"/>.</param>
        /// <param name="arguments">The arguments to fill in the <paramref name="formattableText"/>.</param>
        public static void WriteLine(
            VerbosityLevel minimumVerbosity,
            CultureInfo culture,
            StringIfNotFormattableStringAdapter formattableText,
            params object[] arguments)
        {
            lock (LoggingLock)
            {
                try
                {
                    var text = string.Format(culture, formattableText, arguments);
                    WriteLine(minimumVerbosity, text);
                }
                catch (Exception e)
                {
                    WriteLine(VerbosityLevel.Warn, $"Error when formatting text: {e.Message}");
                    WriteLine(minimumVerbosity, formattableText);
                }
            }
        }

        /// <summary>
        /// Writes the <paramref name="format"/> using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="minimumVerbosity">The minimum verbosity a logger must have in order to receive the message.
        /// </param>
        /// <param name="format">The interpolated string to write.</param>
        public static void WriteLine(VerbosityLevel minimumVerbosity, FormattableString format)
        {
            lock (LoggingLock)
            {
                if (ReferenceEquals(format, null))
                {
                    WriteLine(minimumVerbosity, "");
                    return;
                }

                WriteLine(minimumVerbosity, CultureInfo.InvariantCulture, format);
            }
        }

        /// <summary>
        /// Writes the <paramref name="format"/> using <paramref name="culture"/>.
        /// </summary>
        /// <param name="minimumVerbosity">The minimum verbosity a logger must have in order to receive the message.
        /// </param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for formatting.</param>
        /// <param name="format">The interpolated string to write.</param>
        public static void WriteLine(VerbosityLevel minimumVerbosity, CultureInfo culture, FormattableString format)
        {
            lock (LoggingLock)
            {
                if (ReferenceEquals(culture, null))
                {
                    throw new ArgumentNullException(nameof(culture));
                }

                if (ReferenceEquals(format, null))
                {
                    WriteLine(minimumVerbosity, "");
                    return;
                }

                var text = format.ToString(culture);
                WriteLine(minimumVerbosity, text);
            }
        }

        /// <summary>
        /// Sets a common NLog configuration.
        /// Logs to console + <paramref name="outputFilename"/>.
        /// </summary>
        /// <param name="outputFilename">
        /// The output filename.
        /// It will be combined with <see cref="PathUtils.GetAbsolutePathFromCurrentDirectory"/>.
        /// </param>
        public static void Configure(string outputFilename)
        {
            // Step 1. Create configuration object
            var config = new LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget(ConsoleTargetName, consoleTarget);

            var fileTarget = new FileTarget();
            config.AddTarget("nlogOutput", fileTarget);

            // Step 3. Set target properties
            var exportPath = PathUtils.GetAbsolutePathFromCurrentDirectory(outputFilename);
            consoleTarget.Layout = @"[${level}] ${date:format=HH\:mm\:ss.fff}: ${message}";
            fileTarget.FileName = exportPath;
            fileTarget.Layout = @"[${level}][${logger}] ${date:format=HH\:mm\:ss.fff}: ${message}";

            // Step 4. Define rules
            var rule1 = new LoggingRule("*", LogLevel.Info, consoleTarget);
            config.LoggingRules.Add(rule1);

            var rule2 = new LoggingRule("*", LogLevel.Debug, fileTarget);
            config.LoggingRules.Add(rule2);

            // Step 5. Activate the configuration
            LogManager.Configuration = config;
        }

        /// <summary>
        /// Changes the logging level set for console.
        /// <para>Assumes there is a rule which applies to a single target named <see cref="ConsoleTargetName"/>.</para>
        /// </summary>
        /// <param name="verbosity">Specifies the verbosity the console should have.</param>
        [SuppressMessage(
            "NDepend",
            "ND2302:CautionWithListContains",
            Justification = "Contains will only be called if the list has a length of 1. Thus: Non-costly.")]
        public static void ChangeConsoleLoggingLevel(VerbosityLevel verbosity)
        {
            var console = LogManager.Configuration.FindTargetByName(ConsoleTargetName);
            var consoleRule = LogManager.Configuration.LoggingRules.FirstOrDefault(
                rule => rule.Targets.Count == 1 && rule.Targets.Contains(console));

            if (consoleRule == null)
            {
                return;
            }

            // Replace logging levels.
            foreach (var level in consoleRule.Levels)
            {
                consoleRule.DisableLoggingForLevel(level);
            }

            consoleRule.EnableLoggingForLevels(TransformToMaxLogLevel(verbosity), LogLevel.Fatal);
            LogManager.ReconfigExistingLoggers();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Transforms a <see cref="VerbosityLevel"/> value into a maximum <see cref="NLog.LogLevel"/>.
        /// </summary>
        /// <param name="verbosity">Level to transform.</param>
        /// <returns>The transformed <see cref="NLog.LogLevel"/>.</returns>
        private static LogLevel TransformToMaxLogLevel(VerbosityLevel verbosity)
        {
            switch (verbosity)
            {
                case VerbosityLevel.Warn:
                    return LogLevel.Warn;
                case VerbosityLevel.Info:
                    return LogLevel.Info;
                case VerbosityLevel.Debug:
                    return LogLevel.Debug;
                case VerbosityLevel.Trace:
                    return LogLevel.Trace;
                default: throw new ArgumentOutOfRangeException(nameof(verbosity), verbosity, "Enum value not handled.");
            }
        }

        #endregion
    }
}