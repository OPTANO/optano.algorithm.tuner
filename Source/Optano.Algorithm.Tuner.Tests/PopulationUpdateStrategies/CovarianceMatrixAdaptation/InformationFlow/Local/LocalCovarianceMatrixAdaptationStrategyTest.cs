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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Local
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Local;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="LocalCovarianceMatrixAdaptationStrategy{TInstance,TResult}"/> class.
    /// </summary>
    public class LocalCovarianceMatrixAdaptationStrategyTest : CovarianceMatrixAdaptationStrategyBaseTest<PartialGenomeSearchPoint>
    {
        #region Public Methods and Operators

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="LocalCovarianceMatrixAdaptationStrategy{TInstance,TResult}"/> class without a configuration
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingConfiguration()
        {
            Assert.Throws<ArgumentNullException>(
                () => new LocalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                    configuration: null,
                    parameterTree: this.ParameterTree,
                    genomeBuilder: this.GenomeBuilder,
                    genomeSorter: this.GenomeSorter,
                    targetRunResultStorage: this.ResultStorageActor));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="LocalCovarianceMatrixAdaptationStrategy{TInstance,TResult}"/> class without a <see cref="ParameterTree"/>
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingParameterTree()
        {
            Assert.Throws<ArgumentNullException>(
                () => new LocalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                    this.Configuration,
                    parameterTree: null,
                    genomeBuilder: this.GenomeBuilder,
                    genomeSorter: this.GenomeSorter,
                    targetRunResultStorage: this.ResultStorageActor));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="LocalCovarianceMatrixAdaptationStrategy{TInstance,TResult}"/> class without a <see cref="GenomeBuilder"/>
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingGenomeBuilder()
        {
            Assert.Throws<ArgumentNullException>(
                () => new LocalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                    this.Configuration,
                    this.ParameterTree,
                    genomeBuilder: null,
                    genomeSorter: this.GenomeSorter,
                    targetRunResultStorage: this.ResultStorageActor));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="LocalCovarianceMatrixAdaptationStrategy{TInstance,TResult}"/> class without a genome sorter
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingGenomeSorter()
        {
            Assert.Throws<ArgumentNullException>(
                () => new LocalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                    this.Configuration,
                    this.ParameterTree,
                    this.GenomeBuilder,
                    genomeSorter: null,
                    targetRunResultStorage: this.ResultStorageActor));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="LocalCovarianceMatrixAdaptationStrategy{TInstance,TResult}"/> class without a result storage actor
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingResultStorageActor()
        {
            Assert.Throws<ArgumentNullException>(
                () => new LocalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                    this.Configuration,
                    this.ParameterTree,
                    this.GenomeBuilder,
                    this.GenomeSorter,
                    targetRunResultStorage: null));
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.Initialize"/>
        /// sets CMA-ES's initial distribution mean to the current incumbent.
        /// </summary>
        [Fact]
        public void InitializeUsesIncumbentForDistributionMean()
        {
            var incumbent = this.CreateIncumbentGenomeWrapper();
            this.Strategy.Initialize(this.CreatePopulation(), incumbent, this.SingleTestInstance);
            this.Strategy.DumpStatus();

            var status = StatusBase.ReadFromFile<CmaEsStatus>(this.CmaEsStatusFilePath);
            Assert.Equal(
                PartialGenomeSearchPoint.CreateFromGenome(incumbent.IncumbentGenome, this.ParameterTree, 0).Values,
                status.Data.DistributionMean);
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.Initialize"/>
        /// throws a <see cref="ArgumentNullException"/> if called without an incumbent.
        /// </summary>
        [Fact]
        public void InitializeThrowsForMissingIncumbent()
        {
            Assert.Throws<ArgumentNullException>(
                () => this.Strategy.Initialize(this.CreatePopulation(), null, this.SingleTestInstance));
        }

        /// <summary>
        /// Checks that the <see cref="LocalCovarianceMatrixAdaptationStrategy{TInstance,TResult}"/> fixes discrete
        /// values.
        /// </summary>
        [Fact]
        public void PerformIterationFixesDiscreteValues()
        {
            var population = this.CreatePopulation();

            this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.DumpStatus();

            var status =
                StatusBase.ReadFromFile<CovarianceMatrixAdaptationStrategyStatus<PartialGenomeSearchPoint, TestInstance>>(this.StatusFilePath);
            var expectedDiscreteValue = status.OriginalIncumbent.GetGeneValue("categorical").GetValue().ToString();

            foreach (var point in status.MostRecentSorting)
            {
                Assert.Equal(
                    expectedDiscreteValue,
                    point.Genome.CreateMutableGenome().GetGeneValue("categorical").GetValue().ToString());
            }
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.FinishPhase"/>:
        /// * replaces the provided percentage of the competitive population with the best points found by CMA-ES,
        /// * keeps the old incumbent, 
        /// * copies all non-competitive genomes, and
        /// * keeps the population's age structure.
        /// </summary>
        [Fact]
        public void FinishPhaseWorks()
        {
            // Create a population which contains the same genome twice.
            var basePopulation = this.CreatePopulation();
            var incumbent = new IncumbentGenomeWrapper<IntegerResult>
                                {
                                    IncumbentGeneration = 0,
                                    IncumbentGenome = basePopulation.GetCompetitiveIndividuals()[1],
                                    IncumbentInstanceResults = new List<IntegerResult>().ToImmutableList(),
                                };
            basePopulation.AddGenome(new Genome(incumbent.IncumbentGenome), isCompetitive: true);

            // Find some search points using CMA-ES. 
            this.Strategy.Initialize(basePopulation, incumbent, this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.DumpStatus();
            var status =
                StatusBase.ReadFromFile<CovarianceMatrixAdaptationStrategyStatus<PartialGenomeSearchPoint, TestInstance>>(this.StatusFilePath);
            var searchPoints = status.MostRecentSorting;

            var updatedPopulation = this.Strategy.FinishPhase(basePopulation);

            // Start by checking non-competitive genomes are the same.
            Assert.Equal(
                basePopulation.GetNonCompetitiveMates().Select(genome => genome.ToString()).OrderBy(x => x).ToArray(),
                updatedPopulation.GetNonCompetitiveMates().Select(genome => genome.ToString()).OrderBy(x => x).ToArray());

            // Then check if best points were added to population.
            Assert.True(
                updatedPopulation.GetCompetitiveIndividuals().Contains(
                    searchPoints[0].Genome.CreateMutableGenome(),
                    new Genome.GeneValueComparator()),
                "Updated population should contain best search point, but does not.");
            Assert.True(
                updatedPopulation.GetCompetitiveIndividuals().Contains(
                    searchPoints[1].Genome.CreateMutableGenome(),
                    new Genome.GeneValueComparator()),
                "Updated population should contain second best search point, but does not.");
            Assert.False(
                updatedPopulation.GetCompetitiveIndividuals().Contains(
                    searchPoints[2].Genome.CreateMutableGenome(),
                    new Genome.GeneValueComparator()),
                "Updated population should not contain worst search point.");

            // Incumbent should still exist.
            Assert.True(
                updatedPopulation.GetCompetitiveIndividuals().Contains(incumbent.IncumbentGenome, new Genome.GeneValueComparator()),
                "Updated population should contain incumbent, but does not.");

            // Finally, check ages.
            for (int age = 0; age < 3; age++)
            {
                Assert.True(
                    basePopulation.GetCompetitiveIndividuals().Count(individual => individual.Age == age)
                    == updatedPopulation.GetCompetitiveIndividuals().Count(individual => individual.Age == age),
                    $"Different number of genomes with age {age}.");
            }

            Assert.False(
                updatedPopulation.GetCompetitiveIndividuals().Any(individual => individual.Age < 0 || individual.Age > 3),
                "There exists a genome with age not in age range!");
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.FinishPhase"/>
        /// keeps the original incumbent even at a replacement rate of 1.
        /// </summary>
        [Fact]
        public void FinishPhaseKeepsIncumbentForMaximumReplacementRate()
        {
            // Create population along with an incumbent.
            var basePopulation = this.CreatePopulation();
            var incumbent = this.CreateIncumbentGenomeWrapper();
            basePopulation.AddGenome(new Genome(incumbent.IncumbentGenome), isCompetitive: true);

            // Create strategy with a replacement rate of 1.
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMaximumNumberParallelEvaluations(1)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .AddDetailedConfigurationBuilder(
                    CovarianceMatrixAdaptationStrategyArgumentParser.Identifier,
                    new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                        .SetFixInstances(this.FixInstances)
                        .SetReplacementRate(1)
                        .SetMaximumNumberGenerations(30));
            this.Strategy = new LocalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                configuration.Build(),
                this.ParameterTree,
                this.GenomeBuilder,
                this.GenomeSorter,
                this.ResultStorageActor);

            // Perform an iteration and finish the phase.
            this.Strategy.Initialize(basePopulation, incumbent, this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.DumpStatus();
            var updatedPopulation = this.Strategy.FinishPhase(basePopulation);

            // Incumbent should still exist.
            Assert.True(
                updatedPopulation.GetCompetitiveIndividuals().Contains(incumbent.IncumbentGenome, new Genome.GeneValueComparator()),
                "Updated population should contain incumbent, but does not.");

            // Age structure should be correct.
            for (int age = 0; age < 3; age++)
            {
                Assert.True(
                    basePopulation.GetCompetitiveIndividuals().Count(individual => individual.Age == age)
                    == updatedPopulation.GetCompetitiveIndividuals().Count(individual => individual.Age == age),
                    $"Different number of genomes with age {age}.");
            }

            Assert.False(
                updatedPopulation.GetCompetitiveIndividuals().Any(individual => individual.Age < 0 || individual.Age > 3),
                "There exists a genome with age not in age range!");
        }

        /// <summary>
        /// Checks that <see cref="LocalCovarianceMatrixAdaptationStrategy{TInstance,TResult}.UseStatusDump"/> reads
        /// the status in such a way that the base genome does not change from the first optimization part.
        /// </summary>
        [Fact]
        public void UseStatusDumpReinitializesBaseGenome()
        {
            var population = this.CreatePopulation();

            this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.DumpStatus();
            var originalStatus =
                StatusBase.ReadFromFile<CovarianceMatrixAdaptationStrategyStatus<PartialGenomeSearchPoint, TestInstance>>(this.StatusFilePath);
            var originalIncumbent = originalStatus.OriginalIncumbent;

            var newStrategy = this.CreateStrategy(this.Configuration);
            newStrategy.UseStatusDump(null);
            var newStatus =
                StatusBase.ReadFromFile<CovarianceMatrixAdaptationStrategyStatus<PartialGenomeSearchPoint, TestInstance>>(this.StatusFilePath);

            var expectedDiscreteValue = originalIncumbent.GetGeneValue("categorical").GetValue().ToString();
            foreach (var point in newStatus.MostRecentSorting)
            {
                Assert.Equal(
                    expectedDiscreteValue,
                    point.Genome.CreateMutableGenome().GetGeneValue("categorical").GetValue().ToString());
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override IPopulationUpdateStrategy<TestInstance, IntegerResult> CreateStrategy(AlgorithmTunerConfiguration configuration)
        {
            return new LocalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                this.Configuration,
                this.ParameterTree,
                this.GenomeBuilder,
                this.GenomeSorter,
                this.ResultStorageActor);
        }

        #endregion
    }
}