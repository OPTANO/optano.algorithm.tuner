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

namespace Optano.Algorithm.Tuner.Parameters.Domains
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;

    /// <summary>
    /// A domain that consists of a set of possible values.
    /// </summary>
    /// <typeparam name="T">The values' type.</typeparam>
    public class CategoricalDomain<T> : DomainBase<T>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoricalDomain{T}" /> class.
        /// </summary>
        /// <param name="possibleValues">All values the domain consists of.</param>
        /// <param name="defaultValue">The optional default value.</param>
        public CategoricalDomain(List<T> possibleValues, Allele<T>? defaultValue = null)
            : base(defaultValue)
        {
            this.PossibleValues = new List<T>(possibleValues).AsReadOnly();

            if (defaultValue.HasValue && !this.ContainsGeneValue(defaultValue.Value))
            {
                throw new ArgumentException($"{defaultValue.Value} is not a member of this domain.");
            }
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets all possible values of the domain.
        /// </summary>
        public IReadOnlyList<T> PossibleValues { get; }

        /// <summary>
        /// Gets the domain size.
        /// </summary>
        public override double DomainSize
        {
            get
            {
                return this.PossibleValues.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this domain is an instance of <see cref="CategoricalDomain{T}"/>.
        /// </summary>
        public override bool IsCategoricalDomain
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Convert a double-encoding back to the underlying domain value.
        /// </summary>
        /// <param name="value">
        /// The value to convert back.
        /// </param>
        /// <returns>
        /// Nothing. Method not supported for <see cref="CategoricalDomain{T}"/>.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Use a <see cref="CategoricalEncodingBase"/> to convert categorical domain values.
        /// </exception>
        public override IAllele ConvertBack(double value)
        {
            throw new NotSupportedException("Please use the ICategoricalEncoding to convert back a categorical value!");
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this domain.
        /// </summary>
        /// <returns>A <see cref="string" /> representation of the domain.</returns>
        public override string ToString()
        {
            return "{" + string.Join(", ", this.PossibleValues) + "}";
        }

        /// <summary>
        /// Converts the given <paramref name="member"/> of this <see cref="IDomain"/> into a dobule value.
        /// </summary>
        /// <param name="member">A member of the current domain.</param>
        /// <returns>A double that represents the <paramref name="member"/>.</returns>
        [SuppressMessage(
            "NDepend",
            "ND1203:CheckIfOverridesOfMethodShouldCallBase.Method",
            Justification = "Even though this violates Liskov's Substitution Principle, we explicitly want to prevent anyone from calling ConvertToDouble on this type directly. The conversion needs to be done by using Optano.Algorithm.Tuner.Parameters.ParameterConverters.CategoricalEncodingBase.")]
        public override double ConvertToDouble(IAllele member)
        {
            throw new NotSupportedException("Please use the ICategoricalEncoding to convert a categorical value to double!");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Generates a value from the domain uniformly at random.
        /// </summary>
        /// <returns>The generated value.</returns>
        protected override T GenerateRandomValue()
        {
            return this.PossibleValues[Randomizer.Instance.Next(this.PossibleValues.Count)];
        }

        /// <summary>
        /// Generates a value from the domain that results from mutating the given value.
        /// In the case of <see cref="CategoricalDomain{T}" />, this is just a random value from the domain.
        /// </summary>
        /// <param name="value">
        /// The value to base the mutated value on.
        /// Ignored in the case of <see cref="CategoricalDomain{T}" />.
        /// </param>
        /// <param name="variancePercentage">
        /// Mutation might utilize Gaussian distributions.
        /// This parameter defines the respective variance as a certain percentage of the variable's domain.
        /// Ignored in the case of <see cref="CategoricalDomain{T}" />.
        /// </param>
        /// <returns>The generated value.</returns>
        protected override T Mutate(T value, double variancePercentage)
        {
            return this.GenerateRandomValue();
        }

        /// <summary>
        /// Checks if the given value is part of the domain.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>Whether or not the value is part of the domain.</returns>
        protected override bool Contains(T value)
        {
            return this.PossibleValues.Contains(value);
        }

        /// <summary>
        /// Returns a double-respresentation for the given <paramref name="member"/>.
        /// </summary>
        /// <param name="member">
        /// The member to convert to double.
        /// </param>
        /// <returns>
        /// Method is not supported by <see cref="CategoricalDomain{T}"/>.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Use a <see cref="CategoricalEncodingBase"/> to convert categorical domain values.
        /// </exception>
        protected override double ConvertMemberToDouble(T member)
        {
            throw new NotSupportedException("Please use the ICategoricalEncoding to convert a categorical value to double!");
        }

        #endregion
    }
}