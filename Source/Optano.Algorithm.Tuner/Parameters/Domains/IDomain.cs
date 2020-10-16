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
    /// Represents a parameter's domain.
    /// </summary>
    public interface IDomain
    {
        #region Public properties

        /// <summary>
        /// Gets the magnitude of this domain.
        /// </summary>
        double DomainSize { get; }

        /// <summary>
        /// Gets a value indicating whether this domain is an instance of <see cref="CategoricalDomain{T}"/>.
        /// </summary>
        bool IsCategoricalDomain { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Gets the default value, if one was specified by the user.
        /// Otherwise, a <see cref="GenerateRandomGeneValue"/> is returned.
        /// </summary>
        /// <returns>The default value.</returns>
        IAllele GetDefaultValue();

        /// <summary>
        /// Generates a gene value taken from the domain uniformly at random.
        /// </summary>
        /// <returns>The generated gene value.</returns>
        IAllele GenerateRandomGeneValue();

        /// <summary>
        /// Generates a gene value from the domain that results from mutating the given gene value.
        /// </summary>
        /// <param name="allele">
        /// The value to base the mutated value on. Has to be part of the domain.
        /// </param>
        /// <param name="variancePercentage">
        /// Mutation might utilize Gaussian distributions.
        /// This parameter defines the respective variance as a certain percentage of the variable's domain.
        /// Needs to be positive and at most 1.
        /// </param>
        /// <returns>
        /// The generated gene value.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the given value is not part of the domain
        /// or the given percentage is not a positive value at most 1.
        /// </exception>
        IAllele MutateGeneValue(IAllele allele, double variancePercentage);

        /// <summary>
        /// Checks if the given gene value is part of the domain.
        /// </summary>
        /// <param name="allele">
        /// Gene value to check.
        /// </param>
        /// <returns>
        /// Whether or not the given gene value is part of the domain.
        /// </returns>
        bool ContainsGeneValue(IAllele allele);

        /// <summary>
        /// Converts the given <paramref name="member"/> of this <see cref="IDomain"/> into a dobule value.
        /// </summary>
        /// <param name="member">
        /// A member of the current domain.
        /// </param>
        /// <returns>
        /// A double that represents the <paramref name="member"/>.
        /// </returns>
        double ConvertToDouble(IAllele member);

        /// <summary>
        /// Restores the <see cref="IAllele"/> from a previous double-conversion.
        /// Do not call this method for <see cref="IsCategoricalDomain"/>.
        /// </summary>
        /// <param name="value">
        /// The value to convert back.
        /// </param>
        /// <returns>
        /// The <see cref="IAllele"/>.
        /// </returns>
        IAllele ConvertBack(double value);

        #endregion
    }
}