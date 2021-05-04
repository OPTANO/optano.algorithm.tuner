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

namespace Optano.Algorithm.Tuner.Tests.Tracking
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData.SamplingStrategies;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.Tests.Serialization;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tracking;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="Status{TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>.
    /// </summary>
    public class StatusTest : StatusBaseTest<Status<TestInstance, TestResult, GenomePredictionRandomForest<ReuseOldTreesStrategy>,
        GenomePredictionForestModel<GenomePredictionTree>, ReuseOldTreesStrategy>>
    {
        #region Fields

        /// <summary>
        /// Population that can be used in tests.
        /// </summary>
        private readonly Population _population =
            new Population(new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().Build(maximumNumberParallelEvaluations: 1));

        /// <summary>
        /// Information history that can be used in tests.
        /// </summary>
        private readonly List<GenerationInformation> _informationHistory = new List<GenerationInformation>();

        /// <summary>
        /// Configuration that can be used in tests.
        /// </summary>
        private AlgorithmTunerConfiguration _configuration;

        #endregion

        #region Properties

        /// <summary>
        /// Gets path to status file that gets written in some tests.
        /// </summary>
        protected override string StatusFilePath => PathUtils.GetAbsolutePathFromExecutableFolderRelative("status/status");

        /// <summary>
        /// Gets or sets the <see cref="ParameterTree"/> to use in tests.
        /// </summary>
        private ParameterTree DummyTree { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="GeneticEngineering{TLearnerModel,TPredictorModel,TSamplingStrategy}"/> to use in tests.
        /// </summary>
        private GeneticEngineering<GenomePredictionRandomForest<ReuseOldTreesStrategy>, GenomePredictionForestModel<GenomePredictionTree>,
            ReuseOldTreesStrategy> DummyEngineering { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentNullException"/> if no population is provided.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingPopulation()
        {
            Assert.Throws<ArgumentNullException>(
                () => this.CreateStatus(
                    generation: 0,
                    population: null,
                    configuration: this._configuration,
                    currentUpdateStrategyIndex: 0,
                    informationHistory: this._informationHistory));
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentNullException"/> if no configuration is provided.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingAlgorithmTunerConfiguration()
        {
            Assert.Throws<ArgumentNullException>(
                () => this.CreateStatus(
                    0,
                    this._population,
                    configuration: null,
                    currentUpdateStrategyIndex: 0,
                    informationHistory: this._informationHistory));
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentNullException"/> if no information history is
        /// provided.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingInformationHistory()
        {
            Assert.Throws<ArgumentNullException>(
                () => this.CreateStatus(
                    0,
                    this._population,
                    configuration: this._configuration,
                    currentUpdateStrategyIndex: 0,
                    informationHistory: null));
        }

        /// <summary>
        /// Checks that <see cref="Status{TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.Generation"/>
        /// returns the value provided on initialization.
        /// </summary>
        [Fact]
        public void GenerationIsSetCorrectly()
        {
            int generation = 3;
            var status = this.CreateStatus(
                generation,
                this._population,
                this._configuration,
                0,
                this._informationHistory);
            Assert.Equal(
                generation,
                status.Generation);
        }

        /// <summary>
        /// Checks that
        /// <see cref="Status{TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.Population"/>
        /// returns the population provided on initialization.
        /// </summary>
        [Fact]
        public void PopulationIsSetCorrectly()
        {
            var status = this.CreateStatus(0, this._population, this._configuration, 0, this._informationHistory);
            Assert.Equal(
                this._population,
                status.Population);
        }

        /// <summary>
        /// Checks that
        /// <see cref="Status{TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.Configuration"/>
        /// returns the configuration provided on initialization.
        /// </summary>
        [Fact]
        public void AlgorithmTunerConfigurationIsSetCorrectly()
        {
            var status = this.CreateStatus(0, this._population, this._configuration, 0, this._informationHistory);
            Assert.Equal(
                this._configuration,
                status.Configuration);
        }

        /// <summary>
        /// Checks that
        /// <see cref="Status{TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.CurrentUpdateStrategyIndex"/>
        /// returns the strategy index provided on initialization.
        /// </summary>
        [Fact]
        public void CurrentUpdateStrategyIndexIsSetCorectly()
        {
            var status = this.CreateStatus(0, this._population, this._configuration, 43, this._informationHistory);
            Assert.Equal(
                43,
                status.CurrentUpdateStrategyIndex);
        }

        /// <summary>
        /// Checks that
        /// <see cref="Status{TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.InformationHistory"/>
        /// returns the history provided on initialization.
        /// </summary>
        [Fact]
        public void InformationHistoryIsSetCorrectly()
        {
            // Fill information history with dummy values.
            for (int i = 0; i < 13; i++)
            {
                this._informationHistory.Add(
                    new GenerationInformation(i, TimeSpan.FromSeconds(i), i * 100, typeof(string), new ImmutableGenome(new Genome()), "id"));
            }

            var status = this.CreateStatus(13, this._population, this._configuration, 43, this._informationHistory);
            Assert.Equal(
                this._informationHistory,
                status.InformationHistory);
        }

        /// <summary>
        /// Checks that
        /// <see cref="Status{TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.RunResults"/>
        /// returns the run results provided by
        /// <see cref="Status{TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.SetRunResults"/>.
        /// </summary>
        [Fact]
        public void SetRunResultsSetsRunResults()
        {
            /* Create status object. */
            var status = this.CreateStatus(0, this._population, this._configuration, 0, this._informationHistory);

            /* Set results. */
            var runResults = StatusTest.CreateRunResultsForTest();
            status.SetRunResults(runResults);

            /* Check RunResults property. */
            Assert.Equal(
                runResults,
                status.RunResults);
        }

        /// <summary>
        /// Checks that
        /// <see cref="Status{TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.SetRunResults"/>
        /// overwrites existing run results.
        /// </summary>
        [Fact]
        public void SetRunResultsOverwritesOldResults()
        {
            /* Create status object. */
            var status = this.CreateStatus(0, this._population, this._configuration, 0, this._informationHistory);

            /* Set results. */
            var runResults = StatusTest.CreateRunResultsForTest();
            status.SetRunResults(runResults);
            Assert.Equal(runResults.Count, status.RunResults.Count);

            /* Set them again. */
            var runResults2 = new Dictionary<ImmutableGenome, ImmutableDictionary<TestInstance, TestResult>>();
            status.SetRunResults(runResults2.ToImmutableDictionary());

            /* Make sure second one is stored. */
            Assert.Empty(status.RunResults);
        }

        /// <summary>
        /// Checks that
        /// <see cref="Status{TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.WriteToFile"/>
        /// throws an <see cref="InvalidOperationException"/> if
        /// called before
        /// <see cref="Status{TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.SetRunResults"/>.
        /// </summary>
        [Fact]
        public void WriteToFileWithoutRunResultsThrowsException()
        {
            var status = this.CreateStatus(0, this._population, this._configuration, 0, this._informationHistory);
            Assert.Throws<InvalidOperationException>(() => status.WriteToFile(this.StatusFilePath));
        }

        /// <summary>
        /// Checks that <see cref="StatusBase.ReadFromFile{Status}"/> correctly deserializes a
        /// status object written to file by <see cref="StatusBase.WriteToFile"/>.
        /// </summary>
        [Fact]
        public override void ReadFromFileDeserializesCorrectly()
        {
            /* Create status. */
            /* (1) population */
            var competitiveGenome = new Genome(2);
            competitiveGenome.SetGene("a", new Allele<int>(6));
            var nonCompetitiveGenome = new Genome(1);
            nonCompetitiveGenome.SetGene("b", new Allele<string>("oh"));
            this._population.AddGenome(competitiveGenome, isCompetitive: true);
            this._population.AddGenome(nonCompetitiveGenome, isCompetitive: false);
            /* (2) generation number, strategy index */
            var generation = 2;
            var strategyIndex = 17;
            /* (3) information history */
            var generation0 =
                new GenerationInformation(0, TimeSpan.FromSeconds(30), 100, typeof(int), new ImmutableGenome(competitiveGenome), "id");
            var generation1 =
                new GenerationInformation(1, TimeSpan.FromSeconds(60), 234, typeof(string), new ImmutableGenome(competitiveGenome), "id");
            generation0.IncumbentTrainingScore = 12;
            generation0.IncumbentTestScore = 345.8;
            this._informationHistory.Add(generation0);
            this._informationHistory.Add(generation1);
            var status = this.CreateStatus(
                generation,
                this._population,
                this._configuration,
                strategyIndex,
                this._informationHistory);
            /* (4) run results */
            var instance = new TestInstance("1");
            var result = new TestResult(TimeSpan.FromMilliseconds(3));
            var results = new Dictionary<TestInstance, TestResult> { { instance, result } };
            var runResults = new Dictionary<ImmutableGenome, ImmutableDictionary<TestInstance, TestResult>>();
            runResults.Add(new ImmutableGenome(new Genome()), results.ToImmutableDictionary());
            status.SetRunResults(runResults.ToImmutableDictionary());

            /* Write and read it from file. */
            status.WriteToFile(this.StatusFilePath);
            var deserializedStatus = StatusBase
                .ReadFromFile<Status<TestInstance, TestResult, GenomePredictionRandomForest<ReuseOldTreesStrategy>,
                    GenomePredictionForestModel<GenomePredictionTree>,
                    ReuseOldTreesStrategy>>(this.StatusFilePath);

            /* Check it's still the same. */
            /* (a) generation number, strategy index */
            Assert.Equal(generation, deserializedStatus.Generation);
            Assert.Equal(
                strategyIndex,
                deserializedStatus.CurrentUpdateStrategyIndex);
            /* (b) population */
            Assert.Equal(
                1,
                deserializedStatus.Population.GetCompetitiveIndividuals().Count);
            var deserializedCompetitiveGenome = deserializedStatus.Population.GetCompetitiveIndividuals().First();
            Assert.True(
                Genome.GenomeComparer.Equals(competitiveGenome, deserializedCompetitiveGenome),
                "Expected different competive genome.");
            Assert.Equal(
                competitiveGenome.Age,
                deserializedCompetitiveGenome.Age);
            Assert.Equal(
                1,
                deserializedStatus.Population.GetNonCompetitiveMates().Count);
            var deserializedNonCompetitiveGenome = deserializedStatus.Population.GetNonCompetitiveMates().First();
            Assert.True(
                Genome.GenomeComparer.Equals(nonCompetitiveGenome, deserializedNonCompetitiveGenome),
                "Expected different non-competive genome.");
            Assert.Equal(
                nonCompetitiveGenome.Age,
                deserializedNonCompetitiveGenome.Age);
            /* (c) configuration */
            Assert.True(
                deserializedStatus.Configuration.IsCompatible(this._configuration),
                "Expected different configuration.");
            /* (d) generation history */
            Assert.Equal(
                this._informationHistory.Select(information => information.ToString()).ToArray(),
                deserializedStatus.InformationHistory.Select(information => information.ToString()).ToArray());
            /* (e) run results */
            var instanceToResult = deserializedStatus.RunResults.Single().Value.Single();
            Assert.Equal(instance.ToString(), instanceToResult.Key.ToString());
            Assert.Equal(result.Runtime, instanceToResult.Value.Runtime);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called before every test case.
        /// </summary>
        protected override void InitializeDefault()
        {
            this.DummyTree = this.GetDefaultParameterTree();
            this._configuration = this.GetDefaultAlgorithmTunerConfiguration();
        }

        /// <summary>
        /// Creates a status object which can be (de)serialized successfully.
        /// </summary>
        /// <returns>The created object.</returns>
        protected override
            Status<TestInstance, TestResult, GenomePredictionRandomForest<ReuseOldTreesStrategy>, GenomePredictionForestModel<GenomePredictionTree>,
                ReuseOldTreesStrategy> CreateTestStatus()
        {
            var status = this.CreateStatus(0, this._population, this._configuration, 0, this._informationHistory);
            status.SetRunResults(StatusTest.CreateRunResultsForTest());
            return status;
        }

        /// <summary>
        /// Creates run results for two genomes and two test instances.
        /// </summary>
        /// <returns>The created run results.</returns>
        private static ImmutableDictionary<ImmutableGenome, ImmutableDictionary<TestInstance, TestResult>> CreateRunResultsForTest()
        {
            // Create results for two genomes and two test instances:
            var runResults = new Dictionary<ImmutableGenome, ImmutableDictionary<TestInstance, TestResult>>();

            // (a) Create instances.
            var instance1 = new TestInstance("1");
            var instance2 = new TestInstance("2");

            // (b) Create results for two genomes.
            var results1 = new Dictionary<TestInstance, TestResult>();
            var results2 = new Dictionary<TestInstance, TestResult>();
            results1.Add(instance1, new TestResult(TimeSpan.FromMilliseconds(1)));
            results1.Add(instance2, new TestResult(TimeSpan.FromMilliseconds(2)));
            results2.Add(instance1, new TestResult(TimeSpan.FromMilliseconds(3)));
            results2.Add(instance2, new TestResult(TimeSpan.FromMilliseconds(4)));

            // (c) Add them to dictionary using two different genomes as keys.
            runResults.Add(new ImmutableGenome(new Genome()), results1.ToImmutableDictionary());
            runResults.Add(new ImmutableGenome(new Genome()), results2.ToImmutableDictionary());

            return runResults.ToImmutableDictionary();
        }

        /// <summary>
        /// Creates a <see cref="Status{TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>.
        /// For everything not provided, dummy values are added.
        /// </summary>
        /// <param name="generation">Generation to use.</param>
        /// <param name="population"><see cref="Population"/> to use.</param>
        /// <param name="configuration"><see cref="AlgorithmTunerConfiguration"/> to use.</param>
        /// <param name="currentUpdateStrategyIndex">
        /// Index of the current <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/>.
        /// </param>
        /// <param name="informationHistory">Information history to use.</param>
        /// <returns>The created <see cref="Status{TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>.</returns>
        private Status<TestInstance, TestResult, GenomePredictionRandomForest<ReuseOldTreesStrategy>,
            GenomePredictionForestModel<GenomePredictionTree>,
            ReuseOldTreesStrategy> CreateStatus(
            int generation,
            Population population,
            AlgorithmTunerConfiguration configuration,
            int currentUpdateStrategyIndex,
            List<GenerationInformation> informationHistory)
        {
            if (this.DummyEngineering == null)
            {
                this.DummyEngineering =
                    new GeneticEngineering<GenomePredictionRandomForest<ReuseOldTreesStrategy>, GenomePredictionForestModel<GenomePredictionTree>,
                        ReuseOldTreesStrategy>(this.DummyTree, configuration);
            }

            return new Status<TestInstance, TestResult, GenomePredictionRandomForest<ReuseOldTreesStrategy>,
                GenomePredictionForestModel<GenomePredictionTree>, ReuseOldTreesStrategy>(
                generation,
                population,
                configuration,
                this.DummyEngineering,
                currentUpdateStrategyIndex,
                new List<double>(),
                new IncumbentGenomeWrapper<TestResult>()
                    {
                        IncumbentGenome = new Genome(),
                        IncumbentGeneration = 0,
                        IncumbentInstanceResults = new List<TestResult>().ToImmutableList(),
                    },
                informationHistory,
                elapsedTime: TimeSpan.Zero);
        }

        #endregion
    }
}