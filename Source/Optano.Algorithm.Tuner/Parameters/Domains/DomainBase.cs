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

    using Optano.Algorithm.Tuner.Genomes.Values;

    /// <summary>
    /// Represents a parameter's domain containing values of a certain type.
    /// </summary>
    /// <typeparam name="T">The parameter's type.</typeparam>
    public abstract class DomainBase<T> : IDomain
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainBase{T}" /> class.
        /// </summary>
        /// <param name="defaultValue">The optional default value.</param>
        protected DomainBase(Allele<T>? defaultValue)
        {
            if (defaultValue.HasValue)
            {
                this.DefaultValue = defaultValue.Value;
            }
        }

        #region Public properties

        /// <summary>
        /// Gets the optional default value.
        /// </summary>
        protected Allele<T>? DefaultValue { get; }

        /// <summary>
        /// Gets the Domain Size.
        /// </summary>
        public abstract double DomainSize { get; }

        /// <summary>
        /// Gets a value indicating whether this domain is an instance of <see cref="CategoricalDomain{T}"/>.
        /// </summary>
        public virtual bool IsCategoricalDomain => false;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Generates a gene value from the domain that results from mutating the given gene value.
        /// </summary>
        /// <param name="allele">The value to base the mutated value on. Has to be part of the domain.</param>
        /// <param name="variancePercentage">
        /// Mutation might utilize Gaussian distributions.
        /// This parameter defines the respective variance as a certain percentage of the variable's domain.
        /// Needs to be positive and at most 1.
        /// </param>
        /// <returns>The generated gene value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the given value is not part of the domain
        /// or the given percentage is not a positive value at most 1.
        /// </exception>
        public IAllele MutateGeneValue(IAllele allele, double variancePercentage)
        {
            if (!this.ContainsGeneValue(allele))
            {
                throw new ArgumentOutOfRangeException(
                    $"{allele.GetValue().GetType()} {allele} is not part of the {typeof(T)} domain {this}.");
            }

            var typedValue = (T)allele.GetValue();
            return new Allele<T>(this.Mutate(typedValue, variancePercentage));
        }

        /// <summary>
        /// Checks if the given gene value is part of the domain.
        /// </summary>
        /// <param name="allele">Gene value to check.</param>
        /// <returns>Whether or not the given gene value is part of the domain.</returns>
        public bool ContainsGeneValue(IAllele allele)
        {
            // Check given value's type.
            if (!(allele.GetValue() is T))
            {
                return false;
            }

            // Check if it is contained in domain.
            var typedValue = (T)allele.GetValue();
            return this.Contains(typedValue);
        }

        /// <inheritdoc />
        public IAllele GetDefaultValue()
        {
            if (this.DefaultValue.HasValue)
            {
                return this.DefaultValue;
            }

            return this.GenerateRandomGeneValue();
        }

        /// <summary>
        /// Generates a gene value from the domain uniformly at random.
        /// </summary>
        /// <returns>The generated gene value.</returns>
        public Allele<T> GenerateRandomGeneValue()
        {
            return new Allele<T>(this.GenerateRandomValue());
        }

        /// <summary>
        /// Converts the given <paramref name="member"/> of this <see cref="IDomain"/> into a double value.
        /// </summary>
        /// <param name="member">A member of the current domain.</param>
        /// <returns>A double that represents the <paramref name="member"/>.</returns>
        public virtual double ConvertToDouble(IAllele member)
        {
            // check if this is a member
            if (this.ContainsGeneValue(member))
            {
                return this.ConvertMemberToDouble((T)member.GetValue());
            }

            throw new ArgumentException($"Allele {member} is not a member of this Domain.", nameof(member));
        }

        /// <summary>
        /// Creates an <see cref="IAllele"/> with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <returns>
        /// The representing <see cref="IAllele"/>.
        /// </returns>
        public abstract IAllele ConvertBack(double value);

        #endregion

        #region Explicit Interface Methods

        /// <summary>
        /// Generates a gene value from the domain uniformly at random.
        /// </summary>
        /// <returns>The generated gene value as an <see cref="IAllele" />.</returns>
        IAllele IDomain.GenerateRandomGeneValue()
        {
            return this.GenerateRandomGeneValue();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Convert member to double.
        /// Only valid for non-<see cref="IsCategoricalDomain"/>s.
        /// </summary>
        /// <param name="alleleValue">
        /// The allele value.
        /// </param>
        /// <returns>
        /// The <see cref="double"/> representation.
        /// </returns>
        protected abstract double ConvertMemberToDouble(T alleleValue);

        /// <summary>
        /// Generates a value from the domain uniformly at random.
        /// </summary>
        /// <returns>The generated value.</returns>
        protected abstract T GenerateRandomValue();

        /// <summary>
        /// Generates a value from the domain that results from mutating the given value.
        /// </summary>
        /// <param name="value">The value to base the mutated value on.</param>
        /// <param name="variancePercentage">
        /// Mutation might utilize Gaussian distributions.
        /// This parameter defines the respective variance as a certain percentage of the variable's domain.
        /// Needs to be positive and at most 1.
        /// </param>
        /// <returns>The generated value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the given percentage
        /// is not a positive value at most 1.
        /// </exception>
        protected abstract T Mutate(T value, double variancePercentage);

        /// <summary>
        /// Checks if the given value is part of the domain.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>Whether or not the value is part of the domain.</returns>
        protected abstract bool Contains(T value);

        #endregion
    }
}