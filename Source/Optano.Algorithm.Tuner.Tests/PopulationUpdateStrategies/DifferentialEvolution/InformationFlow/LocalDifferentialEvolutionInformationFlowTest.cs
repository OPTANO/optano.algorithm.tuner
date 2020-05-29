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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.DifferentialEvolution.InformationFlow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.InformationFlow;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.SearchPoint;
    using Optano.Algorithm.Tuner.Tests.GenomeBuilders;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="LocalDifferentialEvolutionInformationFlow"/> class.
    /// </summary>
    public class LocalDifferentialEvolutionInformationFlowTest : TestBase
    {
        #region Fields

        /// <summary>
        /// The <see cref="AlgorithmTunerConfiguration"/> used in tests.
        /// </summary>
        private AlgorithmTunerConfiguration _completeConfiguration;

        /// <summary>
        /// The <see cref="DifferentialEvolutionStrategyConfiguration"/> used in tests.
        /// </summary>
        private DifferentialEvolutionStrategyConfiguration _deConfiguration;

        /// <summary>
        /// The <see cref="ParameterTree"/> used in tests.
        /// </summary>
        private ParameterTree _parameterTree;

        /// <summary>
        /// The <see cref="GenomeBuilder"/> used in tests.
        /// </summary>
        private GenomeBuilder _genomeBuilder;

        #endregion
        
        
        #region Public Methods and Operators
        


        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="LocalDifferentialEvolutionInformationFlow"/> class without a
        /// strategy configuration throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingConfiguration()
        {
            Assert.Throws<ArgumentNullException>(
                () => new LocalDifferentialEvolutionInformationFlow(
                    strategyConfiguration: null,
                    parameterTree: this._parameterTree,
                    genomeBuilder: this._genomeBuilder));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="LocalDifferentialEvolutionInformationFlow"/> class without a <see cref="ParameterTree"/>
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingParameterTree()
        {
            Assert.Throws<ArgumentNullException>(
                () => new LocalDifferentialEvolutionInformationFlow(
                    this._deConfiguration,
                    parameterTree: null,
                    genomeBuilder: this._genomeBuilder));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="LocalDifferentialEvolutionInformationFlow"/> class without a <see cref="GenomeBuilder"/>
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingGenomeBuilder()
        {
            Assert.Throws<ArgumentNullException>(
                () => new LocalDifferentialEvolutionInformationFlow(
                    this._deConfiguration,
                    this._parameterTree,
                    genomeBuilder: null));
        }

        /// <summary>
        /// Checks that <see cref="LocalDifferentialEvolutionInformationFlow.DetermineInitialPoints"/>
        /// returns valid individuals based on the incumbent.
        /// </summary>
        [Fact]
        public void DetermineInitialPointsBuildsValidPointsBasedOnIncumbent()
        {
            // Create population.
            var population = this.CreatePopulation();

            // Make sure genomes can be invalid (important: only do this after creating the population!).
            this._genomeBuilder = new ConfigurableGenomeBuilder(
                this._parameterTree,
                isValidFunction: g => (int)g.GetGeneValue("quasi-continuous").GetValue() < this._deConfiguration.MinimumDomainSize / 2,
                mutationRate: 0);
            var informationFlow = new LocalDifferentialEvolutionInformationFlow(
                this._deConfiguration,
                this._parameterTree,
                this._genomeBuilder);

            // Determine initial points.
            var incumbent = this._genomeBuilder.CreateRandomGenome(age: 2);
            var points = informationFlow.DetermineInitialPoints(population, incumbent);

            // Check validity and discrete parameter (they should stay the same).
            foreach (var point in points)
            {
                Assert.True(point.IsValid(), $"Created invalid point, associated genome: {point.Genome}.");
                Assert.Equal(
                    incumbent.GetGeneValue(ExtractIntegerValue.ParameterName).GetValue(),
                    point.Genome.CreateMutableGenome().GetGeneValue(ExtractIntegerValue.ParameterName).GetValue());
            }
        }

        /// <summary>
        /// Checks that <see cref="LocalDifferentialEvolutionInformationFlow.DetermineInitialPoints"/>
        /// throws a <see cref="ArgumentNullException"/> if called without an incumbent.
        /// </summary>
        [Fact]
        public void DetermineInitialPointsThrowsForMissingIncumbent()
        {
            var informationFlow = new LocalDifferentialEvolutionInformationFlow(
                this._deConfiguration,
                this._parameterTree,
                this._genomeBuilder);
            Assert.Throws<ArgumentNullException>(
                () => informationFlow.DetermineInitialPoints(this.CreatePopulation(), currentIncumbent: null));
        }

        /// <summary>
        /// Checks that <see cref="LocalDifferentialEvolutionInformationFlow.DetermineInitialPoints"/>
        /// can deal with tight constraints by using a repair operation.
        /// </summary>
        [Fact]
        public void DetermineInitialPointsUsesRepairOperationIfRequired()
        {
            // Create a parameter tree with a discrete and a continuous aspect.
            var root = new AndNode();
            root.AddChild(new ValueNode<int>(ExtractIntegerValue.ParameterName, new IntegerDomain(-1, 3)));
            root.AddChild(new ValueNode<int>("quasi-continuous", new IntegerDomain(0, this._deConfiguration.MinimumDomainSize + 1)));
            root.AddChild(new ValueNode<double>("continuous", new ContinuousDomain(0, 1)));
            this._parameterTree = new ParameterTree(root);

            // Generate some genomes.
            this._genomeBuilder = new GenomeBuilder(this._parameterTree, this._completeConfiguration);
            var population = this.CreatePopulation();
            var incumbent = this._genomeBuilder.CreateRandomGenome(age: 2);

            // Then setup a condition which only accepts a single value for the continuous parameter.
            this._genomeBuilder = new ConfigurableGenomeBuilder(
                this._parameterTree,
                isValidFunction: g => (double)g.GetGeneValue("continuous").GetValue() == 0.125,
                makeValidFunction: g => g.SetGene("continuous", new Allele<double>(0.125)),
                mutationRate: 0);

            // Try to create points. Due to the strict validity constraint, they will most likely use the repair
            // operation.
            var informationFlow = new LocalDifferentialEvolutionInformationFlow(
                this._deConfiguration,
                this._parameterTree,
                this._genomeBuilder);
            var points = informationFlow.DetermineInitialPoints(population, incumbent);

            // For each point not looking like the incumbent: Check it is valid.
            bool IndividualRepresentsIncumbent(GenomeSearchPoint point)
            {
                return object.Equals(point.Genome.CreateMutableGenome().ToString(), incumbent.ToString());
            }

            foreach (var point in points.Where(point => !IndividualRepresentsIncumbent(point)))
            {
                Assert.True(point.IsValid(), $"{point} is not valid.");
            }
        }

        /// <summary>
        /// Checks that <see cref="LocalDifferentialEvolutionInformationFlow.DetermineInitialPoints"/>
        /// throws a <see cref="TimeoutException"/> if no valid point exists.
        /// </summary>
        [Fact]
        public void DetermineInitialPointsThrowsIfNoValidPointExists()
        {
            // First generate some genomes.
            var population = this.CreatePopulation();
            var incumbent = this._genomeBuilder.CreateRandomGenome(age: 2);

            // Then make sure all subsequent genomes are invalid.
            this._genomeBuilder = new ConfigurableGenomeBuilder(
                this._parameterTree,
                isValidFunction: g => false,
                mutationRate: 0);
            var informationFlow = new LocalDifferentialEvolutionInformationFlow(
                this._deConfiguration,
                this._parameterTree,
                this._genomeBuilder);

            // Try to call the method.
            Assert.Throws<TimeoutException>(() => informationFlow.DetermineInitialPoints(population, incumbent));
        }

        /// <summary>
        /// Verifies that using a competitive population size of 5 throws a <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void DetermineInitialPointsThrowsForJadePopulationSmallerThree()
        {
            // Generate a population with only 5 competitive genomes.
            var population = this.CreatePopulation(numberCompetitives: 5);
            var incumbent = this._genomeBuilder.CreateRandomGenome(age: 2);

            // Try to call the method.
            var informationFlow = new LocalDifferentialEvolutionInformationFlow(
                this._deConfiguration,
                this._parameterTree,
                this._genomeBuilder);
            Assert.Throws<ArgumentException>(() => informationFlow.DetermineInitialPoints(population, incumbent));
        }

        /// <summary>
        /// Verifies that using a competitive population size of 6 does not throw an error.
        /// </summary>
        [Fact]
        public void DetermineInitialPointsDoesNotThrowForJadePopulationEqualToThree()
        {
            // Generate a population with 6 competitive genomes.
            var population = this.CreatePopulation(numberCompetitives: 6);
            var incumbent = this._genomeBuilder.CreateRandomGenome(age: 2);

            // Try to call the method. --> Should work.
            var informationFlow = new LocalDifferentialEvolutionInformationFlow(
                this._deConfiguration,
                this._parameterTree,
                this._genomeBuilder);
            informationFlow.DetermineInitialPoints(population, incumbent);
        }

        /// <summary>
        /// Checks that <see cref="LocalDifferentialEvolutionInformationFlow.DefineCompetitivePopulation"/>,
        /// called with a positive replacement rate:
        /// * replaces that percentage of the competitive population with the best points found, and
        /// * keeps the population's age structure.
        /// </summary>
        [Fact]
        public void DefineCompetitivePopulationWorksWithPositiveReplacementRate()
        {
            var informationFlow = new LocalDifferentialEvolutionInformationFlow(
                this._deConfiguration,
                this._parameterTree,
                this._genomeBuilder);

            // Create a competitive population part s.t. two genomes should be replaced using the default replacement
            // rate of 0.25.
            var originalCompetitives = this.CreateRandomGenomes(number: 6).ToList();

            // Define some search points.
            var searchPoints = this.CreateRandomSearchPoints();
            var incumbent = this._genomeBuilder.CreateRandomGenome(age: 2);

            // Define new competitive population.
            var updatedCompetitives = informationFlow
                .DefineCompetitivePopulation(originalCompetitives, incumbent, searchPoints)
                .ToList();

            // Check if best points were added to population.
            Assert.True(
                updatedCompetitives.Contains(searchPoints[0].Genome.CreateMutableGenome(), new Genome.GeneValueComparator()),
                "Updated population should contain best search point, but does not.");
            Assert.True(
                updatedCompetitives.Contains(searchPoints[1].Genome.CreateMutableGenome(), new Genome.GeneValueComparator()),
                "Updated population should contain second best search point, but does not.");
            Assert.False(
                updatedCompetitives.Contains(searchPoints[2].Genome.CreateMutableGenome(), new Genome.GeneValueComparator()),
                "Updated population should not contain worst search point.");

            // Then check ages.
            for (int age = 0; age < 3; age++)
            {
                Assert.True(
                    originalCompetitives.Count(individual => individual.Age == age) ==
                    updatedCompetitives.Count(individual => individual.Age == age),
                    $"Different number of genomes with age {age}.");
            }

            Assert.False(
                updatedCompetitives.Any(individual => individual.Age < 0 || individual.Age > 3),
                "There exists a genome with age not in age range!");
        }

        /// <summary>
        /// Checks that <see cref="LocalDifferentialEvolutionInformationFlow.DefineCompetitivePopulation"/>,
        /// called with a replacement rate of 0:
        /// * replaces the incumbent by the best point found by DE,
        /// * keeps the incumbent's age, and
        /// * copies all other genomes.
        /// </summary>
        [Fact]
        public void DefineCompetitivePopulationWorksForIncumbentChangeOnly()
        {
            // Create configuration with a replacement rate of 0.
            this._completeConfiguration = LocalDifferentialEvolutionInformationFlowTest.CreateConfiguration(replacementRate: 0);
            this._deConfiguration =
                this._completeConfiguration.ExtractDetailedConfiguration<DifferentialEvolutionStrategyConfiguration>(
                    DifferentialEvolutionStrategyArgumentParser.Identifier);
            var informationFlow = new LocalDifferentialEvolutionInformationFlow(
                this._deConfiguration,
                this._parameterTree,
                this._genomeBuilder);

            // Create a competitive population which contains the same genome twice.
            var originalCompetitives = this.CreateRandomGenomes(number: 6).ToList();
            var incumbent = originalCompetitives[3];
            originalCompetitives.Add(new Genome(incumbent));
            Assert.Equal(
                2,
                originalCompetitives.Count(individual => new Genome.GeneValueComparator().Equals(individual, incumbent)));

            // Call define competitive population with some search points.
            var searchPoints = this.CreateRandomSearchPoints();
            var updatedCompetitives = informationFlow.DefineCompetitivePopulation(
                originalCompetitives,
                incumbent,
                searchPoints);

            // Best point should now replace incumbent in competitive population.
            var expectedCompetitives = originalCompetitives
                .Select(genome => genome.ToString())
                .ToList();
            expectedCompetitives.Remove(incumbent.ToString());
            expectedCompetitives.Add(new Genome(searchPoints[0].Genome.CreateMutableGenome(), incumbent.Age).ToString());
            Assert.Equal(
                expectedCompetitives.OrderBy(x => x).ToArray(),
                updatedCompetitives.Select(genome => genome.ToString()).OrderBy(x => x).ToArray());
        }

        /// <summary>
        /// Checks that <see cref="LocalDifferentialEvolutionInformationFlow.DefineCompetitivePopulation"/>,
        /// called with a replacement rate of 0 and no original incumbent throws a <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void DefineCompetitivePopulationThrowsForIncumbentChangeOnlyWithoutIncumbent()
        {
            // Create configuration with a replacement rate of 0.
            this._completeConfiguration = LocalDifferentialEvolutionInformationFlowTest.CreateConfiguration(replacementRate: 0);
            this._deConfiguration =
                this._completeConfiguration.ExtractDetailedConfiguration<DifferentialEvolutionStrategyConfiguration>(
                    DifferentialEvolutionStrategyArgumentParser.Identifier);
            var informationFlow = new LocalDifferentialEvolutionInformationFlow(
                this._deConfiguration,
                this._parameterTree,
                this._genomeBuilder);

            // Call defineCompetitivePopulation without an incumbent.
            Assert.Throws<ArgumentNullException>(
                () => informationFlow
                    .DefineCompetitivePopulation(
                        originalCompetitives: this.CreateRandomGenomes(number: 6).ToList(),
                        originalIncumbent: null,
                        mostRecentSorting: this.CreateRandomSearchPoints())
                    .ToList());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called before every test.
        /// </summary>
        protected override void InitializeDefault()
        {
            base.InitializeDefault();

            this._completeConfiguration = LocalDifferentialEvolutionInformationFlowTest.CreateConfiguration(replacementRate: 0.25);
            this._deConfiguration =
                this._completeConfiguration.ExtractDetailedConfiguration<DifferentialEvolutionStrategyConfiguration>(
                    DifferentialEvolutionStrategyArgumentParser.Identifier);

            var root = new AndNode();
            root.AddChild(new ValueNode<int>(ExtractIntegerValue.ParameterName, new IntegerDomain(-1, 3)));
            root.AddChild(new ValueNode<int>("quasi-continuous", new IntegerDomain(0, this._deConfiguration.MinimumDomainSize + 1)));
            this._parameterTree = new ParameterTree(root);

            this._genomeBuilder = new GenomeBuilder(this._parameterTree, this._completeConfiguration);
        }

        /// <summary>
        /// Creates a <see cref="AlgorithmTunerConfiguration"/> using differential evolution with a local update
        /// strategy.
        /// </summary>
        /// <param name="replacementRate">The replacement rate.</param>
        /// <returns>The created <see cref="AlgorithmTunerConfiguration"/>.</returns>
        private static AlgorithmTunerConfiguration CreateConfiguration(double replacementRate)
        {
            var deConfigurationBuilder = new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                .SetFocusOnIncumbent(true)
                .SetReplacementRate(replacementRate)
                .SetDifferentialEvolutionConfigurationBuilder(
                    new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder().SetBestPercentage(0.2));

            return new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEnableRacing(true)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .AddDetailedConfigurationBuilder(
                    DifferentialEvolutionStrategyArgumentParser.Identifier,
                    deConfigurationBuilder)
                .Build(maximumNumberParallelEvaluations: 1);
        }

        /// <summary>
        /// Creates a <see cref="Population"/> of <see cref="Genome"/>s according to
        /// <see cref="GenomeBuilder"/>.
        /// </summary>
        /// <param name="numberCompetitives">The number of competitive individuals to create.</param>
        /// <returns>The created <see cref="Population"/>.</returns>
        private Population CreatePopulation(int numberCompetitives = 6)
        {
            var population = new Population(this._completeConfiguration);
            for (int i = 0; i < numberCompetitives; i++)
            {
                population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: i % 3), isCompetitive: true);
            }

            for (int i = 0; i < 3; i++)
            {
                population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: i % 3), isCompetitive: false);
            }

            return population;
        }

        /// <summary>
        /// Creates <see cref="Genome"/>s according to <see cref="GenomeBuilder"/>.
        /// </summary>
        /// <param name="number">The number of <see cref="Genome"/>s to create.</param>
        /// <returns>The created <see cref="Genome"/>s.</returns>
        private IEnumerable<Genome> CreateRandomGenomes(int number)
        {
            for (int i = 0; i < number; i++)
            {
                yield return this._genomeBuilder.CreateRandomGenome(age: i % 3);
            }
        }

        /// <summary>
        /// Creates <see cref="GenomeSearchPoint"/>s according to <see cref="GenomeBuilder"/>.
        /// </summary>
        /// <returns>The created <see cref="GenomeSearchPoint"/>s.</returns>
        private List<GenomeSearchPoint> CreateRandomSearchPoints()
        {
            var searchPoints = new List<GenomeSearchPoint>();
            for (int i = 0; i < 8; i++)
            {
                searchPoints.Add(
                    GenomeSearchPoint.CreateFromGenome(
                        this._genomeBuilder.CreateRandomGenome(age: 0),
                        this._parameterTree,
                        0,
                        this._genomeBuilder));
            }

            return searchPoints;
        }

        #endregion
    }
}
