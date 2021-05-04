﻿#region Copyright (c) OPTANO GmbH

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

namespace Optano.Algorithm.Tuner.Tests
{
    using System.Linq;

    using NLog;

    using Xunit;

    /// <summary>
    /// Contains utility methods that are helpful in tests.
    /// </summary>
    public static class TestUtils
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <paramref name="value"/> equals <paramref name="expected"/> within a certain tolerance.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="expected">The expected value.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="additionalInformation">The additional information to write on failure.</param>
        public static void Equals(double value, double expected, double tolerance, string additionalInformation = null)
        {
            var message = $"{value} does not equal {expected} with tolerance {tolerance}";
            if (additionalInformation != null)
            {
                message += $"\n{additionalInformation}";
            }

            Assert.True(System.Math.Abs(value - expected) < tolerance, message);
        }

        /// <summary>
        /// Checks that the given sets contain the same items. Order does not matter.
        /// </summary>
        /// <typeparam name="T">The type of both sets' items.</typeparam>
        /// <param name="set1">First set.</param>
        /// <param name="set2">Second set.</param>
        /// <returns>Whether or not the sets are equivalent.</returns>
        public static bool SetsAreEquivalent<T>(System.Collections.Generic.IEnumerable<T> set1, System.Collections.Generic.IEnumerable<T> set2)
        {
            return set1.Count() == set2.Count() && !set1.Except(set2).Any();
        }

        /// <summary>
        /// Prints a list in the form { item1, item2, item3, item4 }.
        /// </summary>
        /// <typeparam name="T">The type of the list items.</typeparam>
        /// <param name="list">The list to print.</param>
        /// <returns>A <see cref="string"/> representing the given list.</returns>
        public static string PrintList<T>(System.Collections.Generic.IEnumerable<T> list)
        {
            return $"{{{string.Join(", ", list)}}}";
        }

        /// <summary>
        /// Checks output on invoking a certain action.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        /// <param name="check">Checks to do on the output.</param>
        public static void CheckOutput(System.Action action, System.Action<string> check)
        {
            // grab output to console.
            var originalOut = System.Console.Out;
            var writer = new System.IO.StringWriter();

            try
            {
                System.Console.SetOut(writer);
                action.Invoke();
                check.Invoke(writer.ToString());
            }
            finally
            {
                System.Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Initializes the <see cref="NLog"/> logger with a config that is suitable for tests.
        /// </summary>
        public static void InitializeLogger()
        {
            // Step 1. Create configuration object 
            var config = new NLog.Config.LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration 
            var consoleTarget = new NLog.Targets.ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);

            // Step 3. Set target properties 
            consoleTarget.Layout = @"${message}";

            // Step 4. Define rules
            var rule1 = new NLog.Config.LoggingRule("*", LogLevel.Trace, consoleTarget);
            config.LoggingRules.Add(rule1);

            // Step 5. Activate the configuration
            NLog.LogManager.Configuration = config;
        }

        #endregion
    }
}