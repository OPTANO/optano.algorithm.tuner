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

    using Accord.Statistics.Distributions.Univariate;
    using Accord.Statistics.Testing;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters.Domains;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="IntegerDomain"/>.
    /// </summary>
    public class IntegerDomainTest : NumericalDomainTest
    {
        #region Static Fields

        /// <summary>
        /// The number of iterations in tests that test random methods.
        /// </summary>
        private readonly static int triesForRandomTests = 1000;

        /// <summary>
        /// Minimum value used in tests.
        /// </summary>
        private readonly static int minimum = -1;

        /// <summary>
        /// Maximum value used in tests.
        /// </summary>
        private readonly static int maximum = 5;

        #endregion

        #region Fields

        /// <summary>
        /// Integer domain. Has to be initialized for each test.
        /// </summary>
        private IntegerDomain _integerDomain;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Test minimum and maximum value properties.
        /// </summary>
        [Fact]
        public override void MinAndMaxCorrectlySet()
        {
            this._integerDomain = new IntegerDomain(IntegerDomainTest.minimum, IntegerDomainTest.maximum);
            Assert.Equal(IntegerDomainTest.maximum, this._integerDomain.Maximum);
            Assert.Equal(IntegerDomainTest.minimum, this._integerDomain.Minimum);
        }

        /// <summary>
        /// Checks that the default minimum value is <see cref="int.MinValue"/>.
        /// </summary>
        [Fact]
        public void TestDefaultMinimumIsMinimumInteger()
        {
            this._integerDomain = new IntegerDomain(maximum: IntegerDomainTest.maximum);
            Assert.Equal(int.MinValue, this._integerDomain.Minimum);
        }

        /// <summary>
        /// Checks that the default maximum value is <see cref="int.MaxValue"/>.
        /// </summary>
        [Fact]
        public void TestDefaultMaximumIsMaximumInteger()
        {
            this._integerDomain = new IntegerDomain(minimum: IntegerDomainTest.minimum);
            Assert.Equal(int.MaxValue, this._integerDomain.Maximum);
        }

        /// <summary>
        /// Checks that <see cref="IDomain.DomainSize"/> correctly returns the domain's magnitude.
        /// </summary>
        [Fact]
        public override void DomainSizeIsCorrect()
        {
            var boundedDomain = new IntegerDomain(IntegerDomainTest.minimum, IntegerDomainTest.maximum);
            Assert.Equal(
                IntegerDomainTest.maximum - IntegerDomainTest.minimum + 1,
                boundedDomain.DomainSize);

            var upperBoundedDomain = new IntegerDomain(maximum: IntegerDomainTest.maximum);
            Assert.Equal(
                double.PositiveInfinity,
                upperBoundedDomain.DomainSize);

            var lowerBoundedDomain = new IntegerDomain(minimum: IntegerDomainTest.minimum);
            Assert.Equal(
                double.PositiveInfinity,
                lowerBoundedDomain.DomainSize);

            var unbounded = new IntegerDomain();
            Assert.Equal(
                double.PositiveInfinity,
                unbounded.DomainSize);
        }

        /// <summary>
        /// Checks that the random generator always returns a valid value.
        /// </summary>
        [Fact]
        public void RandomGenerationCreatesLegalGenes()
        {
            this._integerDomain = new IntegerDomain(IntegerDomainTest.minimum, IntegerDomainTest.maximum);

            // For a lot of tries:
            for (int i = 0; i < IntegerDomainTest.triesForRandomTests; i++)
            {
                // Check that the generated gene is legal.
                IAllele generated = this._integerDomain.GenerateRandomGeneValue();
                Assert.True(
                    this._integerDomain.ContainsGeneValue(generated),
                    $"Generated value {generated} is not a legal gene of {this._integerDomain}.");
            }
        }

        /// <summary>
        /// Uses the Chi-Squared test to verify that many calls of <see cref="IntegerDomain.GenerateRandomValue"/>
        /// produce a distribution that does not depart from the uniform distribution.
        /// </summary>
        [Fact]
        public void GenerateRandomGeneValueDoesNotDepartFromUniformDistribution()
        {
            this._integerDomain = new IntegerDomain(IntegerDomainTest.minimum, IntegerDomainTest.maximum);

            // Remember which values were hit for a lot of iterations.
            double[] observations = new double[IntegerDomainTest.triesForRandomTests];
            for (int i = 0; i < IntegerDomainTest.triesForRandomTests; i++)
            {
                observations[i] = this._integerDomain.GenerateRandomGeneValue().GetValue();
            }

            // Apply the Chi-Squared test.
            ChiSquareTest uniformTest = new ChiSquareTest(observations, new UniformDiscreteDistribution(IntegerDomainTest.minimum, IntegerDomainTest.maximum));
            Assert.False(
                uniformTest.Significant,
                $"Random generation was found to be not uniform by the Chi-Squared test with significance level {uniformTest.Size}.");
        }

        /// <summary>
        /// Tests that <see cref="IntegerDomain.ToString()"/> prints out the expected string
        /// containing the correct minimum and maximum values.
        /// </summary>
        [Fact]
        public override void ToStringShowsMinimumAndMaximum()
        {
            this._integerDomain = new IntegerDomain(IntegerDomainTest.minimum, IntegerDomainTest.maximum);
            Assert.Equal(
                $"integers in [{this._integerDomain.Minimum}, {this._integerDomain.Maximum}]",
                this._integerDomain.ToString());
        }

        /// <summary>
        /// Checks that the distribution created by multiple calls to
        /// <see cref="IDomain.MutateGeneValue(IAllele, double)"/> has the original value as mean
        /// and the specified variance / standard deviation.
        /// </summary>
        /// <remarks>This is only a rough test up to 10 percent.</remarks>
        [Fact]
        public void MutationDistributionMeanAndStandardDeviationAreRoughlyAsExpected()
        {
            // Build up unbounded domain.
            this._integerDomain = new IntegerDomain();

            // Fix value to mutate and the variance percentage corresponding to some expected values.
            int expectedStandardDeviation = 5;
            int expectedMean = 3;
            Allele<int> valueToMutate = new Allele<int>(expectedMean);
            double variancePercentage = Math.Pow(expectedStandardDeviation, 2) /
                                        ((double)this._integerDomain.Maximum - this._integerDomain.Minimum);

            // Collect results of a lot of mutations.
            double[] mutations = new double[IntegerDomainTest.triesForRandomTests];
            for (int i = 0; i < IntegerDomainTest.triesForRandomTests; i++)
            {
                mutations[i] =
                    (int)this._integerDomain.MutateGeneValue(valueToMutate, variancePercentage).GetValue();
            }

            // Create distribution.
            var distribution = new EmpiricalDistribution(mutations);

            // Check mean & standard deviation.
            Assert.True(
                Math.Abs(expectedMean - distribution.Mean) < 0.1 * expectedMean);
            Assert.True(
                Math.Abs(expectedStandardDeviation - distribution.StandardDeviation) < 0.1 * expectedStandardDeviation);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a <see cref="IntegerDomain"/>.
        /// </summary>
        /// <param name="minimum">The minimum value.</param>
        /// <param name="maximum">The maximum value.</param>
        /// <returns>The created domain.</returns>
        protected override IDomain CreateNumericalDomain(int minimum, int maximum)
        {
            return new IntegerDomain(minimum, maximum);
        }

        /// <summary>
        /// Wraps the given value into an <see cref="Allele{T}"/>.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>An <see cref="IAllele"/> containing the value.</returns>
        protected override IAllele WrapInAlleleWithCorrectType(int value)
        {
            return new Allele<int>(value);
        }

        #endregion
    }
}
