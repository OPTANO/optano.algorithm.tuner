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
    /// A domain for integer values.
    /// </summary>
    public class IntegerDomain : NumericalDomain<int>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegerDomain"/> class.
        /// </summary>
        /// <param name="minimum">
        /// The minimum value. Default is <see cref="int.MinValue"/>.
        /// </param>
        /// <param name="maximum">
        /// The maximum value. Default is <see cref="int.MaxValue"/>.
        /// </param>
        /// <param name="defaultValue">The optional default value.</param>
        public IntegerDomain(int minimum = int.MinValue, int maximum = int.MaxValue, Allele<int>? defaultValue = null)
            : base(minimum, maximum, defaultValue)
        {
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the domain size.
        /// </summary>
        public override double DomainSize
        {
            get
            {
                if (this.Minimum == int.MinValue || this.Maximum == int.MaxValue)
                {
                    return double.PositiveInfinity;
                }

                return this.Maximum - this.Minimum + 1;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates an <see cref="IAllele"/> with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <returns>The representing <see cref="IAllele"/>.</returns>
        public override IAllele ConvertBack(double value)
        {
            return new Allele<int>((int)value);
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this domain.
        /// </summary>
        /// <returns>A <see cref="string" /> representation of the domain.</returns>
        public override string ToString()
        {
            return "integers in " + base.ToString();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Samples a value contained in the domain using a Gaussian distribution.
        /// You should usually call <see cref="NumericalDomain{T}.Mutate(T, double)"/> instead.
        /// </summary>
        /// <param name="mean">
        /// The distribution's mean.
        /// </param>
        /// <param name="variancePercentage">
        /// The distribution's variance is set to this percentage of the variable's
        /// domain. Needs to be positive and at most 1.
        /// </param>
        /// <returns>
        /// The sampled value.
        /// </returns>
        internal override int SampleFromGaussianDistribution(int mean, double variancePercentage)
        {
            // If stdDev would be 0, just return the mean.
            if (this.Maximum == this.Minimum)
            {
                return mean;
            }

            // Otherwise, sample from a truncated normal...
            // Multiply before subtraction to prevent overflows.
            var stdDev = 10 * Math.Sqrt(((variancePercentage / 100) * this.Maximum) - ((variancePercentage / 100) * this.Minimum));
            var continuousSample = Randomizer.Instance.SampleFromTruncatedNormal(mean, stdDev, this.Minimum, this.Maximum);

            // ...and round down.
            return (int)Math.Round(continuousSample);
        }

        /// <summary>
        /// Generates a value from the domain uniformly at random.
        /// </summary>
        /// <returns>The generated value.</returns>
        protected override int GenerateRandomValue()
        {
            var exclusiveMaxValue = this.Maximum != int.MaxValue ? this.Maximum + 1 : this.Maximum;
            return Randomizer.Instance.Next(this.Minimum, exclusiveMaxValue);
        }

        /// <summary>
        /// Convert member to double.
        /// </summary>
        /// <param name="alleleValue">
        /// The allele value.
        /// </param>
        /// <returns>
        /// The <see cref="double"/> representation.
        /// </returns>
        protected override double ConvertMemberToDouble(int alleleValue)
        {
            return alleleValue;
        }

        #endregion
    }
}