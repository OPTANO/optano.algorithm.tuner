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

namespace Optano.Algorithm.Tuner.Tests.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Akka.Configuration;

    using Optano.Algorithm.Tuner;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="AlgorithmTunerConfiguration"/> and <see cref="AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder"/>.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class AlgorithmTunerConfigurationTest : ConfigurationBaseTest
    {
        #region Fields

        /// <summary>
        /// Builder used for tests.
        /// </summary>
        private readonly AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder _builder;

        /// <summary>
        /// <see cref="GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder"/> used for tests.
        /// </summary>
        private readonly GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder _randomForestConfigBuilder;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmTunerConfigurationTest"/> class.
        /// </summary>
        public AlgorithmTunerConfigurationTest()
        {
            this._builder = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder();
            this._randomForestConfigBuilder =
                new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.Verbosity"/> returns
        /// <see cref="VerbosityLevel.Info"/> if nothing is set.
        /// </summary>
        [Fact]
        public void DefaultBuilderVerbosityIsStatus()
        {
            Assert.Equal(
                VerbosityLevel.Info,
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().Verbosity);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.Verbosity"/> returns
        /// the level that was set.
        /// </summary>
        [Fact]
        public void BuilderVerbosityPropertyWorks()
        {
            var verboseBuilder =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetVerbosity(VerbosityLevel.Trace);
            Assert.Equal(
                VerbosityLevel.Trace,
                verboseBuilder.Verbosity);
        }

        /// <summary>
        /// Checks that all values get transferred from builder to configuration.
        /// </summary>
        [Fact]
        public override void AllValuesGetTransferred()
        {
            // Set all values in builder and build the configuration.
            var expectedAkkaConfiguration = ConfigurationFactory.Default();
            this._randomForestConfigBuilder.SetFeaturesPerSplitRatio(0.19);
            var config = this._builder
                .SetEnableRacing(true)
                .SetPopulationSize(45)
                .SetGenerations(20)
                .SetMaxGenomeAge(4)
                .SetMaximumMiniTournamentSize(12)
                .SetTournamentWinnerPercentage(0.3)
                .SetCpuTimeout(TimeSpan.FromMilliseconds(400))
                .SetCrossoverSwitchProbability(0.11)
                .SetMutationRate(0.2)
                .SetMutationVariancePercentage(0.23)
                .SetMaxRepairAttempts(30)
                .SetInstanceNumbers(3, 79)
                .SetGoalGeneration(4)
                .SetVerbosity(VerbosityLevel.Info)
                .SetAkkaConfiguration(expectedAkkaConfiguration)
                .SetMaximumNumberConsecutiveFailuresPerEvaluation(1)
                .SetStatusFileDirectory("foo/bar")
                .SetLogFilePath("foo")
                .SetTrainModel(true)
                .SetEngineeredProportion(0.26)
                .SetPopulationMutantRatio(0.27)
                .SetStartEngineeringAtIteration(1)
                .SetTopPerformerThreshold(0.28)
                .SetEnableSexualSelection(true)
                .SetCrossoverProbabilityCompetitive(0.29)
                .SetHammingDistanceRelativeThreshold(0.3)
                .SetTargetSampleSize(200)
                .SetMaxRanksCompensatedByDistance(0)
                .SetFeatureSubsetRatioForDistance(0.31)
                .SetTrackConvergenceBehavior(true)
                .SetDistanceMetric(DistanceMetric.L1Average.ToString())
                .SetStrictCompatibilityCheck(false)
                .SetMaximumNumberParallelEvaluations(3)
                .SetMaximumNumberParallelThreads(5)
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.Jade)
                .SetMaximumNumberGgaGenerations(245)
                .SetMaximumNumberGgaGenerationsWithSameIncumbent(16)
                .AddDetailedConfigurationBuilder("a", this._randomForestConfigBuilder)
                .SetEvaluationLimit(13)
                .SetZipOldStatusFiles(true)
                .SetScoreGenerationHistory(true)
                .SetAddDefaultGenome(false)
                .Build();

            // Check the values in the configuration.
            Assert.True(config.EnableRacing);
            Assert.Equal(45, config.PopulationSize);
            Assert.Equal(20, config.Generations);
            Assert.Equal(4, config.MaxGenomeAge);
            Assert.Equal(
                12,
                config.MaximumMiniTournamentSize);
            Assert.Equal(
                0.3,
                config.TournamentWinnerPercentage);
            Assert.Equal(400, config.CpuTimeout.TotalMilliseconds);
            Assert.Equal(
                0.11,
                config.CrossoverSwitchProbability);
            Assert.Equal(0.2, config.MutationRate);
            Assert.Equal(
                0.23,
                config.MutationVariancePercentage);
            Assert.Equal(
                30,
                config.MaxRepairAttempts);
            Assert.Equal(
                3,
                config.StartNumInstances);
            Assert.Equal(79, config.EndNumInstances);
            Assert.Equal(4, config.GoalGeneration);
            Assert.Equal(
                VerbosityLevel.Info,
                config.Verbosity);
            Assert.Equal(
                expectedAkkaConfiguration,
                config.AkkaConfiguration);
            Assert.Equal(
                3,
                config.MaximumNumberParallelEvaluations);
            Assert.Equal(5, config.MaximumNumberParallelThreads);
            Assert.Equal(
                1,
                config.MaximumNumberConsecutiveFailuresPerEvaluation);
            Assert.Equal(
                "foo/bar",
                config.StatusFileDirectory);
            Assert.Equal("foo", config.LogFilePath);
            Assert.True(config.TrainModel);
            Assert.Equal(
                0.26,
                config.EngineeredPopulationRatio);
            Assert.Equal(
                0.27,
                config.PopulationMutantRatio);
            Assert.Equal(
                1,
                config.StartEngineeringAtIteration);
            Assert.Equal(
                0.28,
                config.TopPerformerThreshold);
            Assert.True(config.EnableSexualSelection);
            Assert.Equal(
                0.29,
                config.CrossoverProbabilityCompetitive);
            Assert.Equal(
                0.3,
                config.HammingDistanceRelativeThreshold);
            Assert.Equal(200, config.TargetSamplingSize);
            Assert.Equal(
                0,
                config.MaxRanksCompensatedByDistance);
            Assert.Equal(
                0.31,
                config.FeatureSubsetRatioForDistanceComputation);
            Assert.True(config.TrackConvergenceBehavior);
            Assert.Equal(
                DistanceMetric.L1Average,
                config.DistanceMetric);
            Assert.False(config.StrictCompatibilityCheck);
            Assert.Equal(
                "a",
                config.DetailedConfigurations.Keys.Single());
            Assert.Equal(13, config.EvaluationLimit);
            Assert.Equal(
                ContinuousOptimizationMethod.Jade,
                config.ContinuousOptimizationMethod);
            Assert.Equal(
                245,
                config.MaximumNumberGgaGenerations);
            Assert.True(config.ScoreGenerationHistory);
            Assert.True(config.ZipOldStatusFiles);
            Assert.Equal(
                16,
                config.MaximumNumberGgaGenerationsWithSameIncumbent);

            // Only check a single value of the detailed config. Rest is tested in its own test.
            var randomForestConfig = (GenomePredictionRandomForestConfig)config.DetailedConfigurations["a"];
            Assert.Equal(
                0.19,
                randomForestConfig.FeaturesPerSplitRatio);
            Assert.False(config.AddDefaultGenome);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration"/> has correct default values.
        /// </summary>
        [Fact]
        public override void DefaultsAreSetCorrectly()
        {
            // Build the configuration without explicitly setting values.
            var config = this._builder.Build(maximumNumberParallelEvaluations: 1);

            // Check the values in the configuration, that control parallel behaviour.
            Assert.Equal(1, config.MaximumNumberParallelEvaluations);
            Assert.Equal(1, config.MaximumNumberParallelThreads);

            // Check all other values in the configuration.
            Assert.Equal(AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultEnableRacing, config.EnableRacing);
            Assert.Equal(128, config.PopulationSize);
            Assert.Equal(100, config.Generations);
            Assert.Equal(3, config.MaxGenomeAge);
            Assert.Equal(8, config.MaximumMiniTournamentSize);
            Assert.Equal(0.125, config.TournamentWinnerPercentage);
            Assert.Equal(TimeSpan.FromMilliseconds(int.MaxValue), config.CpuTimeout);
            Assert.Equal(0.1, config.CrossoverSwitchProbability);
            Assert.Equal(AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultMutationRate, config.MutationRate);
            Assert.Equal(0.1, config.MutationVariancePercentage);
            Assert.Equal(20, config.MaxRepairAttempts);
            Assert.Equal(5, config.StartNumInstances);
            Assert.Equal(100, config.EndNumInstances);
            Assert.Equal(74, config.GoalGeneration);
            Assert.Equal(VerbosityLevel.Info, config.Verbosity);
            Assert.Equal(
                ConfigurationFactory.Load(),
                config.AkkaConfiguration);
            Assert.Equal(
                3,
                config.MaximumNumberConsecutiveFailuresPerEvaluation);
            Assert.Equal(
                PathUtils.GetAbsolutePathFromExecutableFolderRelative("status"),
                config.StatusFileDirectory);
            Assert.Equal(
                PathUtils.GetAbsolutePathFromCurrentDirectory("tunerLog.txt"),
                config.LogFilePath);
            Assert.False(config.TrainModel);
            Assert.Equal(
                0,
                config.EngineeredPopulationRatio);
            Assert.Equal(AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultPopulationMutantRatio, config.PopulationMutantRatio);
            Assert.Equal(
                3,
                config.StartEngineeringAtIteration);
            Assert.Equal(0.1, config.TopPerformerThreshold);
            Assert.False(config.EnableSexualSelection);
            Assert.Equal(
                0.5,
                config.CrossoverProbabilityCompetitive);
            Assert.Equal(
                0.01,
                config.HammingDistanceRelativeThreshold);
            Assert.Equal(
                AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultTargetSamplingSize,
                config.TargetSamplingSize);
            Assert.Equal(
                1.6,
                config.MaxRanksCompensatedByDistance);
            Assert.Equal(
                0.3,
                config.FeatureSubsetRatioForDistanceComputation);
            Assert.False(config.TrackConvergenceBehavior);
            Assert.Equal(
                DistanceMetric.HammingDistance,
                config.DistanceMetric);
            Assert.True(config.StrictCompatibilityCheck);
            Assert.Equal(
                int.MaxValue,
                config.EvaluationLimit);
            Assert.Equal(
                AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultContinuousOptimizationMethod,
                config.ContinuousOptimizationMethod);
            Assert.Equal(
                AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultMaximumNumberGgaGenerations,
                config.MaximumNumberGgaGenerations);
            Assert.Equal(
                AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultMaximumNumberGgaGenerationsWithSameIncumbent,
                config.MaximumNumberGgaGenerationsWithSameIncumbent);
            Assert.False(config.ScoreGenerationHistory);
            Assert.False(config.ZipOldStatusFiles);
            Assert.True(config.AddDefaultGenome);
        }

        /// <summary>
        /// Checks that all values are copied if
        /// <see cref="AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.BuildWithFallback"/>
        /// is called on a builder without anything set.
        /// </summary>
        [Fact]
        public override void BuildWithFallbackUsesFallbacks()
        {
            // Set all values in builder and build a configuration.
            var expectedAkkaConfiguration = ConfigurationFactory.Default();
            var randomForestFallback = this._randomForestConfigBuilder.SetFeaturesPerSplitRatio(0.19);
            var fallback = this._builder
                .SetEnableRacing(true)
                .SetPopulationSize(45)
                .SetGenerations(20)
                .SetMaxGenomeAge(4)
                .SetMaximumMiniTournamentSize(12)
                .SetTournamentWinnerPercentage(0.3)
                .SetCpuTimeout(TimeSpan.FromMilliseconds(400))
                .SetCrossoverSwitchProbability(0.11)
                .SetMutationRate(0.2)
                .SetMutationVariancePercentage(0.23)
                .SetMaxRepairAttempts(30)
                .SetInstanceNumbers(3, 79)
                .SetGoalGeneration(4)
                .SetVerbosity(VerbosityLevel.Info)
                .SetAkkaConfiguration(expectedAkkaConfiguration)
                .SetMaximumNumberConsecutiveFailuresPerEvaluation(1)
                .SetStatusFileDirectory("foo/bar")
                .SetLogFilePath("foo")
                .SetTrainModel(true)
                .SetEngineeredProportion(0.26)
                .SetPopulationMutantRatio(0.27)
                .SetStartEngineeringAtIteration(1)
                .SetTopPerformerThreshold(0.28)
                .SetEnableSexualSelection(true)
                .SetCrossoverProbabilityCompetitive(0.29)
                .SetHammingDistanceRelativeThreshold(0.3)
                .SetTargetSampleSize(200)
                .SetMaxRanksCompensatedByDistance(0)
                .SetFeatureSubsetRatioForDistance(0.31)
                .SetTrackConvergenceBehavior(true)
                .SetDistanceMetric(DistanceMetric.L1Average.ToString())
                .SetStrictCompatibilityCheck(false)
                .SetMaximumNumberParallelEvaluations(3)
                .SetMaximumNumberParallelThreads(5)
                .AddDetailedConfigurationBuilder("a", randomForestFallback)
                .SetEvaluationLimit(13)
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.Jade)
                .SetMaximumNumberGgaGenerations(245)
                .SetMaximumNumberGgaGenerationsWithSameIncumbent(16)
                .SetScoreGenerationHistory(true)
                .SetZipOldStatusFiles(true)
                .SetAddDefaultGenome(false)
                .Build();

            // Create a new builder based on it and let it build a configuration.
            var config = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .BuildWithFallback(fallback);

            // Check all values.
            Assert.Equal(fallback.EnableRacing, config.EnableRacing);
            Assert.Equal(fallback.PopulationSize, config.PopulationSize);
            Assert.Equal(fallback.Generations, config.Generations);
            Assert.Equal(fallback.MaxGenomeAge, config.MaxGenomeAge);
            Assert.Equal(
                fallback.MaximumMiniTournamentSize,
                config.MaximumMiniTournamentSize);
            Assert.Equal(
                fallback.TournamentWinnerPercentage,
                config.TournamentWinnerPercentage);
            Assert.Equal(fallback.CpuTimeout, config.CpuTimeout);
            Assert.Equal(
                fallback.CrossoverSwitchProbability,
                config.CrossoverSwitchProbability);
            Assert.Equal(fallback.MutationRate, config.MutationRate);
            Assert.Equal(
                fallback.MutationVariancePercentage,
                config.MutationVariancePercentage);
            Assert.Equal(
                fallback.MaxRepairAttempts,
                config.MaxRepairAttempts);
            Assert.Equal(
                fallback.StartNumInstances,
                config.StartNumInstances);
            Assert.Equal(
                fallback.EndNumInstances,
                config.EndNumInstances);
            Assert.Equal(fallback.GoalGeneration, config.GoalGeneration);
            Assert.Equal(fallback.Verbosity, config.Verbosity);
            Assert.Equal(
                fallback.AkkaConfiguration,
                config.AkkaConfiguration);
            Assert.Equal(
                fallback.MaximumNumberConsecutiveFailuresPerEvaluation,
                config.MaximumNumberConsecutiveFailuresPerEvaluation);
            Assert.Equal(
                fallback.StatusFileDirectory,
                config.StatusFileDirectory);
            Assert.Equal(fallback.LogFilePath, config.LogFilePath);
            Assert.True(config.TrainModel);
            Assert.Equal(0.26, config.EngineeredPopulationRatio);
            Assert.Equal(0.27, config.PopulationMutantRatio);
            Assert.Equal(1, config.StartEngineeringAtIteration);
            Assert.Equal(0.28, config.TopPerformerThreshold);
            Assert.True(config.EnableSexualSelection);
            Assert.Equal(
                0.29,
                config.CrossoverProbabilityCompetitive);
            Assert.Equal(
                0.3,
                config.HammingDistanceRelativeThreshold);
            Assert.Equal(200, config.TargetSamplingSize);
            Assert.Equal(
                0,
                config.MaxRanksCompensatedByDistance);
            Assert.Equal(
                0.31,
                config.FeatureSubsetRatioForDistanceComputation);
            Assert.True(config.TrackConvergenceBehavior);
            Assert.Equal(DistanceMetric.L1Average, config.DistanceMetric);
            Assert.False(config.StrictCompatibilityCheck);
            Assert.Equal(
                3,
                config.MaximumNumberParallelEvaluations);
            Assert.Equal(5, config.MaximumNumberParallelThreads);
            Assert.Equal(
                "a",
                config.DetailedConfigurations.Keys.Single());
            Assert.Equal(13, config.EvaluationLimit);
            Assert.Equal(
                fallback.ContinuousOptimizationMethod,
                config.ContinuousOptimizationMethod);
            Assert.Equal(
                fallback.MaximumNumberGgaGenerations,
                config.MaximumNumberGgaGenerations);
            Assert.Equal(
                fallback.MaximumNumberGgaGenerationsWithSameIncumbent,
                config.MaximumNumberGgaGenerationsWithSameIncumbent);
            Assert.Equal(
                fallback.ScoreGenerationHistory,
                config.ScoreGenerationHistory);
            Assert.Equal(
                fallback.ZipOldStatusFiles,
                config.ZipOldStatusFiles);
            Assert.Equal(
                fallback.AddDefaultGenome,
                config.AddDefaultGenome);

            // Only check a single value of the detailed config. Rest is tested in its own test.
            var randomForestConfig = (GenomePredictionRandomForestConfig)config.DetailedConfigurations["a"];
            Assert.Equal(0.19, randomForestConfig.FeaturesPerSplitRatio);
        }

        /// <summary>
        /// Checks that
        /// <see cref="AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.BuildWithFallback"/>
        /// also uses fallbacks for detailed configurations what is specified in the new builder.
        /// </summary>
        [Fact]
        public void BuildWithFallbackUsesFallbacksForDetailedConfigurations()
        {
            var randomForestFallback =
                new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder()
                    .SetMaximumTreeDepth(3)
                    .SetFeaturesPerSplitRatio(0.19);
            var fallback = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .AddDetailedConfigurationBuilder("a", randomForestFallback).Build(1);

            var randomForest = this._randomForestConfigBuilder.SetMaximumTreeDepth(387);
            var config = this._builder
                .AddDetailedConfigurationBuilder("a", randomForest)
                .BuildWithFallback(fallback);

            Assert.Equal(
                "a",
                config.DetailedConfigurations.Keys.Single());
            var randomForestConfig = (GenomePredictionRandomForestConfig)config.DetailedConfigurations["a"];
            Assert.Equal(
                0.19,
                randomForestConfig.FeaturesPerSplitRatio);
            Assert.Equal(
                387,
                randomForestConfig.MaximumTreeDepth);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns true if basic parameters like the verbosity
        /// or the number of tolerable failures before an exception are different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsTrueForDifferentBaseParameters()
        {
            // Build default configuration.
            var defaultConfig = this._builder.SetStrictCompatibilityCheck(false).Build(maximumNumberParallelEvaluations: 1);

            // Create configuration based on that which changes parameters dependent on technology, parameters managing
            // exceptions, and the verbosity.
            var configWithDifferentBaseParameters = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetAkkaConfiguration(Config.Empty)
                .SetMaxRepairAttempts(defaultConfig.MaxRepairAttempts + 2)
                .SetMaximumNumberConsecutiveFailuresPerEvaluation(
                    defaultConfig.MaximumNumberConsecutiveFailuresPerEvaluation - 1)
                .SetStatusFileDirectory(defaultConfig.StatusFileDirectory + "diff")
                .SetLogFilePath("diff")
                .SetVerbosity(VerbosityLevel.Info)
                .SetMaximumNumberParallelEvaluations(defaultConfig.MaximumNumberParallelEvaluations + 2)
                .SetStrictCompatibilityCheck(true)
                .SetTrackConvergenceBehavior(true)
                .SetScoreGenerationHistory(true)
                .SetZipOldStatusFiles(true)
                .BuildWithFallback(defaultConfig);

            // Check those two configuration are compatible.
            Assert.True(
                defaultConfig.IsCompatible(configWithDifferentBaseParameters),
                "Configurations should be compatible.");
            Assert.Equal(
                defaultConfig.IsCompatible(configWithDifferentBaseParameters),
                configWithDifferentBaseParameters.IsCompatible(defaultConfig));
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the population size is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentPopulationSize()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetPopulationSize(defaultConfig.PopulationSize + 1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            AlgorithmTunerConfigurationTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the evaluation limit
        /// decreases.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForSmallerEvaluationLimit()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEvaluationLimit(defaultConfig.EvaluationLimit - 1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);

            // We do not have symmetry here, so just check once.
            Assert.False(
                otherConfig.IsCompatible(defaultConfig),
                "Configurations should not be compatible.");
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns true if the evaluation limit
        /// increases.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsTrueForGreaterEvaluationLimit()
        {
            var firstConfig = this._builder.SetEvaluationLimit(12).Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEvaluationLimit(firstConfig.EvaluationLimit + 1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(firstConfig);

            // We do not have symmetry here, so just check once.
            Assert.True(
                otherConfig.IsCompatible(firstConfig),
                "Configurations should be compatible.");
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if one configuration indicates that
        /// runtime should be tuned and the other doesn't.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentTuningType()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEnableRacing(!defaultConfig.EnableRacing)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            AlgorithmTunerConfigurationTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the generation number is
        /// different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentGenerationNumbers()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetGenerations(defaultConfig.Generations + 1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the number of
        /// generations per GGA phase is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMaximumNumberGgaGenerations()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMaximumNumberGgaGenerations(defaultConfig.MaximumNumberGgaGenerations - 1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the maximum number of
        /// consecutive GGA generations with the same incumbent is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMaximumNumberGgaGenerationsWithSameIncumbent()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMaximumNumberGgaGenerationsWithSameIncumbent(defaultConfig.MaximumNumberGgaGenerationsWithSameIncumbent - 1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the
        /// continuous optimization method to combine with GGA is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentContinuousOptimizationMethod()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.Jade)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the maximum genome age is
        /// different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMaximumGenomeAge()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMaxGenomeAge(defaultConfig.MaxGenomeAge + 1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the maximum mini tournament size
        /// is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMaximumMiniTournamentSize()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMaximumMiniTournamentSize(defaultConfig.MaximumMiniTournamentSize + 1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the winner percentage is
        /// different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentWinnerPercentages()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetTournamentWinnerPercentage(defaultConfig.TournamentWinnerPercentage + 0.1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the CPU timeout is
        /// different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentCpuTimeouts()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetCpuTimeout(TimeSpan.FromMilliseconds(1))
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the crossover switch probability
        /// is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentCrossoverSwitchProbability()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetCrossoverSwitchProbability(defaultConfig.CrossoverSwitchProbability + 0.1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the mutation rate
        /// is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMutationRates()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMutationRate(defaultConfig.MutationRate + 0.1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the mutation variance percentage
        /// is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMutationVariancePercentage()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMutationVariancePercentage(defaultConfig.MutationVariancePercentage + 0.1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the minimum number of instances
        /// is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMinimumNumberInstances()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetInstanceNumbers(defaultConfig.StartNumInstances + 1, defaultConfig.EndNumInstances)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the maximum number of instances
        /// is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentMaximumNumberInstances()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetInstanceNumbers(defaultConfig.StartNumInstances, defaultConfig.EndNumInstances + 1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the goal generation number is
        /// different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentGoalGenerations()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetGoalGeneration(defaultConfig.GoalGeneration + 1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the engineered
        /// population ratio is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentEngineeredPopulationRatio()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEngineeredProportion(defaultConfig.EngineeredPopulationRatio + 0.1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the population
        /// mutant ratio is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentPopulationMutantRatio()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetPopulationMutantRatio(defaultConfig.PopulationMutantRatio + 0.1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the genetic engineering
        /// start generation is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentGeneticEngineeringStartGeneration()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetStartEngineeringAtIteration(defaultConfig.StartEngineeringAtIteration + 1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the top performer
        /// threshold is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentTopPerformerThreshold()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetTopPerformerThreshold(defaultConfig.TopPerformerThreshold + 0.1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the sexual
        /// selection setting is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentSexualSelectionSetting()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEnableSexualSelection(!defaultConfig.EnableSexualSelection)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            ConfigurationBaseTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the crossover
        /// probability for competitive genomes in genetic engineering is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentCrossoverProbabilityCompetitive()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetCrossoverProbabilityCompetitive(defaultConfig.CrossoverProbabilityCompetitive + 0.1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            AlgorithmTunerConfigurationTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the hamming
        /// distance threshold is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentHammingDistanceTreshold()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetHammingDistanceRelativeThreshold(defaultConfig.HammingDistanceRelativeThreshold + 0.1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            AlgorithmTunerConfigurationTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the target
        /// sampling size is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentTargetSamplingSize()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetTargetSampleSize(defaultConfig.TargetSamplingSize + 1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            AlgorithmTunerConfigurationTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the maximum number
        /// ranks compensated by distance is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentNumberRanksCompensatedByDistance()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMaxRanksCompensatedByDistance(defaultConfig.MaxRanksCompensatedByDistance + 1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            AlgorithmTunerConfigurationTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the feature subset
        /// ratio for distance is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentFeatureSubsetRatioForDistance()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetFeatureSubsetRatioForDistance(defaultConfig.FeatureSubsetRatioForDistanceComputation + 0.1)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            AlgorithmTunerConfigurationTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the distance metric
        /// is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForDifferentDistanceMetric()
        {
            var defaultConfig = this._builder.Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetDistanceMetric(DistanceMetric.L1Average.ToString())
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            AlgorithmTunerConfigurationTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the train
        /// model setting changes from false to true.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForNewTrainModelSetting()
        {
            var defaultConfig = this._builder.SetTrainModel(false).Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetTrainModel(true)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            Assert.False(otherConfig.IsCompatible(defaultConfig), "Configurations should not be compatible.");
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if the
        /// add default genome value is different.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseNewAddDefaultGenomeValue()
        {
            var defaultConfig = this._builder.SetTrainModel(false).Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetAddDefaultGenome(false)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            Assert.False(otherConfig.IsCompatible(defaultConfig), "Configurations should not be compatible.");
            Assert.False(defaultConfig.IsCompatible(otherConfig), "Configurations should not be compatible.");
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsCompatible"/> returns false if associated
        /// <see cref="GenomePredictionRandomForestConfig"/> is incompatible.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForIncompatibleRandomForestConfiguration()
        {
            var defaultConfig = this._builder
                .AddDetailedConfigurationBuilder(RegressionForestArgumentParser.Identifier, this._randomForestConfigBuilder)
                .Build(maximumNumberParallelEvaluations: 1);
            var otherRandomForestConfigBuilder = this._randomForestConfigBuilder.SetTreeCount(365);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMaximumNumberParallelEvaluations(1)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    otherRandomForestConfigBuilder)
                .BuildWithFallback(defaultConfig);
            AlgorithmTunerConfigurationTest.CheckIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsTechnicallyCompatible"/> returns true if
        /// all inherent random forest parameters stay the same.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsTrueForSameRandomForestParameters()
        {
            // Build default configuration.
            var defaultConfig = this._builder
                .SetTrainModel(true)
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.Jade)
                .Build(maximumNumberParallelEvaluations: 1);

            // Create configuration based on that which changes all parameters apart from extra arguments for
            // #Learning.
            var configWithDifferentBaseParameters = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEnableRacing(!defaultConfig.EnableRacing)
                .SetPopulationSize(45)
                .SetGenerations(20)
                .SetMaxGenomeAge(4)
                .SetMaximumMiniTournamentSize(12)
                .SetTournamentWinnerPercentage(0.3)
                .SetCpuTimeout(TimeSpan.FromMilliseconds(400))
                .SetCrossoverSwitchProbability(0.11)
                .SetMutationRate(0.2)
                .SetMutationVariancePercentage(0.23)
                .SetMaxRepairAttempts(30)
                .SetInstanceNumbers(3, 79)
                .SetGoalGeneration(4)
                .SetVerbosity(VerbosityLevel.Info)
                .SetAkkaConfiguration(Config.Empty)
                .SetMaximumNumberConsecutiveFailuresPerEvaluation(1)
                .SetStatusFileDirectory("foo/bar")
                .SetLogFilePath("foo")
                .SetTrainModel(false)
                .SetEngineeredProportion(0.26)
                .SetPopulationMutantRatio(0.27)
                .SetStartEngineeringAtIteration(1)
                .SetTopPerformerThreshold(0.28)
                .SetEnableSexualSelection(true)
                .SetCrossoverProbabilityCompetitive(0.29)
                .SetHammingDistanceRelativeThreshold(0.3)
                .SetTargetSampleSize(200)
                .SetMaxRanksCompensatedByDistance(0)
                .SetFeatureSubsetRatioForDistance(0.31)
                .SetTrackConvergenceBehavior(true)
                .SetDistanceMetric(DistanceMetric.L1Average.ToString())
                .SetStrictCompatibilityCheck(false)
                .SetMaximumNumberParallelEvaluations(7)
                .SetEvaluationLimit(1)
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.None)
                .SetMaximumNumberGgaGenerations(234)
                .SetMaximumNumberGgaGenerationsWithSameIncumbent(134)
                .SetScoreGenerationHistory(true)
                .SetZipOldStatusFiles(true)
                .BuildWithFallback(defaultConfig);

            // Check those two configuration are technically compatible.
            Assert.True(
                configWithDifferentBaseParameters.IsTechnicallyCompatible(defaultConfig),
                "Configurations should be compatible.");
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsTechnicallyCompatible"/> returns false if the train
        /// model setting changes from false to true.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForNewTrainModelSetting()
        {
            var defaultConfig = this._builder.SetTrainModel(false).Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetTrainModel(true)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            Assert.False(otherConfig.IsTechnicallyCompatible(defaultConfig), "Configurations should not be compatible.");
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsTechnicallyCompatible"/> returns false if the
        /// continuous optimization method to combine with GGA(++) changes to something other than 'none'.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForNewContinuousOptimizationMethod()
        {
            var defaultConfig = this._builder.SetTrainModel(false).Build(maximumNumberParallelEvaluations: 1);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.Jade)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            Assert.False(otherConfig.IsTechnicallyCompatible(defaultConfig), "Configurations should not be compatible.");
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.IsTechnicallyCompatible"/> returns false if associated
        /// <see cref="GenomePredictionRandomForestConfig"/> is technically incompatible.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForIncompatibleRandomForestConfiguration()
        {
            var defaultConfig = this._builder
                .AddDetailedConfigurationBuilder(RegressionForestArgumentParser.Identifier, this._randomForestConfigBuilder)
                .Build(maximumNumberParallelEvaluations: 1);
            var otherRandomForestConfigBuilder = this._randomForestConfigBuilder.SetTreeCount(365);
            var otherConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    otherRandomForestConfigBuilder)
                .SetMaximumNumberParallelEvaluations(1)
                .BuildWithFallback(defaultConfig);
            AlgorithmTunerConfigurationTest.CheckTechnicalIncompatibility(defaultConfig, otherConfig);
        }

        /// <summary>
        /// Checks that calling
        /// <see cref="AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.AddDetailedConfigurationBuilder"/>
        /// twice with the same key throws an <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void AddingSameDetailedConfigurationBuilderTwiceThrowsError()
        {
            this._builder.AddDetailedConfigurationBuilder("hi", this._randomForestConfigBuilder);
            Assert.Throws<ArgumentException>(() => this._builder.AddDetailedConfigurationBuilder("hi", this._randomForestConfigBuilder));
        }

        /// <summary>
        /// Checks that calling
        /// <see cref="AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.AddDetailedConfigurationBuilder"/>
        /// twice with the null as builder throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void AddingNullAsDetailedConfigurationBuilderThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => this._builder.AddDetailedConfigurationBuilder("hi", null));
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.ExtractDetailedConfiguration{TConfiguration}"/>
        /// throws a <see cref="KeyNotFoundException"/> if no configuration with identifier
        /// <see cref="RegressionForestArgumentParser.Identifier"/> exists.
        /// </summary>
        [Fact]
        public void ExtractDetailedConfigurationThrowsForMissingConfiguration()
        {
            var config = this._builder.Build(1);
            Assert.Throws<KeyNotFoundException>(() => config.ExtractDetailedConfiguration<GenomePredictionRandomForestConfig>("does_not_exist"));
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.ExtractDetailedConfiguration{TConfiguration}"/>
        /// throws a <see cref="InvalidCastException"/> if the provided type does not match the found one.
        /// </summary>
        [Fact]
        public void ExtractDetailedConfigurationThrowsForUnexpectedType()
        {
            this._builder.AddDetailedConfigurationBuilder("hi", this._randomForestConfigBuilder);
            var config = this._builder.Build(1);
            Assert.Throws<InvalidCastException>(() => config.ExtractDetailedConfiguration<DifferentialEvolutionStrategyConfiguration>("hi"));
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.ExtractDetailedConfiguration{TConfiguration}"/>
        /// extracts the correct configuration if it exists.
        /// </summary>
        [Fact]
        public void ExtractDetailedConfigurationExtractsConfiguration()
        {
            this._builder.AddDetailedConfigurationBuilder("hi", this._randomForestConfigBuilder);
            this._builder.AddDetailedConfigurationBuilder(
                "ho",
                new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder());
            var config = this._builder.Build(1);

            var forestConfig = config.ExtractDetailedConfiguration<GenomePredictionRandomForestConfig>("hi");
            Assert.NotNull(forestConfig);
        }

        /// <summary>
        /// Verifies that setting the population size to 1 results in an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void TooSmallPopulationSizeThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetPopulationSize(1));
        }

        /// <summary>
        /// Checks that setting the population size to 2 does not provoke an error.
        /// </summary>
        [Fact]
        public void MinimumPopulationDoesNotThrowError()
        {
            this._builder.SetPopulationSize(2).SetMaxGenomeAge(1).Build(3);
        }

        /// <summary>
        /// Verifies that setting the evaluation limit to -1 results in an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void NegativeEvaluationLimitThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetEvaluationLimit(-1));
        }

        /// <summary>
        /// Checks that setting the evaluation limit to 0 does not provoke an error.
        /// </summary>
        [Fact]
        public void ZeroEvaluationsAllowedDoesNotThrowError()
        {
            this._builder.SetEvaluationLimit(0).Build(3);
        }

        /// <summary>
        /// Verifies that setting the number of generations to 0 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void TooSmallGenerationNumberThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetGenerations(0));
        }

        /// <summary>
        /// Checks that setting the number of generations to 1 does not provoke an error.
        /// </summary>
        [Fact]
        public void MinimumGenerationNumberDoesNotThrowError()
        {
            this._builder.SetGenerations(1).SetGoalGeneration(0).Build(3);
        }

        /// <summary>
        /// Verifies that setting the number of generations per GGA phase to -1 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void NegativeNumberGenerationsPerPhaseNumberThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetMaximumNumberGgaGenerations(-1));
        }

        /// <summary>
        /// Checks that setting the number of generations per GGA phase to 0 does not provoke an error.
        /// </summary>
        [Fact]
        public void MinimumGenerationsPerPhaseNumberDoesNotThrowError()
        {
            this._builder.SetMaximumNumberGgaGenerations(0).SetGoalGeneration(0).Build(3);
        }

        /// <summary>
        /// Verifies that setting the maximum number of consecutive GGA generations with no newly found incumbent to 0
        /// results in an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ZeroGenerationsWithoutNewlyFoundIncumbentThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetMaximumNumberGgaGenerationsWithSameIncumbent(0));
        }

        /// <summary>
        /// Checks that setting the maximum number of consecutive GGA generations with no newly found incumbent to 1
        /// does not provoke an error.
        /// </summary>
        [Fact]
        public void MinimumoGenerationsWithoutNewlyFoundIncumbentDoesNotThrowError()
        {
            this._builder
                .SetMaximumNumberGgaGenerationsWithSameIncumbent(1)
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.Jade)
                .Build(3);
        }

        /// <summary>
        /// Verifies that setting the maximum genome age to 0 results in an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void TooSmallMaxGenomeAgeThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetMaxGenomeAge(0));
        }

        /// <summary>
        /// Checks that setting the maximum genome age to 1 does not provoke an error.
        /// </summary>
        [Fact]
        public void MinimumMaxGenomeAgeDoesNotThrowError()
        {
            this._builder.SetMaxGenomeAge(1).Build(3);
        }

        /// <summary>
        /// Verifies that setting the maximum genome age to a value higher than the bigger half of the population
        /// throws an <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void MaxGenomeAgeGreaterThanBothGenderPopulationThrowsError()
        {
            Assert.Throws<ArgumentException>(() => this._builder.SetMaxGenomeAge(6).SetPopulationSize(9).Build(1));
        }

        /// <summary>
        /// Checks that setting the maximum genome age to the size of the bigger half of the population does not
        /// provoke an error.
        /// </summary>
        [Fact]
        public void MaxGenomeAgeEqualsBiggerGenderPopulationSizeDoesNotThrowError()
        {
            this._builder.SetMaxGenomeAge(5).SetPopulationSize(9).Build(1);
        }

        /// <summary>
        /// Verifies that setting the number of tournament winners to 0% results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ZeroPercentTournamentWinnerPercentageThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetTournamentWinnerPercentage(0));
        }

        /// <summary>
        /// Verifies that setting the number of tournament winners to 100% results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void OneHundredPercentTournamentWinnerPercentageThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetTournamentWinnerPercentage(1));
        }

        /// <summary>
        /// Verifies that setting the CPU timeout to 0 results in an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ZeroCpuTimeoutThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetCpuTimeout(TimeSpan.FromMilliseconds(0)));
        }

        /// <summary>
        /// Verifies that setting the CPU timeout to -1 results in an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void NegativeCpuTimeoutThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetCpuTimeout(TimeSpan.FromMilliseconds(-1)));
        }

        /// <summary>
        /// Verifies that setting the crossover switch probability to a negative value results in a
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void NegativeCrossoverSwitchProbabilityThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetCrossoverSwitchProbability(-0.1));
        }

        /// <summary>
        /// Verifies that setting the crossover switch probability to a value larger than 100% results in a
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void Above1CrossoverSwitchProbabilityThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetCrossoverSwitchProbability(1.1));
        }

        /// <summary>
        /// Checks that setting the crossover switch probability to 0 does not provoke an error.
        /// </summary>
        [Fact]
        public void ZeroCrossoverSwitchProbabilityDoesNotThrowError()
        {
            this._builder.SetCrossoverSwitchProbability(0).Build(3);
        }

        /// <summary>
        /// Checks that setting the crossover switch probability to 1 does not provoke an error.
        /// </summary>
        [Fact]
        public void AlwaysSwitchDoesNotThrowError()
        {
            this._builder.SetCrossoverSwitchProbability(1).Build(3);
        }

        /// <summary>
        /// Verifies that setting the mutation rate to -0.1 results in an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void NegativeMutationRateThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetMutationRate(-0.1));
        }

        /// <summary>
        /// Verifies that setting the mutation rate to 1.1 results in an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void Above1MutationRateThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetMutationRate(1.1));
        }

        /// <summary>
        /// Checks that setting the mutation rate to 0 does not provoke an error.
        /// </summary>
        [Fact]
        public void ZeroMutationRateDoesNotThrowError()
        {
            this._builder.SetMutationRate(0).Build(3);
        }

        /// <summary>
        /// Checks that setting the mutation rate to 1 does not provoke an error.
        /// </summary>
        [Fact]
        public void AlwaysMutateDoesNotThrowError()
        {
            this._builder.SetMutationRate(1).Build(3);
        }

        /// <summary>
        /// Verifies that setting the mutation variance percentage to 0 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ZeroMutationVariancePercentageThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetMutationVariancePercentage(0));
        }

        /// <summary>
        /// Verifies that setting the mutation variance percentage to 1.1 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void Above1MutationVariancePercentageThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetMutationVariancePercentage(1.1));
        }

        /// <summary>
        /// Checks that setting the mutation variance percentage to 1 does not provoke an error.
        /// </summary>
        [Fact]
        public void MutationVariancePercentageOf1DoesNotThrowError()
        {
            this._builder.SetMutationVariancePercentage(1).Build(3);
        }

        /// <summary>
        /// Verifies that setting the maximum number of repair attempts to -1 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void NegativeNumberRepairAttemptsThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetMaxRepairAttempts(-1));
        }

        /// <summary>
        /// Checks that setting the maximum number of repair attempts to 0 does not provoke an error.
        /// </summary>
        [Fact]
        public void ZeroRepairAttemptsDoesNotThrowError()
        {
            this._builder.SetMaxRepairAttempts(0).Build(maximumNumberParallelEvaluations: 1);
        }

        /// <summary>
        /// Verifies that setting the maximum number of tries per evaluation to -1 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void NegativeMaximumNumberTriesPerEvaluationThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetMaximumNumberConsecutiveFailuresPerEvaluation(-1));
        }

        /// <summary>
        /// Checks that setting the maximum number of tries per evaluation and tolerating a failure to 0 does not
        /// provoke an error.
        /// </summary>
        [Fact]
        public void ZeroMaximumNumberTriesPerEvaluationDoesNotThrowError()
        {
            this._builder
                .SetMaximumNumberConsecutiveFailuresPerEvaluation(0)
                .Build(maximumNumberParallelEvaluations: 1);
        }

        /// <summary>
        /// Verifies that setting the start number of instances to 0 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ZeroNumberOfInstancesThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetInstanceNumbers(0, 200));
        }

        /// <summary>
        /// Verifies that setting the final number of instances to less than the start number results in an <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void DecreasingNumberOfInstancesThrowsError()
        {
            Assert.Throws<ArgumentException>(() => this._builder.SetInstanceNumbers(100, 40));
        }

        /// <summary>
        /// Checks that setting both the final and the start number of instances to 1 does not provoke an error.
        /// </summary>
        [Fact]
        public void ConstantlyOneInstanceDoesNotThrowError()
        {
            this._builder.SetInstanceNumbers(1, 1).Build(3);
        }

        /// <summary>
        /// Verifies that setting the goal generation to -1 results in an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void NegativeGoalGenerationThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetGoalGeneration(-1));
        }

        /// <summary>
        /// Checks that setting the goal generation to 0 does not provoke an error.
        /// </summary>
        [Fact]
        public void GoalGenerationZeroDoesNotThrowError()
        {
            this._builder.SetGoalGeneration(0).Build(maximumNumberParallelEvaluations: 1);
        }

        /// <summary>
        /// Verifies that setting the goal generation index to the same number as number generations results in an
        /// <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void GoalGenerationEqualsGenerationNumberThrowsError()
        {
            Assert.Throws<ArgumentException>(() => this._builder.SetGenerations(30).SetGoalGeneration(30).Build(maximumNumberParallelEvaluations: 1));
        }

        /// <summary>
        /// Checks that setting the goal generation to the last generation (#number generations - 1) does not provoke
        /// an error.
        /// </summary>
        [Fact]
        public void GoalGenerationLastGenerationDoesNotThrowError()
        {
            this._builder.SetGenerations(30).SetGoalGeneration(29).Build(maximumNumberParallelEvaluations: 1);
        }

        /// <summary>
        /// Verifies that setting the Akka.NET configuration to null results in an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void NullReferenceAsAkkaConfigurationThrowsError()
        {
            Assert.Throws<ArgumentNullException>(() => this._builder.SetAkkaConfiguration(null));
        }

        /// <summary>
        /// Verifies that setting the number of cores to 0 results in an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ZeroCoresThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.Build(0));
        }

        /// <summary>
        /// Checks that setting the number of cores to 1 does not provoke an error.
        /// </summary>
        [Fact]
        public void OneCoreDoesNotThrowError()
        {
            this._builder.Build(1);
        }

        /// <summary>
        /// Verifies that setting the maximum number of parallel evaluations per node to 0 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ZeroMaximumNumberParallelEvaluationsThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.Build(maximumNumberParallelEvaluations: 0));
        }

        /// <summary>
        /// Checks that setting the maximum number of parallel evaluations per node to 1 does not
        /// provoke an error.
        /// </summary>
        [Fact]
        public void MaximumNumberOfParallelEvaluationsOf1DoesNotThrowError()
        {
            this._builder.Build(maximumNumberParallelEvaluations: 1);
        }

        /// <summary>
        /// Verifies that setting the maximum mini tournament size to 0 results in an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ZeroMaximumMiniTournamentSizeThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetMaximumMiniTournamentSize(0).Build(maximumNumberParallelEvaluations: 1));
        }

        /// <summary>
        /// Checks that setting the maximum mini tournament size to 1 does not provoke an error.
        /// </summary>
        [Fact]
        public void MaximumMiniTournamentSizeOf1DoesNotThrowError()
        {
            this._builder.SetMaximumMiniTournamentSize(1).Build(maximumNumberParallelEvaluations: 1);
        }

        /// <summary>
        /// Verifies that not specifying the maximum number of parallel evaluations leads to an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void MissingMaximumNumberParallelEvaluationsThrowsError()
        {
            Assert.Throws<InvalidOperationException>(() => this._builder.Build());
        }

        /// <summary>
        /// Checks that setting the maximum number of parallel evaluations per node to a value higher than the maximum
        /// mini tournament size writes out a warning.
        /// </summary>
        [Fact]
        public void HigherNumberOfParallelEvaluationsThanTournamentSizeWritesWarning()
        {
            TestUtils.CheckOutput(
                action: () =>
                    {
                        // Build configuration with suspicious settings.
                        this._builder.SetMaximumMiniTournamentSize(3).Build(maximumNumberParallelEvaluations: 4);
                    },
                check: consoleOutput =>
                    {
                        // Check that a warning is written to console.
                        Assert.True(consoleOutput.ToString().Contains("Warning"), "No warning was written to console.");
                    });
        }

        /// <summary>
        /// Verifies that setting the verbosity level to an undefined value throws an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void UndefinedVerbosityLevelThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetVerbosity((VerbosityLevel)42));
        }

        /// <summary>
        /// Verifies that setting the continuous optimization method to an undefined value throws an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void UndefinedContinuousOptimizationMethodThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._builder.SetContinuousOptimizationMethod((ContinuousOptimizationMethod)42));
        }

        /// <summary>
        /// Verifies that setting the maximum number of GGA generations per phase smaller than the overall number of
        /// generations without using an additional method writes out a warning.
        /// </summary>
        [Fact]
        public void SmallNumberGgaGenerationsWritesWarningWithoutContinuousOptimizationMethod()
        {
            TestUtils.CheckOutput(
                action: () =>
                    {
                        // Build configuration with suspicious settings.
                        this._builder
                            .SetGenerations(33)
                            .SetGoalGeneration(0)
                            .SetMaximumNumberGgaGenerations(32)
                            .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.None)
                            .Build(maximumNumberParallelEvaluations: 4);
                    },
                check: consoleOutput =>
                    {
                        // Check that a warning is written to console.
                        Assert.True(consoleOutput.ToString().Contains("Warning"), "No warning was written to console.");
                    });
        }

        /// <summary>
        /// Verifies that setting the maximum number of consecutive GGA generations with the same incumbent smaller
        /// than the overall number of generations without using an additional method writes out a warning.
        /// </summary>
        [Fact]
        public void SmallNumberGgaGenerationsWithSameIncumbentWritesWarningWithoutContinuousOptimizationMethod()
        {
            TestUtils.CheckOutput(
                action: () =>
                    {
                        // Build configuration with suspicious settings.
                        this._builder
                            .SetGenerations(33)
                            .SetGoalGeneration(0)
                            .SetMaximumNumberGgaGenerationsWithSameIncumbent(31)
                            .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.None)
                            .Build(maximumNumberParallelEvaluations: 4);
                    },
                check: consoleOutput =>
                    {
                        // Check that a warning is written to console.
                        Assert.True(consoleOutput.ToString().Contains("Warning"), "No warning was written to console.");
                    });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a valid configuration object.
        /// </summary>
        /// <returns>The created object.</returns>
        protected override ConfigurationBase CreateTestConfiguration()
        {
            return this._builder.Build(1);
        }

        /// <summary>
        /// Creates a valid configuration builder object.
        /// </summary>
        /// <returns>The created object.</returns>
        protected override IConfigBuilder<ConfigurationBase> CreateTestConfigurationBuilder()
        {
            return new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder();
        }

        #endregion
    }
}