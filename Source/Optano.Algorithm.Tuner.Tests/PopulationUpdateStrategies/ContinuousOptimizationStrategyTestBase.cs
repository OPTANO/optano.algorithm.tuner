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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestOutOfBox;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData.SamplingStrategies;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.GeneticAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    using Xunit;

    /// <summary>
    /// Contains tests important for any <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/> inheriting from
    /// <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}"/>.
    /// </summary>
    public abstract class ContinuousOptimizationStrategyTestBase : TestBase
    {
        #region Fields

        /// <summary>
        /// The values genomes created via <see cref="GenomeBuilder"/>
        /// will have for <see cref="ExtractIntegerValue.ParameterName"/>, in order of creation.
        /// </summary>
        private readonly int[] _genomeValues = { 1, 42, -3, 0, -1, -2, -3, -4, -5, -6, 1, 3 };

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators.IRunEvaluator{TInstance, TResult}"/> used in
        /// tests.
        /// </summary>
        protected IRunEvaluator<TestInstance, IntegerResult> RunEvaluator { get; } =
            new TargetAlgorithm.InterfaceImplementations.ValueConsideration.SortByValue<TestInstance>();

        /// <summary>
        /// Gets a list of <see cref="TestInstance"/>s consisting of a single instance.
        /// </summary>
        protected List<TestInstance> SingleTestInstance { get; } = new List<TestInstance> { new TestInstance("a") };

        /// <summary>
        /// Gets the structure representing the tunable parameters.
        /// </summary>
        protected ParameterTree ParameterTree { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="Optano.Algorithm.Tuner.Genomes.GenomeBuilder" /> used in tuning.
        /// </summary>
        protected GenomeBuilder GenomeBuilder { get; set; }

        /// <summary>
        /// Gets an <see cref="IActorRef" /> to a <see cref="ResultStorageActor{TInstance,TResult}" />
        /// which knows about all executed target algorithm runs and their results.
        /// </summary>
        protected IActorRef ResultStorageActor { get; private set; }

        /// <summary>
        /// Gets an <see cref="IActorRef" /> to a <see cref="GenerationEvaluationActor" />.
        /// </summary>
        protected IActorRef GenerationEvaluationActor { get; private set; }

        /// <summary>
        /// Gets or sets a <see cref="AlgorithmTunerConfiguration"/> with many default values.
        /// </summary>
        protected AlgorithmTunerConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="CreateTunerConfigurationBuilder"/> should
        /// create a configuration with <see cref="StrategyConfigurationBase{TConfiguration}.FixInstances"/> set to
        /// <c>true</c>.
        /// </summary>
        protected bool FixInstances { get; set; } = false;

        /// <summary>
        /// Gets or sets the <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/> to test.
        /// </summary>
        protected IPopulationUpdateStrategy<TestInstance, IntegerResult> Strategy { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.Initialize"/>
        /// throws a <see cref="ArgumentNullException"/> if called without a population to base the method on.
        /// </summary>
        [Fact]
        public void InitializeThrowsForMissingBasePopulation()
        {
            Assert.Throws<ArgumentNullException>(
                () => this.Strategy.Initialize(
                    basePopulation: null,
                    currentIncumbent: this.CreateIncumbentGenomeWrapper(),
                    instancesForEvaluation: this.SingleTestInstance));
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.Initialize"/>
        /// throws a <see cref="ArgumentOutOfRangeException"/> if the provided population does not contain competitive
        /// genomes.
        /// </summary>
        [Fact]
        public void InitializeThrowsForNoCompetitiveGenomes()
        {
            var population = new Population(this.Configuration);
            population.AddGenome(this.GenomeBuilder.CreateRandomGenome(age: 1), isCompetitive: false);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance));
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.Initialize"/>
        /// resets the continuous optimization method's generation to 0.
        /// </summary>
        [Fact]
        public void InitializeResetsGenerationToZero()
        {
            var population = this.CreatePopulation();

            this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);

            this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            this.Strategy.DumpStatus();

            Assert.Equal(0, this.FindCurrentGeneration());
        }

        /// <summary>
        /// Checks that
        /// <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.PerformIteration"/> throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a negative generation index.
        /// </summary>
        [Fact]
        public void PerformIterationThrowsForNegativeGenerationIndex()
        {
            var population = this.CreatePopulation();
            this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => this.Strategy.PerformIteration(-1, this.SingleTestInstance));
        }

        /// <summary>
        /// Checks that
        /// <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.PerformIteration"/> performs
        /// an iteration of the continuous optimization method.
        /// </summary>
        [Fact]
        public void PerformIterationPerformsContinuousOptimizationMethodIteration()
        {
            var population = this.CreatePopulation();

            this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.DumpStatus();

            Assert.Equal(1, this.FindCurrentGeneration());
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}"/> only updates
        /// the instances on phase initialization if
        /// <see cref="StrategyConfigurationBase{TConfiguration}.FixInstances"/> is turned on.
        /// </summary>
        [Fact]
        public void InstancesAreOnlyUpdatedOncePerPhaseIfConfigurationIndicatesThat()
        {
            // Create configuration which determines that instances should be fixed in each phase.
            this.FixInstances = true;
            this.Configuration = this.CreateTunerConfigurationBuilder().Build();
            this.Strategy = this.CreateStrategy(this.Configuration);

            var severalInstances = new List<TestInstance> { new TestInstance("c"), new TestInstance("d") };
            var population = this.CreatePopulation();

            // Start first phase.
            this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            this.Strategy.PerformIteration(0, severalInstances);
            this.Strategy.DumpStatus();

            // Check instances have not been updated.
            var incumbent = this.Strategy.FindIncumbentGenome();
            Assert.Single(
                incumbent.IncumbentInstanceResults);

            // Start second phase.
            this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), severalInstances);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.DumpStatus();

            // Instances should have been updated now.
            incumbent = this.Strategy.FindIncumbentGenome();
            Assert.Equal(
                2,
                incumbent.IncumbentInstanceResults.Count);
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint, TInstance,TResult}.FinishPhase"/>
        /// only produces valid genomes.
        /// </summary>
        [Fact]
        public void FinishPhaseOnlyReturnsValidGenomes()
        {
            var originalPopulation = this.CreatePopulation();
            var incumbent = this.CreateIncumbentGenomeWrapper();
            this.Strategy.Initialize(originalPopulation, incumbent, this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.DumpStatus();

            var updatedPopulation = this.Strategy.FinishPhase(originalPopulation);
            foreach (var genome in updatedPopulation.AllGenomes)
            {
                Assert.True(this.GenomeBuilder.IsGenomeValid(genome), $"{genome} should be valid.");
            }
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.FinishPhase"/>
        /// throws a <see cref="ArgumentNullException"/> if called without a base population.
        /// </summary>
        [Fact]
        public void FinishPhaseThrowsForMissingBasePopulation()
        {
            this.Strategy.Initialize(
                this.CreatePopulation(),
                this.CreateIncumbentGenomeWrapper(),
                this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);

            Assert.Throws<ArgumentNullException>(() => this.Strategy.FinishPhase(basePopulation: null));
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint, TInstance,TResult}.FinishPhase"/>
        /// copies non-competitive genomes from the original population.
        /// </summary>
        [Fact]
        public void FinishPhaseCopiesNonCompetitive()
        {
            var originalPopulation = this.CreatePopulation();
            this.Strategy.Initialize(originalPopulation, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.DumpStatus();

            var updatedPopulation = this.Strategy.FinishPhase(originalPopulation);

            Assert.Equal(
                originalPopulation.GetNonCompetitiveMates().Select(genome => genome.ToString()).OrderBy(x => x).ToArray(),
                updatedPopulation.GetNonCompetitiveMates().Select(genome => genome.ToString()).OrderBy(x => x).ToArray());
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint, TInstance,TResult}.FinishPhase"/>
        /// returns the base population in case
        /// <see cref="ContinuousOptimizationStrategyBase{TSearchPoint, TInstance,TResult}.PerformIteration"/> was not
        /// called beforehand.
        /// </summary>
        [Fact]
        public void FinishPhaseReturnsOriginalPopulationWithoutPriorUpdate()
        {
            // Execute a complete phase beforehand.
            var basePopulation = this.CreatePopulation();
            this.Strategy.Initialize(basePopulation, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            var dePopulation = this.Strategy.FinishPhase(basePopulation);

            Assert.NotEqual(dePopulation, basePopulation);
            this.Strategy.Initialize(basePopulation, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            Assert.Equal(
                basePopulation,
                this.Strategy.FinishPhase(basePopulation));
        }

        /// <summary>
        /// Checks that <see cref="IPopulationUpdateStrategy{TInstance,TResult}.NextStrategy"/> throws a
        /// <see cref="ArgumentNullException"/> if called without any strategies.
        /// </summary>
        [Fact]
        public void NextStrategyThrowsForMissingStrategies()
        {
            Assert.Throws<ArgumentNullException>(() => this.Strategy.NextStrategy(populationUpdateStrategies: null));
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.NextStrategy"/>
        /// returns an index corresponding to a <see cref="GgaStrategy{TInstance,TResult}"/>.
        /// </summary>
        [Fact]
        public void NextStrategyIsGga()
        {
            var geneticEngineering =
                new GeneticEngineering<StandardRandomForestLearner<ReuseOldTreesStrategy>, GenomePredictionForestModel<GenomePredictionTree>,
                    ReuseOldTreesStrategy>(
                    this.ParameterTree,
                    this.Configuration);
            var ggaStrategy = new GgaStrategy<TestInstance, IntegerResult>(
                this.Configuration,
                this.ParameterTree,
                this.GenomeBuilder,
                this.GenerationEvaluationActor,
                geneticEngineering);

            var strategies = new List<IPopulationUpdateStrategy<TestInstance, IntegerResult>>
                                 { this.Strategy, ggaStrategy };
            int nextIndex = this.Strategy.NextStrategy(strategies);
            Assert.Equal(1, nextIndex);
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.DumpStatus"/>
        /// creates status files at the correct places.
        /// </summary>
        [Fact]
        public abstract void DumpStatusCreatesStatusFiles();

        /// <summary>
        /// Checks that all properties important for the status have been dumped to file.
        /// </summary>
        [Fact]
        public abstract void DumpedStatusHasNoEmptyProperties();

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.UseStatusDump"/>
        /// reads relevant information from file.
        /// </summary>
        [Fact]
        public abstract void UseStatusDumpReadsInformationFromFile();

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.UseStatusDump"/>
        /// updates the evaluation instances.
        /// </summary>
        [Fact]
        public void UseStatusDumpUpdatesEvaluationInstances()
        {
            // Create configuration which determines that instances should be fixed in each phase.
            // This makes sure that the update is required.
            this.FixInstances = true;
            this.Configuration = this.CreateTunerConfigurationBuilder().Build();
            this.Strategy = this.CreateStrategy(this.Configuration);

            // Use the strategy.
            var severalInstances = new List<TestInstance> { new TestInstance("c"), new TestInstance("d") };
            this.Strategy.Initialize(this.CreatePopulation(), this.CreateIncumbentGenomeWrapper(), severalInstances);
            this.Strategy.PerformIteration(0, severalInstances);
            this.Strategy.DumpStatus();

            // Create new strategy to read the status dump.
            var newStrategy = this.CreateStrategy(this.Configuration);
            newStrategy.UseStatusDump(null);
            newStrategy.PerformIteration(0, severalInstances);

            // Check instances are as before.
            var incumbent = newStrategy.FindIncumbentGenome();
            Assert.Equal(
                2,
                incumbent.IncumbentInstanceResults.Count);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called before every test case.
        /// </summary>
        protected override void InitializeDefault()
        {
            this.Configuration = this.CreateTunerConfigurationBuilder().Build();
            this.ActorSystem = ActorSystem.Create(TestBase.ActorSystemName, this.Configuration.AkkaConfiguration);

            // Create parameter tree with quasi-continuous and categorical parameters.
            var root = new AndNode();
            root.AddChild(
                new ValueNode<int>(ExtractIntegerValue.ParameterName, new IntegerDomain(-6, 143)));
            root.AddChild(
                new ValueNode<string>("categorical", new CategoricalDomain<string>(new List<string> { "a", "b" })));
            this.ParameterTree = new ParameterTree(root);

            this.ResultStorageActor = this.ActorSystem.ActorOf(
                Props.Create(() => new ResultStorageActor<TestInstance, IntegerResult>()),
                AkkaNames.ResultStorageActor);

            this.GenerationEvaluationActor = this.ActorSystem.ActorOf(
                Props.Create(
                    () => new GenerationEvaluationActor<ExtractIntegerValue, TestInstance, IntegerResult>(
                        new ExtractIntegerValueCreator(),
                        this.RunEvaluator,
                        this.Configuration,
                        this.ResultStorageActor,
                        this.ParameterTree)),
                AkkaNames.GenerationEvaluationActor);

            this.GenomeBuilder = new ValueGenomeBuilder(this.ParameterTree, this.Configuration, this._genomeValues);

            this.Strategy = this.CreateStrategy(this.Configuration);
        }

        /// <summary>
        /// Creates a builder for a valid <see cref="AlgorithmTunerConfiguration"/>.
        /// </summary>
        /// <returns>The created <see cref="AlgorithmTunerConfiguration"/>.</returns>
        protected virtual AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder CreateTunerConfigurationBuilder()
        {
            return new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMaximumNumberParallelEvaluations(1)
                .SetEnableRacing(false)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder());
        }

        /// <summary>
        /// Creates a new <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/> used in testing.
        /// </summary>
        /// <param name="configuration">The configuration to use in creating the strategy.</param>
        /// <returns>The newly created <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/>.</returns>
        protected abstract IPopulationUpdateStrategy<TestInstance, IntegerResult> CreateStrategy(AlgorithmTunerConfiguration configuration);

        /// <summary>
        /// Finds the continuous optimization method's current generation, e.g. by reading the latest status file.
        /// </summary>
        /// <returns>The current generation.</returns>
        protected abstract int FindCurrentGeneration();

        /// <summary>
        /// Creates a <see cref="Population"/> of <see cref="Genome"/>s according to
        /// <see cref="GenomeBuilder"/>.
        /// </summary>
        /// <returns>The created <see cref="Population"/>.</returns>
        protected Population CreatePopulation()
        {
            var population = new Population(this.Configuration);
            for (int i = 0; i < 6; i++)
            {
                population.AddGenome(this.GenomeBuilder.CreateRandomGenome(age: i % 3), isCompetitive: true);
            }

            for (int i = 0; i < 3; i++)
            {
                population.AddGenome(this.GenomeBuilder.CreateRandomGenome(age: i % 3), isCompetitive: false);
            }

            return population;
        }

        /// <summary>
        /// Creates a <see cref="IncumbentGenomeWrapper{TResult}"/> with generation 0, a random genome and empty
        /// results.
        /// </summary>
        /// <returns>The created <see cref="IncumbentGenomeWrapper{TResult}"/>.</returns>
        protected IncumbentGenomeWrapper<IntegerResult> CreateIncumbentGenomeWrapper()
        {
            return new IncumbentGenomeWrapper<IntegerResult>
                       {
                           IncumbentGeneration = 0,
                           IncumbentGenome = this.GenomeBuilder.CreateRandomGenome(age: 0),
                           IncumbentInstanceResults = new List<IntegerResult>().ToImmutableList(),
                       };
        }

        #endregion
    }
}