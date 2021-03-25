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

    /// <summary>
    /// Argument Parser Helper that contains a generic <typeparamref name="TConfigBuilder"/>, which builds a Configuration for a given set of <see cref="T:string[]"/> arguments.
    /// </summary>
    /// <typeparam name="TConfigBuilder">
    /// The config builder used to build the current <see cref="ConfigurationBase"/>.
    /// </typeparam>
    public abstract class HelpSupportingArgumentParser<TConfigBuilder> : HelpSupportingArgumentParserBase, IConfigurationParser
        where TConfigBuilder : IConfigBuilder<ConfigurationBase>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpSupportingArgumentParser{TConfigBuilder}"/> class.
        /// </summary>
        /// <param name="allowAdditionalArguments">
        /// True, if unknown arguments should be allowed when calling <see cref="HelpSupportingArgumentParserBase.ParseArguments(string[])"/>.
        /// </param>
        protected HelpSupportingArgumentParser(bool allowAdditionalArguments = false)
            : base(allowAdditionalArguments)
        {
            this.InternalConfigurationBuilder = new TConfigBuilder();
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets a configuration builder configured with the parsed arguments.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called before <see cref="HelpSupportingArgumentParserBase.ParseArguments(string[])" />
        /// has been executed.
        /// </exception>
        public TConfigBuilder ConfigurationBuilder
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return this.InternalConfigurationBuilder;
            }
        }

        #endregion

        #region Explicit Interface properties

        /// <summary>
        /// Gets a configuration builder configured with the parsed arguments.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called before <see cref="HelpSupportingArgumentParserBase.ParseArguments(string[])" />
        /// has been executed.
        /// </exception>
        IConfigBuilder<ConfigurationBase> IConfigurationParser.ConfigurationBuilder => this.ConfigurationBuilder;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a  configuration builder which gets configured when parsing the arguments.
        /// <remarks>
        /// Same as <see cref="ConfigurationBuilder"/>, but without the check that parsing has to be done.
        /// </remarks>
        /// </summary>
        protected TConfigBuilder InternalConfigurationBuilder { get; }

        #endregion
    }
}