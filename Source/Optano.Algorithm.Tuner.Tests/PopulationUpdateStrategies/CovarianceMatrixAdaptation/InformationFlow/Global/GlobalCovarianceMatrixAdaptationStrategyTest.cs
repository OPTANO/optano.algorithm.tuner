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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Global
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Global;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GlobalCovarianceMatrixAdaptationStrategy{TInstance,TResult}"/> class.
    /// </summary>
    public class GlobalCovarianceMatrixAdaptationStrategyTest
        : CovarianceMatrixAdaptationStrategyBaseTest<ContinuizedGenomeSearchPoint>
    {
        #region Public Methods and Operators

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="GlobalCovarianceMatrixAdaptationStrategy{TInstance,TResult}"/> class without a configuration
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingConfiguration()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GlobalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                    configuration: null,
                    parameterTree: this.ParameterTree,
                    genomeBuilder: this.GenomeBuilder,
                    this.GenerationEvaluationActor,
                    targetRunResultStorage: this.ResultStorageActor));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="GlobalCovarianceMatrixAdaptationStrategy{TInstance,TResult}"/> class without a <see cref="ParameterTree"/>
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingParameterTree()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GlobalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                    this.Configuration,
                    parameterTree: null,
                    genomeBuilder: this.GenomeBuilder,
                    this.GenerationEvaluationActor,
                    targetRunResultStorage: this.ResultStorageActor));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="GlobalCovarianceMatrixAdaptationStrategy{TInstance,TResult}"/> class without a <see cref="GenomeBuilder"/>
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingGenomeBuilder()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GlobalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                    this.Configuration,
                    this.ParameterTree,
                    genomeBuilder: null,
                    this.GenerationEvaluationActor,
                    targetRunResultStorage: this.ResultStorageActor));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="GlobalCovarianceMatrixAdaptationStrategy{TInstance,TResult}"/> class without a generation evaluation actor
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingGenerationEvaluationActor()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GlobalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                    this.Configuration,
                    this.ParameterTree,
                    this.GenomeBuilder,
                    null,
                    targetRunResultStorage: this.ResultStorageActor));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="GlobalCovarianceMatrixAdaptationStrategy{TInstance,TResult}"/> class without a result storage actor
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingResultStorageActor()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GlobalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                    this.Configuration,
                    this.ParameterTree,
                    this.GenomeBuilder,
                    this.GenerationEvaluationActor,
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
                ContinuizedGenomeSearchPoint.CreateFromGenome(incumbent.IncumbentGenome, this.ParameterTree).Values,
                status.Data.DistributionMean);
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint,TInstance,TResult}.Initialize"/>
        /// sets CMA-ES's initial distribution mean to the mean of the competitive individuals if no incumbent is
        /// provided.
        /// </summary>
        [Fact]
        public void InitializeUsesCompleteCompetitivesIfNoIncumbentExistis()
        {
            var population = new Population(this.Configuration);
            for (int i = 0; i < 5; i++)
            {
                population.AddGenome(this.GenomeBuilder.CreateRandomGenome(age: i % 3), isCompetitive: true);
            }

            for (int i = 0; i < 2; i++)
            {
                population.AddGenome(this.GenomeBuilder.CreateRandomGenome(age: i % 3), isCompetitive: false);
            }

            var competitivesAsSearchPoints = population.GetCompetitiveIndividuals()
                .Select(individual => ContinuizedGenomeSearchPoint.CreateFromGenome(individual, this.ParameterTree));

            this.Strategy.Initialize(population, null, this.SingleTestInstance);
            this.Strategy.DumpStatus();

            var status = StatusBase.ReadFromFile<CmaEsStatus>(this.CmaEsStatusFilePath);
            var expectedDistributionMean = Vector<double>.Build.Dense(
                Enumerable.Range(0, this.ParameterTree.GetParameters().Count())
                    .Select(i => competitivesAsSearchPoints.Average(point => point.Values[i]))
                    .ToArray());
            Assert.Equal(
                expectedDistributionMean,
                status.Data.DistributionMean);
        }

        /// <summary>
        /// Checks the replacement mechanism of
        /// <see cref="ContinuousOptimizationStrategyBase{TSearchPoint, TInstance,TResult}.FinishPhase"/> for
        /// competitive genomes: These should be the all but the worst search point found by the CMA-ES runner and also
        /// the original incumbent. Search point ages are chosen s.t. the age distribution is kept stable.
        /// </summary>
        [Fact]
        public void FinishPhaseReplacesCompetitiveGenomes()
        {
            var originalPopulation = this.CreatePopulation();
            var incumbent = new IncumbentGenomeWrapper<IntegerResult>
                                {
                                    IncumbentGeneration = 0,
                                    IncumbentGenome = originalPopulation.GetCompetitiveIndividuals().First(),
                                    IncumbentInstanceResults = new List<IntegerResult>().ToImmutableList(),
                                };
            this.Strategy.Initialize(originalPopulation, incumbent, this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.DumpStatus();

            var status =
                StatusBase.ReadFromFile<CovarianceMatrixAdaptationStrategyStatus<ContinuizedGenomeSearchPoint, TestInstance>>(this.StatusFilePath);
            var searchPoints = status.MostRecentSorting;

            var updatedPopulation = this.Strategy.FinishPhase(originalPopulation);
            Assert.Equal(
                originalPopulation.GetCompetitiveIndividuals().Count,
                updatedPopulation.GetCompetitiveIndividuals().Count);

            var valueComparer = Genome.GenomeComparer;
            foreach (var point in searchPoints.Take(searchPoints.Count - 1))
            {
                var mappedGenome = point.Genome.CreateMutableGenome();
                Assert.True(
                    updatedPopulation.GetCompetitiveIndividuals()
                        .Any(genome => valueComparer.Equals(genome, mappedGenome)),
                    $"{mappedGenome} is not worst search point, but was not found in new competitives.");
            }

            Assert.Contains(
                incumbent.IncumbentGenome,
                updatedPopulation.GetCompetitiveIndividuals().ToArray());

            for (int age = 0; age < this.Configuration.MaxGenomeAge; age++)
            {
                Assert.True(
                    originalPopulation.GetCompetitiveIndividuals().Count(genome => genome.Age == age) ==
                    updatedPopulation.GetCompetitiveIndividuals().Count(genome => genome.Age == age),
                    $"Number of competitive genomes with age {age} changed.");
            }
        }

        /// <summary>
        /// Checks that <see cref="ContinuousOptimizationStrategyBase{TSearchPoint, TInstance,TResult}.FinishPhase"/>
        /// also works without an original incumbent by replacing the complete population and correctly choosing ages.
        /// </summary>
        [Fact]
        public void FinishPhaseWorksWithoutOriginalIncumbent()
        {
            var originalPopulation = this.CreatePopulation();
            this.Strategy.Initialize(
                originalPopulation,
                currentIncumbent: null,
                instancesForEvaluation: this.SingleTestInstance);
            this.Strategy.PerformIteration(0, this.SingleTestInstance);
            this.Strategy.DumpStatus();

            var status =
                StatusBase.ReadFromFile<CovarianceMatrixAdaptationStrategyStatus<ContinuizedGenomeSearchPoint, TestInstance>>(this.StatusFilePath);
            var searchPoints = status.MostRecentSorting;

            var updatedPopulation = this.Strategy.FinishPhase(originalPopulation);
            Assert.Equal(
                originalPopulation.GetCompetitiveIndividuals().Count,
                updatedPopulation.GetCompetitiveIndividuals().Count);

            var valueComparer = Genome.GenomeComparer;
            foreach (var point in searchPoints)
            {
                var mappedGenome = point.Genome.CreateMutableGenome();
                Assert.True(
                    updatedPopulation.GetCompetitiveIndividuals()
                        .Any(genome => valueComparer.Equals(genome, mappedGenome)),
                    $"{mappedGenome} was not found in new competitives.");
            }

            for (int age = 0; age < this.Configuration.MaxGenomeAge; age++)
            {
                Assert.True(
                    originalPopulation.GetCompetitiveIndividuals().Count(genome => genome.Age == age) ==
                    updatedPopulation.GetCompetitiveIndividuals().Count(genome => genome.Age == age),
                    $"Number of competitive genomes with age {age} changed.");
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override IPopulationUpdateStrategy<TestInstance, IntegerResult> CreateStrategy(AlgorithmTunerConfiguration configuration)
        {
            return new GlobalCovarianceMatrixAdaptationStrategy<TestInstance, IntegerResult>(
                this.Configuration,
                this.ParameterTree,
                this.GenomeBuilder,
                this.GenerationEvaluationActor,
                this.ResultStorageActor);
        }

        #endregion
    }
}