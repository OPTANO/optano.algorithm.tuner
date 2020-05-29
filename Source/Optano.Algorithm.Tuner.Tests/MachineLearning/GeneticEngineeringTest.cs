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

namespace Optano.Algorithm.Tuner.Tests.MachineLearning
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestOutOfBox;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData.SamplingStrategies;
    using Optano.Algorithm.Tuner.Parameters;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GeneticEngineering{TLearnerModel,TPredictorModel,TSamplingStrategy}"/> class.
    /// </summary>
    public class GeneticEngineeringTest : TestBase
    {
        #region Constants

        /// <summary>
        /// Half of the population size.
        /// </summary>
        private const int PopsizeHalf = 32;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="ParameterTree"/> to use in tests.
        /// </summary>
        protected ParameterTree CurrentTree { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="AlgorithmTunerConfiguration"/> to use in tests.
        /// </summary>
        protected AlgorithmTunerConfiguration CurrentConfig { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TrainingDataWrapper"/> to use in tests.
        /// </summary>
        protected TrainingDataWrapper TrainingData { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IGeneticEngineering"/> used in tests.
        /// </summary>
        private IGeneticEngineering EngineeringInstance { get; set; }

        /// <summary>
        /// Gets or sets the competitive part of the population.
        /// </summary>
        private List<Genome> PopulationCompetitive { get; set; }

        /// <summary>
        /// Gets or sets the non competitive part of the population.
        /// </summary>
        private List<Genome> PopulationNonCompetitive { get; set; }

        /// <summary>
        /// Gets or sets the total population.
        /// </summary>
        private List<Genome> PopulationCurrentComplete { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="GeneticEngineering{TLearnerModel,TPredictorModel,TSamplingStrategy}"/>'s constructor
        /// throws a <see cref="KeyNotFoundException"/> if no configuration with identifier
        /// <see cref="RegressionForestArgumentParser.Identifier"/> exists.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingRandomForestConfiguration()
        {
            var incompleteConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetInstanceNumbers(1, 1)
                .SetEngineeredProportion(1)
                .SetMaximumNumberParallelEvaluations(1)
                .Build();
            Assert.Throws<KeyNotFoundException>(() => this.InitializeAndPopulateProperties(this.GetDefaultParameterTree(), incompleteConfig));
        }

        /// <summary>
        /// Checks that <see cref="GeneticEngineering{TLearnerModel,TPredictorModel,TSamplingStrategy}"/>'s constructor
        /// throws a <see cref="InvalidCastException"/> if no <see cref="GenomePredictionRandomForestConfig"/> exists.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForWrongTypeRandomForestConfiguration()
        {
            var wrongConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetInstanceNumbers(1, 1)
                .SetEngineeredProportion(1)
                .SetMaximumNumberParallelEvaluations(1)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetMaximumNumberParallelEvaluations(1))
                .Build();
            Assert.Throws<InvalidCastException>(() => this.InitializeAndPopulateProperties(this.GetDefaultParameterTree(), wrongConfig));
        }

        /// <summary>
        /// Checks that <see cref="IGeneticEngineering.TrainForest"/> works successfully if
        /// <see cref="InitializeAndPopulateProperties"/> is called beforehand.
        /// </summary>
        [Fact]
        public void InitializeAndTrain()
        {
            this.InitializeAndPopulateProperties(this.GetDefaultParameterTree(), this.GetDefaultAlgorithmTunerConfiguration());
            Assert.NotNull(this.EngineeringInstance);
            Assert.NotNull(this.CurrentConfig);
            Assert.NotNull(this.CurrentTree);
            Assert.NotNull(this.TrainingData);
            this.EngineeringInstance.TrainForest(this.TrainingData);
        }

        /// <summary>
        /// Checks that engineered genomes
        /// * are created in the correct quantity,
        /// * are marked as being 'engineered',
        /// * have valid values, and
        /// * are of better fitness than their parents.
        /// </summary>
        [Fact]
        public void EngineerValidGenomes()
        {
            this.InitializeAndPopulateProperties(this.GetDefaultParameterTree(), this.GetDefaultAlgorithmTunerConfiguration());
            this.EngineeringInstance.TrainForest(this.TrainingData);

            var engineeredGenomes = this.EngineeringInstance.EngineerGenomes(
                this.PopulationCompetitive,
                this.PopulationNonCompetitive.AsReadOnly(),
                this.PopulationCurrentComplete).ToList();

            Assert.NotNull(engineeredGenomes);
            Assert.Equal(this.PopulationCompetitive.Count, engineeredGenomes.Count);

            foreach (var genome in engineeredGenomes)
            {
                Assert.NotNull(genome);
                Assert.True(genome.IsEngineered);
                this.ValidateGenomeValues(genome);
            }

            var populationPerformance = TestDataUtils.EvaluateTargetFunction(
                this.EngineeringInstance.GenomeTransformator,
                this.PopulationCurrentComplete);
            var engineeredPerformance = TestDataUtils.EvaluateTargetFunction(
                this.EngineeringInstance.GenomeTransformator,
                engineeredGenomes);

            var popAverage = populationPerformance.Average();
            var engAverage = engineeredPerformance.Average();

            Assert.True(engAverage <= popAverage);
        }

        #endregion

        #region Methods

        /// <summary>
        /// I don't know a good way to use 'TestInitialize' flag with test-specific parameters.
        /// </summary>
        /// <param name="tree">The <see cref="ParameterTree"/> to use.</param>
        /// <param name="config">The <see cref="AlgorithmTunerConfiguration"/> to use.</param>
        private void InitializeAndPopulateProperties(ParameterTree tree, AlgorithmTunerConfiguration config)
        {
            this.CurrentTree = tree;
            this.CurrentConfig = config;
            this.EngineeringInstance =
                new GeneticEngineering<StandardRandomForestLearner<ReuseOldTreesStrategy>, GenomePredictionForestModel<GenomePredictionTree>,
                    ReuseOldTreesStrategy>(
                    tree,
                    config);
            this.InitializeTrainingDataAndCurrentPopulation();
        }

        /// <summary>
        /// Initializes <see cref="TrainingData"/> and <see cref="PopulationNonCompetitive"/>, <see cref="PopulationCompetitive"/> as well as <see cref="PopulationCurrentComplete"/>.
        /// </summary>
        private void InitializeTrainingDataAndCurrentPopulation()
        {
            this.TrainingData = this.GetDefaultTrainingData(this.CurrentTree);
            this.PopulationNonCompetitive = TestDataUtils.GenerateGenomes(
                this.CurrentTree,
                this.CurrentConfig,
                GeneticEngineeringTest.PopsizeHalf);
            this.PopulationCompetitive = this.TrainingData.Genomes.ToList().InflateAndShuffle(GeneticEngineeringTest.PopsizeHalf);
            this.PopulationCurrentComplete = this.PopulationCompetitive.Union(this.PopulationNonCompetitive).ToList();
        }

        /// <summary>
        /// Generates training data.
        /// </summary>
        /// <param name="tree"><see cref="ParameterTree"/> to base genomes on.</param>
        /// <returns>The generated <see cref="TrainingDataWrapper"/>.</returns>
        private TrainingDataWrapper GetDefaultTrainingData(ParameterTree tree)
        {
            return TestDataUtils.GenerateTrainingData(
                tree,
                this.EngineeringInstance.GenomeTransformator,
                GeneticEngineeringTest.PopsizeHalf,
                1,
                this.CurrentConfig);
        }

        /// <summary>
        /// Checks if all <see cref="IAllele"/> in the given <paramref name="genome"/> contain valid values.
        /// Assert.Fail() if genome contains an invalid value.
        /// </summary>
        /// <param name="genome">The <see cref="Genome"/> to validate.</param>
        private void ValidateGenomeValues(Genome genome)
        {
            foreach (var parameter in this.CurrentTree.GetParameters())
            {
                var allele = genome.GetGeneValue(parameter.Identifier);
                Assert.True(parameter.Domain.ContainsGeneValue(allele));
            }
        }

        #endregion
    }
}
