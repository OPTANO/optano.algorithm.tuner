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
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.Parameters.Domains;

    /// <summary>
    /// A <see cref="CategoricalDomain{T}"/> can be converted, using different <see cref="CategoricalEncodingBase"/>.
    /// The resulting conversion is represented by this interface.
    /// Note: A converted parameter may require more than one column when converted into input for <see cref="IGeneticEngineering"/>.
    /// See <see cref="ColumnCount"/>.
    /// </summary>
    public interface IConvertedCategory
    {
        #region Public properties

        /// <summary>
        /// Gets the underlying categorical domain.
        /// </summary>
        IDomain UnderlyingCategoricalDomain { get; }

        /// <summary>
        /// Gets the number of columns that is required in order to represent a converted member of the <see cref="UnderlyingCategoricalDomain"/>.
        /// </summary>
        int ColumnCount { get; }

        /// <summary>
        /// Gets the parameter name.
        /// </summary>
        string ParameterName { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Computes the column representation for the given <paramref name="categoricalValue"/>.
        /// </summary>
        /// <param name="categoricalValue">
        /// The categorical value.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/> representation.
        /// The representation will always be of length <see cref="ColumnCount"/>.
        /// </returns>
        double[] GetColumnRepresentation(object categoricalValue);

        /// <summary>
        /// Convert a double representation back into a member of the respective <see cref="CategoricalDomain{T}"/> and
        /// wraps it into a <see cref="IAllele"/>.
        /// </summary>
        /// <param name="key">
        /// The key to convert back.
        /// Needs to be of length <see cref="ColumnCount"/>.
        /// </param>
        /// <returns>
        /// The <see cref="IAllele"/> that was encoded as <paramref name="key"/>.
        /// </returns>
        IAllele GetDomainValueAsAllele(double[] key);

        /// <summary>
        /// Initialize the <see cref="IConvertedCategory"/>.
        /// </summary>
        void Initialize();

        #endregion
    }
}