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

namespace Optano.Algorithm.Tuner.Tests.Parameters.Domains
{
    using System;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters.Domains;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="NumericalDomain{T}"/> class.
    /// </summary>
    public abstract class NumericalDomainTest : DomainBaseTest
    {
        #region Static Fields

        /// <summary>
        /// A minimum value often used in tests. Valid for all subtypes of <see cref="NumericalDomain{T}"/>.
        /// </summary>
        private static readonly int minimum = 2;

        /// <summary>
        /// A maximum value often used in tests. Valid for all subtypes of <see cref="NumericalDomain{T}"/>.
        /// </summary>
        private static readonly int maximum = 13;

        /// <summary>
        /// The variance percentage value needed for <see cref="IDomain.MutateGeneValue(IAllele, double)"/>.
        /// Use this field if the variance is irrelevant for the test.
        /// </summary>
        private static readonly double irrelevantVariance = 0.1;

        #endregion

        #region Fields

        /// <summary>
        /// The default <see cref="NumericalDomain{T}"/> to use in tests. Has to be initialized.
        /// </summary>
        private readonly IDomain _domain;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NumericalDomainTest"/> class.
        /// </summary>
        public NumericalDomainTest()
        {
            this._domain = this.CreateNumericalDomain(NumericalDomainTest.minimum, NumericalDomainTest.maximum);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Tests that the ToString method prints out the expected string
        /// containing the correct minimum and maximum values.
        /// </summary>
        public abstract void ToStringShowsMinimumAndMaximum();

        /// <summary>
        /// Test minimum and maximum value properties.
        /// </summary>
        public abstract void MinAndMaxCorrectlySet();

        /// <summary>
        /// Test that setting a higher minimum than maximum throws an error.
        /// </summary>
        [Fact]
        public void EmptyDomainThrowsException()
        {
            Assert.Throws<ArgumentException>(() => this.CreateNumericalDomain(minimum: NumericalDomainTest.maximum, maximum: NumericalDomainTest.minimum));
        }

        /// <summary>
        /// Checks that <see cref="IDomain.DomainSize"/> correctly returns the domain's magnitude.
        /// </summary>
        public abstract override void DomainSizeIsCorrect();

        /// <summary>
        /// Checks that <see cref="IDomain.MutateGeneValue(IAllele, double)"/> throws an
        /// <see cref="ArgumentException"/> if called with a type that is not a subtype of the domain objects' type.
        /// </summary>
        [Fact]
        public override void MutateGeneValueThrowsExceptionForWrongType()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._domain.MutateGeneValue(new Allele<string>("type"), NumericalDomainTest.irrelevantVariance));
        }

        /// <summary>
        /// Checks that <see cref="IDomain.MutateGeneValue(IAllele, double)"/> throws an
        /// <see cref="ArgumentOutOfRangeException"/> if called with a gene value that is not contained in the domain.
        /// </summary>
        [Fact]
        public override void MutateGeneValueThrowsExceptionForInvalidValue()
        {
            IAllele geneValue = this.WrapInAlleleWithCorrectType(NumericalDomainTest.maximum + 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => this._domain.MutateGeneValue(geneValue, NumericalDomainTest.irrelevantVariance));
        }

        /// <summary>
        /// Checks that <see cref="IDomain.MutateGeneValue(IAllele, double)"/> throws an
        /// <see cref="ArgumentOutOfRangeException"/> if called with a variance percentage of 0.
        /// </summary>
        [Fact]
        public void MutateGeneValueThrowsExceptionForZeroVariancePercentage()
        {
            IAllele geneValue = this.WrapInAlleleWithCorrectType(NumericalDomainTest.minimum);
            Assert.Throws<ArgumentOutOfRangeException>(() => this._domain.MutateGeneValue(geneValue, variancePercentage: 0));
        }

        /// <summary>
        /// Checks that <see cref="IDomain.MutateGeneValue(IAllele, double)"/> does not throw an
        /// error if called with a variance percentage of 1.
        /// </summary>
        [Fact]
        public void MutateGeneValueDoesNotThrowForVariancePercentage100()
        {
            IAllele geneValue = this.WrapInAlleleWithCorrectType(NumericalDomainTest.minimum);
            this._domain.MutateGeneValue(geneValue, variancePercentage: 1);
        }

        /// <summary>
        /// Checks that <see cref="IDomain.ContainsGeneValue(IAllele)"/> returns false if called with a type
        /// that is not a subtype of the domain objects' type.
        /// </summary>
        [Fact]
        public override void ContainsGeneValueReturnsFalseForWrongType()
        {
            Assert.False(
                this._domain.ContainsGeneValue(new Allele<string>("type")),
                $"Wrong type is categorized as legal gene value.");
        }

        /// <summary>
        /// Checks that <see cref="IDomain.ContainsGeneValue(IAllele)"/> returns true for minimum value.
        /// </summary>
        [Fact]
        public void ContainsGeneValueReturnsTrueForMinimum()
        {
            IAllele minimumValue = this.WrapInAlleleWithCorrectType(NumericalDomainTest.minimum);
            Assert.True(
                this._domain.ContainsGeneValue(minimumValue),
                $"{minimumValue} supposedly not contained in {this._domain}.");
        }

        /// <summary>
        /// Checks that <see cref="IDomain.ContainsGeneValue(IAllele)"/> returns true for maximum value.
        /// </summary>
        [Fact]
        public void ContainsGeneValueReturnsTrueForMaximum()
        {
            IAllele maximumValue = this.WrapInAlleleWithCorrectType(NumericalDomainTest.maximum);
            Assert.True(
                this._domain.ContainsGeneValue(maximumValue),
                $"{maximumValue} supposedly not contained in {this._domain}.");
        }

        /// <summary>
        /// Checks that <see cref="IDomain.ContainsGeneValue(IAllele)"/> returns false for too low values.
        /// </summary>
        [Fact]
        public void ContainsGeneValueReturnsFalseForLowerValue()
        {
            IAllele lowValue = this.WrapInAlleleWithCorrectType(NumericalDomainTest.minimum - 1);
            Assert.False(
                this._domain.ContainsGeneValue(lowValue),
                $"{lowValue} supposedly contained in {this._domain}.");
        }

        /// <summary>
        /// Checks that <see cref="IDomain.ContainsGeneValue(IAllele)"/> return false for too high values.
        /// </summary>
        [Fact]
        public void ContainsGeneValueReturnsFalseForHigherValue()
        {
            IAllele highValue = this.WrapInAlleleWithCorrectType(NumericalDomainTest.maximum + 1);
            Assert.False(
                this._domain.ContainsGeneValue(highValue),
                $"{highValue} supposedly contained in {this._domain}.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a numerical domain of the type that is being tested.
        /// </summary>
        /// <param name="minimum">The minimum value.</param>
        /// <param name="maximum">The maximum value.</param>
        /// <returns>The created domain.</returns>
        protected abstract IDomain CreateNumericalDomain(int minimum, int maximum);

        /// <summary>
        /// Wraps the given value into an <see cref="IAllele"/> in a way that
        /// the type being tested can handle.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>An <see cref="IAllele"/> containing the value.</returns>
        protected abstract IAllele WrapInAlleleWithCorrectType(int value);

        #endregion
    }
}