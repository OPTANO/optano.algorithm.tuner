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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters.Domains;

    /// <summary>
    /// Representation of a converted categorical parameter.
    /// Note: A converted parameter may require more than one column. See <see cref="ColumnCount"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the <see cref="CategoricalDomain{T}"/> that of the converted categorical parameter.
    /// </typeparam>
    public class ConvertedCategory<T> : IConvertedCategory
    {
        #region Fields

        /// <summary>
        /// The <see cref="KeyToAllele"/> lock.
        /// </summary>
        private readonly object _keyToAlleleLock = new object();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertedCategory{T}"/> class.
        /// </summary>
        /// <param name="parameterName">
        /// The name of the represented parameter. 
        /// </param>
        /// <param name="categoryValues">
        /// The column representation for each <typeparamref name="T"/> in <see cref="CategoricalDomain{T}.PossibleValues"/>.
        /// </param>
        /// <param name="underlyingDomain">
        /// The underlying <see cref="CategoricalDomain{T}"/>.
        /// </param>
        public ConvertedCategory(string parameterName, Dictionary<T, double[]> categoryValues, CategoricalDomain<T> underlyingDomain)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException("String \"parameterName\" must not be null, empty, or whitespace.", nameof(parameterName));
            }

            if (categoryValues is null)
            {
                throw new ArgumentNullException(nameof(categoryValues));
            }

            var prevLength = -1;
            foreach (var mappingValuePair in categoryValues)
            {
                if (mappingValuePair.Key == null || (prevLength >= 0 && prevLength != mappingValuePair.Value.Length))
                {
                    throw new ArgumentException("All category values need to be initialized and of the same length.", nameof(categoryValues));
                }

                prevLength = mappingValuePair.Value.Length;
            }

            var distinctRepresentations = categoryValues.Select(cv => cv.Value).Distinct(new DoubleArrayEqualityComparer()).Count();
            if (categoryValues.Count != distinctRepresentations)
            {
                throw new ArgumentException("Double Representations of features need to be distinct.");
            }

            this.ParameterName = parameterName;
            this.UnderlyingCategoricalDomain = underlyingDomain ?? throw new ArgumentException("Domain musn't be null.", nameof(underlyingDomain));
            this.CategoryValues = categoryValues;
            this.ColumnCount = this.CategoryValues.Any() ? this.CategoryValues.First().Value.Length : -1;

            ((IConvertedCategory)this).Initialize();
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets the underlying categorical domain.
        /// </summary>
        public CategoricalDomain<T> UnderlyingCategoricalDomain { get; protected set; }

        /// <summary>
        /// Gets or sets the number of columns that is required in order to represent 
        /// a converted member of the <see cref="UnderlyingCategoricalDomain"/>.
        /// </summary>
        public int ColumnCount { get; protected set; }

        /// <summary>
        /// Gets or sets the categorical parameter name.
        /// </summary>
        public string ParameterName { get; protected set; }

        #endregion

        #region Explicit Interface properties

        /// <summary>
        /// Gets the underlying categorical domain.
        /// </summary>
        IDomain IConvertedCategory.UnderlyingCategoricalDomain => this.UnderlyingCategoricalDomain;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the inverse category values.
        /// </summary>
        protected Dictionary<double[], T> InverseCategoryValues { get; set; }

        /// <summary>
        /// Gets or sets a cache to store restored alleles for a given key.
        /// </summary>
        protected Dictionary<double[], IAllele> KeyToAllele { get; set; }

        /// <summary>
        /// Gets the internal data structure that contains the column representation for each possible category value (by rank as index).
        /// </summary>
        private IReadOnlyDictionary<T, double[]> CategoryValues { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Get the column representation for a categorical value.
        /// </summary>
        /// <param name="categoricalValue">
        /// The value to convert into a <see cref="T:double[]"/> representation.
        /// </param>
        /// <returns>
        /// A shallow copy of the precomputed column representation.
        /// </returns>
        public double[] GetColumnRepresentation(T categoricalValue)
        {
            if (!this.CategoryValues.ContainsKey(categoricalValue))
            {
                throw new ArgumentException(
                    $"Value {categoricalValue} is not valid for the underlying categorical domain.",
                    nameof(categoricalValue));
            }

            return this.CategoryValues[categoricalValue];
        }

        /// <summary>
        /// Casts the <paramref name="categoricalValue"/> to <typeparamref name="T"/> and returns <see cref="GetColumnRepresentation(T)"/>.
        /// </summary>
        /// <param name="categoricalValue">
        /// The value to represent as <see cref="V:double[]"/>.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/>.
        /// </returns>
        public double[] GetColumnRepresentation(object categoricalValue)
        {
            return this.GetColumnRepresentation((T)categoricalValue);
        }

        /// <summary>
        /// Returns the domain value that is represented by <paramref name="key"/> as <see cref="IAllele"/>.
        /// </summary>
        /// <param name="key">
        /// The column representation of the requested domain value.
        /// </param>
        /// <returns>
        /// The domain value as <see cref="IAllele"/>.
        /// </returns>
        public IAllele GetDomainValueAsAllele(double[] key)
        {
            lock (this._keyToAlleleLock)
            {
                // convert the domain member value to allele, if not already done
                if (!this.KeyToAllele.ContainsKey(key))
                {
                    // get the value represented as domain member
                    var value = this.GetDomainValue(key);

                    var allele = new Allele<T>(value);
                    this.KeyToAllele.Add(key, allele);
                }

                return this.KeyToAllele[key];
            }
        }

        #endregion

        #region Explicit Interface Methods

        /// <summary>
        /// Initializes the inverse value dictionary.
        /// </summary>
        void IConvertedCategory.Initialize()
        {
            if (this.CategoryValues == null)
            {
                return;
            }

            var reversedCategoryValues = new Dictionary<double[], T>(new DoubleArrayEqualityComparer());

            foreach (var categoryRank in this.CategoryValues)
            {
                reversedCategoryValues.Add(categoryRank.Value, categoryRank.Key);
            }

            this.InverseCategoryValues = reversedCategoryValues;
            this.KeyToAllele = new Dictionary<double[], IAllele>(this.InverseCategoryValues.Count, new DoubleArrayEqualityComparer());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Retrieves the domain value for a given column representation of that value.
        /// </summary>
        /// <param name="key">
        /// The column-representation.
        /// </param>
        /// <returns>
        /// The <typeparamref name="T"/> represented by <paramref name="key"/>.
        /// </returns>
        private T GetDomainValue(double[] key)
        {
            if (key == null || key.Length != this.ColumnCount)
            {
                throw new ArgumentException($"Nake sure to provide an array of length {this.ColumnCount}.", nameof(key));
            }

            if (!this.InverseCategoryValues.ContainsKey(key))
            {
                throw new ArgumentException(
                    $"Key is not a representation for any of {typeof(CategoricalDomain<T>).Name}'s possible values.\r\nYou might encounter this Exception if you chose to drop a categorical domain. However, there is no need for you to call this method. Unknown Key: [{string.Join(",", key.Select(k => k.ToString(CultureInfo.InvariantCulture)))}]",
                    nameof(key));
            }

            return this.InverseCategoryValues[key];
        }

        #endregion
    }
}