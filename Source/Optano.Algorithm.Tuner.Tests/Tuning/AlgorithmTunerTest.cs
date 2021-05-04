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

namespace Optano.Algorithm.Tuner.Tests.Tuning
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using Moq;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.GrayBox;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.MachineLearning.Prediction;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData.SamplingStrategies;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Local;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.GeneticAlgorithm;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.InstanceValueConsideration;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;
    using Optano.Algorithm.Tuner.Tracking;
    using Optano.Algorithm.Tuner.Tuning;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/>.
    /// </summary>
    public class AlgorithmTunerTest : TestBase
    {
        #region Static Fields

        /// <summary>
        /// An <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/> which creates an <see cref="ITargetAlgorithm{TestInstance, TestResult}"/>
        /// that takes empty instances and returns empty results.
        /// </summary>
        private static readonly TargetAlgorithmFactory<NoOperation, TestInstance, TestResult> NoopFactory =
            new TargetAlgorithmFactory<NoOperation, TestInstance, TestResult>(
                targetAlgorithmCreator: () => new NoOperation());

        /// <summary>
        /// A simple parameter tree that represents a single integer value.
        /// </summary>
        private static readonly ParameterTree SimpleParameterTree =
            new ParameterTree(new ValueNode<int>(ExtractIntegerValue.ParameterName, new IntegerDomain()));

        #endregion

        #region Fields

        /// <summary>
        /// Path to status file that gets written in some tests.
        /// </summary>
        private readonly string _statusFilePath =
            PathUtils.GetAbsolutePathFromExecutableFolderRelative($"status{Path.DirectorySeparatorChar}status.oatstat");

        /// <summary>
        /// Path to log file that gets written in some tests.
        /// </summary>
        private readonly string _logFilePath = PathUtils.GetAbsolutePathFromCurrentDirectory("tunerLog.txt");

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that initializing an instance of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> class without a target algorithm
        /// factory throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void MissingTargetAlgorithmFactoryThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AlgorithmTunerBuilder().SetTargetAlgorithmFactory(null).ExecuteAlgorithmTunerConstructor());
        }

        /// <summary>
        /// Verifies that initializing an instance of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> class without a run evaluator.
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void MissingRunEvaluatorThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AlgorithmTunerBuilder().SetRunEvaluator(null).ExecuteAlgorithmTunerConstructor());
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> class without a set of training instances
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void MissingTrainingInstancesThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AlgorithmTunerBuilder().SetTrainingInstances(null).ExecuteAlgorithmTunerConstructor());
        }

        /// <summary>
        /// Verifies that initializing an instance of the
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> class without a set of test instances
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void MissingTestInstancesThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AlgorithmTunerBuilder().SetTestInstances(null).ExecuteAlgorithmTunerConstructor());
        }

        /// <summary>
        /// Verifies that initializing an instance of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> class without a parameter tree
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void MissingParameterTreeThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AlgorithmTunerBuilder().SetParameterTree(null).ExecuteAlgorithmTunerConstructor());
        }

        /// <summary>
        /// Verifies that initializing an instance of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> class without a configuration
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void MissingConfigurationThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AlgorithmTunerBuilder().SetConfiguration(null).ExecuteAlgorithmTunerConstructor());
        }

        /// <summary>
        /// Verifies that initializing an instance of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> class with a genome builder set to
        /// null throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void MissingGenomeBuilderThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AlgorithmTunerBuilder()
                    .SetGenomeBuilder(null)
                    .ExecuteAlgorithmTunerConstructor(specifyGenomeBuilder: true));
        }

        /// <summary>
        /// Verifies that initializing an instance of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> class with a smaller number of
        /// training instances than the maximum number given in the <see cref="AlgorithmTunerConfiguration"/> throws an
        /// <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void NotEnoughTrainingInstancesThrowsException()
        {
            // Create configuration that needs at least 40 instances...
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetInstanceNumbers(startNumInstances: 1, endNumInstances: 40)
                .Build(maximumNumberParallelEvaluations: 1);
            // ...then provide 39.
            Assert.Throws<ArgumentException>(
                () => new AlgorithmTunerBuilder()
                    .SetConfiguration(configuration)
                    .SetTrainingInstances(AlgorithmTunerTest.BuildEmptyInstances(number: 39))
                    .ExecuteAlgorithmTunerConstructor());
        }

        /// <summary>
        /// Verifies that initializing an instance of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> class with a parameter tree that does
        /// not contain any parameters throws an <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void ParameterTreeWithoutParametersThrowsException()
        {
            // Build up a parameter tree only consisting of an and node...
            var parameterLessTree = new ParameterTree(new AndNode());
            // ...and provide it to the constructor.
            Assert.Throws<ArgumentException>(
                () => new AlgorithmTunerBuilder().SetParameterTree(parameterLessTree).ExecuteAlgorithmTunerConstructor());
        }

        /// <summary>
        /// Verifies that initializing an instance of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> class with a parameter tree that
        /// contains duplicate identifiers throws an <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void ParameterTreeWithDuplicatedIdentifiersThrowsException()
        {
            // Create tree consisting of two nodes with the same identifier.
            var firstNode = new ValueNode<int>("a", new IntegerDomain());
            var secondNode = new ValueNode<int>("a", new IntegerDomain());
            firstNode.SetChild(secondNode);
            var duplicateParametersTree = new ParameterTree(firstNode);

            // Try to call algorithm tuner constructor.
            Assert.Throws<ArgumentException>(
                () => new AlgorithmTunerBuilder().SetParameterTree(duplicateParametersTree).ExecuteAlgorithmTunerConstructor());
        }

        /// <summary>
        /// Checks that enabling data recording without providing a gray box target algorithm throws an <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void DataRecordingWithoutGrayBoxTargetAlgorithmThrowsException()
        {
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .SetEnableDataRecording(true)
                .Build(maximumNumberParallelEvaluations: 1);

            Assert.Throws<ArgumentException>(
                () => new AlgorithmTuner<NoOperation, TestInstance, TestResult>(
                    new Mock<ITargetAlgorithmFactory<NoOperation, TestInstance, TestResult>>().Object,
                    new Mock<IRunEvaluator<TestInstance, TestResult>>().Object,
                    AlgorithmTunerTest.BuildEmptyInstances(number: 100),
                    AlgorithmTunerTest.SimpleParameterTree,
                    configuration,
                    null));
        }

        /// <summary>
        /// Checks that enabling data recording while providing a gray box target algorithm throws no <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void DataRecordingWithGrayBoxTargetAlgorithmThrowsNoException()
        {
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .SetEnableDataRecording(true)
                .Build(maximumNumberParallelEvaluations: 1);

            var tuner = new AlgorithmTuner<GrayBoxNoOperation, TestInstance, TestResult>(
                new Mock<ITargetAlgorithmFactory<GrayBoxNoOperation, TestInstance, TestResult>>().Object,
                new Mock<IRunEvaluator<TestInstance, TestResult>>().Object,
                AlgorithmTunerTest.BuildEmptyInstances(number: 100),
                AlgorithmTunerTest.SimpleParameterTree,
                configuration,
                null);

            Assert.NotNull(tuner);
            tuner.Dispose();
        }

        /// <summary>
        /// Checks that enabling gray box tuning without providing custom gray box methods throws an <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void GrayBoxTuningWithoutCustomGrayBoxMethodsThrowsException()
        {
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .SetEnableDataRecording(true)
                .SetEnableGrayBox(true)
                .Build(maximumNumberParallelEvaluations: 1);

            Assert.Throws<ArgumentException>(
                () => new AlgorithmTuner<GrayBoxNoOperation, TestInstance, TestResult>(
                    new Mock<ITargetAlgorithmFactory<GrayBoxNoOperation, TestInstance, TestResult>>().Object,
                    new Mock<IRunEvaluator<TestInstance, TestResult>>().Object,
                    AlgorithmTunerTest.BuildEmptyInstances(number: 100),
                    AlgorithmTunerTest.SimpleParameterTree,
                    configuration,
                    null));
        }

        /// <summary>
        /// Checks that enabling gray box tuning while providing custom gray box methods throws no <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void GrayBoxTuningWithCustomGrayBoxMethodsThrowsNoException()
        {
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .SetEnableDataRecording(true)
                .SetEnableGrayBox(true)
                .Build(maximumNumberParallelEvaluations: 1);

            var tuner = new AlgorithmTuner<GrayBoxNoOperation, TestInstance, TestResult>(
                new Mock<ITargetAlgorithmFactory<GrayBoxNoOperation, TestInstance, TestResult>>().Object,
                new Mock<IRunEvaluator<TestInstance, TestResult>>().Object,
                AlgorithmTunerTest.BuildEmptyInstances(number: 100),
                AlgorithmTunerTest.SimpleParameterTree,
                configuration,
                new Mock<ICustomGrayBoxMethods<TestResult>>().Object);

            Assert.NotNull(tuner);
            tuner.Dispose();
        }

        /// <summary>
        /// Checks that
        /// <see cref="AlgorithmTuner{TTargetAlgorithm, TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}"/>'s
        /// constructor produces an object without throwing an exception
        /// when valid parameters are provided.
        /// </summary>
        [Fact]
        public void ValidAlgorithmTunerObjectCanBeBuild()
        {
            var tuner = new AlgorithmTunerBuilder().ExecuteAlgorithmTunerConstructor();
            Assert.NotNull(tuner);
            tuner.Dispose();
        }

        /// <summary>
        /// Checks that the
        /// <see cref="AlgorithmTuner{TTargetAlgorithm, TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}.Run"/>
        /// command works in a way that it actually returns a result.
        /// </summary>
        [Fact(Timeout = 10000)]
        public void AlgorithmTunerRunReturnsResult()
        {
            // Build up an algorithm tuner instance with few generations.
            var tuner = AlgorithmTunerTest.CreateSmallAlgorithmTuner(generations: 2);

            // Test if a run results in a result.
            Dictionary<string, IAllele> result = tuner.Run();
            Assert.NotNull(result);
            tuner.Dispose();
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.EvaluationLimit"/> can cut a run short.
        /// </summary>
        [Fact]
        public void EvaluationLimitForcesTermination()
        {
            // Set evaluation limit to half population size + a third of that
            // --> should be reached after two generations if instance numbers stay 1 throughout the tuning.
            var config = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEvaluationLimit(64 + 21)
                .SetInstanceNumbers(1, 1)
                .SetEngineeredProportion(0)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .Build(maximumNumberParallelEvaluations: 1);
            var tuner = new AlgorithmTunerBuilder()
                .SetConfiguration(config)
                .ExecuteAlgorithmTunerConstructor();

            tuner.Run();
            tuner.Dispose();
            var status = this.ReadAlgorithmTunerStatusFile();
            Assert.Equal(2, status.Generation);
        }

        /// <summary>
        /// Checks that <see cref="AlgorithmTunerConfiguration.EvaluationLimit"/> is observed when using a status dump
        /// which has already exceeded the limit.
        /// </summary>
        [Fact]
        public void EvaluationLimitIsRespectedWhenUsingStatusDump()
        {
            // Set evaluation limit to half population size + a third of that
            // --> should be reached after two generations if instance numbers stay 1 throughout the tuning.
            var config = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEvaluationLimit(64 + 21)
                .SetInstanceNumbers(1, 1)
                .SetEngineeredProportion(0)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .Build(maximumNumberParallelEvaluations: 1);
            var tuner = new AlgorithmTunerBuilder()
                .SetConfiguration(config)
                .ExecuteAlgorithmTunerConstructor();

            // Complete run.
            tuner.Run();
            tuner.Dispose();

            // Start a new run based on that.
            var subsequentTunerRun = new AlgorithmTunerBuilder()
                .SetConfiguration(config)
                .ExecuteAlgorithmTunerConstructor();
            subsequentTunerRun.UseStatusDump(this._statusFilePath);
            subsequentTunerRun.Run();
            subsequentTunerRun.Dispose();

            // Check it was terminated correctly.
            var status = StatusBase
                .ReadFromFile<Status<TestInstance, TestResult, GenomePredictionRandomForest<ReuseOldTreesStrategy>,
                    GenomePredictionForestModel<GenomePredictionTree>, ReuseOldTreesStrategy>>(this._statusFilePath);
            Assert.Equal(2, status.Generation);
        }

        /// <summary>
        /// Checks that
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}"/>
        /// switches between update strategies when they terminate.
        /// </summary>
        [Fact]
        public void PopulationUpdateStrategiesAreSwappedWhenTerminated()
        {
            var strategySwitchConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.Jade)
                .AddDetailedConfigurationBuilder(
                    DifferentialEvolutionStrategyArgumentParser.Identifier,
                    new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                        .SetDifferentialEvolutionConfigurationBuilder(
                            new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder())
                        .SetMaximumNumberGenerations(3))
                .SetMaximumNumberGgaGenerations(2)
                .SetGenerations(2)
                .SetInstanceNumbers(1, 1)
                .SetGoalGeneration(1)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .Build(maximumNumberParallelEvaluations: 1);
            var runner = new AlgorithmTunerBuilder()
                .SetConfiguration(strategySwitchConfig)
                .ExecuteAlgorithmTunerConstructor();

            runner.Run();
            runner.Dispose();
            Assert.Equal(
                0,
                this.ReadAlgorithmTunerStatusFile().CurrentUpdateStrategyIndex);

            var continuedRunner = new AlgorithmTunerBuilder()
                .SetConfiguration(
                    new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                        .SetStrictCompatibilityCheck(false)
                        .SetGenerations(5)
                        .BuildWithFallback(strategySwitchConfig))
                .ExecuteAlgorithmTunerConstructor();
            continuedRunner.UseStatusDump(this._statusFilePath);
            continuedRunner.Run();
            continuedRunner.Dispose();

            Assert.Equal(
                1,
                this.ReadAlgorithmTunerStatusFile().CurrentUpdateStrategyIndex);

            var lastRunner = new AlgorithmTunerBuilder()
                .SetConfiguration(
                    new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                        .SetStrictCompatibilityCheck(false)
                        .SetGenerations(8)
                        .BuildWithFallback(strategySwitchConfig))
                .ExecuteAlgorithmTunerConstructor();
            lastRunner.UseStatusDump(this._statusFilePath);
            lastRunner.Run();
            lastRunner.Dispose();

            Assert.Equal(
                0,
                this.ReadAlgorithmTunerStatusFile().CurrentUpdateStrategyIndex);
        }

        /// <summary>
        /// Checks that
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}"/>
        /// does not switch to strategies which are already terminated.
        /// </summary>
        [Fact]
        public void TerminatedPopulationUpdateStrategiesAreNotSelected()
        {
            // Create a configuration where DE phases are 1 generation long and GGA phases 0 => should always use DE.
            var zeroGenerationDeConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.Jade)
                .AddDetailedConfigurationBuilder(
                    DifferentialEvolutionStrategyArgumentParser.Identifier,
                    new DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder()
                        .SetDifferentialEvolutionConfigurationBuilder(
                            new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder())
                        .SetMaximumNumberGenerations(1))
                .SetMaximumNumberGgaGenerations(0)
                .SetGenerations(3)
                .SetInstanceNumbers(1, 1)
                .SetGoalGeneration(1)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .Build(maximumNumberParallelEvaluations: 1);
            var runner = new AlgorithmTunerBuilder()
                .SetConfiguration(zeroGenerationDeConfig)
                .ExecuteAlgorithmTunerConstructor();

            runner.Run();
            runner.Dispose();
            Assert.Equal(
                1,
                this.ReadAlgorithmTunerStatusFile().CurrentUpdateStrategyIndex);
        }

        /// <summary>
        /// Checks that
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}"/>
        /// updates its population, at least when strategies terminate.
        /// </summary>
        [Fact]
        public void RunnerUpdatesBasePopulationOnStrategyTermination()
        {
            int generations = 5;
            var runner = AlgorithmTunerTest.CreateSmallAlgorithmTuner(generations);
            runner.Run();
            runner.Dispose();
            var firstStatus = this.ReadAlgorithmTunerStatusFile();

            // Add one additional generation because the last one is not dumped.
            var continuedRunner = AlgorithmTunerTest.CreateSmallAlgorithmTuner(
                (2 * generations) + 1,
                allowConfigChangeForSubsequentRun: true);
            continuedRunner.UseStatusDump(this._statusFilePath);
            continuedRunner.Run();
            continuedRunner.Dispose();
            var secondStatus = this.ReadAlgorithmTunerStatusFile();

            Assert.NotEqual(
                firstStatus.Population.AllGenomes.Select(genome => genome.ToString()).OrderBy(x => x).ToArray(),
                secondStatus.Population.AllGenomes.Select(genome => genome.ToString()).OrderBy(x => x).ToArray());
        }

        /// <summary>
        /// Checks that a log file is written after every generation.
        /// This test only works on a Windows machine, since there are problems with FileSystemWatcher on other OS.
        /// </summary>
        [SkippableFact]
        public void LogIsWrittenAfterEveryGeneration()
        {
            // Check, if current OS is Windows.
            Skip.IfNot(Environment.OSVersion.Platform == PlatformID.Win32NT);

            // Build up an algorithm tuner instance with few generations.
            var tuner = AlgorithmTunerTest.CreateSmallAlgorithmTuner(generations: 2);

            // Add watcher to find out when log files are written.
            var writtenFiles = new List<ICollection<string>>(2);
            using (var watcher = new FileSystemWatcher(PathUtils.GetAbsolutePathFromCurrentDirectory(string.Empty)))
            {
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.EnableRaisingEvents = true;
                watcher.Renamed += (sender, e) =>
                    {
                        if (!e.FullPath.Contains("tunerLog.txt"))
                        {
                            return;
                        }

                        // When file was renamed, wait a while to make sure it is not opened anymore.
                        Thread.Sleep(millisecondsTimeout: 10);
                        writtenFiles.Add(File.ReadLines(this._logFilePath).ToList());
                    };

                // Wait a while to ensure the first file rename is caught
                Task.Delay(100).Wait();

                // Run the algorithm tuner.
                tuner.Run();
                tuner.Dispose();
            }

            // Wait a while to catch the latest file rename
            Task.Delay(100).Wait();

            // Find the generations the files were written for.
            var generationsWithLog = new List<int>(2);
            foreach (var fileContent in writtenFiles)
            {
                var match = Regex.Match(fileContent.First(), @"Finished generation (\d+) / \d+");
                generationsWithLog.Add(int.Parse(match.Groups[1].Value));
            }

            // Check a log file was written for each generation.
            Assert.True(
                generationsWithLog.SequenceEqual(new[] { 1, 2 }),
                $"Expected log to be written to file for generations 1 and 2, but was written for {TestUtils.PrintList(generationsWithLog)}.");
        }

        /// <summary>
        /// Checks that the log file contains the correct number of evaluations and the genome that is currently
        /// classified as best.
        /// </summary>
        [Fact]
        public void LogIsCalledWithCorrectParameters()
        {
            // Build up a runner with a single generation which starts with genomes of specified value.
            var values = new[] { 4, 1, 42, -3 };
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetGenerations(1)
                .SetInstanceNumbers(1, 2)
                .SetGoalGeneration(0)
                .SetPopulationSize(values.Length)
                .SetPopulationMutantRatio(0)
                .SetMaxGenomeAge(2)
                .SetEvaluationLimit(20)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .Build(maximumNumberParallelEvaluations: 1);
            var runner = new AlgorithmTuner<ExtractIntegerValue, TestInstance, IntegerResult>(
                new ExtractIntegerValueCreator(),
                new TargetAlgorithm.InterfaceImplementations.ValueConsideration.SortByDescendingIntegerValue<TestInstance>(),
                AlgorithmTunerTest.BuildEmptyInstances(number: 2),
                AlgorithmTunerTest.SimpleParameterTree,
                configuration,
                new ValueGenomeBuilder(AlgorithmTunerTest.SimpleParameterTree, configuration, values));

            runner.Run();
            runner.Dispose();

            var lines = File.ReadLines(this._logFilePath).ToList();
            Assert.Equal(
                $"Evaluations: 4 / 20",
                lines[1]);
            Assert.Equal(
                $"\t{ExtractIntegerValue.ParameterName}: 42",
                lines[5]);
            Assert.Equal(
                $"\t0:\t42",
                lines[7]);
            Assert.Equal(
                $"\t1:\t42",
                lines[8]);
        }

        /// <summary>
        /// Checks that a correct information history is written to status every generation.
        /// This test only works on a Windows machine, since there are problems with FileSystemWatcher on other OS.
        /// </summary>
        [SkippableFact]
        public void CorrectInformationHistoryIsWrittenEveryGeneration()
        {
            // Check, if current OS is Windows.
            Skip.IfNot(Environment.OSVersion.Platform == PlatformID.Win32NT);

            // Build up a target algorithm instance with many strategy switches.
            var config = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetGenerations(3)
                .SetGoalGeneration(2)
                .SetMaximumNumberGgaGenerations(1)
                .SetContinuousOptimizationMethod(ContinuousOptimizationMethod.CmaEs)
                .AddDetailedConfigurationBuilder(
                    CovarianceMatrixAdaptationStrategyArgumentParser.Identifier,
                    new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                        .SetFocusOnIncumbent(true)
                        .SetMaximumNumberGenerations(1))
                .SetInstanceNumbers(1, 1)
                .SetEngineeredProportion(0)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .Build(maximumNumberParallelEvaluations: 1);
            var tuner = new AlgorithmTunerBuilder()
                .SetConfiguration(config)
                .ExecuteAlgorithmTunerConstructor();

            // Add watcher to grab all status files.
            var statusDumps =
                new List<Status<TestInstance, TestResult, GenomePredictionRandomForest<ReuseOldTreesStrategy>,
                    GenomePredictionForestModel<GenomePredictionTree>, ReuseOldTreesStrategy>>();
            using (var watcher = new FileSystemWatcher(PathUtils.GetAbsolutePathFromExecutableFolderRelative("status")))
            {
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.EnableRaisingEvents = true;
                watcher.Renamed += (sender, e) =>
                    {
                        if (!e.FullPath.Contains("status.oatstat"))
                        {
                            return;
                        }

                        var writtenStatus = this.ReadAlgorithmTunerStatusFile();
                        statusDumps.Add(writtenStatus);
                    };

                // Run algorithm tuner.
                tuner.Run();
                tuner.Dispose();
            }

            // Check all generation informations.
            // First, check length and overall consistency.
            Assert.Equal(2, statusDumps.Count);
            Assert.Single(
                statusDumps[0].InformationHistory);
            Assert.Equal(
                2,
                statusDumps[1].InformationHistory.Count);
            Assert.Equal(
                statusDumps[0].InformationHistory[0].ToString(),
                statusDumps[1].InformationHistory[0].ToString());

            // Then check what should be the same for every information.
            foreach (var information in statusDumps[1].InformationHistory)
            {
                Assert.Null(information.IncumbentTrainingScore);
                Assert.Null(information.IncumbentTestScore);
            }

            // Next: What can we check with the help of the status itself?
            for (int i = 0; i < statusDumps.Count; i++)
            {
                var information = statusDumps[i].InformationHistory.Last();
                Assert.Equal(i, information.Generation);
                Assert.Equal(
                    statusDumps[i].RunResults.Sum(result => result.Value.Count),
                    information.TotalNumberOfEvaluations);
                Assert.True(
                    Genome.GenomeComparer.Equals(
                        statusDumps[i].IncumbentGenomeWrapper.IncumbentGenome,
                        information.Incumbent.CreateMutableGenome()),
                    $"Expected different incumbent in {i}th generation.");
            }

            // Finally check individual fields.
            var firstInformation = statusDumps[0].InformationHistory.Single();
            var secondInformation = statusDumps[1].InformationHistory.Last();
            Assert.Equal(
                typeof(GgaStrategy<TestInstance, TestResult>),
                firstInformation.Strategy);
            Assert.Equal(
                typeof(LocalCovarianceMatrixAdaptationStrategy<TestInstance, TestResult>),
                secondInformation.Strategy);
        }

        /// <summary>
        /// Checks that status is dumped to file after every generation.
        /// This test only works on a Windows machine, since there are problems with FileSystemWatcher on other OS.
        /// </summary>
        [SkippableFact]
        public void StatusIsDumpedAfterEveryGeneration()
        {
            // Check, if current OS is Windows.
            Skip.IfNot(Environment.OSVersion.Platform == PlatformID.Win32NT);

            // Build up a target algorithm instance with few generations.
            var tuner = AlgorithmTunerTest.CreateSmallAlgorithmTuner(generations: 3);

            // Add watcher to find out when status files are written.
            var generationsWithStatusDump = new List<int>(2);
            using (var watcher = new FileSystemWatcher(PathUtils.GetAbsolutePathFromExecutableFolderRelative("status")))
            {
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.EnableRaisingEvents = true;
                watcher.Renamed += (sender, e) =>
                    {
                        if (!e.FullPath.Contains("status.oatstat"))
                        {
                            return;
                        }

                        // Read the file to find the generation it was written for.
                        var writtenStatus = this.ReadAlgorithmTunerStatusFile();
                        generationsWithStatusDump.Add(writtenStatus.Generation);
                    };

                // Run algorithm tuner.
                tuner.Run();
                tuner.Dispose();
            }

            // Check a status file was written for each generation.
            Assert.True(
                generationsWithStatusDump.SequenceEqual(new[] { 1, 2 }),
                $"Expected status to be written to file for generations 1 and 2, but was written for {TestUtils.PrintList(generationsWithStatusDump)}.");
        }

        /// <summary>
        /// Checks that all properties important for the status have been dumped to file.
        /// </summary>
        [Fact]
        public void DumpedStatusHasNoEmptyProperties()
        {
            // Build up an algorithm tuner instance with few generations.
            var tuner = AlgorithmTunerTest.CreateSmallAlgorithmTuner(generations: 2);

            // Let it run completely.
            tuner.Run();
            tuner.Dispose();

            // Check last status dump. Update: Last generation is no longer computed/dumped, since it is never used/evaluated. -> Last dump should be the penultimate generation
            var lastStatus = this.ReadAlgorithmTunerStatusFile();
            Assert.Equal(1, lastStatus.Generation);
            Assert.Equal(
                64,
                lastStatus.Population.GetCompetitiveIndividuals().Count);
            Assert.Equal(
                64,
                lastStatus.Population.GetNonCompetitiveMates().Count);
            Assert.True(lastStatus.RunResults.Count > 0, "Run results should have been dumped.");
            Assert.Equal(
                2,
                lastStatus.Configuration.Generations);
        }

        /// <summary>
        /// Checks that tuning can continue with a status dump if
        /// <see cref="AlgorithmTuner{TTargetAlgorithm, TInstance, TResult, TModelLearner, TPredictorModel, TSamplingStrategy}.UseStatusDump"/>
        /// is called.
        /// This test only works on a Windows machine, since there are problems with FileSystemWatcher on other OS.
        /// </summary>
        [SkippableFact]
        public void CheckRunStartsWithStatusDumpIfMethodGotCalled()
        {
            // Check, if current OS is Windows.
            Skip.IfNot(Environment.OSVersion.Platform == PlatformID.Win32NT);

            // Run a short run simulating a canceled algorithm tuner run.
            var canceledTunerRun = AlgorithmTunerTest.CreateSmallAlgorithmTuner(generations: 3, allowConfigChangeForSubsequentRun: true);
            canceledTunerRun.Run();
            canceledTunerRun.Dispose();

            // Then start a new one using the existing status dump.
            var subsequentTunerRun = AlgorithmTunerTest.CreateSmallAlgorithmTuner(generations: 4, allowConfigChangeForSubsequentRun: true);
            subsequentTunerRun.UseStatusDump(this._statusFilePath);

            // Make sure it actually used the status dump.
            bool alreadyHandledFirstEvent = false;
            var firstGeneration = -1;
            var populationSize = -1;
            using (var watcher = new FileSystemWatcher(PathUtils.GetAbsolutePathFromExecutableFolderRelative("status")))
            {
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.EnableRaisingEvents = true;
                watcher.Renamed += (sender, e) =>
                    {
                        if (!e.FullPath.Contains("status.oatstat") || alreadyHandledFirstEvent)
                        {
                            return;
                        }

                        // Then, find out what was written to file.
                        var writtenStatus = this.ReadAlgorithmTunerStatusFile();

                        // Subtract one because we do not log the status at the start of tuning,
                        // but only after going to the next generation (doing some work).
                        firstGeneration = writtenStatus.Generation - 1;
                        populationSize = writtenStatus.Population.GetCompetitiveIndividuals().Count
                                         + writtenStatus.Population.GetNonCompetitiveMates().Count;

                        alreadyHandledFirstEvent = true;
                    };

                // Run new algorithm tuner instance.
                subsequentTunerRun.Run();
                subsequentTunerRun.Dispose();
            }

            Assert.Equal(2, firstGeneration);
            Assert.Equal(128, populationSize);
        }

        /// <summary>
        /// Checks that old status files are zipped after every generation if
        /// <see cref="AlgorithmTunerConfiguration.ZipOldStatusFiles"/> is set to <c>true</c>.
        /// </summary>
        [Fact]
        public void OldStatusFilesAreZippedIfFlagIsSet()
        {
            // Build up a target algorithm instance with few generations.
            var config = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetZipOldStatusFiles(true)
                .SetGenerations(4)
                .SetMaximumNumberGgaGenerations(5)
                .SetInstanceNumbers(1, 1)
                .SetGoalGeneration(1)
                .SetEngineeredProportion(0)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .Build(maximumNumberParallelEvaluations: 1);
            var tuner = new AlgorithmTunerBuilder()
                .SetConfiguration(config)
                .ExecuteAlgorithmTunerConstructor();

            // Run it.
            tuner.Run();
            tuner.Dispose();

            // Count number of zipped status directories.
            // Should be two less than generations: Last status is not written,
            // and therefore the status before is not zipped, either.
            var zips = Directory.GetFiles(config.ZippedStatusFileDirectory, "status_*.zip");
            Assert.True(
                config.Generations - 2 == zips.Length,
                $"Found the following zip files: {string.Join(",", zips)}, but expected {config.Generations - 1}.");
        }

        /// <summary>
        /// Checks that
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}.CompleteAndExportGenerationHistory"/>
        /// writes a "generationHistory.csv" file, and also a "scores.csv" file, but the latter only if the run evaluator is a
        /// <see cref="IMetricRunEvaluator{TInstance,TResult}"/>.
        /// </summary>
        [Fact]
        public void CompleteAndExportGenerationHistoryDoesOnlyWriteScoresForMetricRunEvaluators()
        {
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetGenerations(1)
                .SetInstanceNumbers(1, 1)
                .SetGoalGeneration(0)
                .SetPopulationMutantRatio(0)
                .SetScoreGenerationHistory(true)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .Build(maximumNumberParallelEvaluations: 1);

            var nonMetricTuner = new AlgorithmTunerBuilder()
                .SetConfiguration(configuration)
                .ExecuteAlgorithmTunerConstructor();
            nonMetricTuner.Run();
            nonMetricTuner.CompleteAndExportGenerationHistory();
            nonMetricTuner.Dispose();
            Assert.True(File.Exists("generationHistory.csv"), "Generation history should have been written.");
            File.Delete("generationHistory.csv");
            Assert.False(File.Exists("scores.csv"), "There should be no scores file for non-metric result.");

            var metricTuner = new AlgorithmTuner<ExtractIntegerValue, TestInstance, IntegerResult>(
                new ExtractIntegerValueCreator(),
                new TargetAlgorithm.InterfaceImplementations.ValueConsideration.SortByDescendingIntegerValue<TestInstance>(),
                AlgorithmTunerTest.BuildEmptyInstances(number: 1),
                AlgorithmTunerTest.SimpleParameterTree,
                configuration);
            metricTuner.Run();
            metricTuner.SetTestInstances(AlgorithmTunerTest.BuildEmptyInstances(2));
            metricTuner.CompleteAndExportGenerationHistory();
            metricTuner.Dispose();
            Assert.True(File.Exists("generationHistory.csv"), "Generation history should have been written.");
            Assert.True(File.Exists("scores.csv"), "There should be a scores file for metric result.");
        }

        /// <summary>
        /// Checks that
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}.CompleteAndExportGenerationHistory"/>
        /// writes a "generationHistory.csv" file, and also a "scores.csv" file, but the latter only if
        /// <see cref="AlgorithmTunerConfiguration.ScoreGenerationHistory"/> is activated.
        /// </summary>
        [Fact]
        public void CompleteAndExportGenerationHistoryDoesOnlyWriteScoresIfOptionIsActivated()
        {
            var configurationBuilder = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetGenerations(1)
                .SetInstanceNumbers(1, 1)
                .SetGoalGeneration(0)
                .SetPopulationMutantRatio(0)
                .SetScoreGenerationHistory(false)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder());

            var plainTuner = new AlgorithmTuner<ExtractIntegerValue, TestInstance, IntegerResult>(
                new ExtractIntegerValueCreator(),
                new TargetAlgorithm.InterfaceImplementations.ValueConsideration.SortByDescendingIntegerValue<TestInstance>(),
                AlgorithmTunerTest.BuildEmptyInstances(number: 1),
                AlgorithmTunerTest.SimpleParameterTree,
                configurationBuilder.Build(maximumNumberParallelEvaluations: 1));
            plainTuner.Run();
            plainTuner.CompleteAndExportGenerationHistory();
            plainTuner.Dispose();
            Assert.True(File.Exists("generationHistory.csv"), "Generation history should have been written.");
            File.Delete("generationHistory.csv");
            Assert.False(File.Exists("scores.csv"), "There should be no scores file if option is not activated.");

            var tunerWithScores = new AlgorithmTuner<ExtractIntegerValue, TestInstance, IntegerResult>(
                new ExtractIntegerValueCreator(),
                new TargetAlgorithm.InterfaceImplementations.ValueConsideration.SortByDescendingIntegerValue<TestInstance>(),
                AlgorithmTunerTest.BuildEmptyInstances(number: 1),
                AlgorithmTunerTest.SimpleParameterTree,
                configurationBuilder.SetScoreGenerationHistory(true).Build(maximumNumberParallelEvaluations: 1));
            tunerWithScores.Run();
            tunerWithScores.SetTestInstances(AlgorithmTunerTest.BuildEmptyInstances(2));
            tunerWithScores.CompleteAndExportGenerationHistory();
            tunerWithScores.Dispose();
            Assert.True(File.Exists("generationHistory.csv"), "Generation history should have been written.");
            Assert.True(File.Exists("scores.csv"), "There should be a scores file if option is activated.");
        }

        /// <summary>
        /// Checks that
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}.CompleteAndExportGenerationHistory"/>
        /// uses the correct instances, information generation, and evaluation limit.
        /// </summary>
        [Fact]
        public void CompleteAndExportGenerationHistoryUsesCorrectData()
        {
            var values = new[] { 4, 1, 42, -3 };
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetGenerations(1)
                .SetPopulationSize(values.Length)
                .SetEvaluationLimit(1000)
                .SetInstanceNumbers(1, 1)
                .SetGoalGeneration(0)
                .SetMaxGenomeAge(2)
                .SetPopulationMutantRatio(0)
                .SetScoreGenerationHistory(true)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .Build(maximumNumberParallelEvaluations: 1);
            var tuner = new AlgorithmTuner<MultiplyIntegerWithSeed, InstanceSeedFile, IntegerResult>(
                new MultiplyIntegerWithSeedCreator(),
                new TargetAlgorithm.InterfaceImplementations.ValueConsideration.SortByDescendingIntegerValue<InstanceSeedFile>(),
                new List<InstanceSeedFile> { new InstanceSeedFile("train", 12) },
                AlgorithmTunerTest.SimpleParameterTree,
                configuration,
                new ValueGenomeBuilder(AlgorithmTunerTest.SimpleParameterTree, configuration, values));
            tuner.Run();
            tuner.SetTestInstances(new List<InstanceSeedFile> { new InstanceSeedFile("test_old", 5) });
            tuner.SetTestInstances(new List<InstanceSeedFile> { new InstanceSeedFile("test", 0) });
            tuner.CompleteAndExportGenerationHistory();
            tuner.Dispose();

            Assert.True(File.Exists("generationHistory.csv"), "Generation history should have been written.");
            Assert.True(
                2 == File.ReadAllLines("generationHistory.csv").Length,
                "There should be 2 lines: 1 legend, 1 generation information.");

            Assert.True(File.Exists("scores.csv"), "There should be a scores file.");
            var scores = File.ReadAllLines("scores.csv");
            Assert.True(
                11 == scores.Length,
                "With an evaluation limit of 1000, there should be 11 lines (1 legend).");
            Assert.Equal("100;504;0", scores[1]);
        }

        /// <summary>
        /// Checks that
        /// <see cref="AlgorithmTuner{TTargetAlgorithm, TInstance, TResult, TModelLearner, TPredictorModel, TSamplingStrategy}.UseStatusDump"/>
        /// throws an <see cref="InvalidOperationException"/> if called after tuning completed.
        /// </summary>
        [Fact]
        public void CheckUseStatusDumpThrowsIfNotCalledBeforeTuningStart()
        {
            // Run a short run simulating a canceled algorithm tuner run.
            var canceledTunerRun = AlgorithmTunerTest.CreateSmallAlgorithmTuner(generations: 3);
            canceledTunerRun.Run();
            canceledTunerRun.Dispose();

            // Prevent error from already existing zip files.
            var oldStatusFileDirectory = PathUtils.GetAbsolutePathFromExecutableFolderRelative("old_status_files");
            if (Directory.Exists(oldStatusFileDirectory))
            {
                Directory.Delete(oldStatusFileDirectory, recursive: true);
            }

            Directory.CreateDirectory(oldStatusFileDirectory);

            // Start subsequent tuning.
            var tuner = AlgorithmTunerTest.CreateSmallAlgorithmTuner(generations: 2);
            tuner.Run();

            // Then try to use status dump.
            Assert.Throws<InvalidOperationException>(() => tuner.UseStatusDump(this._statusFilePath));
            tuner.Dispose();
        }

        /// <summary>
        /// Checks that
        /// <see cref="AlgorithmTuner{TTargetAlgorithm, TInstance, TResult, TModelLearner, TPredictorModel, TSamplingStrategy}.UseStatusDump"/>
        /// throws an <see cref="InvalidOperationException"/> if
        /// the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> object's configuration and the configuration stored in the status file are
        /// incompatible.
        /// </summary>
        [Fact(Timeout = 20000)]
        public void CheckUseStatusDumpThrowsIfConfigurationsNotCompatible()
        {
            // Run a short run which writes a status file.
            var tuner = AlgorithmTunerTest.CreateSmallAlgorithmTuner(generations: 2);
            tuner.Run();
            tuner.Dispose();

            // Then try to use that file for another runner configured to use a different number of generations.
            var differentRunner = AlgorithmTunerTest.CreateSmallAlgorithmTuner(generations: 3);
            Assert.Throws<InvalidOperationException>(() => differentRunner.UseStatusDump(this._statusFilePath));
            differentRunner.Dispose();
        }

        /// <summary>
        /// Checks that
        /// <see cref="AlgorithmTuner{TTargetAlgorithm, TInstance, TResult, TModelLearner, TPredictorModel, TSamplingStrategy}.UseStatusDump"/>
        /// does not throw an <see cref="InvalidOperationException"/> if the
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> object's configuration and the
        /// configuration stored in the status file are incompatible, but strict configuration checking is turned off.
        /// </summary>
        [Fact]
        public void CheckUseStatusDumpIgnoresContinuityIssuesWhenStrictCheckingIsTurnedOff()
        {
            // Run a short run which writes a status file.
            var tuner = AlgorithmTunerTest.CreateSmallAlgorithmTuner(generations: 2);
            tuner.Run();
            tuner.Dispose();

            // Then try to use that file for another runner configured to use a different number of generations.
            // Turn off strict configuration checking.
            var differentRunner = AlgorithmTunerTest.CreateSmallAlgorithmTuner(generations: 3, allowConfigChangeForSubsequentRun: true);
            differentRunner.UseStatusDump(this._statusFilePath);

            // Check that no exception happened.
            Assert.NotNull(differentRunner);

            differentRunner.Dispose();
        }

        /// <summary>
        /// Checks that
        /// <see cref="AlgorithmTuner{TTargetAlgorithm, TInstance, TResult, TModelLearner, TPredictorModel, TSamplingStrategy}.UseStatusDump"/>
        /// does throw
        /// an <see cref="InvalidOperationException"/> if the
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> object's configuration and the
        /// configuration stored in the status file are technically incompatible and strict configuration checking is
        /// turned off.
        /// </summary>
        [Fact]
        public void CheckUseStatusDumpStillChecksTechnicalParametersWhenStrictCheckingIsTurnedOff()
        {
            var configBuilder = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetGenerations(2)
                .SetInstanceNumbers(1, 1)
                .SetGoalGeneration(1)
                .SetEngineeredProportion(0)
                .SetMaximumNumberParallelEvaluations(1)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder());

            // Run a short run which writes a status file.
            var tuner = new AlgorithmTunerBuilder()
                .SetConfiguration(configBuilder.Build())
                .ExecuteAlgorithmTunerConstructor();
            tuner.Run();
            tuner.Dispose();

            // Then try to use that file for another runner configured to train a model.
            // Turn off strict configuration checking.
            configBuilder.SetStrictCompatibilityCheck(false).SetTrainModel(true);
            var differentRunner = new AlgorithmTunerBuilder()
                .SetConfiguration(configBuilder.Build())
                .ExecuteAlgorithmTunerConstructor();
            Assert.Throws<InvalidOperationException>(() => differentRunner.UseStatusDump(this._statusFilePath));
            differentRunner.Dispose();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called before every test.
        /// </summary>
        protected override void InitializeDefault()
        {
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEnableRacing(true)
                .Build(maximumNumberParallelEvaluations: 1);
            Directory.CreateDirectory(configuration.ZippedStatusFileDirectory);
        }

        /// <summary>
        /// Deletes the status file(s) if any was written.
        /// </summary>
        protected override void CleanupDefault()
        {
            if (File.Exists(this._statusFilePath))
            {
                File.Delete(this._statusFilePath);
            }

            if (File.Exists(this._logFilePath))
            {
                File.Delete(this._logFilePath);
            }
        }

        /// <summary>
        /// Builds empty <see cref="TestInstance"/>s named 1, 2, 3, etc.
        /// </summary>
        /// <param name="number">Number of test instances to build.</param>
        /// <returns>The instances.</returns>
        private static IEnumerable<TestInstance> BuildEmptyInstances(int number)
        {
            for (int i = 0; i < number; i++)
            {
                yield return new TestInstance(i.ToString());
            }
        }

        /// <summary>
        /// Creates a fast running <see cref="AlgorithmTuner{NoOperation, TestInstance, TestResult}"/> instance with a single
        /// training instance and mainly default parameters.
        /// </summary>
        /// <param name="generations">The number of generations to execute.</param>
        /// <param name="allowConfigChangeForSubsequentRun">Whether subsequent "continue" runs should override the
        /// configuration.</param>
        /// <returns>The created <see cref="AlgorithmTuner{NoOperation, TestInstance, TestResult}"/> instance.</returns>
        private static AlgorithmTuner<NoOperation, TestInstance, TestResult, GenomePredictionRandomForest<ReuseOldTreesStrategy>,
            GenomePredictionForestModel<GenomePredictionTree>, ReuseOldTreesStrategy> CreateSmallAlgorithmTuner(
            int generations,
            bool allowConfigChangeForSubsequentRun = false)
        {
            LoggingHelper.Configure("test.txt");
            var config = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetGenerations(generations)
                .SetMaximumNumberGgaGenerations(5)
                .SetInstanceNumbers(1, 1)
                .SetGoalGeneration(1)
                .SetEngineeredProportion(0)
                .SetEnableRacing(true)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .SetStrictCompatibilityCheck(!allowConfigChangeForSubsequentRun)
                .Build(maximumNumberParallelEvaluations: 1);
            return new AlgorithmTunerBuilder()
                .SetConfiguration(config)
                .ExecuteAlgorithmTunerConstructor();
        }

        /// <summary>
        /// Reads the algorithm tuner status from <see cref="_statusFilePath"/>.
        /// </summary>
        /// <returns>The status.</returns>
        private Status<TestInstance, TestResult, GenomePredictionRandomForest<ReuseOldTreesStrategy>,
            GenomePredictionForestModel<GenomePredictionTree>, ReuseOldTreesStrategy> ReadAlgorithmTunerStatusFile()
        {
            return StatusBase
                .ReadFromFile<Status<TestInstance, TestResult, GenomePredictionRandomForest<ReuseOldTreesStrategy>,
                    GenomePredictionForestModel<GenomePredictionTree>,
                    ReuseOldTreesStrategy>>(this._statusFilePath);
        }

        #endregion

        /// <summary>
        /// Short form of <see cref="AlgorithmTunerBuilder"/> using
        /// <see cref="GenomePredictionRandomForest{ReuseOldTreesStrategy}"/> as learner model,
        /// <see cref="GenomePredictionForestModel{GenomePredictionTree}"/> as predictor model, and
        /// <see cref="ReuseOldTreesStrategy"/> as sampling strategy.
        /// </summary>
        private class AlgorithmTunerBuilder :
            AlgorithmTunerBuilder<GenomePredictionRandomForest<ReuseOldTreesStrategy>, GenomePredictionForestModel<GenomePredictionTree>,
                ReuseOldTreesStrategy>
        {
        }

        /// <summary>
        /// Convenience class for building a <see cref="AlgorithmTuner{NoOperation, TestInstance, TestResult}"/> instance.
        /// Specifies default constructor parameters.
        /// </summary>
        /// <typeparam name="TLearnerModel">The type of learning model.</typeparam>
        /// <typeparam name="TPredictorModel">The type of prediction model.</typeparam>
        /// <typeparam name="TSamplingStrategy">The type of sampling strategy for trees.</typeparam>
        private class AlgorithmTunerBuilder<TLearnerModel, TPredictorModel, TSamplingStrategy>
            where TLearnerModel : IGenomeLearner<TPredictorModel, TSamplingStrategy>
            where TPredictorModel : IEnsemblePredictor<GenomePredictionTree>
            where TSamplingStrategy : IEnsembleSamplingStrategy<IGenomePredictor>, new()
        {
            #region Fields

            /// <summary>
            /// The <see cref="ITargetAlgorithmFactory{NoOperation, TestInstance, TestResult}"/> to provide.
            /// </summary>
            private ITargetAlgorithmFactory<NoOperation, TestInstance, TestResult> _targetAlgorithmFactory
                = AlgorithmTunerTest.NoopFactory;

            /// <summary>
            /// The <see cref="IRunEvaluator{TestInstance, TestResult}"/> to provide.
            /// </summary>
            private IRunEvaluator<TestInstance, TestResult> _runEvaluator = new KeepSuggestedOrder<TestInstance, TestResult>();

            /// <summary>
            /// The <see cref="TestInstance"/>s to provide for training.
            /// </summary>
            private IEnumerable<TestInstance> _trainingInstances = AlgorithmTunerTest.BuildEmptyInstances(number: 100);

            /// <summary>
            /// The <see cref="TestInstance"/>s to provide for testing.
            /// </summary>
            private IEnumerable<TestInstance> _testInstances = AlgorithmTunerTest.BuildEmptyInstances(number: 100);

            /// <summary>
            /// The <see cref="ParameterTree"/> to provide.
            /// </summary>
            private ParameterTree _parameterTree = AlgorithmTunerTest.SimpleParameterTree;

            /// <summary>
            /// The <see cref="AlgorithmTunerConfiguration"/> to provide.
            /// </summary>
            private AlgorithmTunerConfiguration _configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .AddDetailedConfigurationBuilder(
                        RegressionForestArgumentParser.Identifier,
                        new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                    .SetEnableRacing(true)
                    .Build(maximumNumberParallelEvaluations: 1);

            /// <summary>
            /// The <see cref="GenomeBuilder"/> to provide.
            /// </summary>
            private GenomeBuilder _genomeBuilder;

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// Executes <see cref="AlgorithmTuner{NoOperation, TestInstance, TestResult}"/>'s constructor using the configured arguments.
            /// </summary>
            /// <param name="specifyGenomeBuilder">Whether or not to use the constructor which expects a <see cref="GenomeBuilder"/>.</param>
            /// <returns>The result of the constructor execution.</returns>
            public AlgorithmTuner<NoOperation, TestInstance, TestResult, TLearnerModel, TPredictorModel, TSamplingStrategy>
                ExecuteAlgorithmTunerConstructor(bool specifyGenomeBuilder = false)
            {
                // Call different constructors depending on flag.
                AlgorithmTuner<NoOperation, TestInstance, TestResult, TLearnerModel, TPredictorModel, TSamplingStrategy> tuner;
                if (!specifyGenomeBuilder)
                {
                    tuner = new AlgorithmTuner<NoOperation, TestInstance, TestResult, TLearnerModel, TPredictorModel, TSamplingStrategy>(
                        this._targetAlgorithmFactory,
                        this._runEvaluator,
                        this._trainingInstances,
                        this._parameterTree,
                        this._configuration);
                }
                else
                {
                    tuner = new AlgorithmTuner<NoOperation, TestInstance, TestResult, TLearnerModel, TPredictorModel, TSamplingStrategy>(
                        this._targetAlgorithmFactory,
                        this._runEvaluator,
                        this._trainingInstances,
                        this._parameterTree,
                        this._configuration,
                        this._genomeBuilder);
                }

                tuner.SetTestInstances(this._testInstances);
                return tuner;
            }

            /// <summary>
            /// Sets the <see cref="ITargetAlgorithmFactory{NoOperation, TestInstance, TestResult}"/> to provide to
            /// the <see cref="AlgorithmTuner{NoOperation, TestInstance, TestResult}"/> constructor.
            /// Default is a factory which creates the same noop target algorithm <see cref="NoOperation"/>
            /// for all inputs.
            /// </summary>
            /// <param name="targetAlgorithmFactory">The target algorithm factory to provide to the
            /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>
            /// constructor.</param>
            /// <returns>The <see cref="AlgorithmTunerBuilder"/> in its new state.</returns>
            public AlgorithmTunerBuilder<TLearnerModel, TPredictorModel, TSamplingStrategy> SetTargetAlgorithmFactory(
                ITargetAlgorithmFactory<NoOperation, TestInstance, TestResult> targetAlgorithmFactory)
            {
                this._targetAlgorithmFactory = targetAlgorithmFactory;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="IRunEvaluator{TestInstance, TestResult}"/> to provide to the
            /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>
            /// constructor.
            /// Default is an evaluator which simply keeps the supplied order.
            /// </summary>
            /// <param name="runEvaluator">The evaluator to provide to the
            /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>
            /// constructor.</param>
            /// <returns>The <see cref="AlgorithmTunerBuilder"/> in its new state.</returns>
            public AlgorithmTunerBuilder<TLearnerModel, TPredictorModel, TSamplingStrategy> SetRunEvaluator(
                IRunEvaluator<TestInstance, TestResult> runEvaluator)
            {
                this._runEvaluator = runEvaluator;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="TestInstance"/>s to provide for training to the
            /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>
            /// constructor.
            /// Default are 100 <see cref="TestInstance"/>s.
            /// </summary>
            /// <param name="instances">The training instances to provide to the
            /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>
            /// constructor.</param>
            /// <returns>The <see cref="AlgorithmTunerBuilder"/> in its new state.</returns>
            public AlgorithmTunerBuilder<TLearnerModel, TPredictorModel, TSamplingStrategy> SetTrainingInstances(
                IEnumerable<TestInstance> instances)
            {
                this._trainingInstances = instances;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="TestInstance"/>s to provide for testing to the
            /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>
            /// constructor.
            /// Default are 100 <see cref="TestInstance"/>s.
            /// </summary>
            /// <param name="instances">The test instances to provide to the
            /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>
            /// constructor.</param>
            /// <returns>The <see cref="AlgorithmTunerBuilder"/> in its new state.</returns>
            public AlgorithmTunerBuilder<TLearnerModel, TPredictorModel, TSamplingStrategy> SetTestInstances(
                IEnumerable<TestInstance> instances)
            {
                this._testInstances = instances;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="ParameterTree"/> to provide to the
            /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>
            /// constructor.
            /// Default is a simple parameter tree representing a single integer value.
            /// </summary>
            /// <param name="parameterTree">The parameter tree to provide to the
            /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>
            /// constructor.</param>
            /// <returns>The <see cref="AlgorithmTunerBuilder"/> in its new state.</returns>
            public AlgorithmTunerBuilder<TLearnerModel, TPredictorModel, TSamplingStrategy> SetParameterTree(ParameterTree parameterTree)
            {
                this._parameterTree = parameterTree;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="AlgorithmTunerConfiguration"/> to provide to the
            /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>
            /// constructor.
            /// Default is the default <see cref="AlgorithmTunerConfiguration"/> with 1 core.
            /// </summary>
            /// <param name="configuration">The configuration to provide to the
            /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>
            /// constructor.</param>
            /// <returns>The <see cref="AlgorithmTunerBuilder"/> in its new state.</returns>
            public AlgorithmTunerBuilder<TLearnerModel, TPredictorModel, TSamplingStrategy> SetConfiguration(
                AlgorithmTunerConfiguration configuration)
            {
                this._configuration = configuration;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="GenomeBuilder"/> to provide to the
            /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>
            /// constructor.
            /// Default is null.
            /// </summary>
            /// <param name="genomeBuilder">The genome builder to provide to the
            /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/>
            /// constructor.</param>
            /// <returns>The <see cref="AlgorithmTunerBuilder"/> in its new state.</returns>
            public AlgorithmTunerBuilder<TLearnerModel, TPredictorModel, TSamplingStrategy> SetGenomeBuilder(GenomeBuilder genomeBuilder)
            {
                this._genomeBuilder = genomeBuilder;
                return this;
            }

            #endregion
        }
    }
}