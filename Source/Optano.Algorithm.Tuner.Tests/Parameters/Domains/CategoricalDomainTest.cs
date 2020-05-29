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
    using System.Collections.Generic;
    using System.Linq;

    using Accord.Statistics.Distributions.Univariate;
    using Accord.Statistics.Testing;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters.Domains;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="CategoricalDomain{T}"/>.
    /// </summary>
    public class CategoricalDomainTest : DomainBaseTest
    {
        #region Static Fields

        /// <summary>
        /// Category values used in tests.
        /// </summary>
        private static readonly List<int> categories = new List<int> { 1, 5, 23, -2 };

        /// <summary>
        /// The number of iterations in tests that test random methods.
        /// </summary>
        private static readonly int triesForRandomTests = 1000;

        /// <summary>
        /// The variance percentage value needed for <see cref="IDomain.MutateGeneValue(IAllele, double)"/>.
        /// Categorical domains don't use it.
        /// </summary>
        private static readonly double dummyVariancePercentage = 0.1;

        #endregion

        #region Fields

        /// <summary>
        /// Categorical domain. Will be initialized each time with <see cref="categories"/>.
        /// </summary>
        private CategoricalDomain<int> _categoricalDomain;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoricalDomainTest"/> class.
        /// </summary>
        public CategoricalDomainTest()
        {
            this._categoricalDomain = new CategoricalDomain<int>(CategoricalDomainTest.categories);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="CategoricalDomain{T}.PossibleValues"/> contains the values provided at construction.
        /// </summary>
        [Fact]
        public void PossibleValuesAreCorrectlySet()
        {
            Assert.True(
                TestUtils.SetsAreEquivalent(this._categoricalDomain.PossibleValues, CategoricalDomainTest.categories),
                $"{TestUtils.PrintList(this._categoricalDomain.PossibleValues)} is different from {TestUtils.PrintList(CategoricalDomainTest.categories)}.");
        }

        /// <summary>
        /// Checks that <see cref="CategoricalDomain{T}.PossibleValues"/> are persistent even if the collection used
        /// for construction is modified.
        /// </summary>
        [Fact]
        public void PossibleValuesAreIndependentFromProvidedList()
        {
            // Create CategoricalDomain
            var provided = new List<int>(CategoricalDomainTest.categories);
            this._categoricalDomain = new CategoricalDomain<int>(provided);

            // Check that values are correctly set.
            Assert.True(TestUtils.SetsAreEquivalent(this._categoricalDomain.PossibleValues, provided));

            // Change the list that was used for initialization.
            provided.Add(4);

            // It should now be different from the possible values.
            Assert.False(
                TestUtils.SetsAreEquivalent(this._categoricalDomain.PossibleValues, provided),
                $"Values of categorical domain have been changed externally.");
        }

        /// <summary>
        /// Checks that randomly generated values are contained in domain.
        /// </summary>
        [Fact]
        public void RandomGenerationStaysInDomain()
        {
            // For a lot of tries:
            for (int i = 0; i < CategoricalDomainTest.triesForRandomTests; i++)
            {
                // Check that the generated value is in the domain.
                IAllele generated = this._categoricalDomain.GenerateRandomGeneValue();
                Assert.True(
                    this._categoricalDomain.ContainsGeneValue(generated),
                    $"Generated value {generated} which is not contained in {this._categoricalDomain}");
            }
        }

        /// <summary>
        /// Checks that <see cref="CategoricalDomain{T}.GenerateRandomValue"/> may hit all values of the domain.
        /// </summary>
        [Fact]
        public void RandomGenerationSpansWholeDomain()
        {
            // Remember which values were hit...
            Dictionary<int, bool> coveredNumber = CategoricalDomainTest.categories.ToDictionary(num => num, num => false);
            // ...for a lot of iterations.
            for (int i = 0; i < CategoricalDomainTest.triesForRandomTests; i++)
            {
                int generated = this._categoricalDomain.GenerateRandomGeneValue().GetValue();
                coveredNumber[generated] = true;
            }

            // Check that each value was hit at least once.
            for (int i = 0; i < CategoricalDomainTest.categories.Count; i++)
            {
                Assert.True(
                    coveredNumber[CategoricalDomainTest.categories[i]],
                    $"{CategoricalDomainTest.categories[i]} has never been generated in {CategoricalDomainTest.triesForRandomTests} tries.");
            }
        }

        /// <summary>
        /// Checks that <see cref="IDomain.DomainSize"/> correctly returns the domain's magnitude.
        /// </summary>
        [Fact]
        public override void DomainSizeIsCorrect()
        {
            Assert.Equal(
                CategoricalDomainTest.categories.Count,
                this._categoricalDomain.DomainSize);
        }

        /// <summary>
        /// Checks that <see cref="DomainBase{T}.ContainsGeneValue(IAllele)"/> returns true for a value that is one of the categories.
        /// </summary>
        [Fact]
        public void ContainsGeneValueReturnsTrueForIncludedValue()
        {
            Assert.True(
                this._categoricalDomain.ContainsGeneValue(new Allele<int>(CategoricalDomainTest.categories[1])),
                $"{CategoricalDomainTest.categories[1]} is supposedly not contained in {this._categoricalDomain}");
        }

        /// <summary>
        /// Checks that <see cref="IDomain.ContainsGeneValue(IAllele)"/> returns false if called with a gene
        /// value that is not contained in the domain.
        /// </summary>
        [Fact]
        public void ContainsGeneValueReturnsFalseForInvalidValue()
        {
            int value = -42;
            Assert.False(
                this._categoricalDomain.ContainsGeneValue(new Allele<int>(value)),
                $"{value} is supposedly contained in {this._categoricalDomain}");
        }

        /// <summary>
        /// Checks that <see cref="IDomain.ContainsGeneValue(IAllele)"/> returns false if called with a type
        /// that is not a subtype of the domain objects' type.
        /// </summary>
        [Fact]
        public override void ContainsGeneValueReturnsFalseForWrongType()
        {
            IDomain domain = new CategoricalDomain<int>(new List<int> { 1 });
            Assert.False(
                domain.ContainsGeneValue(new Allele<double>(1.0)),
                $"Wrong type is categorized as legal gene value.");
        }

        /// <summary>
        /// Checks that <see cref="IDomain.ContainsGeneValue(IAllele)"/> returns true
        /// if a subtype with correct value is given as parameter.
        /// </summary>
        [Fact]
        public void ContainsGeneValueReturnsTrueForSubtype()
        {
            DomainBase<object> objectDomain = new CategoricalDomain<object>(new List<object> { "a1" });

            var stringAllele = new Allele<string>("a1");
            Assert.True(
                objectDomain.ContainsGeneValue(stringAllele),
                $"{stringAllele} was not identified as legal value of domain {objectDomain}.");
        }

        /// <summary>
        /// Checks that <see cref="IDomain.MutateGeneValue(IAllele, double)"/> throws an
        /// <see cref="ArgumentException"/> if called with a type that is not a subtype of the domain objects' type.
        /// </summary>
        [Fact]
        public override void MutateGeneValueThrowsExceptionForWrongType()
        {
            IDomain domain = new CategoricalDomain<int>(new List<int> { 1 });
            Assert.Throws<ArgumentOutOfRangeException>(() => domain.MutateGeneValue(new Allele<double>(1.0), CategoricalDomainTest.dummyVariancePercentage));
        }

        /// <summary>
        /// Checks that <see cref="IDomain.MutateGeneValue(IAllele, double)"/> throws an
        /// <see cref="ArgumentException"/> if called with a gene value that is not contained in the domain.
        /// </summary>
        [Fact]
        public override void MutateGeneValueThrowsExceptionForInvalidValue()
        {
            IAllele geneValue = new Allele<int>(-42);
            Assert.False(this._categoricalDomain.ContainsGeneValue(geneValue));

            Assert.Throws<ArgumentOutOfRangeException>(() => this._categoricalDomain.MutateGeneValue(geneValue, CategoricalDomainTest.dummyVariancePercentage));
        }

        /// <summary>
        /// Checks that <see cref="CategoricalDomain{T}.ToString()"/> returns a string of the form
        /// {category1, category2, ..., categoryn}.
        /// </summary>
        [Fact]
        public void ToStringShowsAllCategories()
        {
            IDomain domain = new CategoricalDomain<int>(new List<int> { 1, -4, 17 });
            Assert.Equal(
                "{1, -4, 17}",
                domain.ToString());
        }

        /// <summary>
        /// Uses the Chi-Squared test to verify that many calls of <see cref="CategoricalDomain{T}.Mutate(T, double)"/>
        /// produce a distribution that does not depart from the uniform distribution.
        /// </summary>
        [Fact]
        public void MutateDoesNotDepartFromUniformDistribution()
        {
            // Set up categorical domain with integer values 0 - 3.
            var possibleValues = new List<int> { 0, 1, 2, 3 };
            CategoricalDomain<int> domain = new CategoricalDomain<int>(possibleValues);

            // Remember which values were generated for a lot of iterations.
            double[] observations = new double[CategoricalDomainTest.triesForRandomTests];
            Allele<int> geneValue = new Allele<int>(1);
            for (int i = 0; i < CategoricalDomainTest.triesForRandomTests; i++)
            {
                observations[i] = (int)domain.MutateGeneValue(geneValue, CategoricalDomainTest.dummyVariancePercentage).GetValue();
            }

            // Apply the Chi-Squared test.
            ChiSquareTest uniformTest = new ChiSquareTest(observations, new UniformDiscreteDistribution(0, 3));
            Assert.False(
                uniformTest.Significant,
                $"Mutation was found to not produce a uniform distribution by the Chi-Squared test with significance level {uniformTest.Size}.");
        }

        #endregion
    }
}