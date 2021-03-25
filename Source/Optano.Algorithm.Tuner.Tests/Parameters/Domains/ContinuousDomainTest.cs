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

namespace Optano.Algorithm.Tuner.Tests.Parameters.Domains
{
    using System;

    using Accord.Statistics.Distributions.Univariate;
    using Accord.Statistics.Testing;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters.Domains;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="ContinuousDomain"/>. 
    /// </summary>
    public class ContinuousDomainTest : NumericalDomainTest
    {
        #region Static Fields

        /// <summary>
        /// The number of iterations in tests that test random methods.
        /// </summary>
        protected readonly static int TriesForRandomTests = 1000;

        /// <summary>
        /// Minimum value used in tests.
        /// </summary>
        private readonly static double minimum = -0.3;

        /// <summary>
        /// Maximum value used in tests.
        /// </summary>
        private readonly static double maximum = 5.2;

        #endregion

        #region Fields

        /// <summary>
        /// Continuous domain. Has to be initialized for each test.
        /// </summary>
        private ContinuousDomain _continuousDomain;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Test minimum and maximum value properties.
        /// </summary>
        [Fact]
        public override void MinAndMaxCorrectlySet()
        {
            this._continuousDomain = new ContinuousDomain(ContinuousDomainTest.minimum, ContinuousDomainTest.maximum);
            Assert.Equal(ContinuousDomainTest.maximum, this._continuousDomain.Maximum);
            Assert.Equal(ContinuousDomainTest.minimum, this._continuousDomain.Minimum);
        }

        /// <summary>
        /// Checks that the default minimum value is <see cref="double.MinValue"/>.
        /// </summary>
        [Fact]
        public void TestDefaultMinimumIsMinimumDouble()
        {
            this._continuousDomain = new ContinuousDomain(maximum: ContinuousDomainTest.maximum);
            Assert.Equal(
                double.MinValue,
                this._continuousDomain.Minimum);
        }

        /// <summary>
        /// Checks that the default maximum value is <see cref="double.MaxValue"/>.
        /// </summary>
        [Fact]
        public void TestDefaultMaximumIsMaximumDouble()
        {
            this._continuousDomain = new ContinuousDomain(minimum: ContinuousDomainTest.minimum);
            Assert.Equal(
                double.MaxValue,
                this._continuousDomain.Maximum);
        }

        /// <summary>
        /// Checks that randomly generated values are contained in domain.
        /// </summary>
        [Fact]
        public void RandomGenerationStaysInDomain()
        {
            IDomain domain = new ContinuousDomain(ContinuousDomainTest.minimum, ContinuousDomainTest.maximum);
            // For a lot of tries:
            for (int i = 0; i < ContinuousDomainTest.TriesForRandomTests; i++)
            {
                // Check that the generated value is in the domain.
                IAllele generated = domain.GenerateRandomGeneValue();
                Assert.True(
                    domain.ContainsGeneValue(generated),
                    $"Generated value {generated} which is not contained in {domain}");
            }
        }

        /// <summary>
        /// Uses the Anderson-Darling test to verify that many calls of
        /// <see cref="ContinuousDomain.GenerateRandomValue"/> produce a distribution that does not depart from the
        /// uniform distribution.
        /// </summary>
        [Fact]
        public void GenerateRandomGeneValueDoesNotDepartFromUniformDistribution()
        {
            // Create domain.
            ContinuousDomain domain = new ContinuousDomain(ContinuousDomainTest.minimum, ContinuousDomainTest.maximum);

            // Collect results of value generation.
            double[] generatedValues = new double[ContinuousDomainTest.TriesForRandomTests];
            for (int i = 0; i < ContinuousDomainTest.TriesForRandomTests; i++)
            {
                Allele<double> generated = domain.GenerateRandomGeneValue();
                generatedValues[i] = generated.GetValue();
            }

            // Apply the Anderson-Darling test.
            AndersonDarlingTest uniformTest = new AndersonDarlingTest(
                generatedValues,
                new UniformContinuousDistribution(ContinuousDomainTest.minimum, ContinuousDomainTest.maximum));
            Assert.False(
                uniformTest.Significant,
                $"Random generation was found to be not uniform by the Anderson-Darling test with significance level of {uniformTest.Size}.");
        }

