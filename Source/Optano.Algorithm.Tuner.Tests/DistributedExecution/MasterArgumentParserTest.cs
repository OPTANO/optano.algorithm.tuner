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

namespace Optano.Algorithm.Tuner.Tests.DistributedExecution
{
    using System;

    using NDesk.Options;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.DistributedExecution;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration;
    using Optano.Algorithm.Tuner.Tests.Configuration.ArgumentParsers;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="MasterArgumentParser"/> class.
    /// </summary>
    public class MasterArgumentParserTest : HelpSupportingArgumentParserBaseTest
    {
        #region Fields

        /// <summary>
        /// The <see cref="MasterArgumentParser"/> used in tests. Must be initialized.
        /// </summary>
        private readonly MasterArgumentParser _parser;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MasterArgumentParserTest"/> class.
        /// </summary>
        public MasterArgumentParserTest()
        {
            this._parser = new MasterArgumentParser();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="MasterArgumentParser"/> used in tests as a <see cref="HelpSupportingArgumentParserBase"/> to use
        /// in base class tests.
        /// </summary>
        protected override HelpSupportingArgumentParserBase Parser => this._parser;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that accessing <see cref="HelpSupportingArgumentParser{T}.ConfigurationBuilder"/> before calling
        /// <see cref="MasterArgumentParser.ParseArguments(string[])"/> throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void AccessingConfigurationBuilderBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.ConfigurationBuilder);
        }

        /// <summary>
        /// Verifies that accessing <see cref="MasterArgumentParser.MaximumNumberParallelEvaluations"/> before calling
        /// <see cref="MasterArgumentParser.ParseArguments(string[])"/> throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void AccessingMaximumNumberParallelEvaluationsBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.MaximumNumberParallelEvaluations);
        }

