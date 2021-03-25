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

namespace Optano.Algorithm.Tuner.Tests.Genomes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Accord.Statistics.Testing;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="GenomeBuilder"/>.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class GenomeBuilderTest : IDisposable
    {
        #region Constants

        /// <summary>
        /// Identifier of a parameter used in tests.
        /// </summary>
        private const string DecisionParameter = "aOrB";

        /// <summary>
        /// Identifier of a parameter used in tests.
        /// </summary>
        private const string SmallValueParameter = "int1to5";

        /// <summary>
        /// Identifier of a parameter used in tests.
        /// </summary>
        private const string DiscreteParameter = "int";

        /// <summary>
        /// Identifier of a parameter used in tests.
        /// </summary>
        private const string ContinuousParameter = "double01to08";

        #endregion

        #region Static Fields

        /// <summary>
        /// An <see cref="IEqualityComparer{T}"/> that only checks for gene values regardless of genome age.
        /// </summary>
        private static readonly IEqualityComparer<Genome> genomeValueComparer = Genome.GenomeComparer;

        /// <summary>
        /// The number of times each test using randomness is performed.
        /// </summary>
        private static readonly int loopCountForRandomTests = 50;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeBuilderTest"/> class.
        /// </summary>
        public GenomeBuilderTest()
        {
            Randomizer.Reset();
            Randomizer.Configure();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Resets the <see cref="Randomizer"/>.
        /// </summary>
        public void Dispose()
        {
            Randomizer.Reset();
        }

        /// <summary>
        /// Verifies that calling <see cref="GenomeBuilder"/>'s constructor without a <see cref="ParameterTree"/>
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingParameterTree()
        {
            var configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().Build(maximumNumberParallelEvaluations: 1);
            Assert.Throws<ArgumentNullException>(
                () => new GenomeBuilder(
                    parameterTree: null,
                    configuration: configuration));
        }

        /// <summary>
        /// Verifies that calling <see cref="GenomeBuilder"/>'s constructor without a <see cref="AlgorithmTunerConfiguration"/>
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingConfiguration()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenomeBuilder(
                    parameterTree: GenomeBuilderTest.BuildParameterTree(),
                    configuration: null));
        }

        /// <summary>
        /// Checks that <see cref="GenomeBuilder.Mutate(Genome)"/> does not do anything
        /// if the mutation rate is set to 0 and the original genome is valid.
        /// </summary>
        [Fact]
        public void MutateIsNoOperationIfMutationRateIs0()
        {
            // Create genome builder with a mutation rate of 0.
            var genomeBuilder = GenomeBuilderTest.CreateGenomeBuilderWithoutRandomMutation();

            // Build a fitting genome and store original description.
            var genome = GenomeBuilderTest.BuildFittingGenome();
            string originalDescription = genome.ToString();

            // Check many times:
            for (int i = 0; i < GenomeBuilderTest.loopCountForRandomTests; i++)
            {
                // If mutate is called, the genome does not change.
                genomeBuilder.Mutate(genome);
                Assert.Equal(
                    originalDescription,
                    genome.ToString());
            }
        }

        /// <summary>
        /// Checks that <see cref="GenomeBuilder.Mutate(Genome)"/> changes the genome if the mutation rate is set to 1.
        /// </summary>
        [Fact]
        public void MutateChangesGivenGenomeIfMutationRateIs1()
        {
            // Create genome builder with a mutation rate of 1.
            var configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetMutationRate(1)
                    .Build(maximumNumberParallelEvaluations: 1);
            var genomeBuilder = new GenomeBuilder(GenomeBuilderTest.BuildParameterTree(), configuration);

            // Build a fitting genome and store original description.
            var genome = GenomeBuilderTest.BuildFittingGenome();
            string originalDescription = genome.ToString();

            // Call mutate and check genome changed.
            genomeBuilder.Mutate(genome);
            Assert.NotEqual(
                originalDescription,
                genome.ToString());
        }

        /// <summary>
        /// Checks that the number of mutations that occured when calling <see cref="GenomeBuilder.Mutate(Genome)"/>
        /// roughly corresponds to the set mutation rate.
        /// </summary>
        [Fact]
        public void MutateRespectsMutationRate()
        {
            // Create genome builder with a mutation rate of 0.35.
            double mutationRate = 0.35;
            var configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetMutationRate(mutationRate)
                    .Build(maximumNumberParallelEvaluations: 1);
            var genomeBuilder = new GenomeBuilder(GenomeBuilderTest.BuildParameterTree(), configuration);

            // Create genome.
            var genome = GenomeBuilderTest.BuildFittingGenome();

            // For a lot of iterations:
            IAllele oldGeneValue = genome.GetGeneValue(GenomeBuilderTest.ContinuousParameter);
            int changeCount = 0;
            int numberLoops = 1000;
            for (int i = 0; i < numberLoops; i++)
            {
                // Mutate the genome ...
                genomeBuilder.Mutate(genome);
                // ... compare the continuous parameter gene...
                IAllele newGeneValue = genome.GetGeneValue(GenomeBuilderTest.ContinuousParameter);
                if (!object.Equals(newGeneValue, oldGeneValue))
                {
                    changeCount++;
                }

                // ... and count the number of times it changes.
                oldGeneValue = newGeneValue;
            }

            // Finally compare the number of mutations with the expected number.
            double expectedNumberMutations = mutationRate * numberLoops;
            Assert.True(
                Math.Abs(expectedNumberMutations - changeCount) <= 0.1 * expectedNumberMutations,
                "Number of mutations was not as expected.");
        }

        /// <summary>
        /// Checks that calls to <see cref="GenomeBuilder.Mutate(Genome)"/> may change every part of the genome.
        /// </summary>
        [Fact]
        public void MutateMayChangeEveryParameter()
        {
            // Create genome builder with a mutation rate of 1.
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMutationRate(1)
                .Build(maximumNumberParallelEvaluations: 1);
            var genomeBuilder = new GenomeBuilder(GenomeBuilderTest.BuildParameterTree(), configuration);

            // Create genome.
            var genome = GenomeBuilderTest.BuildFittingGenome();

            // Prepare structure to store which values have been mutated.
            string[] identifiers =
                {
                    GenomeBuilderTest.DecisionParameter,
                    GenomeBuilderTest.SmallValueParameter,
                    GenomeBuilderTest.DiscreteParameter,
                    GenomeBuilderTest.ContinuousParameter,
                };
            Dictionary<string, bool> hasBeenMutated = identifiers.ToDictionary(
                identifier => identifier,
                identifier => false);
            Dictionary<string, IAllele> originalValues = identifiers.ToDictionary(
                identifier => identifier,
                identifier => genome.GetGeneValue(identifier));

            // Do a lot of mutations.
            for (int i = 0; i < GenomeBuilderTest.loopCountForRandomTests; i++)
            {
                genomeBuilder.Mutate(genome);

                // After each one, check which genes have changed...
                foreach (string identifier in identifiers)
                {
                    bool valueChanged = !object.Equals(originalValues[identifier], genome.GetGeneValue(identifier));
                    if (valueChanged)
                    {
                        // ...and store them.
                        hasBeenMutated[identifier] = true;
                    }
                }
            }

            // Finally check that everything has been changed at least once.
            Assert.True(
                hasBeenMutated.All(keyValuePair => keyValuePair.Value),
                $"Genes {TestUtils.PrintList(hasBeenMutated.Where(keyValuePair => !keyValuePair.Value).Select(keyValuePair => keyValuePair.Key))} have not been mutated.");
        }

        /// <summary>
        /// Checks that <see cref="GenomeBuilder.Mutate(Genome)"/> throws a <see cref="TimeoutException"/>
        /// if no valid genomes exist.
        /// </summary>
        [Fact]
        public void MutateThrowsErrorIfNoValidGenomesExist()
        {
            // Create genome builder that does not accept any genome.
            var genomeBuilder = GenomeBuilderTest.CreateCustomGenomeBuilder(
                mutationRate: 0,
                isValidFunction: candidate => false);
            // Build a genome.
            var genome = GenomeBuilderTest.BuildFittingGenome();

            // Mutate will try to repair the genome and should throw an exception as that will never work.
            Assert.Throws<TimeoutException>(() => genomeBuilder.Mutate(genome));
        }

        /// <summary>
        /// Checks that <see cref="GenomeBuilder.Mutate(Genome)"/> manages to repair invalid genomes.
        /// </summary>
        [Fact]
        public void MutateRepairsInvalidGenomes()
        {
            // Invalid genomes must exist. We forbid having a value of 3 for a certain gene.
            int forbiddenValue = 3;
            Func<Genome, bool> doesNotHave3ForInt1To5 = candidate =>
                !object.Equals(candidate.GetGeneValue(GenomeBuilderTest.SmallValueParameter).GetValue(), forbiddenValue);

            // Create a genome builder implementing that check. It also should never mutate randomly
            // so we can be sure the repair was not due to the mutation itself.
            var genomeBuilder = GenomeBuilderTest.CreateCustomGenomeBuilder(
                mutationRate: 0,
                isValidFunction: doesNotHave3ForInt1To5);

            // Build an invalid genome.
            var genome = GenomeBuilderTest.BuildFittingGenome();
            genome.SetGene(GenomeBuilderTest.SmallValueParameter, new Allele<int>(forbiddenValue));

            // Mutate and check the broken gene was fixed.
            genomeBuilder.Mutate(genome);
            Assert.NotEqual(
                forbiddenValue,
                genome.GetGeneValue(GenomeBuilderTest.SmallValueParameter).GetValue());
        }

        /// <summary>
        /// Checks that <see cref="GenomeBuilder.MakeGenomeValid"/> repairs an invalid genome.
        /// </summary>
        [Fact]
        public void MakeGenomeValidRepairsInvalidGenome()
        {
            // Invalid genomes must exist. We forbid having a value of 3 for a certain gene.
            int forbiddenValue = 3;
            Func<Genome, bool> doesNotHave3ForInt1To5 = candidate =>
                !object.Equals(candidate.GetGeneValue(GenomeBuilderTest.SmallValueParameter).GetValue(), forbiddenValue);

            // Create a genome builder implementing that check.
            var genomeBuilder = GenomeBuilderTest.CreateCustomGenomeBuilder(
                mutationRate: 0,
                isValidFunction: doesNotHave3ForInt1To5);

            var genome = GenomeBuilderTest.BuildFittingGenome();
            genome.SetGene(GenomeBuilderTest.SmallValueParameter, new Allele<int>(forbiddenValue));

            genomeBuilder.MakeGenomeValid(genome);
            Assert.True(genomeBuilder.IsGenomeValid(genome), $"{genome} should be valid after repairing it.");
        }

        /// <summary>
        /// Checks that <see cref="GenomeBuilder.MakeGenomeValid"/> does not change valid genomes.
        /// </summary>
        [Fact]
        public void MakeGenomeValidDoesNothingForValidGenome()
        {
            var genomeBuilder = GenomeBuilderTest.CreateGenomeBuilder();
            var genome = GenomeBuilderTest.BuildFittingGenome();

            var originalGenome = new Genome(genome);
            genomeBuilder.MakeGenomeValid(genome);

            Assert.True(
                Genome.GenomeComparer.Equals(originalGenome, genome),
                $"Genome should not have changed from {originalGenome} to {genome}.");
        }

        /// <summary>
        /// Checks that <see cref="GenomeBuilder.MakeGenomeValid(Genome)"/> throws a <see cref="TimeoutException"/>
        /// if no valid genomes exist.
        /// </summary>
        [Fact]
        public void MakeGenomeValidThrowsIfNoValidGenomeExists()
        {
            // Create genome builder that does not accept any genome.
            var genomeBuilder = GenomeBuilderTest.CreateCustomGenomeBuilder(
                mutationRate: 0,
                isValidFunction: candidate => false);

            // Try to repair a genome.
            var genome = GenomeBuilderTest.BuildFittingGenome();
            Assert.Throws<TimeoutException>(() => genomeBuilder.MakeGenomeValid(genome));
        }

        /// <summary>
        /// Checks that <see cref="GenomeBuilder.IsGenomeValid(Genome)"/> returns false
        /// if one gene value is outside of the correct domain.
        /// </summary>
        [Fact]
        public void ValidateGenomeReturnsFalseForWrongDomain()
        {
            // Create a genome builder.
            var genomeBuilder = GenomeBuilderTest.CreateGenomeBuilder();

            // Create correct genome.
            var genome = GenomeBuilderTest.BuildFittingGenome();
            Assert.True(genomeBuilder.IsGenomeValid(genome));

            // Change a gene s.t. it is outside of domain.
            genome.SetGene(GenomeBuilderTest.ContinuousParameter, new Allele<double>(0.9));

            // Check method returns false now.
            Assert.False(
                genomeBuilder.IsGenomeValid(genome),
                $"Genome {genome} was evaluated as valid even if gene '{GenomeBuilderTest.ContinuousParameter}' was set to 0.9.");
        }

        /// <summary>
        /// Checks that <see cref="GenomeBuilder.IsGenomeValid(Genome)"/> returns false
        /// if one gene value has the wrong type.
        /// </summary>
        [Fact]
        public void ValidateGenomeReturnsFalseForWrongType()
        {
            // Create a genome builder.
            var genomeBuilder = GenomeBuilderTest.CreateGenomeBuilder();

            // Create correct genome.
            var genome = GenomeBuilderTest.BuildFittingGenome();
            Assert.True(genomeBuilder.IsGenomeValid(genome));

            // Change a gene s.t. it has the wrong type.
            genome.SetGene(GenomeBuilderTest.DiscreteParameter, new Allele<double>(3));

            // Check method returns false now.
            Assert.False(
                genomeBuilder.IsGenomeValid(genome),
                $"Genome {genome} was evaluated as valid even if gene 'int' was created as double.");
        }

        /// <summary>
        /// Checks that genomes created by <see cref="GenomeBuilder.CreateRandomGenome(int)"/> are valid.
        /// </summary>
        [Fact]
        public void CreateRandomGenomeCreatesValidGenome()
        {
            // Create a genome builder.
            var genomeBuilder = GenomeBuilderTest.CreateGenomeBuilder();

            // For a number of times:
            for (int i = 0; i < GenomeBuilderTest.loopCountForRandomTests; i++)
            {
                // Check that newly created genome is valid.
                var genome = genomeBuilder.CreateRandomGenome(age: 2);
                Assert.True(
                    genomeBuilder.IsGenomeValid(genome),
                    $"Genome {genome} was created by the builder, but not valid.");
            }
        }

        /// <summary>
        /// Checks that <see cref="GenomeBuilder.CreateRandomGenome(int)"/> throws a <see cref="TimeoutException"/>
        /// if no valid genomes exist.
        /// </summary>
        [Fact]
        public void CreateRandomGenomeThrowsErrorIfNoValidGenomesExist()
        {
            // Create genome builder that does not accept any genome.
            var genomeBuilder = GenomeBuilderTest.CreateCustomGenomeBuilder(
                mutationRate: 0,
                isValidFunction: candidate => false);

            // CreateRandomGenome will create a genome and try to repair it afterwards.
            // It should then throw an exception as that will never work.
            Assert.Throws<TimeoutException>(() => genomeBuilder.CreateRandomGenome(age: 0));
        }

        /// <summary>
        /// Checks that a crossover between two genomes with the same gene values generates a child which has the same
        /// gene values again.
        /// </summary>
        [Fact]
        public void CrossoverWithEqualValuedGenomesGeneratesSameGenome()
        {
            var genomeBuilder = GenomeBuilderTest.CreateGenomeBuilder();

            // Create parents with equal gene values.
            var parent1 = GenomeBuilderTest.BuildFittingGenome();
            var parent2 = GenomeBuilderTest.BuildFittingGenome();

            // Do a crossover and check child's gene values.
            var child = genomeBuilder.Crossover(parent1, parent2);
            Assert.True(
                GenomeBuilderTest.genomeValueComparer.Equals(child, parent1),
                $"Crossover of equal valued genomes {parent1} and {parent2} produced child {child} with different values.");
        }

        /// <summary>
        /// Uses the Chi-Squared test to verify that the results of
        /// <see cref="GenomeBuilder.Crossover(Genome, Genome)"/> on a single gene produce children that have both
        /// parents' gene values with equal probability.
        /// </summary>
        [Fact]
        public void CrossoverRandomlyDecidesOnParentToTakeGeneValueFromForSingleGene()
        {
            // Build genome builder with a parameter tree that consists of a single continuous parameter.
            string parameterName = "parameter";
            IParameterNode parameter = new ValueNode<double>(parameterName, new ContinuousDomain());
            var genomeBuilder = new GenomeBuilder(
                new ParameterTree(parameter),
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().Build(maximumNumberParallelEvaluations: 1));

            // Build genomes with different parameter values.
            var parent1 = new Genome();
            var parent2 = new Genome();
            Allele<double> parent1Allele = new Allele<double>(0);
            Allele<double> parent2Allele = new Allele<double>(1);
            parent1.SetGene(parameterName, parent1Allele);
            parent2.SetGene(parameterName, parent2Allele);

            // Observe what gene value the children's genes have.
            int numberLoops = 1000;
            int observedInheritanceFromParent1 = 0;
            for (int i = 0; i < numberLoops; i++)
            {
                var child = genomeBuilder.Crossover(parent1, parent2);
                if (object.Equals(parent1Allele, child.GetGeneValue(parameterName)))
                {
                    observedInheritanceFromParent1++;
                }
            }

            double[] observed = { observedInheritanceFromParent1, numberLoops - observedInheritanceFromParent1 };

            // We would expect each value the same number of times:
            double[] expected = { numberLoops / 2, numberLoops / 2 };

            // Use Chi-Squared Test.
            var equalProbabilityTest = new ChiSquareTest(expected, observed, degreesOfFreedom: numberLoops - 1);
            Assert.False(
                equalProbabilityTest.Significant,
                $"Single gene was found to be not equally distributed in crossovers by the Chi-Squared test with significance level of {equalProbabilityTest.Size}.");
        }

        /// <summary>
        /// Uses the Chi-Squared test to verify that the results of
        /// <see cref="GenomeBuilder.Crossover(Genome, Genome)"/> on two dependent genes produce children whose second
        /// gene is dependent on the 1st one as the probability it is taken from the same parent is the probability set
        /// in the <see cref="AlgorithmTunerConfiguration"/>.
        /// </summary>
        [Fact]
        public void CrossoverRespectsSwitchProbability()
        {
            // Build parameter tree that consists of two dependent continuous parameters.
            string rootParameterName = "parameterRoot";
            string childParameterName = "parameterChild";
            var rootParameter = new ValueNode<double>(rootParameterName, new ContinuousDomain());
            var childParameter = new ValueNode<double>(childParameterName, new ContinuousDomain());
            rootParameter.SetChild(childParameter);

            // Build genome builder with that parameter tree and a specific crossover switch probability.
            double crossoverSwitchParameter = 0.25;
            AlgorithmTunerConfiguration config = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetCrossoverSwitchProbability(crossoverSwitchParameter)
                .Build(maximumNumberParallelEvaluations: 1);
            var genomeBuilder = new GenomeBuilder(new ParameterTree(rootParameter), config);

            // Build parents.
            var parent1 = new Genome();
            var parent2 = new Genome();
            var parent1RootAllele = new Allele<double>(1);
            var parent1ChildAllele = new Allele<double>(2);
            var parent2RootAllele = new Allele<double>(3);
            var parent2ChildAllele = new Allele<double>(4);
            parent1.SetGene(rootParameterName, parent1RootAllele);
            parent1.SetGene(childParameterName, parent1ChildAllele);
            parent2.SetGene(rootParameterName, parent2RootAllele);
            parent2.SetGene(childParameterName, parent2ChildAllele);

            // Observe if children's genes come from the same parent or not.
            int numberLoops = 1000;
            int genesCameFromSameParent = 0;
            for (int i = 0; i < numberLoops; i++)
            {
                var child = genomeBuilder.Crossover(parent1, parent2);
                IAllele rootAllele = child.GetGeneValue(rootParameterName);
                IAllele childAllele = child.GetGeneValue(childParameterName);
                bool geneValuesInheritedFromSameParent =
                    (object.Equals(rootAllele, parent1RootAllele) && object.Equals(childAllele, parent1ChildAllele))
                    || (object.Equals(rootAllele, parent2RootAllele) && object.Equals(childAllele, parent2ChildAllele));
                if (geneValuesInheritedFromSameParent)
                {
                    genesCameFromSameParent++;
                }
            }

            double[] observed = { genesCameFromSameParent, numberLoops - genesCameFromSameParent };

            // We would expect each case according to switch probability:
            int expectedSwitches = (int)(crossoverSwitchParameter * numberLoops);
            double[] expected = { numberLoops - expectedSwitches, expectedSwitches };

            // Use Chi-Squared Test.
            var matchesSwitchProbabilityTest = new ChiSquareTest(expected, observed, degreesOfFreedom: numberLoops - 1);
            Assert.False(
                matchesSwitchProbabilityTest.Significant,
                $"Crossover was found not to respect the switch probability by the Chi-Squared test with significance level of {matchesSwitchProbabilityTest.Size}.");
        }

        /// <summary>
        /// Checks that the <see cref="GenomeBuilder.CreateDefaultGenome"/> uses the specified domain default values.
        /// </summary>
        [Fact]
        public void DefaultGenomeUsesDefaultValues()
        {
            var tree = GenomeBuilderTest.BuildParameterTree(true);
            var builder = new GenomeBuilder(
                tree,
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().Build(maximumNumberParallelEvaluations: 1));

            var defaultGenome = builder.CreateDefaultGenome(1337);

            Assert.NotNull(defaultGenome);
            Assert.Equal(1337, defaultGenome.Age);

            // GenomeBuilderTest.ContinuousParameter should not be active, since "a" node is the default.
            var filteredGenes = defaultGenome.GetFilteredGenes(tree);
            Assert.Equal(3, filteredGenes.Count);

            Assert.Equal("a", filteredGenes[GenomeBuilderTest.DecisionParameter].GetValue());
            Assert.Equal(1, filteredGenes[GenomeBuilderTest.SmallValueParameter].GetValue());
            Assert.Equal(42, filteredGenes[GenomeBuilderTest.DiscreteParameter].GetValue());

            // GenomeBuilderTest.ContinuousParameter should also have a default value, even though it is not active.
            var contAllele = defaultGenome.GetGeneValue(GenomeBuilderTest.ContinuousParameter);
            Assert.NotNull(contAllele);
            Assert.Equal(0.2, contAllele.GetValue());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a simple <see cref="GenomeBuilder"/> that uses the parameter tree from
        /// <see cref="GenomeBuilderTest.BuildParameterTree"/>.
        /// </summary>
        /// <param name="includeDefaultValues">Indicates whether to add default values to the domains.</param>
        /// <returns>The genome builder.</returns>
        private static GenomeBuilder CreateGenomeBuilder(bool includeDefaultValues = false)
        {
            return new GenomeBuilder(
                GenomeBuilderTest.BuildParameterTree(includeDefaultValues),
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().Build(maximumNumberParallelEvaluations: 1));
        }

        /// <summary>
        /// Creates a simple <see cref="GenomeBuilder"/> that has a mutation rate of 0 and uses the parameter tree from
        /// <see cref="GenomeBuilderTest.BuildParameterTree"/>.
        /// </summary>
        /// <returns>The genome builder.</returns>
        private static GenomeBuilder CreateGenomeBuilderWithoutRandomMutation()
        {
            var configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetMutationRate(0)
                    .Build(maximumNumberParallelEvaluations: 1);
            return new GenomeBuilder(GenomeBuilderTest.BuildParameterTree(), configuration);
        }

        /// <summary>
        /// Creates a genome builder with a custom <see cref="GenomeBuilder.IsGenomeValid(Genome)"/> function and a
        /// custom mutation rate.
        /// </summary>
        /// <param name="mutationRate">The mutation rate.</param>
        /// <param name="isValidFunction">What to check for <see cref="GenomeBuilder.IsGenomeValid(Genome)"/>.</param>
        /// <returns>The genome builder.</returns>
        private static GenomeBuilder CreateCustomGenomeBuilder(double mutationRate, Func<Genome, bool> isValidFunction)
        {
            return new GenomeBuilders.ConfigurableGenomeBuilder(
                GenomeBuilderTest.BuildParameterTree(),
                isValidFunction,
                mutationRate);
        }

        /// <summary>
        /// Builds the following parameter tree:
        /// - AND node as root
        /// - 1st child of AND node: OR node with string either a or b (default: "a")
        /// - 2nd child of AND node: value node with integer between 1 and 5 (default: 1)
        /// - OR node, a branch: value node with integer (default: 42).
        /// - OR node, b branch: value node with double between 0.1 and 0.8 (default: 0.2).
        /// </summary>
        /// <param name="includeDefaultValues">Indicates whether to add default values to the domains.</param>
        /// <returns>The build parameter tree.</returns>
        private static ParameterTree BuildParameterTree(bool includeDefaultValues = false)
        {
            // Create all parameter tree nodes.
            var rootNode = new AndNode();
            OrNode<string> decideAOrBNode = new OrNode<string>(
                DecisionParameter,
                new CategoricalDomain<string>(new List<string> { "a", "b" }, includeDefaultValues ? new Allele<string>("a") : (Allele<string>?)null));

            IParameterNode integerParamNode = new ValueNode<int>(
                GenomeBuilderTest.DiscreteParameter,
                new IntegerDomain(defaultValue: includeDefaultValues ? new Allele<int>(42) : (Allele<int>?)null));
            IParameterNode continuousParamNode = new ValueNode<double>(
                GenomeBuilderTest.ContinuousParameter,
                new ContinuousDomain(0.1, 0.8, includeDefaultValues ? new Allele<double>(0.2) : (Allele<double>?)null));

            IParameterNode smallIntegerParamNode = new ValueNode<int>(
                GenomeBuilderTest.SmallValueParameter,
                new IntegerDomain(1, 5, includeDefaultValues ? new Allele<int>(1) : (Allele<int>?)null));

            // Connect them.
            decideAOrBNode.AddChild("a", integerParamNode);
            decideAOrBNode.AddChild("b", continuousParamNode);
            rootNode.AddChild(decideAOrBNode);
            rootNode.AddChild(smallIntegerParamNode);

            // Return tree.
            return new ParameterTree(rootNode);
        }

        /// <summary>
        /// Builds a <see cref="Genome"/> that fits the <see cref="ParameterTree"/> built in
        /// <see cref="BuildParameterTree"/>.
        /// </summary>
        /// <returns>The built <see cref="Genome"/>.</returns>
        private static Genome BuildFittingGenome()
        {
            Genome genome = new Genome();
            genome.SetGene(GenomeBuilderTest.DecisionParameter, new Allele<string>("a"));
            genome.SetGene(GenomeBuilderTest.DiscreteParameter, new Allele<int>(3));
            genome.SetGene(GenomeBuilderTest.ContinuousParameter, new Allele<double>(0.7));
            genome.SetGene(GenomeBuilderTest.SmallValueParameter, new Allele<int>(5));

            return genome;
        }

        #endregion
    }
}