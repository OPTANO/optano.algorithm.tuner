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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.DifferentialEvolution.InformationFlow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.InformationFlow;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.SearchPoint;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GlobalDifferentialEvolutionInformationFlow"/> class.
    /// </summary>
    public class GlobalDifferentialEvolutionInformationFlowTest : TestBase
    {
        #region Fields

        /// <summary>
        /// The <see cref="ParameterTree"/> used in tests.
        /// </summary>
        private readonly ParameterTree _parameterTree = new ParameterTree(
            new ValueNode<int>(ExtractIntegerValue.ParameterName, new IntegerDomain(-6, 143)));

        /// <summary>
        /// The <see cref="AlgorithmTunerConfiguration"/> used in tests.
        /// </summary>
        private AlgorithmTunerConfiguration _completeConfiguration;

        /// <summary>
        /// The <see cref="DifferentialEvolutionStrategyConfiguration"/> used in tests.
        /// </summary>
        private DifferentialEvolutionStrategyConfiguration _deConfiguration;

        /// <summary>
        /// The <see cref="GenomeBuilder"/> used in tests.
        /// </summary>
        private GenomeBuilder _genomeBuilder;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="GlobalDifferentialEvolutionInformationFlow"/> class without a
        /// strategy configuration throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingConfiguration()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GlobalDifferentialEvolutionInformationFlow(
                    strategyConfiguration: null,
                    parameterTree: this._parameterTree,
                    genomeBuilder: this._genomeBuilder));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="GlobalDifferentialEvolutionInformationFlow"/> class without a <see cref="ParameterTree"/>
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingParameterTree()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GlobalDifferentialEvolutionInformationFlow(
                    this._deConfiguration,
                    parameterTree: null,
                    genomeBuilder: this._genomeBuilder));
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="GlobalDifferentialEvolutionInformationFlow"/> class without a <see cref="GenomeBuilder"/>
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingGenomeBuilder()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GlobalDifferentialEvolutionInformationFlow(
                    this._deConfiguration,
                    this._parameterTree,
                    genomeBuilder: null));
        }

        /// <summary>
        /// Verifies that using a competitive population size of 2 throws a <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void DetermineInitialPointsThrowsForJadePopulationSmallerThree()
        {
            // Generate a population with only 2 competitive genomes.
            var population = new Population(this._completeConfiguration);
            for (int i = 0; i < 2; i++)
            {
                population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: i % 3), isCompetitive: true);
            }

            var incumbent = this._genomeBuilder.CreateRandomGenome(age: 2);

            // Try to call the method.
            var informationFlow = new GlobalDifferentialEvolutionInformationFlow(
                this._deConfiguration,
                this._parameterTree,
                this._genomeBuilder);
            Assert.Throws<ArgumentException>(() => informationFlow.DetermineInitialPoints(population, incumbent));
        }

        /// <summary>
        /// Verifies that using a competitive population size of 3 does not throw an error.
        /// </summary>
        [Fact]
        public void DetermineInitialPointsDoesNotThrowForJadePopulationEqualToThree()
        {
            // Generate a population with 3 competitive genomes.
            var population = new Population(this._completeConfiguration);
            for (int i = 0; i < 3; i++)
            {
                population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: i % 3), isCompetitive: true);
            }

            var incumbent = this._genomeBuilder.CreateRandomGenome(age: 2);

            // Try to call the method. --> Should work.
            var informationFlow = new GlobalDifferentialEvolutionInformationFlow(
                this._deConfiguration,
                this._parameterTree,
                this._genomeBuilder);
            informationFlow.DetermineInitialPoints(population, incumbent);
        }

        /// <summary>
        /// Checks that <see cref="GlobalDifferentialEvolutionInformationFlow.DetermineInitialPoints"/>
        /// transforms all competitive individuals into <see cref="GenomeSearchPoint"/>s.
        /// </summary>
        [Fact]
        public void DetermineInitialPointsTransformsAllCompetitives()
        {
            var population = new Population(this._completeConfiguration);
            for (int i = 0; i < 5; i++)
            {
                population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: i % 3), isCompetitive: true);
            }

            for (int i = 0; i < 2; i++)
            {
                population.AddGenome(this._genomeBuilder.CreateRandomGenome(age: i % 3), isCompetitive: false);
            }

            var informationFlowStrategy = new GlobalDifferentialEvolutionInformationFlow(
                this._deConfiguration,
                this._parameterTree,
                this._genomeBuilder);
            var points = informationFlowStrategy.DetermineInitialPoints(population, null);

            Assert.Equal(
                population.GetCompetitiveIndividuals().Select(genome => genome.ToString()).OrderBy(x => x).ToArray(),
                points.Select(point => point.Genome.ToString()).OrderBy(x => x).ToArray());
        }

        /// <summary>
        /// Checks that <see cref="GlobalDifferentialEvolutionInformationFlow.DefineCompetitivePopulation"/>
        /// returns all current search points.
        /// </summary>
        [Fact]
        public void DefineCompetitivePopulationReturnsCurrentSearchPoints()
        {
            var originalCompetitives = new List<Genome>();
            for (int i = 0; i < 5; i++)
            {
                originalCompetitives.Add(this._genomeBuilder.CreateRandomGenome(age: i % 3));
            }

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

            var informationFlowStrategy = new GlobalDifferentialEvolutionInformationFlow(
                this._deConfiguration,
                this._parameterTree,
                this._genomeBuilder);

            var competitiveIndividuals =
                informationFlowStrategy.DefineCompetitivePopulation(originalCompetitives, null, searchPoints);
            Assert.Equal(
                searchPoints.Select(point => point.Genome.ToString()).OrderBy(x => x).ToArray(),
                competitiveIndividuals.Select(genome => genome.ToString()).OrderBy(x => x).ToArray());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called before every test.
        /// </summary>
        protected override void InitializeDefault()
        {
            base.InitializeDefault();

            var strategyConfigurationBuilder = new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                .SetFocusOnIncumbent(false)
                .SetDifferentialEvolutionConfigurationBuilder(
                    new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder().SetBestPercentage(0.2));

            this._completeConfiguration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEnableRacing(true)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .AddDetailedConfigurationBuilder(
                    DifferentialEvolutionStrategyArgumentParser.Identifier,
                    strategyConfigurationBuilder)
                .Build(maximumNumberParallelEvaluations: 1);

            this._genomeBuilder = new GenomeBuilder(this._parameterTree, this._completeConfiguration);
            this._deConfiguration = strategyConfigurationBuilder.BuildWithFallback(null);
        }

        #endregion
    }
}