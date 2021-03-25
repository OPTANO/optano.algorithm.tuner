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

namespace Optano.Algorithm.Tuner.Parameters.ParameterConverters
{
    using System;

    using Optano.Algorithm.Tuner.Parameters.Domains;

    /// <summary>
    /// Encoder that produces a binary encoding for categorical domains.
    /// See: http://www.kdnuggets.com/2015/12/beyond-one-hot-exploration-categorical-variables.html.
    /// </summary>
    public class CategoricalBinaryEncoding : CategoricalEncodingBase
    {
        #region Methods

        /// <summary>
        /// Converts the <see paramref="indexToConvert"/> into a bit representation.
        /// </summary>
        /// <param name="indexToConvert">The index to convert.</param>
        /// <param name="colCount">The target column count. Make sure that it can fit the representation <see paramref="indexToConvert"/> in base 2.</param>
        /// <returns>The columns of the binary representation of <paramref name="indexToConvert"/>.</returns>
        internal static double[] ConvertToBits(int indexToConvert, int colCount)
        {
            var result = new double[colCount];
            var i = 0;
            try
            {
                while (indexToConvert > 0)
                {
                    var bit = indexToConvert % 2;
                    result[i++] = bit;
                    indexToConvert /= 2;
                }
            }
            catch (IndexOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException(
                    $"The target column count of {colCount} is too small to fit the bit representation of {indexToConvert} into it.",
                    e);
            }

            return result;
        }

        /// <summary>
        /// Gets the number of columns that would be generated, if dom was encoded with this encoding.
        /// </summary>
        /// <param name="dom">The domain to test.</param>
        /// <returns>The number of required columns.</returns>
        protected override int NumberOfGeneratedColumnsForCategoricalDomain(IDomain dom)
        {
            if (dom.IsCategoricalDomain)
            {
                return Math.Max(1, (int)Math.Ceiling(Math.Log(dom.DomainSize, 2)));
            }

            throw new InvalidOperationException($"Domain {dom} is not a categorical domain!");
        }

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
            return ConvertToBits(index, columnCount);
        }

        #endregion
    }
}