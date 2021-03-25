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

    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;

    using Xunit;

    /// <summary>
    /// Contains tests that should be executed for all classes inheriting from
    /// <see cref="HelpSupportingArgumentParserBase"/>.
    /// </summary>
    public abstract class HelpSupportingArgumentParserBaseTest
    {
        #region Properties

        /// <summary>
        /// Gets the <see cref="HelpSupportingArgumentParserBase"/> used in tests.
        /// </summary>
        protected abstract HelpSupportingArgumentParserBase Parser { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that accessing <see cref="HelpSupportingArgumentParserBase.HelpTextRequested"/> before calling
        /// <see cref="HelpSupportingArgumentParserBase.ParseArguments(string[])"/> throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void AccessingHelpTextRequestedBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this.Parser.HelpTextRequested);
        }

        /// <summary>
        /// Verifies that accessing <see cref="HelpSupportingArgumentParserBase.AdditionalArguments"/> before calling
        /// <see cref="HelpSupportingArgumentParserBase.ParseArguments(string[])"/> throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void AccessingAdditionalArgumentsBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this.Parser.AdditionalArguments);
        }

        /// <summary>
        /// Checks that calling <see cref="HelpSupportingArgumentParserBase.ParseArguments(string[])"/> with --help
        /// does not throw an exception even if invalid arguments are given.
        /// </summary>
        [Fact]
        public void HelpLongOptionPreventsExceptions()
        {
            string[] args = new string[]
                                {
                                    "--help",
                                    "--invaliiiiid",
                                };
            this.Parser.ParseArguments(args);
            Assert.True(this.Parser.HelpTextRequested, "Help text should have been requested.");
        }

        /// <summary>
        /// Checks that calling <see cref="HelpSupportingArgumentParserBase.ParseArguments(string[])"/> with -h does
        /// not throw an exception even if invalid arguments are given.
        /// </summary>
        [Fact]
        public void HelpShortOptionPreventsExceptions()
        {
            string[] args = new string[]
                                {
                                    "-h",
                                    "--invaliiiiid",
                                };
            this.Parser.ParseArguments(args);
            Assert.True(this.Parser.HelpTextRequested, "Help text should have been requested.");
        }

        #endregion
    }
}