        /// <summary>
        /// Verifies that accessing <see cref="MasterArgumentParser.PathToTrainingInstanceFolder"/> before calling
        /// <see cref="MasterArgumentParser.ParseArguments(string[])"/> throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void AccessingPathToTrainingInstanceFolderBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.PathToTrainingInstanceFolder);
        }

        /// <summary>
        /// Verifies that accessing <see cref="MasterArgumentParser.PathToTestInstanceFolder"/> before calling
        /// <see cref="MasterArgumentParser.ParseArguments(string[])"/> throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void AccessingPathToTestInstanceFolderBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.PathToTestInstanceFolder);
        }

        /// <summary>
        /// Verifies that accessing <see cref="MasterArgumentParser.Port"/> before calling
        /// <see cref="MasterArgumentParser.ParseArguments(string[])"/> throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void AccessingPortBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.Port);
        }

        /// <summary>
        /// Verifies that accessing <see cref="MasterArgumentParser.StartFromExistingStatus"/> before calling
        /// <see cref="MasterArgumentParser.ParseArguments(string[])"/> throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void AccessingStartFromExistingStatusBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.StartFromExistingStatus);
        }

        /// <summary>
        /// Verifies that accessing <see cref="MasterArgumentParser.StatusFileDirectory"/> before calling
        /// <see cref="MasterArgumentParser.ParseArguments(string[])"/> throws an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void AccessingStatusFileDirectoryBeforeParsingThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => this._parser.StatusFileDirectory);
        }

        /// <summary>
        /// Checks that <see cref="MasterArgumentParser.ParseArguments(string[])"/> correctly interprets arguments
        /// given in the --longoption=value format.
        /// </summary>
        [Fact]
        public void LongOptionsAreParsedCorrectly()
        {
            string[] args =
                {
                    "--popSize=23",
                    "--numGens=45",
                    "--goalGen=13",
                    "--cpuTimeout=30",
                    "--winnerPercentage=0.34",
                    "--instanceNumbers=1:23",
                    "--miniTournamentSize=7",
                    "--maxParallelEvaluations=5",
                    "--maxParallelThreads=7",
                    "--trainingInstanceFolder=C:\\Temp",
                    "--testInstanceFolder=C:\\Test",
                    "--port=42",
                    "--enableRacing=false",
                    "--maxGenomeAge=5",
                    "--mutationRate=0.2",
                    "--mutationVariance=0.3",
                    "--crossoverSwitchProbability=0.05",
                    "--maxRepair=50",
                    "--verbose=1",
                    "--faultTolerance=5",
                    "--logFile=foo",
                    "--statusFileDir=bar",
                    "--strictCompatibilityCheck=true",
                    "--trackConvergenceBehavior=true",
                    "--trainModel=true",
                    "--enableSexualSelection=true",
                    "--evaluationLimit=12",
                    "--jade",
                    "--maxGenerationsPerGgaPhase=13",
                    "--maxGgaGenerationsWithSameIncumbent=4",
                    "--scoreGenerationHistory",
                    "--zipOldStatus=true",
                    "--addDefaultGenome=false",
                };

            this._parser.ParseArguments(args);
            var parsedConfig = this._parser.ConfigurationBuilder.Build();

            Assert.Equal(45, parsedConfig.Generations);
            Assert.Equal(13, parsedConfig.GoalGeneration);
            Assert.Equal(23, parsedConfig.PopulationSize);
            Assert.Equal(
                0.34,
                parsedConfig.TournamentWinnerPercentage);
            Assert.Equal(
                30,
                parsedConfig.CpuTimeout.TotalSeconds);
            Assert.True(
                parsedConfig.StartNumInstances == 1 && parsedConfig.EndNumInstances == 23,
                "Number instances was not parsed correctly");
            Assert.Equal(
                5,
                parsedConfig.MaximumNumberParallelEvaluations);
            Assert.Equal(
                7,
                parsedConfig.MaximumNumberParallelThreads);
            Assert.Equal(
                7,
                parsedConfig.MaximumMiniTournamentSize);
            Assert.Equal(42, this._parser.Port);
            Assert.Equal(
                "C:\\Temp",
                this._parser.PathToTrainingInstanceFolder);
            Assert.Equal(
                "C:\\Test",
                this._parser.PathToTestInstanceFolder);
            Assert.False(parsedConfig.EnableRacing);
            Assert.Equal(5, parsedConfig.MaxGenomeAge);
            Assert.Equal(0.2, parsedConfig.MutationRate);
            Assert.Equal(
                0.3,
                parsedConfig.MutationVariancePercentage);
            Assert.Equal(
                0.05,
                parsedConfig.CrossoverSwitchProbability);
            Assert.Equal(50, parsedConfig.MaxRepairAttempts);
            Assert.Equal(
                VerbosityLevel.Info,
                parsedConfig.Verbosity);
            Assert.Equal(
                5,
                parsedConfig.MaximumNumberConsecutiveFailuresPerEvaluation);
            Assert.Equal("foo", parsedConfig.LogFilePath);
            Assert.Equal("bar", parsedConfig.StatusFileDirectory);
            Assert.Equal("bar", this._parser.StatusFileDirectory);
            Assert.True(
                parsedConfig.StrictCompatibilityCheck);
            Assert.True(
                parsedConfig.TrackConvergenceBehavior);
            Assert.True(parsedConfig.TrainModel);
            Assert.True(parsedConfig.EnableSexualSelection);
            Assert.Equal(12, parsedConfig.EvaluationLimit);
            Assert.Equal(
                ContinuousOptimizationMethod.Jade,
                parsedConfig.ContinuousOptimizationMethod);
            Assert.Equal(
                13,
                parsedConfig.MaximumNumberGgaGenerations);
            Assert.Equal(
                4,
                parsedConfig.MaximumNumberGgaGenerationsWithSameIncumbent);
            Assert.True(
                parsedConfig.ScoreGenerationHistory);
            Assert.True(parsedConfig.ZipOldStatusFiles);
            Assert.False(parsedConfig.AddDefaultGenome);
        }

        /// <summary>
        /// Checks that <see cref="MasterArgumentParser.ParseArguments(string[])"/> correctly interprets arguments
        /// given in the -shortoption value format.
        /// </summary>
        [Fact]
        public void ShortOptionsAreParsedCorrectly()
        {
            string[] args =
                {
                    "--maxParallelEvaluations=5",
                    "-p", "23",
                    "-g", "45",
                    "-t", "30",
                    "-w", "0.34",
                    "-i", "1:23",
                    "-s", "7",
                    "-m", "0.2",
                    "-v", "1",
                };
            this._parser.ParseArguments(args);
            var parsedConfig = this._parser.ConfigurationBuilder
                .SetGoalGeneration(0) // Make sure to not provoke an exception here.
                .Build();

            Assert.Equal(45, parsedConfig.Generations);
            Assert.Equal(23, parsedConfig.PopulationSize);
            Assert.Equal(
                0.34,
                parsedConfig.TournamentWinnerPercentage);
            Assert.Equal(
                30,
                parsedConfig.CpuTimeout.TotalSeconds);
            Assert.True(
                parsedConfig.StartNumInstances == 1 && parsedConfig.EndNumInstances == 23);
            Assert.Equal(
                5,
                parsedConfig.MaximumNumberParallelEvaluations);
            Assert.Equal(5, parsedConfig.MaximumNumberParallelThreads);
            Assert.Equal(
                7,
                parsedConfig.MaximumMiniTournamentSize);
            Assert.Equal(0.2, parsedConfig.MutationRate);
            Assert.Equal(
                VerbosityLevel.Info,
                parsedConfig.Verbosity);
        }

        /// <summary>
        /// Checks that using options defined by <see cref="DifferentialEvolutionStrategyArgumentParser"/> without using
        /// --jade, throw <see cref="AggregateException"/>s.
        /// </summary>
        [Fact]
        public void DifferentialEvolutionOptionsThrowExceptionWithoutCorrectContinuousOptimizationMethod()
        {
            string[] args = { "--maxParallelEvaluations=5", "--minDomainSize=234" };
            Assert.Throws<AggregateException>(() => this._parser.ParseArguments(args));
        }

        /// <summary>
        /// Checks that options defined by <see cref="DifferentialEvolutionStrategyArgumentParser"/> are parsed when
        /// using --jade.
        /// </summary>
        [Fact]
        public void DifferentialEvolutionOptionsAreParsedWithDifferentialEvolutionOption()
        {
            string[] args = { "--maxParallelEvaluations=5", "--minDomainSize=234", "--jade" };
            this._parser.ParseArguments(args);
            var parsedConfig = this._parser.ConfigurationBuilder
                .SetGoalGeneration(0) // Make sure to not provoke an exception here.
                .Build();

            var deConfig = parsedConfig.ExtractDetailedConfiguration<DifferentialEvolutionStrategyConfiguration>(
                DifferentialEvolutionStrategyArgumentParser.Identifier);
            Assert.Equal(234, deConfig.MinimumDomainSize);
        }

        /// <summary>
        /// Checks that using options defined by <see cref="CovarianceMatrixAdaptationStrategyArgumentParser"/> without using
        /// --cmaEs, throw <see cref="AggregateException"/>s.
        /// </summary>
        [Fact]
        public void CovarianceMatrixAdaptationOptionsThrowExceptionWithoutCorrectContinuousOptimizationMethod()
        {
            string[] args = { "--maxParallelEvaluations=5", "--maxGenerationsPerCmaEsPhase=3" };
            Assert.Throws<AggregateException>(() => this._parser.ParseArguments(args));
        }

        /// <summary>
        /// Checks that options defined by <see cref="CovarianceMatrixAdaptationStrategyArgumentParser"/> are parsed when
        /// using --jade.
        /// </summary>
        [Fact]
        public void CovarianceMatrixAdaptationOptionsAreParsedWithDifferentialEvolutionOption()
        {
            string[] args = { "--maxParallelEvaluations=5", "--maxGenerationsPerCmaEsPhase=3", "--cmaEs" };
            this._parser.ParseArguments(args);
            var parsedConfig = this._parser.ConfigurationBuilder
                .SetGoalGeneration(0) // Make sure to not provoke an exception here.
                .Build();

            var cmaEsConfig = parsedConfig.ExtractDetailedConfiguration<CovarianceMatrixAdaptationStrategyConfiguration>(
                CovarianceMatrixAdaptationStrategyArgumentParser.Identifier);
            Assert.Equal(3, cmaEsConfig.MaximumNumberGenerations);
        }

        /// <summary>
        /// Checks that setting two continuous optimization methods at the same time throws a
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void SettingTwoContinuousOptimizationMethodsThrowsException()
        {
            string[] args = { "--maxParallelEvaluations=5", "--cmaEs", "--jade" };
            Assert.Throws<InvalidOperationException>(() => this._parser.ParseArguments(args));
        }

        /// <summary>
        /// Checks that the configuration options that may go together with --continue are parsed correctly if given in
        /// their long version.
        /// </summary>
        [Fact]
        public void LongOptionsCompatibleWithContinueParameterAreParsedCorrectly()
        {
            string[] args =
                {
                    "--continue",
                    "--maxParallelEvaluations=5",
                    "--maxParallelThreads=3",
                    "--trainingInstanceFolder=C:\\Temp",
                    "--testInstanceFolder=C:\\Test",
                    "--port=42",
                    "--maxRepair=50",
                    "--verbose=1",
                    "--faultTolerance=5",
                    "--logFile=foo",
                    "--statusFileDir=lala",
                    "--ownHostName=bar",
                    "--trackConvergenceBehavior=true",
                    "--trainModel=true",
                    "--evaluationLimit=12",
                    "--scoreGenerationHistory",
                    "--zipOldStatus=true",
                };

            this._parser.ParseArguments(args);
            var parsedConfig = this._parser.ConfigurationBuilder.Build();

            Assert.True(this._parser.StartFromExistingStatus, "--continue was not parsed correctly.");
            Assert.Equal(
                5,
                parsedConfig.MaximumNumberParallelEvaluations);
            Assert.Equal(
                3,
                parsedConfig.MaximumNumberParallelThreads);
            Assert.Equal(42, this._parser.Port);
            Assert.Equal(
                "C:\\Temp",
                this._parser.PathToTrainingInstanceFolder);
            Assert.Equal(
                "C:\\Test",
                this._parser.PathToTestInstanceFolder);
            Assert.Equal(50, parsedConfig.MaxRepairAttempts);
            Assert.Equal(
                VerbosityLevel.Info,
                parsedConfig.Verbosity);
            Assert.Equal(
                5,
                parsedConfig.MaximumNumberConsecutiveFailuresPerEvaluation);
            Assert.Equal("foo", parsedConfig.LogFilePath);
            Assert.Equal("lala", parsedConfig.StatusFileDirectory);
            Assert.Equal("lala", this._parser.StatusFileDirectory);
            Assert.Equal("bar", this._parser.OwnHostName);
            Assert.True(
                parsedConfig.TrackConvergenceBehavior,
                "Track convergence behavior setting was not parsed correctly.");
            Assert.True(parsedConfig.TrainModel, "Train model setting was not parsed correctly.");
            Assert.Equal(12, parsedConfig.EvaluationLimit);
            Assert.True(
                parsedConfig.ScoreGenerationHistory,
                "Score generation history flag was not parsed correctly.");
            Assert.True(parsedConfig.ZipOldStatusFiles, "Zip old status files option was not parsed correctly.");
        }

        /// <summary>
        /// Checks that the configuration options that may go together with --continue are parsed correctly if given in
        /// their short version.
        /// </summary>
        [Fact]
        public void ShortOptionsCompatibleWithContinueParameterAreParsedCorrectly()
        {
            string[] args = { "--continue", "-maxParallelEvaluations=5", "-v", "1" };
            this._parser.ParseArguments(args);
            var parsedConfig = this._parser.ConfigurationBuilder.Build();

            Assert.Equal(
                VerbosityLevel.Info,
                parsedConfig.Verbosity);
        }

        /// <summary>
        /// Checks that configuration options which change the inner workings of the algorithm do not throw an
        /// <see cref="OptionException"/> if combined with the --continue and --strictCompatibilityCheck=false
        /// parameters.
        /// </summary>
        [Fact]
        public void DisablingStrictCompatibilityCheckEnablesAllConfigurationOptionsForContinueParameter()
        {
            string[] args =
                {
                    "--continue",
                    "--maxParallelEvaluations=5",
                    "--strictCompatibilityCheck=false",
                    "--popSize=23",
                    "--numGens=45",
                    "--goalGen=13",
                    "--cpuTimeout=30",
                    "--winnerPercentage=0.34",
                    "--instanceNumbers=1:23",
                    "--miniTournamentSize=7",
                    "--enableRacing=false",
                    "--maxGenomeAge=5",
                    "--mutationRate=0.2",
                    "--mutationVariance=0.3",
                    "--crossoverSwitchProbability=0.05",
                    "--enableSexualSelection=true",
                    "--populationMutantRatio=0.2",
                    "--enableSexualSelection=true",
                    "--engineeredProportion=0.5",
                    "--topPerformerThreshold=0.9",
                    "--startIterationEngineering=1",
                    "--crossoverProbabilityCompetitive=0.4",
                    "--hammingDistanceRelativeThreshold=0.02",
                    "--targetSampleSize=12",
                    "--maxRanksCompensatedByDistance=45",
                    "--featureSubsetRatioForDistance=0.7",
                    "--distanceMetric=L1Average",
                    "--evaluationLimit=12",
                    "--jade",
                    "--maxGenerationsPerGgaPhase=13",
                    "--maxGgaGenerationsWithSameIncumbent=4",
                };

            // The main test here is that no exception happens.
            this._parser.ParseArguments(args);
            var parsedConfig = this._parser.ConfigurationBuilder.Build();

            // Moreover check, that the given parameters override their default values.
            Assert.Equal(5, parsedConfig.MaximumNumberParallelEvaluations);
            Assert.Equal(23, parsedConfig.PopulationSize);
            Assert.Equal(45, parsedConfig.Generations);
            Assert.Equal(13, parsedConfig.GoalGeneration);
            Assert.Equal(TimeSpan.FromSeconds(30), parsedConfig.CpuTimeout);
            Assert.Equal(0.34, parsedConfig.TournamentWinnerPercentage);
            Assert.Equal(1, parsedConfig.StartNumInstances);
            Assert.Equal(23, parsedConfig.EndNumInstances);
            Assert.Equal(7, parsedConfig.MaximumMiniTournamentSize);
            Assert.False(parsedConfig.EnableRacing);
            Assert.Equal(5, parsedConfig.MaxGenomeAge);
            Assert.Equal(0.2, parsedConfig.MutationRate);
            Assert.Equal(0.3, parsedConfig.MutationVariancePercentage);
            Assert.Equal(0.05, parsedConfig.CrossoverSwitchProbability);
            Assert.True(parsedConfig.EnableSexualSelection);
            Assert.Equal(0.5, parsedConfig.EngineeredPopulationRatio);
            Assert.Equal(0.9, parsedConfig.TopPerformerThreshold);
            Assert.Equal(1, parsedConfig.StartEngineeringAtIteration);
            Assert.Equal(0.4, parsedConfig.CrossoverProbabilityCompetitive);
            Assert.Equal(0.02, parsedConfig.HammingDistanceRelativeThreshold);
            Assert.Equal(12, parsedConfig.TargetSamplingSize);
            Assert.Equal(45, parsedConfig.MaxRanksCompensatedByDistance);
            Assert.Equal(0.7, parsedConfig.FeatureSubsetRatioForDistanceComputation);
            Assert.Equal(DistanceMetric.L1Average, parsedConfig.DistanceMetric);
            Assert.Equal(12, parsedConfig.EvaluationLimit);
            Assert.True(parsedConfig.ContinuousOptimizationMethod == ContinuousOptimizationMethod.Jade);
            Assert.Equal(13, parsedConfig.MaximumNumberGgaGenerations);
            Assert.Equal(4, parsedConfig.MaximumNumberGgaGenerationsWithSameIncumbent);
        }

        /// <summary>
        /// Checks that --help option is recognized even if --continue is provided.
        /// </summary>
        [Fact]
        public void LongHelpOptionWorksOnContinue()
        {
            string[] args = { "--continue", "--help" };
            this._parser.ParseArguments(args);
            Assert.True(this._parser.HelpTextRequested, "Help option was not parsed correctly.");
        }

        /// <summary>
        /// Checks that -h option is recognized even if --continue is provided.
        /// </summary>
        [Fact]
        public void ShortHelpOptionWorksOnContinue()
        {
            string[] args = { "--continue", "-h" };
            this._parser.ParseArguments(args);
            Assert.True(this._parser.HelpTextRequested, "Help option was not parsed correctly.");
        }

        /// <summary>
        /// Verifies that calling <see cref="MasterArgumentParser.ParseArguments(string[])"/> without providing
        /// information about the maximum number of parallel evaluations per node thrown an
        /// <see cref="OptionException"/>.
        /// </summary>
        [Fact]
        public void NoMaximumNumberParallelEvaluationInfoThrowsException()
        {
            string[] args = { "--continue" };
            Assert.Throws<OptionException>(() => this._parser.ParseArguments(args));
        }

        /// <summary>
        /// Verifies that unknown arguments, caused by e.g. typos, provoke an <see cref="AggregateException"/>.
        /// </summary>
        [Fact]
        public void TyposProvokeException()
        {
            string[] args = { "--halp" };
            Assert.Throws<AggregateException>(() => this._parser.ParseArguments(args));
        }

        #endregion
    }
}