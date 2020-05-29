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

    /// <summary>
    /// A domain that contains a minimum and maximum value.
    /// </summary>
    /// <typeparam name="T">The type contained in the domain. Must be comparable.</typeparam>
    public abstract class NumericalDomain<T> : DomainBase<T>, IDomain
        where T : IComparable
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NumericalDomain{T}" /> class.
        /// </summary>
        /// <param name="minimum">The minimum value.</param>
        /// <param name="maximum">The maximum value. Must be at least as large as minimum.</param>
        /// <exception cref="ArgumentException">Thrown if the specified maximum value is smaller than the minimum one.</exception>
        protected NumericalDomain(T minimum, T maximum)
        {
            if (maximum.CompareTo(minimum) < 0)
            {
                throw new ArgumentException($"Maximum ({maximum}) is larger than minimum ({minimum}).");
            }

            this.Minimum = minimum;
            this.Maximum = maximum;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the domain's minimum value.
        /// </summary>
        public T Minimum { get; }

        /// <summary>
        /// Gets the domain's maximum value.
        /// </summary>
        public T Maximum { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns a <see cref="string" /> that represents this domain.
        /// </summary>
        /// <returns>A <see cref="string" /> representation of the domain.</returns>
        public override string ToString()
        {
            return FormattableString.Invariant($"[{this.Minimum}, {this.Maximum}]");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Samples a value contained in the domain using a Gaussian distribution.
        /// You should usually call <see cref="NumericalDomain{T}.Mutate(T, double)" /> instead.
        /// </summary>
        /// <param name="mean">The distribution's mean.</param>
        /// <param name="variancePercentage">
        /// The distribution's variance is set to this percentage of the variable's
        /// domain. Needs to be positive and at most 1.
        /// </param>
        /// <returns>The sampled value.</returns>
        internal abstract T SampleFromGaussianDistribution(T mean, double variancePercentage);

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
        protected override T Mutate(T value, double variancePercentage)
        {
            if ((variancePercentage <= 0) || (variancePercentage > 1))
            {
                throw new ArgumentOutOfRangeException(
                    $"Variance percentage needs to be positive and at most 1, but was {variancePercentage}.");
            }

            return this.SampleFromGaussianDistribution(value, variancePercentage);
        }

        /// <summary>
        /// Checks if the given value is part of the domain.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>Whether or not the value is part of the domain.</returns>
        protected override bool Contains(T value)
        {
            return (value.CompareTo(this.Minimum) >= 0) && (value.CompareTo(this.Maximum) <= 0);
        }

        #endregion
    }
}