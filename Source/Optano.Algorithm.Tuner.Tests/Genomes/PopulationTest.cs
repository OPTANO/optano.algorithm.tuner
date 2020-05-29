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

namespace Optano.Algorithm.Tuner.Tests.Genomes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using Xunit;

    /// <summary>
    /// Contains tests for class <see cref="Population"/>.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class PopulationTest
    {
        #region Fields

        /// <summary>
        /// Genome that is used as a non competitive genome in some tests.
        /// </summary>
        private readonly Genome _nonCompetitiveGenome = new Genome();

        /// <summary>
        /// Genome that is used as a competitive genome in some tests.
        /// </summary>
        private readonly Genome _competitiveGenome = new Genome();

        /// <summary>
        /// The maximum age of a single genome.
        /// </summary>
        private readonly int _maxAge = 3;

        /// <summary>
        /// The ratio of mutants in the non-competitive population after every iteration.
        /// </summary>
        private readonly double _populationMutantRatio = 0.5;

        /// <summary>
        /// Simple genome builder for building random genomes.
        /// </summary>
        private readonly GenomeBuilder _genomeBuilder = new GenomeBuilder(
            new ParameterTree(new AndNode()),
            new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().Build(maximumNumberParallelEvaluations: 1));

        /// <summary>
        /// The <see cref="Population"/> instance used for testing.
        /// </summary>
        private Population _population;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PopulationTest"/> class.
        /// </summary>
        public PopulationTest()
        {
            Randomizer.Reset();
            Randomizer.Configure(42);
            int populationSize = 6;
            this._population = PopulationTest.CreatePopulation(populationSize, this._maxAge, this._populationMutantRatio);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="Population"/>'s copy constructor throws an
        /// <see cref="ArgumentNullException"/> when no original is provided to copy from.
        /// </summary>
        [Fact]
        public void CopyConstructorThrowsForMissingOriginal()
        {
            Assert.Throws<ArgumentNullException>(() => new Population(original: null));
        }

        /// <summary>
        /// Checks that <see cref="Population"/>'s copy constructor copies both population parts.
        /// </summary>
        [Fact]
        public void CopyConstructorCopiesPopulationParts()
        {
            var copy = new Population(this._population);

            // Use strings in comparisons to compare values, not references
            // Order is not important, so order before comparison
            Assert.Equal(
                this._population.GetCompetitiveIndividuals().Select(genome => genome.ToString()).OrderBy(x => x).ToArray(),
                copy.GetCompetitiveIndividuals().Select(genome => genome.ToString()).ToArray());
            Assert.Equal(
                this._population.GetNonCompetitiveMates().Select(genome => genome.ToString()).OrderBy(x => x).ToArray(),
                copy.GetNonCompetitiveMates().Select(genome => genome.ToString()).ToArray());
        }

        /// <summary>
        /// Checks that <see cref="Population"/>'s copy constructor makes an actual copy that is not influenced
        /// by changes to the original.
        /// </summary>
        [Fact]
        public void CopyConstructorMakesDeepCopy()
        {
            // Make sure genome exists in original population.
            this._population.AddGenome(new Genome(), true);

            var copy = new Population(this._population);
            this._population.AddGenome(new Genome(), false);
            Assert.True(
                this._population.Count != copy.Count,
                "Adding genome to original population should not change copy.");

            // Use strings in comparisons to compare values, not references
            // Order is not important, so order before comparison
            Assert.Equal(
                this._population.GetCompetitiveIndividuals().Select(genome => genome.ToString()).OrderBy(x => x).ToArray(),
                copy.GetCompetitiveIndividuals().Select(genome => genome.ToString()).OrderBy(x => x).ToArray());

            // Changing a genome in the original population should not change copy
            var originalCompetitives = this._population.GetCompetitiveIndividuals();
            originalCompetitives[0].SetGene("a", new Allele<int>(123456));
            Assert.NotEqual(
                this._population.GetCompetitiveIndividuals().Select(genome => genome.ToString()).OrderBy(x => x).ToArray(),
                copy.GetCompetitiveIndividuals().Select(genome => genome.ToString()).OrderBy(x => x).ToArray());
        }

        /// <summary>
        /// Tests that a genome that is added as non competitive is returned when calling
        /// <see cref="Population.GetNonCompetitiveMates"/>.
        /// </summary>
        [Fact]
        public void NonCompetitiveGenomeIsReturnedOnNonCompetitiveRequest()
        {
            this._population.AddGenome(this._nonCompetitiveGenome, isCompetitive: false);

            var expectedGenomes = new List<Genome> { this._nonCompetitiveGenome };
            Assert.True(
                Enumerable.SequenceEqual(this._population.GetNonCompetitiveMates(), expectedGenomes),
                $"Expected {TestUtils.PrintList(expectedGenomes)} for non competitive return, but got {TestUtils.PrintList(this._population.GetNonCompetitiveMates())}.");
        }

        /// <summary>
        /// Tests that a genome that is added as competitive is not returned when calling
        /// <see cref="Population.GetNonCompetitiveMates"/>.
        /// </summary>
        [Fact]
        public void CompetitiveGenomeIsNotReturnedOnNonCompetitiveRequest()
        {
            this._population.AddGenome(this._competitiveGenome, isCompetitive: true);

            var expectedGenomes = new List<Genome>();
            Assert.True(
                Enumerable.SequenceEqual(this._population.GetNonCompetitiveMates(), expectedGenomes),
                $"Expected {TestUtils.PrintList(expectedGenomes)} for non competitive return, but got {TestUtils.PrintList(this._population.GetNonCompetitiveMates())}.");
        }

        /// <summary>
        /// Tests that a genome that is added as competitive is returned when calling
        /// <see cref="Population.GetCompetitiveIndividuals"/>.
        /// </summary>
        [Fact]
        public void CompetitiveGenomeIsReturnedOnCompetitiveRequest()
        {
            this._population.AddGenome(this._competitiveGenome, isCompetitive: true);

            var expectedGenomes = new List<Genome> { this._competitiveGenome };
            Assert.True(
                Enumerable.SequenceEqual(this._population.GetCompetitiveIndividuals(), expectedGenomes),
                $"Expected {TestUtils.PrintList(expectedGenomes)} for competitive return, but got {TestUtils.PrintList(this._population.GetCompetitiveIndividuals())}.");
        }

        /// <summary>
        /// Tests that a genome that is added as non competitive is not returned when calling
        /// <see cref="Population.GetCompetitiveIndividuals"/>.
        /// </summary>
        [Fact]
        public void NonCompetitiveGenomeIsNotReturnedOnCompetitiveRequest()
        {
            this._population.AddGenome(this._nonCompetitiveGenome, isCompetitive: false);

            var expectedGenomes = new List<Genome>();
            Assert.True(
                Enumerable.SequenceEqual(this._population.GetCompetitiveIndividuals(), expectedGenomes),
                $"Expected {TestUtils.PrintList(expectedGenomes)} for competitive return, but got {TestUtils.PrintList(this._population.GetCompetitiveIndividuals())}.");
        }

        /// <summary>
        /// Tests that aging increments the age of all genomes in population.
        /// </summary>
        [Fact]
        public void AgingIncreasesAge()
        {
            // Generate genomes with different ages.
            var newGenome = new Genome();
            var olderGenome = new Genome(2);
            // Have them in both parts of the population.
            this._population.AddGenome(newGenome, isCompetitive: true);
            this._population.AddGenome(olderGenome, isCompetitive: false);

            // Make sure their age.
            Assert.Equal(0, newGenome.Age);
            Assert.Equal(2, olderGenome.Age);
            // Age them.
            this._population.Age();

            // Check age was incremented.
            Assert.True(1 == newGenome.Age, "Competitive genome did not age.");
            Assert.True(3 == olderGenome.Age, "Non competitive genome did not age.");
        }

        /// <summary>
        /// Tests that genomes with maximum age are removed from the non competitive part of the population after
        /// aging. 
        /// </summary>
        [Fact]
        public void AgingRemovesDyingGenomesFromNonCompetitivePopulation()
        {
            // Build genomes with extreme ages.
            var nonCompetitiveOldGenome = new Genome(this._maxAge - 1);
            var nonCompetiveDyingGenome = new Genome(this._maxAge);
            // Add them to non competitive population.
            this._population.AddGenome(nonCompetitiveOldGenome, isCompetitive: false);
            this._population.AddGenome(nonCompetiveDyingGenome, isCompetitive: false);

            // Check they are contained.
            var expectedNonCompetitive = new List<Genome> { nonCompetitiveOldGenome, nonCompetiveDyingGenome };
            Assert.True(
                TestUtils.SetsAreEquivalent(this._population.GetNonCompetitiveMates(), expectedNonCompetitive));
            // Age the population.
            this._population.Age();

            // Check dying genome was removed, but old genome wasn't.
            Assert.True(
                this._population.GetNonCompetitiveMates().Contains(nonCompetitiveOldGenome),
                $"Old genome was removed from population even if it had an age of {nonCompetitiveOldGenome.Age}/{this._maxAge}.");
            Assert.False(
                this._population.GetNonCompetitiveMates().Contains(nonCompetiveDyingGenome),
                $"Dying genome was not removed from population with an age of {nonCompetiveDyingGenome.Age} > {this._maxAge}.");
        }

        /// <summary>
        /// Tests that genomes with maximum age are removed from the competitive part of the population after aging.
        /// </summary>
        [Fact]
        public void AgingRemovesDyingGenomesFromCompetitivePopulation()
        {
            // Build genomes with extreme ages.
            var competitiveOldGenome = new Genome(this._maxAge - 1);
            var competitiveDyingGenome = new Genome(this._maxAge);
            // Add them to the competitive population.
            this._population.AddGenome(competitiveOldGenome, isCompetitive: true);
            this._population.AddGenome(competitiveDyingGenome, isCompetitive: true);

            // Check they are contained.
            var expectedCompetitive = new List<Genome> { competitiveOldGenome, competitiveDyingGenome };
            Assert.True(
                TestUtils.SetsAreEquivalent(this._population.GetCompetitiveIndividuals(), expectedCompetitive));
            // Age the population.
            this._population.Age();

            // Check dying genome was removed, but old genome wasn't.
            Assert.True(
                this._population.GetCompetitiveIndividuals().Contains(competitiveOldGenome),
                $"Old genome was removed from population even if it had an age of {competitiveOldGenome.Age}/{this._maxAge}.");
            Assert.False(
                this._population.GetCompetitiveIndividuals().Contains(competitiveDyingGenome),
                $"Dying genome was not removed from population with an age of {competitiveDyingGenome.Age} > {this._maxAge}.");
        }

        /// <summary>
        /// Tests that <see cref="Population.IsEmpty"/> returns false if the population is empty.
        /// </summary>
        [Fact]
        public void IsEmptyReturnsTrueIfPopulationEmpty()
        {
            Assert.True(this._population.IsEmpty(), "Empty population not identified as empty.");
        }

        /// <summary>
        /// Tests that <see cref="Population.IsEmpty"/> returns false if the population contains a competitive genome.
        /// </summary>
        [Fact]
        public void IsEmptyReturnsFalseIfCompetitiveGenomeExists()
        {
            this._population.AddGenome(new Genome(), isCompetitive: true);
            Assert.False(this._population.IsEmpty(), "Non-empty population was identified as empty.");
        }

        /// <summary>
        /// Tests that <see cref="Population.IsEmpty"/> returns false if the population contains a non-competitive
        /// genome.
        /// </summary>
        [Fact]
        public void IsEmptyReturnsFalseIfNonCompetitiveGenomeExists()
        {
            this._population.AddGenome(new Genome(), isCompetitive: false);
            Assert.False(this._population.IsEmpty(), "Non-empty population was identified as empty.");
        }

        /// <summary>
        /// Check that the correct number of individuals get replaced on replacement.
        /// </summary>
        [Fact]
        public void CorrectNumberOfIndividualsGetReplaced()
        {
            int populationSize = 6;
            double replacementPercentage = 0.3;

            // Non competitive part of the population consists of 3 individuals.
            // 30 % of them correspond to one individual.
            int expectedNumberOfReplacedIndividuals = 1;

            // Build up non-competitive population.
            this._population = PopulationTest.CreatePopulation(populationSize, maxAge: 3, populationMutantRatio: replacementPercentage);
            var genome1 = new Genome(1);
            var genome2 = new Genome(2);
            var genome3 = new Genome(3);

            this._population.AddGenome(genome1, isCompetitive: false);
            this._population.AddGenome(genome2, isCompetitive: false);
            this._population.AddGenome(genome3, isCompetitive: false);

            // Check the population and replace individuals.
            var originalNonCompetitive = new List<Genome> { genome1, genome2, genome3 };

            Assert.True(originalNonCompetitive.SequenceEqual(this._population.GetNonCompetitiveMates(), new Genome.GeneValueComparator()));
            this._population.ReplaceIndividualsWithMutants(this._genomeBuilder);

            // Check number of changed individuals.
            var difference = originalNonCompetitive.Except(this._population.GetNonCompetitiveMates());
            Assert.True(
                expectedNumberOfReplacedIndividuals == difference.Count(),
                $"There should be {expectedNumberOfReplacedIndividuals} items changed between {string.Join(";", originalNonCompetitive.Select(g => g.ToString()))} and {string.Join(",", this._population.GetNonCompetitiveMates().Select(g => g.ToString()))}.");
        }

        /// <summary>
        /// Check that age distribution stays the same before and after replacement.
        /// </summary>
        [Fact]
        public void AgeDistributionDoesNotChangeOnReplacement()
        {
            // Initialize a large population.
            int populationSize = 50;
            this._population = PopulationTest.CreatePopulation(populationSize, maxAge: 3, populationMutantRatio: this._populationMutantRatio);

            // Insert individuals with different ages into non competitive population.
            int numAgedZero = 6;
            int numAgedOne = 4;
            int numAgedTwo = 3;
            int numAgedThree = 12;

            for (int i = 0; i < numAgedZero; i++)
            {
                this._population.AddGenome(new Genome(), isCompetitive: false);
            }

            for (int i = 0; i < numAgedOne; i++)
            {
                this._population.AddGenome(new Genome(1), isCompetitive: false);
            }

            for (int i = 0; i < numAgedTwo; i++)
            {
                this._population.AddGenome(new Genome(2), isCompetitive: false);
            }

            for (int i = 0; i < numAgedThree; i++)
            {
                this._population.AddGenome(new Genome(3), isCompetitive: false);
            }

            // Check ages were inserted correctly.
            var nonCompetitive = this._population.GetNonCompetitiveMates();
            Assert.Equal(numAgedZero, nonCompetitive.Where(genome => genome.Age == 0).Count());
            Assert.Equal(numAgedOne, nonCompetitive.Where(genome => genome.Age == 1).Count());
            Assert.Equal(numAgedTwo, nonCompetitive.Where(genome => genome.Age == 2).Count());
            Assert.Equal(numAgedThree, nonCompetitive.Where(genome => genome.Age == 3).Count());

            // Replace some individuals with new random individuals.
            this._population.ReplaceIndividualsWithMutants(this._genomeBuilder);
            nonCompetitive = this._population.GetNonCompetitiveMates();

            // Check age distribution is still the same.
            Assert.Equal(
                numAgedZero,
                nonCompetitive.Where(genome => genome.Age == 0).Count());

            Assert.Equal(
                numAgedOne,
                nonCompetitive.Where(genome => genome.Age == 1).Count());

            Assert.Equal(
                numAgedTwo,
                nonCompetitive.Where(genome => genome.Age == 2).Count());

            Assert.Equal(
                numAgedThree,
                nonCompetitive.Where(genome => genome.Age == 3).Count());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a population with the given configuration.
        /// </summary>
        /// <param name="populationSize">The population's size.</param>
        /// <param name="maxAge">Maximum age of a genome.</param>
        /// <param name="populationMutantRatio">The ratio of non competitive genomes to replace each population.
        /// </param>
        /// <returns>The population.</returns>
        private static Population CreatePopulation(int populationSize, int maxAge, double populationMutantRatio)
        {
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMaxGenomeAge(maxAge)
                .SetPopulationSize(populationSize)
                .SetPopulationMutantRatio(populationMutantRatio)
                .Build(maximumNumberParallelEvaluations: 1);
            return new Population(configuration);
        }

        #endregion
    }
}