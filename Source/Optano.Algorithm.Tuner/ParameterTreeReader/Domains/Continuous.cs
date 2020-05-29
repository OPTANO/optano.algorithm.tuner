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

// ReSharper disable once CheckNamespace
// Namespace is required due to partial class.
namespace Optano.Algorithm.Tuner.ParameterTreeReader.Elements
{
    using System;

    using Optano.Algorithm.Tuner.Parameters.Domains;

    /// <summary>
    /// Represents a continuous parameter domain as defined by an XML document.
    /// </summary>
    /// <remarks>This is the part of the class that was *not* automatically generated and is responsible for converting
    /// the class mirroring the XML element into the behavior implementing class located at Data/Parameters.
    ///
    /// The class definition is marked as generated because it cannot be changed and StyleCop warnings
    /// (e.g. capitalization) can't be fixed here.
    /// </remarks>

    #region Generated Code

#pragma warning disable SA1300 // Element should begin with upper-case letter
    public partial class continuous
#pragma warning restore SA1300 // Element should begin with upper-case letter

        #endregion

    {
        #region Properties

        /// <summary>
        /// Gets the <see cref="Type"/> of values in the domain, i. e. <see cref="double"/>.
        /// </summary>
        internal override Type ValueType => typeof(double);

        #endregion

        #region Methods

        /// <summary>
        /// Converts this domain to a <see cref="ContinuousDomain"/> or <see cref="LogDomain"/>.
        /// </summary>
        /// <returns>The converted <see cref="ContinuousDomain"/> or <see cref="LogDomain"/>.</returns>
        internal override IDomain ConvertToParameterTreeDomain()
        {
            if (this.log)
            {
                return new LogDomain(this.start, this.end);
            }

            return new ContinuousDomain(this.start, this.end);
        }

        #endregion
    }
}