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

namespace Optano.Algorithm.Tuner.Configuration.ArgumentParsers
{
    using System.Collections.Generic;

    /// <summary>
    /// A parser to convert command line arguments into a <see cref="IConfigBuilder{ConfigurationBase}"/>.
    /// </summary>
    public interface IConfigurationParser
    {
        #region Public properties

        /// <summary>
        /// Gets a configuration builder configured with the parsed arguments.
        /// </summary>
        IConfigBuilder<ConfigurationBase> ConfigurationBuilder { get; }

        /// <summary>
        /// Gets the list of arguments that could not be parsed when calling <see cref="ParseArguments(string[])"/>.
        /// </summary>
        IEnumerable<string> AdditionalArguments { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Parses the provided arguments.
        /// </summary>
        /// <param name="args">
        /// Arguments to parse.
        /// </param>
        void ParseArguments(string[] args);

        #endregion
    }
}