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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.GeneticAlgorithm
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Akka.Actor;

    using Optano.Algorithm.Tuner;
    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Actors;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestOutOfBox;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData.SamplingStrategies;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Global;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Local;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.GeneticAlgorithm;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    using Xunit;

    using SortByValue = Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration.SortByValue;

    /// <summary>
    /// Contains tests for <see cref="GgaStrategy{TInstance,TResult}"/>.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class GgaStrategyTest : TestBase
    {
        #region Fields

        /// <summary>
        /// Path to which the status file will get written in tests.
        /// </summary>
        private readonly string _statusFilePath = PathUtils.GetAbsolutePathFromExecutableFolderRelative(Path.Combine("status", GgaStatus.FileName));

        /// <summary>
        /// The <see cref="ParameterTree"/> to use in tests.
        /// </summary>
        private readonly ParameterTree _parameterTree = new ParameterTree(
            new ValueNode<int>(ExtractIntegerValue.ParameterName, new IntegerDomain()));

        /// <summary>
        /// The values genomes created via <see cref="_genomeBuilder"/>
        /// will have for <see cref="ExtractIntegerValue.ParameterName"/>, in order of creation.
        /// </summary>
        private readonly int[] _genomeValues = { 1, 42, -3, 0, -1, -2, -3, -4, -5, -6 };

        /// <summary>
        /// A single <see cref="TestInstance"/>.
        /// </summary>
        private readonly List<TestInstance> _singleTestInstance = new List<TestInstance> { new TestInstance("a") };

        /// <summary>
        /// The <see cref="AlgorithmTunerConfiguration"/> to use in tests.
        /// </summary>
        private AlgorithmTunerConfiguration _configuration;

        /// <summary>
        /// The <see cref="GenomeBuilder"/> to use in tests.
        /// </summary>
        private GenomeBuilder _genomeBuilder;

        /// <summary>
        /// The <see cref="IActorRef" /> to a <see cref="ResultStorageActor{TInstance,TResult}" /> to
        /// use in tests.
        /// </summary>
        private IActorRef _resultStorageActor;

        /// <summary>
        /// The <see cref="IActorRef" /> to a <see cref="GenomeSorter{TInstance,TResult}" /> to
        /// use in tests.
        /// </summary>
        private IActorRef _genomeSorter;

        /// <summary>
        /// The <see cref="IActorRef" /> to a <see cref="TournamentSelector{TTargetAlgorithm,TInstance,TResult}" /> to
        /// use in tests.
        /// </summary>
        private IActorRef _tournamentSelector;

        /// <summary>
        /// The <see cref="IGeneticEngineering"/> to use in tests.
        /// </summary>
        private IGeneticEngineering _geneticEngineering;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that initializing an instance of the <see cref="GgaStrategy{TInstance,TResult}"/> class without a configuration
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void MissingConfigurationThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GgaStrategy<TestInstance, IntegerResult>(
                    configuration: null,
                    parameterTree: this._parameterTree,
                    genomeBuilder: this._genomeBuilder,
                    tournamentSelector: this._tournamentSelector,
                    geneticEngineering: this._geneticEngineering));
        }

        /// <summary>
        /// Verifies that initializing an instance of the <see cref="GgaStrategy{TInstance,TResult}"/> class without a parameter tree
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void MissingParameterTreeThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GgaStrategy<TestInstance, IntegerResult>(
                    this._configuration,
                    parameterTree: null,
                    genomeBuilder: this._genomeBuilder,
                    tournamentSelector: this._tournamentSelector,
                    geneticEngineering: this._geneticEngineering));
        }

        /// <summary>
        /// Verifies that initializing an instance of the <see cref="GgaStrategy{TInstance,TResult}"/> class with a genome builder set to
        /// null throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void MissingGenomeBuilderThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GgaStrategy<TestInstance, IntegerResult>(
                    this._configuration,
                    this._parameterTree,
                    genomeBuilder: null,
                    tournamentSelector: this._tournamentSelector,
                    geneticEngineering: this._geneticEngineering));
        }

        /// <summary>
        /// Verifies that initializing an instance of the <see cref="GgaStrategy{TInstance,TResult}"/> class with
        /// the tournament selector set to null throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void MissingTournamentSelectorThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GgaStrategy<TestInstance, IntegerResult>(
                    this._configuration,
                    this._parameterTree,
                    this._genomeBuilder,
                    tournamentSelector: null,
                    geneticEngineering: this._geneticEngineering));
        }

        /// <summary>
        /// Verifies that initializing an instance of the <see cref="GgaStrategy{TInstance,TResult}"/> class with
        /// the genetic engineering set to null throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void MissingGeneticEngineeringThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GgaStrategy<TestInstance, IntegerResult>(
                    this._configuration,
                    this._parameterTree,
                    this._genomeBuilder,
                    this._tournamentSelector,
                    geneticEngineering: null));
        }

        /// <summary>
        /// Checks that the best genome does not die even if it is too old.
        /// </summary>
        [Fact]
        public void IsElitist()
        {
            // By definition, best genome is the second created genome in population (value of 42).
            var oldGenome = this._genomeBuilder.CreateRandomGenome(age: this._configuration.MaxGenomeAge);
            var bestGenome = this._genomeBuilder.CreateRandomGenome(age: this._configuration.MaxGenomeAge);
            var population = new Population(this._configuration);
            population.AddGenome(bestGenome, isCompetitive: true);
            population.AddGenome(oldGenome, isCompetitive: true);
            population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: 0), isCompetitive: false);

            var strategy = this.CreateTestStrategy();
            strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), null);
            strategy.PerformIteration(0, this._singleTestInstance);
            population = strategy.FinishPhase(null);

            // Old genomes should die if they are not the best genome.
            var competitivePopulation = population.GetCompetitiveIndividuals();
            Assert.True(
                competitivePopulation.Contains(bestGenome, new Genome.GeneValueComparator()),
                $"{TestUtils.PrintList(competitivePopulation)} does not contain {bestGenome}.");
            Assert.False(
                competitivePopulation.Contains(oldGenome, new Genome.GeneValueComparator()),
                $"{TestUtils.PrintList(competitivePopulation)} contains {oldGenome}.");
        }

        /// <summary>
        /// Checks that <see cref="GgaStrategy{TInstance, TResult}.PerformIteration"/> ages genomes.
        /// </summary>
        [Fact]
        public void PerformIterationAgesGenomes()
        {
            var competitive = this._genomeBuilder.CreateRandomGenome(age: 0);
            var nonCompetitive = this._genomeBuilder.CreateRandomGenome(age: 2);
            var population = new Population(this._configuration);
            population.AddGenome(competitive, true);
            population.AddGenome(nonCompetitive, false);

            var strategy = this.CreateTestStrategy();
            strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), null);
            strategy.PerformIteration(0, this._singleTestInstance);
            population = strategy.FinishPhase(null);

            competitive = population.GetCompetitiveIndividuals().Single();
            nonCompetitive = population.GetNonCompetitiveMates().Single();
            Assert.Equal(1, competitive.Age);
            Assert.Equal(3, nonCompetitive.Age);
        }

        /// <summary>
        /// Checks that <see cref="GgaStrategy{TInstance,TResult}.PerformIteration"/> creates as many new
        /// <see cref="Genome"/>s as die (for each population part).
        /// </summary>
        [Fact]
        public void PerformIterationKeepsPopulationStable()
        {
            var population = new Population(this._configuration);
            // Competitive genome: 1 will die due to age, one of the old ones won't because it is the elite.
            population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: 1), true);
            population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: 100), true);
            population.AddGenome(this._genomeBuilder.CreateRandomGenome(this._configuration.MaxGenomeAge), true);
            // Non-competitive genomes: 2 will die.
            population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: 1), false);
            population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: 1), false);
            population.AddGenome(this._genomeBuilder.CreateRandomGenome(this._configuration.MaxGenomeAge), false);
            population.AddGenome(this._genomeBuilder.CreateRandomGenome(this._configuration.MaxGenomeAge), false);

            var strategy = this.CreateTestStrategy();
            strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), null);
            strategy.PerformIteration(0, this._singleTestInstance);
            population = strategy.FinishPhase(null);

            var competitive = population.GetCompetitiveIndividuals().ToList();
            var nonCompetitive = population.GetNonCompetitiveMates().ToList();
            Assert.Equal(3, competitive.Count);
            Assert.True(
                1 == competitive.Count(genome => genome.Age == 1),
                "Wrong number of new competitive genomes added.");
            Assert.Equal(4, nonCompetitive.Count);
            Assert.True(
                2 == nonCompetitive.Count(genome => genome.Age == 1),
                "Wrong number of new non-competitive genomes added.");
        }

        /// <summary>
        /// Checks that <see cref="GgaStrategy{TInstance,TResult}.NextStrategy"/> chooses the next strategy
        /// according to configuration.
        /// </summary>
        [Fact]
        public void NextStrategyIsChosenAccordingToConfiguration()
        {
            this._configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.Jade)
                .AddDetailedConfigurationBuilder(
                    DifferentialEvolutionStrategyArgumentParser.Identifier,
                    new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                        .SetDifferentialEvolutionConfigurationBuilder(
                            new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder()))
                .AddDetailedConfigurationBuilder(
                    CovarianceMatrixAdaptationStrategyArgumentParser.Identifier,
                    new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder())
                .BuildWithFallback(this._configuration);

            var possibleStrategies = new List<IPopulationUpdateStrategy<TestInstance, IntegerResult>>
                                         {
                                             this.CreateTestStrategy(),
                                             new DifferentialEvolutionStrategy<TestInstance, IntegerResult>(
                                                 this._configuration,
                                                 this._parameterTree,
                                                 this._genomeBuilder,
                                                 this._genomeSorter,
                                                 this._resultStorageActor),
                                             new LocalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                                                 this._configuration,
                                                 this._parameterTree,
                                                 this._genomeBuilder,
                                                 this._genomeSorter,
                                                 this._resultStorageActor),
                                             new GlobalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                                                 this._configuration,
                                                 this._parameterTree,
                                                 this._genomeBuilder,
                                                 this._genomeSorter,
                                                 this._resultStorageActor),
                                         };

            var ggaJadeStrategy = this.CreateTestStrategy();
            int nextStrategy = ggaJadeStrategy.NextStrategy(possibleStrategies);
            Assert.Equal(1, nextStrategy);

            this._configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.None)
                .BuildWithFallback(this._configuration);
            var pureGgaStrategy = this.CreateTestStrategy();
            nextStrategy = pureGgaStrategy.NextStrategy(possibleStrategies);
            Assert.Equal(0, nextStrategy);

            this._configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.CmaEs)
                .AddDetailedConfigurationBuilder(
                    CovarianceMatrixAdaptationStrategyArgumentParser.Identifier,
                    new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                        .SetFocusOnIncumbent(true))
                .BuildWithFallback(this._configuration);
            var ggaLocalCmaEsStrategy = this.CreateTestStrategy();
            nextStrategy = ggaLocalCmaEsStrategy.NextStrategy(possibleStrategies);
            Assert.Equal(2, nextStrategy);

            this._configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.CmaEs)
                .AddDetailedConfigurationBuilder(
                    CovarianceMatrixAdaptationStrategyArgumentParser.Identifier,
                    new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                        .SetFocusOnIncumbent(false))
                .BuildWithFallback(this._configuration);
            var ggaGlobalCmaEsStrategy = this.CreateTestStrategy();
            nextStrategy = ggaGlobalCmaEsStrategy.NextStrategy(possibleStrategies);
            Assert.Equal(3, nextStrategy);
        }

        /// <summary>
        /// Checks that <see cref="GgaStrategy{TInstance,TResult}.HasTerminated"/> checks the number of population
        /// updates that have happened in the current phase.
        /// </summary>
        [Fact]
        public void HasTerminatedConsidersGenerationsInPhase()
        {
            // Do not use int.MaxValue generations for the strategy.
            this._configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMaximumNumberGgaGenerations(20)
                .BuildWithFallback(this._configuration);

            var population = new Population(this._configuration);
            population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: 1), true);

            var strategy = this.CreateTestStrategy();
            strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), null);

            // First phase: One generation less than needed.
            for (int generation = 0; generation < this._configuration.MaximumNumberGgaGenerations - 1; generation++)
            {
                strategy.PerformIteration(generation, this._singleTestInstance);
            }

            Assert.False(strategy.HasTerminated(), "Should not have terminated yet.");

            // Start a new phase.
            strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), null);
            for (int generation = 0; generation < this._configuration.MaximumNumberGgaGenerations - 1; generation++)
            {
                strategy.PerformIteration(generation, this._singleTestInstance);
            }

            Assert.False(strategy.HasTerminated(), "Should not have terminated yet.");
            strategy.PerformIteration(this._configuration.MaximumNumberGgaGenerations - 1, this._singleTestInstance);
            Assert.True(strategy.HasTerminated(), "Should have terminated.");
        }

        /// <summary>
        /// Checks that <see cref="GgaStrategy{TInstance,TResult}.DumpStatus"/> creates a status file at the correct
        /// place.
        /// </summary>
        [Fact]
        public void DumpStatusCreatesStatusFile()
        {
            var strategy = this.CreateTestStrategy();
            strategy.DumpStatus();

            Assert.True(File.Exists(this._statusFilePath), $"No file at path {this._statusFilePath}.");
        }

        /// <summary>
        /// Checks that all properties important for the status have been dumped to file.
        /// </summary>
        [Fact]
        public void DumpedStatusHasNoEmptyProperties()
        {
            var population = new Population(this._configuration);
            population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: 1), true);

            var strategy = this.CreateTestStrategy();
            strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), null);
            strategy.PerformIteration(0, this._singleTestInstance);
            strategy.DumpStatus();

            // Check last status dump
            var lastStatus = StatusBase.ReadFromFile<GgaStatus>(this._statusFilePath);
            Assert.Equal(
                1,
                lastStatus.Population.GetCompetitiveIndividuals().Count);
            Assert.Equal(
                1,
                lastStatus.IterationCounter);
            Assert.True(lastStatus.AllKnownRanks.Count > 0);
        }

        /// <summary>
        /// Checks that <see cref="GgaStrategy{TInstance,TResult}.UseStatusDump"/> reads the population from file.
        /// </summary>
        [Fact]
        public void UseStatusDumpReadsPopulationFromFile()
        {
            var population = new Population(this._configuration);
            var competitive = this._genomeBuilder.CreateRandomGenome(age: 4);
            var nonCompetitive = this._genomeBuilder.CreateRandomGenome(age: 1);
            population.AddGenome(competitive, true);
            population.AddGenome(nonCompetitive, false);

            var strategy = this.CreateTestStrategy();
            strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), null);
            strategy.DumpStatus();

            // Create new strategy to read the status dump.
            var newStrategy = this.CreateTestStrategy();
            newStrategy.UseStatusDump(null);
            var strategyPopulation = newStrategy.FinishPhase(null);

            // Use strings in comparisons to also compare age.
            Assert.Equal(
                population.GetCompetitiveIndividuals().Select(genome => genome.ToString()).OrderBy(x => x).ToList(),
                strategyPopulation.GetCompetitiveIndividuals().Select(genome => genome.ToString()).OrderBy(x => x).ToList());
            Assert.Equal(
                population.GetNonCompetitiveMates().Select(genome => genome.ToString()).OrderBy(x => x).ToList(),
                strategyPopulation.GetNonCompetitiveMates().Select(genome => genome.ToString()).OrderBy(x => x).ToList());
        }

        /// <summary>
        /// Checks that <see cref="GgaStrategy{TInstance,TResult}.UseStatusDump"/> reads the iteration counter
        /// from file.
        /// </summary>
        [Fact]
        public void UseStatusDumpReadsIterationCounterFromFile()
        {
            var population = new Population(this._configuration);
            population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: 1), true);

            var strategy = this.CreateTestStrategy();
            strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), null);
            strategy.PerformIteration(0, this._singleTestInstance);
            strategy.PerformIteration(0, this._singleTestInstance);
            strategy.DumpStatus();

            // Create new strategy to read the status dump.
            var newStrategy = this.CreateTestStrategy();
            newStrategy.UseStatusDump(null);
            newStrategy.DumpStatus();
            var newStrategyStatus = StatusBase.ReadFromFile<GgaStatus>(this._statusFilePath);

            Assert.Equal(
                2,
                newStrategyStatus.IterationCounter);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called before every test case.
        /// </summary>
        protected override void InitializeDefault()
        {
            this._configuration = this.GetDefaultAlgorithmTunerConfiguration();
            this._genomeBuilder = new ValueGenomeBuilder(this._parameterTree, this._configuration, this._genomeValues);
            this._geneticEngineering =
                new GeneticEngineering<StandardRandomForestLearner<ReuseOldTreesStrategy>, GenomePredictionForestModel<GenomePredictionTree>,
                    ReuseOldTreesStrategy>(
                    this._parameterTree,
                    this._configuration);

            this.ActorSystem = ActorSystem.Create(TestBase.ActorSystemName, this._configuration.AkkaConfiguration);
            this._resultStorageActor = this.ActorSystem.ActorOf(
                Props.Create(() => new ResultStorageActor<TestInstance, IntegerResult>()),
                AkkaNames.ResultStorageActor);
            this._genomeSorter = this.ActorSystem.ActorOf(
                Props.Create(() => new GenomeSorter<TestInstance, IntegerResult>(new SortByValue())),
                AkkaNames.GenomeSorter);
            this._tournamentSelector = this.CreateTournamentSelector(
                this.ActorSystem,
                new ExtractIntegerValueCreator(),
                new SortByValue());
        }

        /// <summary>
        /// Creates a <see cref="GgaStrategy{TInstance,TResult}"/> to use in tests.
        /// </summary>
        /// <returns>The created <see cref="GgaStrategy{TInstance,TResult}"/>.</returns>
        private GgaStrategy<TestInstance, IntegerResult> CreateTestStrategy()
        {
            return new GgaStrategy<TestInstance, IntegerResult>(
                this._configuration,
                this._parameterTree,
                this._genomeBuilder,
                this._tournamentSelector,
                this._geneticEngineering);
        }

        /// <summary>
        /// Creates a <see cref="TournamentSelector{TTargetAlgorithm, TInstance, TResult}"/>.
        /// </summary>
        /// <typeparam name="TTargetAlgorithm">Algorithm to execute.</typeparam>
        /// <typeparam name="TInstance">Type of instances the algorithm takes.</typeparam>
        /// <typeparam name="TResult">Type of result the algorithm returns.</typeparam>
        /// <param name="system">The <see cref="ActorSystem"/> to add the actor to.</param>
        /// <param name="targetAlgorithmFactory">Specifies how to create an algorithm instance.</param>
        /// <param name="runEvaluator">Specifies how runs should be compared.</param>
        /// <returns>The created <see cref="TournamentSelector{TTargetAlgorithm, TInstance, TResult}"/>.</returns>
        private IActorRef CreateTournamentSelector<TTargetAlgorithm, TInstance, TResult>(
            ActorSystem system,
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            IRunEvaluator<TResult> runEvaluator)
            where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult> where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
        {
            return system.ActorOf(
                Props.Create(
                    () => new TournamentSelector<TTargetAlgorithm, TInstance, TResult>(
                        targetAlgorithmFactory,
                        runEvaluator,
                        this._configuration,
                        this._resultStorageActor,
                        this._parameterTree)),
                AkkaNames.TournamentSelector);
        }

        /// <summary>
        /// Creates a <see cref="IncumbentGenomeWrapper{TResult}"/> with generation 0, a random genome and empty
        /// results.
        /// </summary>
        /// <returns>The created <see cref="IncumbentGenomeWrapper{TResult}"/>.</returns>
        private IncumbentGenomeWrapper<IntegerResult> CreateIncumbentGenomeWrapper()
        {
            return new IncumbentGenomeWrapper<IntegerResult>
                       {
                           IncumbentGeneration = 0,
                           IncumbentGenome = this._genomeBuilder.CreateRandomGenome(age: 0),
                           IncumbentInstanceResults = new List<IntegerResult>().ToImmutableList(),
                       };
        }

        #endregion
    }
}