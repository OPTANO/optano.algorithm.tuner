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

namespace Optano.Algorithm.Tuner.Parameters.Domains
{
    using System;

    using Optano.Algorithm.Tuner.Genomes.Values;

    /// <summary>
    /// A domain for <see cref="int" />s with uniform distribution in log space.
    /// </summary>
    public class DiscreteLogDomain : NumericalDomain<int>
    {
        #region Fields

        /// <summary>
        /// The domain in log space.
        /// </summary>
        private readonly ContinuousDomain _logSpace;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteLogDomain" /> class.
        /// </summary>
        /// <param name="minimum">The minimum value.</param>
        /// <param name="maximum">The maximum value.</param>
        /// <param name="defaultValue">The optional default value.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if minimum value is not positive.</exception>
        public DiscreteLogDomain(int minimum, int maximum, Allele<int>? defaultValue = null)
            : base(minimum, maximum, defaultValue)
        {
            if (minimum <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    $"Logarithmically spaced integer domain must be positive, but minimum is {minimum}.");
            }

            this._logSpace = new ContinuousDomain(Math.Log(minimum), Math.Log(maximum));
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
                return Math.Round(Math.Exp(this._logSpace.Maximum) - Math.Exp(this._logSpace.Minimum)) + 1;
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
            return base.ToString() + " (in discrete log space)";
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
        internal override int SampleFromGaussianDistribution(int mean, double variancePercentage)
        {
            var meanInLogSpace = Math.Log(mean);
            return (int)Math.Round(
                Math.Exp(this._logSpace.SampleFromGaussianDistribution(meanInLogSpace, variancePercentage)),
                MidpointRounding.ToEven);
        }

        /// <summary>
        /// Generates a value from the domain. Selection is uniformly at random in the log space.
        /// </summary>
        /// <returns>A value from the domain.</returns>
        protected override int GenerateRandomValue()
        {
            return (int)Math.Round(Math.Exp(this._logSpace.GenerateRandomGeneValue().GetValue()), MidpointRounding.ToEven);
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