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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.DifferentialEvolution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.SearchPoint;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/> class.
    /// </summary>
    public class DifferentialEvolutionStrategyTest : ContinuousOptimizationStrategyTestBase
    {
        #region Fields

        /// <summary>
        /// Path to which the status file will get written in tests.
        /// </summary>
        private readonly string _statusFilePath = PathUtils.GetAbsolutePathFromExecutableFolderRelative(
            Path.Combine("status", DifferentialEvolutionStrategyStatus<TestInstance>.FileName));

        /// <summary>
        /// Path to which the DE runner status file will get written in tests.
        /// </summary>
        private readonly string _deStatusFilePath = PathUtils.GetAbsolutePathFromExecutableFolderRelative(
            Path.Combine("status", DifferentialEvolutionStatus<GenomeSearchPoint>.FileName));

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/> class without a configuration
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingConfiguration()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DifferentialEvolutionStrategy<TestInstance, IntegerResult>(
                    configuration: null,
                    parameterTree: this.ParameterTree,
                    genomeBuilder: this.GenomeBuilder,
                    this.GenerationEvaluationActor,
                    targetRunResultStorage: this.ResultStorageActor));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/> class without a <see cref="ParameterTree"/>
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingParameterTree()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DifferentialEvolutionStrategy<TestInstance, IntegerResult>(
                    this.Configuration,
                    parameterTree: null,
                    genomeBuilder: this.GenomeBuilder,
                    this.GenerationEvaluationActor,
                    targetRunResultStorage: this.ResultStorageActor));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/> class without a <see cref="GenomeBuilder"/>
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingGenomeBuilder()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DifferentialEvolutionStrategy<TestInstance, IntegerResult>(
                    this.Configuration,
                    this.ParameterTree,
                    genomeBuilder: null,
                    this.GenerationEvaluationActor,
                    targetRunResultStorage: this.ResultStorageActor));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/> class without a generation evaluation actor
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingGenerationEvaluationActor()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DifferentialEvolutionStrategy<TestInstance, IntegerResult>(
                    this.Configuration,
                    this.ParameterTree,
                    this.GenomeBuilder,
                    null,
                    targetRunResultStorage: this.ResultStorageActor));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/> class without a result storage actor
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingResultStorageActor()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DifferentialEvolutionStrategy<TestInstance, IntegerResult>(
                    this.Configuration,
                    this.ParameterTree,
                    this.GenomeBuilder,
                    this.GenerationEvaluationActor,
                    targetRunResultStorage: null));
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.Initialize"/>
        /// keeps the current means the same.
        /// </summary>
        [Fact]
        public void InitializeKeepsCurrentMeans()
        {
            var population = this.CreatePopulation();

            this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance, 0, false);
            this.Strategy.PerformIteration(0, this.SingleTestInstance, false);
            this.Strategy.PerformIteration(0, this.SingleTestInstance, false);
            this.Strategy.DumpStatus();

            var originalStatus =
                StatusBase.ReadFromFile<DifferentialEvolutionStatus<GenomeSearchPoint>>(this._deStatusFilePath);
            var crossoverRate = originalStatus.MeanCrossoverRate;
            var mutationFactor = originalStatus.MeanMutationFactor;

            this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance, 0, false);
            this.Strategy.DumpStatus();

            var newlyInitializedStatus =
                StatusBase.ReadFromFile<DifferentialEvolutionStatus<GenomeSearchPoint>>(this._deStatusFilePath);
            Assert.Equal(
                crossoverRate,
                newlyInitializedStatus.MeanCrossoverRate);
            Assert.Equal(
                mutationFactor,
                newlyInitializedStatus.MeanMutationFactor);
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.FinishPhase"/>
        /// depends on <see cref="StrategyConfigurationBase{TConfiguration}.FocusOnIncumbent"/>.
        /// </summary>
        [Fact]
        public void InformationFlowDependsOnFocusOnIncumbentOption()
        {
            // Create two strategies: One with focus on incumbent, one without.
            var noIncumbentFocusConfiguration = base.CreateTunerConfigurationBuilder()
                .AddDetailedConfigurationBuilder(
                    DifferentialEvolutionStrategyArgumentParser.Identifier,
                    new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                        .SetFocusOnIncumbent(false)
                        .SetReplacementRate(0)
                        .SetDifferentialEvolutionConfigurationBuilder(
                            new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder())).Build();
            var globalStrategy = new DifferentialEvolutionStrategy<TestInstance, IntegerResult>(
                noIncumbentFocusConfiguration,
                this.ParameterTree,
                this.GenomeBuilder,
                this.GenerationEvaluationActor,
                this.ResultStorageActor);
            var incumbentFocusConfiguration = base.CreateTunerConfigurationBuilder()
                .AddDetailedConfigurationBuilder(
                    DifferentialEvolutionStrategyArgumentParser.Identifier,
                    new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                        .SetFocusOnIncumbent(true)
                        .SetReplacementRate(0)
                        .SetDifferentialEvolutionConfigurationBuilder(
                            new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder())).Build();
            var localStrategy = new DifferentialEvolutionStrategy<TestInstance, IntegerResult>(
                incumbentFocusConfiguration,
                this.ParameterTree,
                this.GenomeBuilder,
                this.GenerationEvaluationActor,
                this.ResultStorageActor);

            // Create a population.
            var basePopulation = this.CreatePopulation();
            var incumbent = this.CreateIncumbentGenomeWrapper();
            basePopulation.AddGenome(incumbent.IncumbentGenome, isCompetitive: true);

            // Let both DE strategies run for an iteration.
            globalStrategy.Initialize(basePopulation, incumbent, this.SingleTestInstance, 0, false);
            localStrategy.Initialize(basePopulation, incumbent, this.SingleTestInstance, 0, false);
            globalStrategy.PerformIteration(0, this.SingleTestInstance, false);
            localStrategy.PerformIteration(0, this.SingleTestInstance, false);

            // Count the number of competitive genomes which did not change for the local strategy.
            var newCompetitives = localStrategy.FinishPhase(basePopulation).GetCompetitiveIndividuals();
            int notChanged = 0;
            foreach (var genome in basePopulation.GetCompetitiveIndividuals())
            {
                if (newCompetitives.Any(individual => individual.ToString().Equals(genome.ToString())))
                {
                    notChanged++;
                }
            }

            Assert.True(
                newCompetitives.Count - 1 == notChanged,
                "In the local strategy with replacement rate of 0, only one genome should have changed.");

            // And then do the same for the global strategy.
            var otherCompetitives = globalStrategy.FinishPhase(basePopulation).GetCompetitiveIndividuals();
            notChanged = 0;
            foreach (var genome in basePopulation.GetCompetitiveIndividuals())
            {
                if (otherCompetitives.Any(individual => individual.ToString().Equals(genome.ToString())))
                {
                    notChanged++;
                }
            }

            Assert.True(
                otherCompetitives.Count - 1 > notChanged,
                "In the global strategy with replacement rate of 0, more than one genome should have changed.");
        }

        /// <summary>
        /// Checks that
        /// <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.FindIncumbentGenome"/> returns the
        /// best genome of the current generation, along with current results.
        /// </summary>
        [Fact]
        public void FindIncumbentGenomeReturnsBestInCurrentGeneration()
        {
            this.Strategy.Initialize(this.CreatePopulation(), this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance, 0, false);
            this.Strategy.PerformIteration(
                0,
                new List<TestInstance> { new TestInstance("a"), new TestInstance("b"), new TestInstance("c") },
                false);
            this.Strategy.PerformIteration(
                12,
                new List<TestInstance> { new TestInstance("c"), new TestInstance("d") },
                false);
            this.Strategy.DumpStatus();

            var incumbent = this.Strategy.FindIncumbentGenome();
            Assert.Equal(12, incumbent.IncumbentGeneration);
            Assert.Equal(2, incumbent.IncumbentInstanceResults.Count);

            var status =
                StatusBase.ReadFromFile<DifferentialEvolutionStatus<GenomeSearchPoint>>(this._deStatusFilePath);
            var bestGenome = status.SortedPopulation
                .OrderByDescending(point => point.Values[0])
                .First().Genome.CreateMutableGenome();
            Assert.True(
                Genome.GenomeComparer.Equals(bestGenome, incumbent.IncumbentGenome),
                $"Incumbent is {incumbent.IncumbentGenome} but should be {bestGenome}.");
        }

        /// <summary>
        /// Checks whether
        /// <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.HasTerminated"/> returns true
        /// after the correct number of generations.
        /// </summary>
        [Fact]
        public void HasTerminatedReturnsTrueAfterCorrectNumberGenerations()
        {
            int generation = 0;
            this.Strategy.Initialize(this.CreatePopulation(), this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance, 0, false);
            while (!this.Strategy.HasTerminated())
            {
                this.Strategy.PerformIteration(generation, this.SingleTestInstance, false);
                generation++;
            }

            var expectedNumberGenerations = this.Configuration
                .ExtractDetailedConfiguration<DifferentialEvolutionStrategyConfiguration>(DifferentialEvolutionStrategyArgumentParser.Identifier)
                .MaximumNumberGenerations;
            Assert.Equal(
                expectedNumberGenerations,
                generation);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStrategy{TInstance,TResult}.DumpStatus"/> creates status files
        /// at the correct places.
        /// </summary>
        [Fact]
        public override void DumpStatusCreatesStatusFiles()
        {
            this.Strategy.DumpStatus();
            Assert.True(File.Exists(this._statusFilePath), $"No file at path {this._statusFilePath}.");
            Assert.True(File.Exists(this._deStatusFilePath), $"No file at path {this._deStatusFilePath}.");
        }

        /// <summary>
        /// Checks that all properties important for the status have been dumped to file.
        /// </summary>
        [Fact]
        public override void DumpedStatusHasNoEmptyProperties()
        {
            var incumbent = this.CreateIncumbentGenomeWrapper();
            this.Strategy.Initialize(this.CreatePopulation(), incumbent, this.SingleTestInstance, 0, false);
            this.Strategy.PerformIteration(0, this.SingleTestInstance, false);
            this.Strategy.DumpStatus();

            // Check last status dump
            var lastStatus =
                StatusBase.ReadFromFile<DifferentialEvolutionStrategyStatus<TestInstance>>(this._statusFilePath);
            var sorting = lastStatus.MostRecentSorting;
            for (int i = 0; i + 1 < sorting.Count; i++)
            {
                Assert.True(sorting[i].Values[0] > sorting[i + 1].Values[0], "Points should be sorted.");
            }

            Assert.Equal(
                this.SingleTestInstance.Select(instance => instance.ToString()).ToArray(),
                lastStatus.CurrentEvaluationInstances.Select(instance => instance.ToString()).ToArray());
            Assert.Equal(
                incumbent.IncumbentGenome.ToString(),
                lastStatus.OriginalIncumbent.ToString());
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.UseStatusDump"/>
        /// reads relevant information from file.
        /// </summary>
        [Fact]
        public override void UseStatusDumpReadsInformationFromFile()
        {
            var basePopulation = this.CreatePopulation();
            var incumbent = this.CreateIncumbentGenomeWrapper();

            this.Strategy.Initialize(basePopulation, incumbent, this.SingleTestInstance, 0, false);
            this.Strategy.PerformIteration(0, this.SingleTestInstance, false);
            this.Strategy.DumpStatus();
            var originalPoints = StatusBase.ReadFromFile<DifferentialEvolutionStrategyStatus<TestInstance>>(this._statusFilePath)
                .MostRecentSorting;

            // Create new strategy to read the status dump.
            var newStrategy = new DifferentialEvolutionStrategy<TestInstance, IntegerResult>(
                this.Configuration,
                this.ParameterTree,
                this.GenomeBuilder,
                this.GenerationEvaluationActor,
                this.ResultStorageActor);
            newStrategy.UseStatusDump(null);
            newStrategy.DumpStatus();
            var status =
                StatusBase.ReadFromFile<DifferentialEvolutionStrategyStatus<TestInstance>>(this._statusFilePath);

            // Strategy should have read the last sorting from file.
            // Use strings in comparisons to also compare age.
            Assert.Equal(
                originalPoints.Select(point => point.Genome.ToString()).ToList(),
                status.MostRecentSorting.Select(point => point.Genome.ToString()).ToList());

            // Strategy should have read the original incumbent from file.
            Assert.Equal(
                incumbent.IncumbentGenome.ToString(),
                status.OriginalIncumbent.ToString());

            // Strategy should have read the current evaluation instances from file.
            Assert.Equal(
                this.SingleTestInstance.Select(instance => instance.ToString()).ToArray(),
                status.CurrentEvaluationInstances.Select(instance => instance.ToString()).ToArray());

            // And its DE runner should have also used its status dump.
            var deStatus =
                StatusBase.ReadFromFile<DifferentialEvolutionStatus<GenomeSearchPoint>>(this._deStatusFilePath);
            Assert.True(
                deStatus.SortedPopulation.Count > 0,
                "New DE runner should have read initialized population from old file.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a <see cref="AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder"/> which fits differential evolution.
        /// </summary>
        /// <returns>
        /// The created <see cref="AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder"/>.
        /// </returns>
        protected override AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder CreateTunerConfigurationBuilder()
        {
            return base.CreateTunerConfigurationBuilder()
                .AddDetailedConfigurationBuilder(
                    DifferentialEvolutionStrategyArgumentParser.Identifier,
                    new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                        .SetFixInstances(this.FixInstances)
                        .SetMaximumNumberGenerations(30)
                        .SetDifferentialEvolutionConfigurationBuilder(
                            new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder().SetBestPercentage(0.2)));
        }

        /// <inheritdoc />
        protected override IPopulationUpdateStrategy<TestInstance, IntegerResult> CreateStrategy(AlgorithmTunerConfiguration configuration)
        {
            return new DifferentialEvolutionStrategy<TestInstance, IntegerResult>(
                this.Configuration,
                this.ParameterTree,
                this.GenomeBuilder,
                this.GenerationEvaluationActor,
                this.ResultStorageActor);
        }

        /// <summary>
        /// Finds the continuous optimization method's current generation, e.g. by reading the latest status file.
        /// </summary>
        /// <returns>
        /// The current generation.
        /// </returns>
        protected override int FindCurrentGeneration()
        {
            var status =
                StatusBase.ReadFromFile<DifferentialEvolutionStatus<GenomeSearchPoint>>(this._deStatusFilePath);
            return status.CurrentGeneration;
        }

        #endregion
    }
}