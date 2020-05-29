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

namespace Optano.Algorithm.Tuner.Genomes.Values
{
    using System.Globalization;

    /// <summary>
    /// A gene's typed value.
    /// </summary>
    /// <typeparam name="T">The value's type.</typeparam>
    public struct Allele<T> : IAllele
    {
        #region Fields

        /// <summary>
        /// The value itself.
        /// </summary>
        private readonly T _value;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Allele{T}" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Allele(T value)
        {
            this._value = value;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Gets the gene's value.
        /// </summary>
        /// <returns>The gene's value.</returns>
        public T GetValue()
        {
            return this._value;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this gene value.
        /// </summary>
        /// <returns>A <see cref="string" /> presentation of the gene's value.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}", this._value);
        }

        /// <summary>
        /// Computes the hash code for this allele.
        /// Hash only depends on <see cref="_value"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return this._value?.GetHashCode() ?? 0;
        }

        #endregion

        #region Explicit Interface Methods

        /// <summary>
        /// Gets the gene's value as an <see cref="object" />.
        /// </summary>
        /// <returns>The gene's value.</returns>
        object IAllele.GetValue()
        {
            return this.GetValue();
        }

        #endregion
    }
}