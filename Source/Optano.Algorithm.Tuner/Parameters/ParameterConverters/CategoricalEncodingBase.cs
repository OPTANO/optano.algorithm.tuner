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
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Parameters.Domains;

    /// <summary>
    /// Abstract base class for various encodings of categorical domains.
    /// </summary>
    public abstract class CategoricalEncodingBase
    {
        #region Public Methods and Operators

        /// <summary>
        /// Method to convert a given categorical domain into a sepecific <see cref="CategoricalEncodingBase"/>.
        /// See: http://www.kdnuggets.com/2015/12/beyond-one-hot-exploration-categorical-variables.html.
        /// </summary>
        /// <typeparam name="T">
        /// Domain value type.
        /// </typeparam>
        /// <param name="parameterName">
        /// Name of the parameter that uses <paramref name="domain"/>.
        /// </param>
        /// <param name="domain">
        /// The <see cref="DomainBase{T}"/>.
        /// </param>
        /// <returns>
        /// The <see cref="ConvertedCategory{T}"/>.
        /// </returns>
        public ConvertedCategory<T> Encode<T>(string parameterName, CategoricalDomain<T> domain)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException("String \"parameterName\" must not be null, empty, or whitespace.", nameof(parameterName));
            }

            if (ReferenceEquals(domain, null))
            {
                throw new ArgumentNullException(nameof(domain));
            }

            this.ValidateDomain(domain);
            var categoryValues = domain.PossibleValues.ToArray();
            var categoryToDoubleRepresentation = new Dictionary<T, double[]>();
            var domainRepresentationLength = this.NumberOfGeneratedColumns(domain);

            for (var index = 0; index < categoryValues.Length; index++)
            {
                var currentCategoricalValue = categoryValues[index];
                var singleValueEncoding = this.EncodeNextValue(index, domainRepresentationLength);
                categoryToDoubleRepresentation.Add(currentCategoricalValue, singleValueEncoding);
            }

            var categoryEncoding = new ConvertedCategory<T>(parameterName, categoryToDoubleRepresentation, domain);
            return categoryEncoding;
        }

        /// <summary>
        /// Gets the number of columns that will be created by a <see cref="GenomeTransformation{TCategoricalEncoder}"/> using this <see cref="CategoricalEncodingBase"/>.
        /// </summary>
        /// <param name="domain">
        /// The domain to check for.
        /// </param>
        /// <returns>
        /// The number of columns required to represent the domain.
        /// </returns>
        public int NumberOfGeneratedColumns(IDomain domain)
        {
            if (domain == null)
            {
                throw new ArgumentNullException(nameof(domain));
            }

            if (!domain.IsCategoricalDomain)
            {
                return 1;
            }

            return this.NumberOfGeneratedColumnsForCategoricalDomain(domain);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Encodes the next member of the categorical domain.
        /// </summary>
        /// <param name="index">n-th member that should be encoded.</param>
        /// <param name="columnCount">Total length for double representation of domain members.</param>
        /// <returns>
        /// The <see cref="T:double[]"/> encoding for <see paramref="index"/>.
        /// </returns>
        protected abstract double[] EncodeNextValue(int index, int columnCount);

        /// <summary>
        /// Should return the number of columns required to represent the <see cref="CategoricalDomain{T}"/> <paramref name="domain"/>.
        /// </summary>
        /// <param name="domain">Should only be called with categorical domains.</param>
        /// <returns>The number of columns required to represent this domain as double[].</returns>
        protected abstract int NumberOfGeneratedColumnsForCategoricalDomain(IDomain domain);

        /// <summary>
        /// Checks whether the given <paramref name="dom"/> is a valid <see cref="CategoricalDomain{T}"/>.
        /// </summary>
        /// <param name="dom">
        /// The domain to check.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="dom"/> needs to be a <see cref="CategoricalDomain{T}"/> of finite size.
        /// </exception>
        protected void ValidateDomain(IDomain dom)
        {
            if (!dom.IsCategoricalDomain || double.IsPositiveInfinity(dom.DomainSize))
            {
                throw new InvalidOperationException($"Domain {dom.ToString()} is not a categorical domain.");
            }
        }

        #endregion
    }
}