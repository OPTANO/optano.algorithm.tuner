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
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Akka.Configuration;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.DistributedExecution;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestOutOfBox;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData.SamplingStrategies;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.GeneticAlgorithm;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tracking;
    using Optano.Algorithm.Tuner.Tuning;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="Master{TTargetAlgorithm,TInstance,TResult}"/> class.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupTwoName)]
    public class MasterTest : TestBase
    {
        #region Fields

        /// <summary>
        /// Path to status file that gets written in some tests.
        /// </summary>
        private readonly string _statusFilePath =
            PathUtils.GetAbsolutePathFromExecutableFolderRelative("status/status.oatstat");

        /// <summary>
        /// Path to alternative status file that gets written in some tests.
        /// </summary>
        private readonly string _alternativeStatusFilePath =
            PathUtils.GetAbsolutePathFromExecutableFolderRelative("status2/status.oatstat");

        /// <summary>
        /// Path to alternative log file that gets written in some tests.
        /// </summary>
        private readonly string _alternativeLogFilePath =
            PathUtils.GetAbsolutePathFromExecutableFolderRelative("foo.log");

        /// <summary>
        /// Path to <see cref="GgaStatus"/> file that gets written in some tests.
        /// </summary>
        private string _pathToGgaStatusFile;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="GeneticEngineering{TLearnerModel,TPredictorModel,TSamplingStrategy}"/> to use in tests.
        /// </summary>
        private GeneticEngineering<StandardRandomForestLearner<ReuseOldTreesStrategy>, GenomePredictionForestModel<GenomePredictionTree>,
            ReuseOldTreesStrategy> DummyGeneticEngineering { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ParameterTree"/> to use in tests.
        /// </summary>
        private ParameterTree ParameterTree { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that all command line parameters provided via
        /// <see cref="Master{TTargetAlgorithm, TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.Run"/>
        /// get translated into some kind of configuration.
        /// </summary>
        [Fact]
        public void CommandLineParametersAreTranslatedIntoConfiguration()
        {
            // Specify a value for all command line parameters (except the port parameter). The port parameter is skipped, because specifying the port can lead to problems in cross-platform-tests.
            string[] args =
                {
                    "--popSize=23",
                    "--numGens=1",
                    "--goalGen=0",
                    "--cpuTimeout=30",
                    "--winnerPercentage=0.34",
                    "--instanceNumbers=1:1",
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
                    "--verbose=3",
                    "--faultTolerance=5",
                    $"--logFile={this._alternativeLogFilePath}",
                    "--engineeredProportion=0.0",
                    "--strictCompatibilityCheck=false",
                    "--trackConvergenceBehavior=true",
                    "--trainModel",
                    "--enableSexualSelection=true",
                    "--zipOldStatus=true",
                };

            // Call run.
            Master<NoOperation, TestInstance, TestResult>.Run(
                args,
                (config, pathToTrainingInstances, pathToTestInstances) =>
                    {
                        // Check all parameters are translated into configuration.
                        Assert.Equal(
                            23,
                            config.PopulationSize);
                        Assert.Equal(
                            1,
                            config.Generations);
                        Assert.Equal(
                            0,
                            config.GoalGeneration);
                        Assert.Equal(
                            TimeSpan.FromSeconds(30).TotalMilliseconds,
                            config.CpuTimeout.TotalMilliseconds);
                        Assert.Equal(
                            0.34,
                            config.TournamentWinnerPercentage);
                        Assert.Equal(
                            1,
                            config.StartNumInstances);
                        Assert.Equal(
                            1,
                            config.EndNumInstances);
                        Assert.Equal(
                            7,
                            config.MaximumMiniTournamentSize);
                        Assert.Equal(
                            5,
                            config.MaximumNumberParallelEvaluations);
                        Assert.Equal(
                            7,
                            config.MaximumNumberParallelThreads);
                        Assert.Equal("C:\\Temp", pathToTrainingInstances);
                        Assert.Equal("C:\\Test", pathToTestInstances);
                        Assert.Equal(
                            42,
                            config.AkkaConfiguration.GetInt("akka.remote.dot-netty.tcp.port"));
                        Assert.False(config.EnableRacing);
                        Assert.Equal(
                            5,
                            config.MaxGenomeAge);
                        Assert.Equal(
                            0.2,
                            config.MutationRate);
                        Assert.Equal(
                            0.3,
                            config.MutationVariancePercentage);
                        Assert.Equal(
                            0.05,
                            config.CrossoverSwitchProbability);
                        Assert.Equal(
                            50,
                            config.MaxRepairAttempts);
                        Assert.Equal(
                            (VerbosityLevel)3,
                            config.Verbosity);
                        Assert.Equal(
                            5,
                            config.MaximumNumberConsecutiveFailuresPerEvaluation);
                        Assert.Equal(
                            "DEBUG",
                            config.AkkaConfiguration.GetString("akka.loglevel"));
                        Assert.Equal(
                            this._alternativeLogFilePath,
                            config.LogFilePath);
                        Assert.False(
                            config.StrictCompatibilityCheck);
                        Assert.True(
                            config.TrackConvergenceBehavior);
                        Assert.True(
                            config.TrainModel);
                        Assert.True(
                            config.EnableSexualSelection);
                        Assert.True(
                            config.ZipOldStatusFiles);

                        // Return an algorithm tuner that quickly terminates.
                        return this.BuildSimpleAlgorithmTuner(config, pathToTrainingInstances, pathToTestInstances);
                    });
        }

        /// <summary>
        /// Checks that a call to
        /// <see cref="Master{TTargetAlgorithm, TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.Run"/>
        /// does not start execution if --help is provided as a parameter.
        /// </summary>
        [Fact]
        public void RunDoesNotStartOnHelpParameter()
        {
            // Prepare variable to find out whether run is started.
            bool runIsStarted = false;

            // Call to Master.Run providing required parameters, but also the --help parameter.
            var args = new[] { "--help", "--maxParallelEvaluations=1" };
            Master<NoOperation, TestInstance, TestResult>.Run(
                args,
                (config, trainingInstancePath, testInstancePath) =>
                    {
                        // Remember if run starts.
                        runIsStarted = true;
                        return null;
                    });

            // Wait for a while.
            Thread.Sleep(millisecondsTimeout: 500);

            // Check run was not started.
            Assert.False(runIsStarted, "Run started even though --help was provided.");
        }

        /// <summary>
        /// Checks that a call to
        /// <see cref="Master{TTargetAlgorithm, TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.Run"/>
        /// does not start
        /// execution if parameters are invalid (e.g. not all required parameters are provided).
        /// </summary>
        [Fact]
        public void RunDoesNotStartOnInvalidParameters()
        {
            // Prepare variable to find out whether run is started.
            bool runIsStarted = false;

            // Call to Master.Run providing no parameters.
            var args = new string[0];
            Master<NoOperation, TestInstance, TestResult>.Run(
                args,
                (config, trainingInstancePath, testInstancePath) =>
                    {
                        // Remember if run starts.
                        runIsStarted = true;
                        return null;
                    });

            // Wait for a while.
            Thread.Sleep(millisecondsTimeout: 500);

            // Check run was not started.
            Assert.False(runIsStarted, "Run started even though no parameters have been provided.");
        }

        /// <summary>
        /// Checks that parameters specified alongside "--continue" are combined with the ones from the original
        /// configuration read from status.
        /// </summary>
        [Fact]
        public void OriginalConfigurationGetsOverwrittenWithNewParametersOnContinue()
        {
            // Specify parameters for original configuration
            var originalConfig =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetEnableRacing(false)
                    .SetPopulationSize(8)
                    .SetGenerations(3)
                    .SetMaxGenomeAge(2)
                    .SetMaximumMiniTournamentSize(7)
                    .SetTournamentWinnerPercentage(0.34)
                    .SetCpuTimeout(TimeSpan.FromMilliseconds(30000))
                    .SetCrossoverSwitchProbability(0.05)
                    .SetMutationRate(0.2)
                    .SetMutationVariancePercentage(0.3)
                    .SetMaxRepairAttempts(11)
                    .SetMaximumNumberConsecutiveFailuresPerEvaluation(4)
                    .SetInstanceNumbers(1, 1)
                    .SetGoalGeneration(2)
                    .SetAkkaConfiguration(Config.Empty)
                    .SetVerbosity(VerbosityLevel.Debug)
                    .SetStatusFileDirectory(Path.GetDirectoryName(this._alternativeStatusFilePath))
                    .SetLogFilePath(this._alternativeLogFilePath)
                    .SetTrainModel(true)
                    .SetEngineeredProportion(0.21)
                    .SetTopPerformerThreshold(0.22)
                    .SetStartEngineeringAtIteration(1)
                    .SetPopulationMutantRatio(0.23)
                    .SetEnableSexualSelection(true)
                    .SetStrictCompatibilityCheck(false)
                    .SetCrossoverProbabilityCompetitive(0.24)
                    .SetHammingDistanceRelativeThreshold(0.25)
                    .SetTargetSampleSize(1)
                    .SetMaxRanksCompensatedByDistance(23)
                    .SetFeatureSubsetRatioForDistance(0.26)
                    .SetTrackConvergenceBehavior(true)
                    .SetDistanceMetric(DistanceMetric.L1Average.ToString())
                    .SetMaximumNumberParallelEvaluations(2)
                    .SetMaximumNumberParallelThreads(5)
                    .AddDetailedConfigurationBuilder(
                        RegressionForestArgumentParser.Identifier,
                        new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                    .Build();
            this.WriteConfigurationToStatusFile(originalConfig, this._alternativeStatusFilePath);

            // Call run with continue option and some additional parameters.
            var args = new[]
                           {
                               // required parameters
                               "--continue",
                               $"--statusFileDir={originalConfig.StatusFileDirectory}",
                               // new parameter value
                               "--faultTolerance=750",
                               // equal to original
                               $"--maxRepair={originalConfig.MaxRepairAttempts}",
                               // default parameter value
                               $"--logFile={PathUtils.GetAbsolutePathFromCurrentDirectory("tunerLog.txt")}",
                           };
            Master<NoOperation, TestInstance, TestResult>.Run(
                args,
                (config, pathToTrainingInstances, pathToTestInstances) =>
                    {
                        // Check new parameters get used.
                        Assert.Equal(
                            750,
                            config.MaximumNumberConsecutiveFailuresPerEvaluation);
                        Assert.Equal(
                            originalConfig.MaxRepairAttempts,
                            config.MaxRepairAttempts);
                        Assert.Equal(
                            PathUtils.GetAbsolutePathFromCurrentDirectory("tunerLog.txt"),
                            config.LogFilePath);

                        // Check original paramters are still in use where they did not get overwritten.
                        Assert.Equal(
                            originalConfig.EnableRacing,
                            config.EnableRacing);
                        Assert.Equal(
                            originalConfig.PopulationSize,
                            config.PopulationSize);
                        Assert.Equal(
                            originalConfig.Generations,
                            config.Generations);
                        Assert.Equal(
                            originalConfig.MaxGenomeAge,
                            config.MaxGenomeAge);
                        Assert.Equal(
                            originalConfig.MaximumMiniTournamentSize,
                            config.MaximumMiniTournamentSize);
                        Assert.Equal(
                            originalConfig.TournamentWinnerPercentage,
                            config.TournamentWinnerPercentage);
                        Assert.Equal(
                            originalConfig.CpuTimeout,
                            config.CpuTimeout);
                        Assert.Equal(
                            originalConfig.CrossoverSwitchProbability,
                            config.CrossoverSwitchProbability);
                        Assert.Equal(
                            originalConfig.MutationRate,
                            config.MutationRate);
                        Assert.Equal(
                            originalConfig.MutationVariancePercentage,
                            config.MutationVariancePercentage);
                        Assert.Equal(
                            originalConfig.StartNumInstances,
                            config.StartNumInstances);
                        Assert.Equal(
                            originalConfig.EndNumInstances,
                            config.EndNumInstances);
                        Assert.Equal(
                            originalConfig.GoalGeneration,
                            config.GoalGeneration);
                        Assert.Equal(
                            originalConfig.Verbosity,
                            config.Verbosity);
                        Assert.Equal(
                            originalConfig.StatusFileDirectory,
                            config.StatusFileDirectory);
                        Assert.Equal(
                            originalConfig.TrainModel,
                            config.TrainModel);
                        Assert.Equal(
                            originalConfig.TopPerformerThreshold,
                            config.TopPerformerThreshold);
                        Assert.Equal(
                            originalConfig.StartEngineeringAtIteration,
                            config.StartEngineeringAtIteration);
                        Assert.Equal(
                            originalConfig.PopulationMutantRatio,
                            config.PopulationMutantRatio);
                        Assert.Equal(
                            originalConfig.EnableSexualSelection,
                            config.EnableSexualSelection);
                        Assert.Equal(
                            originalConfig.StrictCompatibilityCheck,
                            config.StrictCompatibilityCheck);
                        Assert.Equal(
                            originalConfig.CrossoverProbabilityCompetitive,
                            config.CrossoverProbabilityCompetitive);
                        Assert.Equal(
                            originalConfig.HammingDistanceRelativeThreshold,
                            config.HammingDistanceRelativeThreshold);
                        Assert.Equal(
                            originalConfig.TargetSamplingSize,
                            config.TargetSamplingSize);
                        Assert.Equal(
                            originalConfig.MaxRanksCompensatedByDistance,
                            config.MaxRanksCompensatedByDistance);
                        Assert.Equal(
                            originalConfig.FeatureSubsetRatioForDistanceComputation,
                            config.FeatureSubsetRatioForDistanceComputation);
                        Assert.Equal(
                            originalConfig.TrackConvergenceBehavior,
                            config.TrackConvergenceBehavior);
                        Assert.Equal(
                            originalConfig.DistanceMetric,
                            config.DistanceMetric);
                        Assert.Equal(
                            originalConfig.MaximumNumberParallelEvaluations,
                            config.MaximumNumberParallelEvaluations);
                        Assert.Equal(
                            originalConfig.MaximumNumberParallelThreads,
                            config.MaximumNumberParallelThreads);
                        Assert.Equal(
                            originalConfig.EngineeredPopulationRatio,
                            config.EngineeredPopulationRatio);

                        // Return an algorithm tuner that quickly terminates.
                        return this.BuildSimpleAlgorithmTuner(config, pathToTrainingInstances, pathToTestInstances);
                    });
        }

        /// <summary>
        /// Checks that an existing status file does not get read if the --continue parameter is not set.
        /// </summary>
        [Fact]
        public void StatusFileGetsIgnoredWithoutContinueParameter()
        {
            // Make sure a status file specifying a certain number of generations exist.
            var configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetGenerations(99)
                    .AddDetailedConfigurationBuilder(
                        RegressionForestArgumentParser.Identifier,
                        new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                    .Build(maximumNumberParallelEvaluations: 1);
            this.WriteConfigurationToStatusFile(configuration, this._statusFilePath);

            // Make sure it does not get used:
            string[] args =
                {
                    "--maxParallelEvaluations=1",
                    "--verbose=0",
                    "--numGens=1",
                    "--goalGen=0",
                    "--instanceNumbers=1:1",
                    "--engineeredProportion=0.0",
                };
            Master<NoOperation, TestInstance, TestResult>.Run(
                args,
                (config, pathToTrainingInstances, pathToTestInstances) =>
                    {
                        // Check the number of generations is different.
                        Assert.True(
                            configuration.Generations != config.Generations,
                            "Generation number should not be taken from status file without --continue.");

                        // Return an algorithm tuner quickly terminates.
                        return this.BuildSimpleAlgorithmTuner(config, pathToTrainingInstances, pathToTestInstances);
                    });
        }

        /// <summary>
        /// Checks that if using --continue, Akka configuration parameters are always taken from the newly specified
        /// parameters.
        /// </summary>
        [Fact]
        public void AkkaConfigurationIsTakenFromNewParametersOnContinue()
        {
            // Specify parameters for simple original configuration
            var originalConfig =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetMaximumNumberParallelEvaluations(2)
                    .SetGenerations(1)
                    .SetGoalGeneration(0)
                    .SetInstanceNumbers(1, 1)
                    .SetEngineeredProportion(0)
                    .AddDetailedConfigurationBuilder(
                        RegressionForestArgumentParser.Identifier,
                        new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                    .Build();
            this.WriteConfigurationToStatusFile(originalConfig, this._statusFilePath);

            // Call run with continue option and different Akka configuraiton.
            var args = new[]
                           {
                               "--continue",
                               // Akka config parameters
                               "--port=1234",
                               "--ownHostName=foo",
                               "--maxParallelEvaluations=1",
                           };
            Master<NoOperation, TestInstance, TestResult>.Run(
                args,
                (config, pathToTrainingInstances, pathToTestInstances) =>
                    {
                        // Check new parameters get used for Akka config.
                        Assert.Equal(
                            1234,
                            config.AkkaConfiguration.GetInt("akka.remote.dot-netty.tcp.port"));
                        Assert.Equal(
                            "foo",
                            config.AkkaConfiguration.GetString("akka.remote.dot-netty.tcp.hostname"));

                        // Return an algorithm tuner that quickly terminates.
                        return this.BuildSimpleAlgorithmTuner(originalConfig, pathToTrainingInstances, pathToTestInstances);
                    });
        }

        /// <summary>
        /// Checks that if using --continue, the instance folder parameter is always taken from the newly specified
        /// parameters.
        /// </summary>
        [Fact]
        public void InstanceFolderIsTakenFromNewParametersOnContinue()
        {
            // Specify parameters for simple original configuration
            var originalConfig =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetMaximumNumberParallelEvaluations(2)
                    .SetGenerations(1)
                    .SetGoalGeneration(0)
                    .SetInstanceNumbers(1, 1)
                    .SetEngineeredProportion(0)
                    .AddDetailedConfigurationBuilder(
                        RegressionForestArgumentParser.Identifier,
                        new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                    .Build();
            this.WriteConfigurationToStatusFile(originalConfig, this._statusFilePath);

            // Call run with continue option and different instance folder.
            var args = new[]
                           {
                               "--continue",
                               // Instance folder parameters
                               "--trainingInstanceFolder=foo",
                               "--testInstanceFolder=bar",
                           };
            Master<NoOperation, TestInstance, TestResult>.Run(
                args,
                (config, pathToTrainingInstances, pathToTestInstances) =>
                    {
                        // Check new parameter get used for instance folders.
                        Assert.Equal(
                            "foo",
                            pathToTrainingInstances);
                        Assert.Equal(
                            "bar",
                            pathToTestInstances);

                        // Return an algorithm tuner that quickly terminates.
                        return this.BuildSimpleAlgorithmTuner(originalConfig, pathToTrainingInstances, pathToTestInstances);
                    });
        }

        /// <summary>
        /// Checks that
        /// <see cref="Master{TTargetAlgorithm, TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.Run"/>
        /// prints the best parameters to console at the end of execution.
        /// </summary>
        [Fact]
        public void MasterPrintsEndResultToConsole()
        {
            TestUtils.CheckOutput(
                action: () =>
                    {
                        // Call to Master.Run with parameters for a quick execution and minimal logging.
                        var args = new[]
                                       {
                                           "--maxParallelEvaluations=1",
                                           "--verbose=1",
                                           "--numGens=1",
                                           "--goalGen=0",
                                           "--instanceNumbers=1:1",
                                           "--engineeredProportion=0.0",
                                       };
                        Master<NoOperation, TestInstance, TestResult>.Run(args, this.BuildSimpleAlgorithmTuner);
                    },
                check: consoleOutput =>
                    {
                        // Check the console output.
                        using (var reader = new StringReader(consoleOutput.ToString()))
                        {
                            // It should contain the best parameter as its last line before shutting down.
                            var output = reader.ReadToEnd();
                            var lastLine = output
                                .Split('\r', '\n')
                                .SkipWhile(s => !s.Contains("Best Configuration"))
                                .Last(s => s != "");
                            Assert.True(
                                lastLine.Contains("a: 1"),
                                $"Best parameters are not being printed at the end of execution. Last relevant line is {lastLine}, complete output {output}.");
                        }
                    });
        }

        /// <summary>
        /// Checks that
        /// <see cref="Master{TTargetAlgorithm, TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.Run"/>
        /// configures the logger correctly.
        /// </summary>
        [Fact]
        public void MasterSetsCorrectLoggingLevel()
        {
            var quickExecutionArgs = new[]
                                         {
                                             "--maxParallelEvaluations=1",
                                             "--numGens=1",
                                             "--goalGen=0",
                                             "--instanceNumbers=1:1",
                                             "--engineeredProportion=0.0",
                                         };

            // Check what happens for verbosity 'Info'.
            TestUtils.CheckOutput(
                action: () =>
                    {
                        var args = new string[quickExecutionArgs.Length + 1];
                        Array.Copy(quickExecutionArgs, args, quickExecutionArgs.Length);
                        args[args.Length - 1] = "--verbose=1";
                        Master<NoOperation, TestInstance, TestResult>.Run(args, this.BuildSimpleAlgorithmTuner);
                    },
                check: consoleOutput =>
                    {
                        using (var reader = new StringReader(consoleOutput.ToString()))
                        {
                            var output = reader.ReadToEnd();
                            Assert.True(
                                output.Contains("[Info]"),
                                $"Output should contain general information, but doesn't. It is: {output}.");
                            Assert.False(
                                output.Contains("[Debug]"),
                                $"Output should not contain debug information, but does. It is: {output}.");
                        }
                    });

            // Then check what happens for another verbosity - 'Debug'.
            Randomizer.Reset();
            TestUtils.CheckOutput(
                action: () =>
                    {
                        var args = new string[quickExecutionArgs.Length + 2];
                        Array.Copy(quickExecutionArgs, args, quickExecutionArgs.Length);
                        args[args.Length - 2] = "--port=12345";
                        args[args.Length - 1] = "--verbose=2";
                        Master<NoOperation, TestInstance, TestResult>.Run(args, this.BuildSimpleAlgorithmTuner);
                    },
                check: consoleOutput =>
                    {
                        using (var reader = new StringReader(consoleOutput.ToString()))
                        {
                            var output = reader.ReadToEnd();
                            Assert.True(
                                output.Contains("[Debug]"),
                                $"Output should contain debug information, but doesn't. It is: {output}.");
                            Assert.False(
                                output.Contains("[Trace]"),
                                $"Output should not contain trace information, but does. It is: {output}.");
                        }
                    });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Deletes the status files if they were written.
        /// </summary>
        protected override void CleanupDefault()
        {
            if (Directory.Exists(Path.GetDirectoryName(this._alternativeStatusFilePath)))
            {
                Directory.Delete(Path.GetDirectoryName(this._alternativeStatusFilePath), recursive: true);
            }

            if (File.Exists(this._alternativeLogFilePath))
            {
                File.Delete(this._alternativeLogFilePath);
            }

            foreach (var fileName in Directory.EnumerateFiles(PathUtils.GetAbsolutePathFromCurrentDirectory(""), "*.*", SearchOption.TopDirectoryOnly)
                .Where(
                    f =>
                        (f.EndsWith(".csv", StringComparison.InvariantCultureIgnoreCase)
                         || f.EndsWith(".log", StringComparison.InvariantCultureIgnoreCase)
                         || f.EndsWith(".txt", StringComparison.InvariantCultureIgnoreCase))))
            {
                try
                {
                    File.Delete(fileName);
                }
                catch
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Initialization method called before each test.
        /// </summary>
        protected override void InitializeDefault()
        {
            this.ParameterTree = new ParameterTree(new ValueNode<int>("a", new IntegerDomain(1, 1)));
            this.DummyGeneticEngineering =
                new GeneticEngineering<StandardRandomForestLearner<ReuseOldTreesStrategy>, GenomePredictionForestModel<GenomePredictionTree>,
                    ReuseOldTreesStrategy>(
                    this.ParameterTree,
                    this.GetDefaultAlgorithmTunerConfiguration());
            this._pathToGgaStatusFile = Path.Combine(Path.GetDirectoryName(this._statusFilePath), GgaStatus.FileName);
            Randomizer.Reset();

            Directory.CreateDirectory(Path.GetDirectoryName(this._alternativeStatusFilePath));
        }

        /// <summary>
        /// Builds a simple <see cref="AlgorithmTuner{NoOperation, TestInstance, TestResult}"/> instance with a single parameter
        /// a which is always 1.
        /// </summary>
        /// <param name="config">The configuratoin to use.</param>
        /// <param name="trainingInstancePath">Path to training instance folder.</param>
        /// <param name="testInstancePath">Path to test instance folder.</param>
        /// <returns>The build <see cref="AlgorithmTuner{NoOperation, TestInstance, TestResult}"/> instance.</returns>
        private AlgorithmTuner<NoOperation, TestInstance, TestResult> BuildSimpleAlgorithmTuner(
            AlgorithmTunerConfiguration config,
            string trainingInstancePath,
            string testInstancePath)
        {
            return new AlgorithmTuner<NoOperation, TestInstance, TestResult>(
                new TargetAlgorithmFactory<NoOperation, TestInstance, TestResult>(
                    () => new NoOperation()),
                new KeepSuggestedOrder<TestInstance, TestResult>(),
                new[] { new TestInstance("train") },
                this.ParameterTree,
                config);
        }

        /// <summary>
        /// Writes the provided <see cref="AlgorithmTunerConfiguration"/> into a status file stored at the provided path.
        /// The generation will be set to 0 and the population and run results will be empty.
        /// </summary>
        /// <param name="config">The <see cref="AlgorithmTunerConfiguration"/> to write to the status file.</param>
        /// <param name="pathToStatusFile">Path to which the status file should be written.</param>
        private void WriteConfigurationToStatusFile(AlgorithmTunerConfiguration config, string pathToStatusFile)
        {
            // Create status objects with correct configuration.
            var status =
                new Status<TestInstance, TestResult, StandardRandomForestLearner<ReuseOldTreesStrategy>,
                    GenomePredictionForestModel<GenomePredictionTree>, ReuseOldTreesStrategy>(
                    generation: 0,
                    population: new Population(config),
                    configuration: config,
                    geneticEngineering: this.DummyGeneticEngineering,
                    currentUpdateStrategyIndex: 0,
                    // dummy data...
                    incumbentQuality: new List<double>(),
                    incumbentGenomeWrapper: new IncumbentGenomeWrapper<TestResult>()
                                                {
                                                    IncumbentGenome = new Genome(),
                                                    IncumbentGeneration = 0,
                                                    IncumbentInstanceResults = new List<TestResult>().ToImmutableList(),
                                                },
                    informationHistory: new List<GenerationInformation>(0),
                    elapsedTime: TimeSpan.Zero);
            status.SetRunResults(
                new Dictionary<ImmutableGenome, ImmutableDictionary<TestInstance, TestResult>>().ToImmutableDictionary());
            var ggaStatus = new GgaStatus(new Population(config), 0, 0, new Dictionary<Genome, List<GenomeTournamentRank>>());

            // Write them to file.
            status.WriteToFile(pathToStatusFile);
            this._pathToGgaStatusFile = Path.Combine(Path.GetDirectoryName(pathToStatusFile), GgaStatus.FileName);
            ggaStatus.WriteToFile(this._pathToGgaStatusFile);
        }

        #endregion
    }
}