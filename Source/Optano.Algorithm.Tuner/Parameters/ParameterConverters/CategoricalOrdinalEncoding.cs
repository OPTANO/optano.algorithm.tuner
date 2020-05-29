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

namespace Optano.Algorithm.Tuner.Parameters.ParameterConverters
{
    using System;

    using Optano.Algorithm.Tuner.Parameters.Domains;

    /// <summary>
    /// Represent a categorical domain as a single column. Assign an arbirtrary order to the elements of that category, which will be used as ordinal identifier.
    /// </summary>
    public class CategoricalOrdinalEncoding : CategoricalEncodingBase
    {
        #region Methods

        /// <summary>
        /// Encodes the next member of the categorical domain.
        /// </summary>
        /// <param name="index">n-th member that should be encoded.</param>
        /// <param name="columnCount">Total length for double representation of domain members.</param>
        /// <returns>
        /// The <see cref="T:double[]"/> encoding for <see paramref="index"/>.
        /// </returns>
        protected override double[] EncodeNextValue(int index, int columnCount)
        {
            return new double[] { index };
        }

        /// <summary>
        /// Gets the number of columns that would be generated, if dom was encoded with this encoding.
        /// </summary>
        /// <param name="dom">The domain to test. Needs to satisfy <see cref="IDomain.IsCategoricalDomain"/>. </param>
        /// <returns>The number of required columns.</returns>
        protected override int NumberOfGeneratedColumnsForCategoricalDomain(IDomain dom)
        {
            if (dom.IsCategoricalDomain)
            {
                return 1;
            }

            throw new InvalidOperationException($"Domain {dom} is not a categorical domain!");
        }

        #endregion
    }
}