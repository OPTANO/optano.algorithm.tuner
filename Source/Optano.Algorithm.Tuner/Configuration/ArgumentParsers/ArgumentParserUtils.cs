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

namespace Optano.Algorithm.Tuner.Configuration.ArgumentParsers
{
    using System;

    using NDesk.Options;

    using Optano.Algorithm.Tuner.Logging;

    /// <summary>
    /// Contains utility methods for argument parsing.
    /// </summary>
    public static class ArgumentParserUtils
    {
        #region Public Methods and Operators

        /// <summary>
        /// Parses command line arguments.
        /// </summary>
        /// <param name="argsParser">Parser to interpret the arguments.</param>
        /// <param name="args">Arguments to parse.</param>
        /// <returns>Whether or not execution should be continued.</returns>
        public static bool ParseArguments(HelpSupportingArgumentParserBase argsParser, string[] args)
        {
            try
            {
                argsParser.ParseArguments(args);
            }
            catch (OptionException e)
            {
                LoggingHelper.WriteLine(VerbosityLevel.Warn, "Invalid options: ");
                LoggingHelper.WriteLine(VerbosityLevel.Warn, e.Message);
                LoggingHelper.WriteLine(VerbosityLevel.Warn, $"Try adding '--help' for more information.");
                return false;
            }
            catch (AggregateException e)
            {
                LoggingHelper.WriteLine(VerbosityLevel.Warn, "One or more arguments could not be interpreted:");
                foreach (var exception in e.InnerExceptions)
                {
                    LoggingHelper.WriteLine(VerbosityLevel.Warn, exception.Message);
                }

                LoggingHelper.WriteLine(VerbosityLevel.Warn, $"Try adding '--help' for more information.");
                return false;
            }

            if (argsParser.HelpTextRequested)
            {
                argsParser.PrintHelp();
                return false;
            }

            return true;
        }

        #endregion
    }
}