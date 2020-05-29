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
namespace Optano.Algorithm.Tuner.Tests.Tracking
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Optano.Algorithm.Tuner;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.Tracking;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="LogWriter{TInstance,TResult}"/> class.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class LogWriterTest : IDisposable
    {
        #region Static Fields

        /// <summary>
        /// Path to log file that gets written in some tests.
        /// </summary>
        private static readonly string LogFilePath = PathUtils.GetAbsolutePathFromCurrentDirectory("tunerLog.txt");

        /// <summary>
        /// An empty instance of <see cref="GenomeResults{TInstance,TResult}"/>, useful in tests.
        /// </summary>
        private static readonly GenomeResults<InstanceFile, RuntimeResult> EmptyResults =
            new GenomeResults<InstanceFile, RuntimeResult>(new Dictionary<InstanceFile, RuntimeResult>());

        #endregion

        #region Fields

        /// <summary>
        /// <see cref="ParameterTree"/> that can be used in tests.
        /// </summary>
        private readonly ParameterTree _parameterTree = LogWriterTest.CreateParameterTree();

        /// <summary>
        /// <see cref="AlgorithmTunerConfiguration"/> that can be used in tests.
        /// </summary>
        private AlgorithmTunerConfiguration _configuration;

        /// <summary>
        /// A <see cref="GenomeBuilder"/> to create <see cref="Genome"/>s used in tests.
        /// </summary>
        private GenomeBuilder _genomeBuilder;

        /// <summary>
        /// The <see cref="LogWriter{I,R}"/> used in tests.
        /// </summary>
        private LogWriter<InstanceFile, RuntimeResult> _writer;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LogWriterTest"/> class.
        /// </summary>
        public LogWriterTest()
        {
            this._configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetGenerations(24)
                .SetGoalGeneration(17)
                .SetLogFilePath(LogWriterTest.LogFilePath)
                .Build(maximumNumberParallelEvaluations: 1);
            this._genomeBuilder = new GenomeBuilder(this._parameterTree, this._configuration);
            Randomizer.Reset();
            Randomizer.Configure(0);

            this._writer = new LogWriter<InstanceFile, RuntimeResult>(this._parameterTree, this._configuration);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Resets randomizer and deletes possibly created log files.
        /// </summary>
        public void Dispose()
        {
            Randomizer.Reset();
            if (File.Exists(LogWriterTest.LogFilePath))
            {
                File.Delete(LogWriterTest.LogFilePath);
            }
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentNullException"/> if no parameter tree is provided.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingParameterTree()
        {
            Assert.Throws<ArgumentNullException>(
                () => new LogWriter<InstanceFile, RuntimeResult>(
                    parameterTree: null,
                    configuration: this._configuration));
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentNullException"/> if no configuration is provided.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingConfiguration()
        {
            Assert.Throws<ArgumentNullException>(
                () => new LogWriter<InstanceFile, RuntimeResult>(this._parameterTree, configuration: null));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> throws an 
        /// <see cref="ArgumentOutOfRangeException"/> if the number of evaluations is provided as zero.
        /// </summary>
        [Fact]
        public void LogFinishedGenerationThrowsOnZeroEvaluations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => this._writer.LogFinishedGeneration(
                    0,
                    totalEvaluationCount: 0,
                    fittestGenome: this._genomeBuilder.CreateRandomGenome(0),
                    results: LogWriterTest.EmptyResults));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> throws an 
        /// <see cref="ArgumentNullException"/> if no genome is provided on call.
        /// </summary>
        [Fact]
        public void LogFinishedGenerationThrowsOnMissingGenome()
        {
            Assert.Throws<ArgumentNullException>(
                () => this._writer.LogFinishedGeneration(0, 1, fittestGenome: null, results: LogWriterTest.EmptyResults));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> throws an 
        /// <see cref="ArgumentNullException"/> if no results are provided on call.
        /// </summary>
        [Fact]
        public void LogFinishedGenerationThrowsOnMissingResults()
        {
            Assert.Throws<ArgumentNullException>(
                () => this._writer.LogFinishedGeneration(0, 1, this._genomeBuilder.CreateRandomGenome(0), results: null));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> logs the finished generation to file.
        /// </summary>
        [Fact]
        public void GenerationIsLoggedCorrectly()
        {
            // Log at a specific generation.
            int generation = 3;
            this._writer.LogFinishedGeneration(generation, 1, this._genomeBuilder.CreateRandomGenome(0), LogWriterTest.EmptyResults);

            var linesInFile = File.ReadLines(LogWriterTest.LogFilePath);
            Assert.Equal(
                $"Finished generation {generation} / 24",
                linesInFile.First());
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> logs the total number of evaluations if an
        /// evaluation limit exists.
        /// </summary>
        [Fact]
        public void NumberEvaluationsIsLoggedCorrectly()
        {
            // Ensure evaluation limit exists.
            this._configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetLogFilePath(LogWriterTest.LogFilePath)
                .SetEvaluationLimit(25)
                .Build(maximumNumberParallelEvaluations: 1);
            this._writer = new LogWriter<InstanceFile, RuntimeResult>(this._parameterTree, this._configuration);

            // Log at a specific number of evaluations.
            int evaluations = 3;
            this._writer.LogFinishedGeneration(1, evaluations, this._genomeBuilder.CreateRandomGenome(0), LogWriterTest.EmptyResults);

            var linesInFile = File.ReadLines(LogWriterTest.LogFilePath);
            Assert.Equal(
                $"Evaluations: {evaluations} / 25",
                linesInFile.ElementAt(1));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> does not log the total number of evaluations
        /// without an evaluation limit.
        /// </summary>
        [Fact]
        public void NumberEvaluationsIsNotLoggedOnUnlimitedEvaluations()
        {
            // Log at a specific number of evaluations.
            int evaluations = 3;
            this._writer.LogFinishedGeneration(1, evaluations, this._genomeBuilder.CreateRandomGenome(0), LogWriterTest.EmptyResults);

            var linesInFile = File.ReadLines(LogWriterTest.LogFilePath);
            Assert.NotEqual(
                $"Evaluations: {evaluations} / 25",
                linesInFile.ElementAt(1));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> correctly logs the elapsed time to file.
        /// </summary>
        [Fact]
        public void ElapsedTimeIsLoggedCorrectly()
        {
            Thread.Sleep(1000);
            this._writer.LogFinishedGeneration(0, 1, this._genomeBuilder.CreateRandomGenome(0), LogWriterTest.EmptyResults);
            
            var passedTime = DateTime.Now.ToUniversalTime() - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            var secondLine = File.ReadLines(LogWriterTest.LogFilePath).Skip(1).First();
            var loggedTime = TimeSpan.Parse(secondLine.Split(' ').Last());
            Assert.True(
                Math.Abs(passedTime.TotalSeconds - loggedTime.TotalSeconds) < passedTime.TotalSeconds * 0.05,
                "Generation was not logged correctly.");
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> correctly logs the fittest genome to file.
        /// </summary>
        [Fact]
        public void FittestGenomeIsLoggedCorrectly()
        {
            // Log a genome as fittest.
            var fittestGenome = this._genomeBuilder.CreateRandomGenome(2);
            fittestGenome.SetGene("decision", new Allele<bool>(true));
            this._writer.LogFinishedGeneration(0, 1, fittestGenome, LogWriterTest.EmptyResults);

            // Check genome was logged correctly.
            // Ignore first three lines as they do not describe the genome.
            var relevantLinesInFile = File.ReadLines(LogWriterTest.LogFilePath).Skip(4).ToList();
            Assert.Equal(
                "\tdecision: True",
                relevantLinesInFile[0]);
            Assert.Equal(
                $"\ta: {fittestGenome.GetGeneValue("a")}",
                relevantLinesInFile[1]);
            Assert.True(
                "Fittest genome's results on instances so far:" == relevantLinesInFile[2],
                "Inactive parameter values should not be printed.");
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> correctly logs the fittest genome's results 
        /// to file.
        /// </summary>
        [Fact]
        public void ResultsAreLoggedCorrectly()
        {
            var results = new SortedDictionary<InstanceFile, RuntimeResult>(
                              Comparer<InstanceFile>.Create((file1, file2) => string.Compare(file1.ToString(), file2.ToString())))
                              {
                                  { new InstanceFile("a"), new RuntimeResult(TimeSpan.FromMilliseconds(42)) },
                                  { new InstanceFile("foo/bar"), ResultBase<RuntimeResult>.CreateCancelledResult(TimeSpan.FromMilliseconds(11)) },
                              };

            var resultMessage = new GenomeResults<InstanceFile, RuntimeResult>(results);
            this._writer.LogFinishedGeneration(0, 1, this._genomeBuilder.CreateRandomGenome(0), resultMessage);

            // Check results were logged correclty.
            // Ignore first six lines as they do not describe the results.
            var relevantLinesInFile = File.ReadLines(LogWriterTest.LogFilePath).Skip(6).ToList();
            Assert.True(
                relevantLinesInFile.Contains(
                    FormattableString.Invariant($"\ta:\t{TimeSpan.FromMilliseconds(42):G}")),
                "Expected first result to be printed.");
            Assert.True(
                relevantLinesInFile.Contains(
                    FormattableString.Invariant($"\tfoo/bar:\tCancelled after {TimeSpan.FromMilliseconds(11):G}")),
                "Expected second result to be printed.");
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> throws a
        /// <see cref="DirectoryNotFoundException"/> if called with a log file path to a non-existing directory.
        /// </summary>
        [Fact]
        public void LogFinishedGenerationThrowsForUnknownDirectory()
        {
            // Set non existing log file path, then log finished generation.
            this._configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetLogFilePath("foo/bar.txt")
                .Build(maximumNumberParallelEvaluations: 1);
            this._genomeBuilder = new GenomeBuilder(this._parameterTree, this._configuration);
            this._writer = new LogWriter<InstanceFile, RuntimeResult>(this._parameterTree, this._configuration);
            Assert.Throws<DirectoryNotFoundException>(
                () => this._writer.LogFinishedGeneration(0, 1, this._genomeBuilder.CreateRandomGenome(0), LogWriterTest.EmptyResults));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> only replaces a log file after a new one was
        /// completed.
        /// </summary>
        [Fact]
        public void LogFinishedGenerationOnlyDeletesLastLogAfterWriteFinished()
        {
            /* Watch directory to see what happens with log files. */
            var tempFileChanged = DateTime.MaxValue;
            var logFileDeleted = DateTime.MaxValue;
            var tempFileRenamed = DateTime.MaxValue;
            using (var watcher = new FileSystemWatcher(PathUtils.GetAbsolutePathFromCurrentDirectory(string.Empty)))
            {
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess;
                watcher.EnableRaisingEvents = true;

                /* Remember when the temporary file got changed last. */
                watcher.Changed += (sender, e) =>
                    {
                        if (e.Name.EndsWith(LogWriter<InstanceFile, RuntimeResult>.WorkInProgressSuffix))
                        {
                            tempFileChanged = DateTime.Now;
                        }
                        else
                        {
                            Assert.True(false, $"Changed file {e.FullPath}.");
                        }
                    };
                /* Remember when old log file was deleted. */
                watcher.Deleted += (sender, e) =>
                    {
                        if (e.FullPath.Equals(LogWriterTest.LogFilePath))
                        {
                            logFileDeleted = DateTime.Now;
                        }
                        else
                        {
                            Assert.True(false, $"Deleted file {e.FullPath}.");
                        }
                    };
                /* Remember when temporary file was renamed to be the log file. */
                watcher.Renamed += (sender, e) =>
                    {
                        if (e.OldName.EndsWith(LogWriter<InstanceFile, RuntimeResult>.WorkInProgressSuffix))
                        {
                            tempFileRenamed = DateTime.Now;
                            Assert.True(LogWriterTest.LogFilePath == e.FullPath, "Should have replaced log file.");
                        }
                        else
                        {
                            Assert.True(false, $"Renamed file {e.FullPath}.");
                        }
                    };

                /* Write to file. */
                this._writer.LogFinishedGeneration(
                    0,
                    1,
                    this._genomeBuilder.CreateRandomGenome(0),
                    new GenomeResults<InstanceFile, RuntimeResult>(new Dictionary<InstanceFile, RuntimeResult>()));

                /* Wait a while for all events to be handled. */
                Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();

                /* Make sure log file was replaced at last possible moment. */
                Assert.True(
                    tempFileChanged < logFileDeleted,
                    "Log file should have been deleted only after the last change to the temporary file.");
                Assert.True(
                    tempFileRenamed - logFileDeleted <= TimeSpan.FromMilliseconds(50),
                    "Temporary file should have been renamed directly after old log file was deleted.");
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a parameter tree which root is an OR node "decision". If that is true, a value node "a" is 
        /// evaluated, otherwise a value node "b".
        /// </summary>
        /// <returns>The created parameter tree.</returns>
        private static ParameterTree CreateParameterTree()
        {
            var decisionNode =
                new OrNode<bool>("decision", new CategoricalDomain<bool>(new List<bool> { true, false }));
            decisionNode.AddChild(true, new ValueNode<double>("a", new ContinuousDomain()));
            decisionNode.AddChild(false, new ValueNode<double>("b", new ContinuousDomain()));
            return new ParameterTree(decisionNode);
        }

        #endregion
    }
}