        /// <summary>
        /// Tests that <see cref="NumericalDomain{T}.ToString()"/> prints out the expected string
        /// containing the correct minimum and maximum values.
        /// </summary>
        [Fact]
        public override void ToStringShowsMinimumAndMaximum()
        {
            this._continuousDomain = new ContinuousDomain(ContinuousDomainTest.minimum, ContinuousDomainTest.maximum);
            Assert.Equal(
                FormattableString.Invariant($"[{this._continuousDomain.Minimum}, {this._continuousDomain.Maximum}]"),
                this._continuousDomain.ToString());
        }

        /// <summary>
        /// Checks that <see cref="IDomain.DomainSize"/> correctly returns the domain's magnitude.
        /// </summary>
        [Fact]
        public override void DomainSizeIsCorrect()
        {
            var boundedDomain = new ContinuousDomain(ContinuousDomainTest.minimum, ContinuousDomainTest.maximum);
            Assert.Equal(
                double.PositiveInfinity,
                boundedDomain.DomainSize);
        }

        /// <summary>
        /// Uses the Kolmogorov-Smirnov test to verify that the results of
        /// <see cref="NumericalDomain{T}.Mutate(T, double)"/> produce a distribution that does not depart
        /// from the respective normal distribution.
        /// </summary>
        [Fact]
        public void MutationDistributionDoesNotDepartFromNormalDistribution()
        {
            // Build up unbounded domain.
            this._continuousDomain = new ContinuousDomain();

            // Fix the value to mutate and the variance percentage.
            Allele<double> valueToMutate = new Allele<double>(3.4);
            // Divide by 2 to prevent overflows. Want: 0.5 / (max - min)
            double variancePercentage =
                (0.5 / 2) / ((this._continuousDomain.Maximum / 2) - (this._continuousDomain.Minimum / 2));

            // Collect results of a lot of mutations.
            double[] mutations = new double[ContinuousDomainTest.TriesForRandomTests];
            for (int i = 0; i < ContinuousDomainTest.TriesForRandomTests; i++)
            {
                mutations[i] =
                    (double)this._continuousDomain.MutateGeneValue(valueToMutate, variancePercentage).GetValue();
            }

            // Apply the Kolmogorov-Smirnov test.
            double stdDev = Math.Sqrt(0.5);
            KolmogorovSmirnovTest normalityTest = new KolmogorovSmirnovTest(
                sample: mutations,
                hypothesizedDistribution: new NormalDistribution(mean: valueToMutate.GetValue(), stdDev: stdDev));
            Assert.False(
                double.IsNaN(normalityTest.PValue) || normalityTest.Significant,
                $"Mutation was found to be not normal by the Kolmogorov-Smirnov test with significance level of {normalityTest.Size}.");
        }

        /// <summary>
        /// Checks that results of <see cref="DomainBase{T}.Mutate(T, double)"/> are contained in the domain.
        /// </summary>
        [Fact]
        public void MutationStaysInDomain()
        {
            // Initialize bounded domain.
            var domain = new ContinuousDomain(ContinuousDomainTest.minimum, ContinuousDomainTest.maximum);

            // Fix the value to mutate and the variance percentage.
            Allele<double> valueToMutate = new Allele<double>(ContinuousDomainTest.maximum - 1);
            double variancePercentage = 1.0;

            // For a lot of tries:
            for (int i = 0; i < ContinuousDomainTest.TriesForRandomTests; i++)
            {
                // Mutate and check that the mutated value is in the domain.
                IAllele mutatedGeneValue = domain.MutateGeneValue(valueToMutate, variancePercentage);
                Assert.True(
                    domain.ContainsGeneValue(mutatedGeneValue),
                    $"Value {mutatedGeneValue} was generated by mutation and is not contained in {domain}");
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a <see cref="ContinuousDomain"/>.
        /// </summary>
        /// <param name="minimum">The minimum value.</param>
        /// <param name="maximum">The maximum value.</param>
        /// <returns>The created domain.</returns>
        protected override IDomain CreateNumericalDomain(int minimum, int maximum)
        {
            return new ContinuousDomain(minimum, maximum);
        }

        /// <summary>
        /// Wraps the given value into an <see cref="Allele{T}"/>.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>An <see cref="IAllele"/> containing the value.</returns>
        protected override IAllele WrapInAlleleWithCorrectType(int value)
        {
            return new Allele<double>(value);
        }

        #endregion
    }
}