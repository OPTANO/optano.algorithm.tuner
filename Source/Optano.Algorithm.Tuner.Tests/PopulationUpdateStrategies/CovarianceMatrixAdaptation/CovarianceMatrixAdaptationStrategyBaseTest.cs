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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.CovarianceMatrixAdaptation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    using Xunit;

    /// <summary>
    /// Contains tests important for any <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/> inheriting from
    /// <see cref="CovarianceMatrixAdaptationStrategyBase{TSearchPoint,TInstance,TResult}"/>.
    /// </summary>
    /// <typeparam name="TSearchPoint">
    /// The type of <see cref="SearchPoint"/> used by the strategy.
    /// </typeparam>
    public abstract class CovarianceMatrixAdaptationStrategyBaseTest<TSearchPoint>
        : ContinuousOptimizationStrategyTestBase
        where TSearchPoint : SearchPoint, IRepairedGenomeRepresentation, IDeserializationRestorer<TSearchPoint>
    {
        #region Properties

        /// <summary>
        /// Gets the path to which the status file will get written in tests.
        /// </summary>
        protected string StatusFilePath { get; } = PathUtils.GetAbsolutePathFromExecutableFolderRelative(
            Path.Combine("status", CovarianceMatrixAdaptationStrategyStatus<TSearchPoint, TestInstance>.FileName));

        /// <summary>
        /// Gets the path to which the CMA-ES runner status file will get written in tests.
        /// </summary>
        protected string CmaEsStatusFilePath { get; } = PathUtils.GetAbsolutePathFromExecutableFolderRelative(
            Path.Combine("status", CmaEsStatus.FileName));

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.Initialize"/>
        /// resets all of CMA-ES's data.
        /// </summary>
        [Fact]
        public void InitializeResetsAllData()
        {
            var population = this.CreatePopulation();

            this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.DumpStatus();

            var originalStatus = StatusBase.ReadFromFile<CmaEsStatus>(this.CmaEsStatusFilePath);

            this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            this.Strategy.DumpStatus();

            var newlyInitializedStatus = StatusBase.ReadFromFile<CmaEsStatus>(this.CmaEsStatusFilePath);
            var expectedStepSize = this.Configuration
                .ExtractDetailedConfiguration<CovarianceMatrixAdaptationStrategyConfiguration>(
                    CovarianceMatrixAdaptationStrategyArgumentParser.Identifier)
                .InitialStepSize;
            Assert.NotEqual(
                originalStatus.Data.DistributionMean,
                newlyInitializedStatus.Data.DistributionMean);
            Assert.NotEqual(
                originalStatus.Data.Covariances,
                newlyInitializedStatus.Data.Covariances);
            Assert.Equal(
                expectedStepSize,
                newlyInitializedStatus.Data.StepSize);
            Assert.NotEqual(
                originalStatus.Data.EvolutionPath,
                newlyInitializedStatus.Data.EvolutionPath);
            Assert.NotEqual(
                originalStatus.Data.ConjugateEvolutionPath,
                newlyInitializedStatus.Data.ConjugateEvolutionPath);
            Assert.Equal(
                0,
                newlyInitializedStatus.Data.Generation);
        }

        /// <summary>
        /// Checks that <see cref="IPopulationUpdateStrategy{TInstance,TResult}" /> updates the instances on
        /// every population update if <see cref="StrategyConfigurationBase{TConfiguration}.FixInstances" /> is turned
        /// off.
        /// </summary>
        [Fact]
        public void InstancesAreUpdatedOnEveryUpdateIfConfigurationIndicatesThat()
        {
            var severalInstances = new List<TestInstance> { new TestInstance("c"), new TestInstance("d") };
            var population = this.CreatePopulation();

            // Start first phase, but update with different instances.
            this.Strategy.Initialize(population, this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            this.Strategy.PerformIteration(0, severalInstances);
            this.Strategy.DumpStatus();

            // Instances should have been updated now.
            var status =
                StatusBase.ReadFromFile<CovarianceMatrixAdaptationStrategyStatus<TSearchPoint, TestInstance>>(this.StatusFilePath);
            Assert.Equal(
                2,
                status.CurrentEvaluationInstances.Count);
        }

        /// <summary>
        /// Checks that
        /// <see cref="ContinuousOptimizationStrategyBase{TSearchPoint, TInstance,TResult}.FindIncumbentGenome"/>
        /// returns the best candidate as found by the CMA-ES runner.
        /// </summary>
        [Fact]
        public void FindIncumbentGenomeReturnsGenomeFoundByCmaEs()
        {
            // Update with different instances to check we return the correct results later on.
            this.Strategy.Initialize(this.CreatePopulation(), this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            this.Strategy.PerformIteration(
                0,
                new List<TestInstance> { new TestInstance("a"), new TestInstance("b"), new TestInstance("c") });
            this.Strategy.PerformIteration(
                12,
                new List<TestInstance> { new TestInstance("c"), new TestInstance("d") });
            this.Strategy.DumpStatus();

            var incumbent = this.Strategy.FindIncumbentGenome();
            Assert.Equal(12, incumbent.IncumbentGeneration);
            Assert.Equal(
                2,
                incumbent.IncumbentInstanceResults.Count);

            var status =
                StatusBase.ReadFromFile<CovarianceMatrixAdaptationStrategyStatus<TSearchPoint, TestInstance>>(this.StatusFilePath);
            Assert.Equal(
                status.MostRecentSorting.First().Genome.ToString(),
                this.Strategy.FindIncumbentGenome().IncumbentGenome.ToString());
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint, TInstance,TResult}.FinishPhase"/>
        /// keeps the age distribution of competitive genomes stable even if there exists an incumbent with a large
        /// age.
        /// </summary>
        [Fact]
        public void FinishPhaseKeepsAgeDistributionEvenWithOldElitistIncumbent()
        {
            var originalPopulation = this.CreatePopulation();
            var incumbent = new IncumbentGenomeWrapper<IntegerResult>
                                {
                                    IncumbentGeneration = 0,
                                    IncumbentGenome = originalPopulation.GetCompetitiveIndividuals().First(),
                                    IncumbentInstanceResults = new List<IntegerResult>().ToImmutableList(),
                                };
            while (incumbent.IncumbentGenome.Age <= this.Configuration.MaxGenomeAge)
            {
                incumbent.IncumbentGenome.AgeOnce();
            }

            Assert.Equal(
                1,
                originalPopulation.GetCompetitiveIndividuals().Count(genome => genome.Age == this.Configuration.MaxGenomeAge + 1));

            this.Strategy.Initialize(originalPopulation, incumbent, this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            var updatedPopulation = this.Strategy.FinishPhase(originalPopulation);

            for (int age = 0; age < this.Configuration.MaxGenomeAge; age++)
            {
                Assert.True(
                    originalPopulation.GetCompetitiveIndividuals().Count(genome => genome.Age == age)
                    == updatedPopulation.GetCompetitiveIndividuals().Count(genome => genome.Age == age),
                    $"Number of competitive genomes with age {age} changed.");
            }
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
            this.Strategy.Initialize(this.CreatePopulation(), this.CreateIncumbentGenomeWrapper(), this.SingleTestInstance);
            while (!this.Strategy.HasTerminated())
            {
                this.Strategy.PerformIteration(generation, this.SingleTestInstance);
                generation++;
            }

            var expectedNumberGenerations = this.Configuration
                .ExtractDetailedConfiguration<CovarianceMatrixAdaptationStrategyConfiguration>(
                    CovarianceMatrixAdaptationStrategyArgumentParser.Identifier)
                .MaximumNumberGenerations;
            Assert.Equal(
                expectedNumberGenerations,
                generation);
        }

        /// <summary>
        /// Checks that <see cref="IPopulationUpdateStrategy{TInstance,TResult}.DumpStatus" /> creates status files
        /// at the correct places.
        /// </summary>
        [Fact]
        public override void DumpStatusCreatesStatusFiles()
        {
            this.Strategy.DumpStatus();
            Assert.True(File.Exists(this.StatusFilePath), $"No file at path {this.StatusFilePath}.");
            Assert.True(File.Exists(this.CmaEsStatusFilePath), $"No file at path {this.CmaEsStatusFilePath}.");
        }

        /// <summary>
        /// Checks that all properties important for the status have been dumped to file.
        /// </summary>
        [Fact]
        public override void DumpedStatusHasNoEmptyProperties()
        {
            var incumbent = this.CreateIncumbentGenomeWrapper();
            this.Strategy.Initialize(this.CreatePopulation(), incumbent, this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.DumpStatus();

            // Check last status dump
            var lastStatus =
                StatusBase.ReadFromFile<CovarianceMatrixAdaptationStrategyStatus<TSearchPoint, TestInstance>>(this.StatusFilePath);
            var sorting = lastStatus.MostRecentSorting;
            for (int i = 0; i + 1 < sorting.Count; i++)
            {
                Assert.True(
                    Convert.ToInt32(sorting[i].Genome.CreateMutableGenome().GetGeneValue(ExtractIntegerValue.ParameterName).GetValue()) >
                    Convert.ToInt32(sorting[i + 1].Genome.CreateMutableGenome().GetGeneValue(ExtractIntegerValue.ParameterName).GetValue()),
                    "Points should be sorted.");
            }

            Assert.Equal(
                this.SingleTestInstance.Select(instance => instance.ToString()).ToArray(),
                lastStatus.CurrentEvaluationInstances.Select(instance => instance.ToString()).ToArray());
            Assert.Equal(
                incumbent.IncumbentGenome.ToString(),
                lastStatus.OriginalIncumbent.ToString());
        }

        /// <summary>
        /// Checks that <see cref="IPopulationUpdateStrategy{TInstance,TResult}.UseStatusDump" /> reads relevant information from
        /// file.
        /// </summary>
        [Fact]
        public override void UseStatusDumpReadsInformationFromFile()
        {
            var basePopulation = this.CreatePopulation();
            var incumbent = this.CreateIncumbentGenomeWrapper();

            this.Strategy.Initialize(basePopulation, incumbent, this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.DumpStatus();
            var originalPoints = StatusBase.ReadFromFile<CovarianceMatrixAdaptationStrategyStatus<TSearchPoint, TestInstance>>(this.StatusFilePath)
                .MostRecentSorting;

            var newStrategy = this.CreateStrategy(this.Configuration);
            newStrategy.UseStatusDump(null);
            newStrategy.DumpStatus();
            var status =
                StatusBase.ReadFromFile<CovarianceMatrixAdaptationStrategyStatus<TSearchPoint, TestInstance>>(this.StatusFilePath);

            // Strategy should have read the last sorting from file.
            // Use strings in comparisons to also compare age.
            Assert.Equal(
                originalPoints.Select(point => point.Genome.ToString()).OrderBy(x => x).ToList(),
                status.MostRecentSorting.Select(point => point.Genome.ToString()).OrderBy(x => x).ToList());

            // Strategy should have read the current evaluation instances from file.
            Assert.Equal(
                this.SingleTestInstance.Select(instance => instance.ToString()).ToArray(),
                status.CurrentEvaluationInstances.Select(instance => instance.ToString()).ToArray());

            // Strategy should have read the original incumbent from file.
            Assert.Equal(
                incumbent.IncumbentGenome.ToString(),
                status.OriginalIncumbent.ToString());

            // And its CMA-ES runner should have also used its status dump.
            var cmaEsStatus = StatusBase.ReadFromFile<CmaEsStatus>(this.CmaEsStatusFilePath);
            Assert.True(
                cmaEsStatus.Data.IsCompletelySpecified(),
                "New CMA-ES runner should have read internal data from old file.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a <see cref="AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder"/> which fits CMA-ES.
        /// </summary>
        /// <returns>
        /// The created <see cref="AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder"/>.
        /// </returns>
        protected override AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder CreateTunerConfigurationBuilder()
        {
            return base.CreateTunerConfigurationBuilder()
                .AddDetailedConfigurationBuilder(
                    CovarianceMatrixAdaptationStrategyArgumentParser.Identifier,
                    new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                        .SetFixInstances(this.FixInstances)
                        .SetReplacementRate(0.25)
                        .SetMaximumNumberGenerations(30));
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
                StatusBase.ReadFromFile<CmaEsStatus>(this.CmaEsStatusFilePath);
            return status.Data.Generation;
        }

        #endregion
    }
}