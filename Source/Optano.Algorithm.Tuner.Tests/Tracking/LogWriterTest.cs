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
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.Tracking;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="LogWriter{TInstance,TResult}"/> class.
    /// </summary>
    public class LogWriterTest : IDisposable
    {
        #region Static Fields

        /// <summary>
        /// The path to log file, used in tests.
        /// </summary>
        private static readonly string LogFilePath = PathUtils.GetAbsolutePathFromCurrentDirectory("tunerLog.txt");

        #endregion

        #region Fields

        /// <summary>
        /// The <see cref="GenomeResults{TInstance,TResult}"/>, used in tests.
        /// </summary>
        private readonly GenomeResults<InstanceFile, RuntimeResult> _testGenomeResults;

        /// <summary>
        /// The <see cref="ParameterTree"/>, used in tests.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// The <see cref="AlgorithmTunerConfiguration"/>, used in tests.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _configuration;

        /// <summary>
        /// The <see cref="LogWriter{I,R}"/>, used in tests.
        /// </summary>
        private readonly LogWriter<InstanceFile, RuntimeResult> _writer;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LogWriterTest"/> class.
        /// </summary>
        public LogWriterTest()
        {
            Randomizer.Reset();
            Randomizer.Configure(0);

            this._parameterTree = LogWriterTest.CreateParameterTree();

            this._configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetGenerations(24)
                .SetGoalGeneration(17)
                .SetLogFilePath(LogWriterTest.LogFilePath)
                .Build(1);

            this._testGenomeResults = new GenomeResults<InstanceFile, RuntimeResult>(
                new ImmutableGenome(new GenomeBuilder(this._parameterTree, this._configuration).CreateRandomGenome(4)),
                new SortedDictionary<InstanceFile, RuntimeResult>(
                    Comparer<InstanceFile>.Create((file1, file2) => string.CompareOrdinal(file1.ToString(), file2.ToString())))
                    {
                        { new InstanceFile("a"), new RuntimeResult(TimeSpan.FromMilliseconds(42)) },
                        { new InstanceFile("foo/bar"), ResultBase<RuntimeResult>.CreateCancelledResult(TimeSpan.FromMilliseconds(11)) },
                    });

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
                () => new LogWriter<InstanceFile, RuntimeResult>(null, this._configuration));
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentNullException"/> if no configuration is provided.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingConfiguration()
        {
            Assert.Throws<ArgumentNullException>(
                () => new LogWriter<InstanceFile, RuntimeResult>(this._parameterTree, null));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> throws an 
        /// <see cref="ArgumentOutOfRangeException"/> if the number of finished generations is provided as zero.
        /// </summary>
        [Fact]
        public void LogFinishedGenerationThrowsOnZeroGenerations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => this._writer.LogFinishedGeneration(
                    0,
                    1,
                    this._testGenomeResults,
                    true));
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
                    1,
                    0,
                    this._testGenomeResults,
                    true));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinalIncumbentGeneration"/> throws an 
        /// <see cref="ArgumentOutOfRangeException"/> if the number of evaluations is provided as zero.
        /// </summary>
        [Fact]
        public void LogFinalIncumbentGenerationThrowsOnZeroEvaluations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => this._writer.LogFinalIncumbentGeneration(
                    0,
                    this._testGenomeResults,
                    1,
                    2,
                    true));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> throws an 
        /// <see cref="ArgumentNullException"/> if no genome results are provided on call.
        /// </summary>
        [Fact]
        public void LogFinishedGenerationThrowsOnMissingGenomeResults()
        {
            Assert.Throws<ArgumentNullException>(
                () => this._writer.LogFinishedGeneration(1, 1, null, true));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinalIncumbentGeneration"/> throws an 
        /// <see cref="ArgumentNullException"/> if no genome results are provided on call.
        /// </summary>
        [Fact]
        public void LogFinalIncumbentGenerationThrowsOnMissingGenomeResults()
        {
            Assert.Throws<ArgumentNullException>(
                () => this._writer.LogFinalIncumbentGeneration(1, null, 1, 2, true));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinalIncumbentGeneration"/> throws an 
        /// <see cref="ArgumentOutOfRangeException"/> if the first generation as incumbent is provided, but the last generation as incumbent is null.
        /// </summary>
        [Fact]
        public void LogFinalIncumbentGenerationThrowsOnPositiveFirstGenerationAsIncumbentAndNullLastGenerationAsIncumbent()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => this._writer.LogFinalIncumbentGeneration(
                    1,
                    this._testGenomeResults,
                    1,
                    null,
                    true));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinalIncumbentGeneration"/> throws an 
        /// <see cref="ArgumentOutOfRangeException"/> if the first generation as incumbent is null, but the last generation as incumbent is provided.
        /// </summary>
        [Fact]
        public void LogFinalIncumbentGenerationThrowsOnNullFirstGenerationAsIncumbentAndPositiveLastGenerationAsIncumbent()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => this._writer.LogFinalIncumbentGeneration(
                    1,
                    this._testGenomeResults,
                    null,
                    1,
                    true));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinalIncumbentGeneration"/> throws an 
        /// <see cref="ArgumentOutOfRangeException"/> if the first generation as incumbent is provided as zero.
        /// </summary>
        [Fact]
        public void LogFinalIncumbentGenerationThrowsOnZeroFirstGenerationAsIncumbent()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => this._writer.LogFinalIncumbentGeneration(
                    1,
                    this._testGenomeResults,
                    0,
                    1,
                    true));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinalIncumbentGeneration"/> throws an 
        /// <see cref="ArgumentOutOfRangeException"/> if the first generation as incumbent is greater than the last generation as incumbent.
        /// </summary>
        [Fact]
        public void LogFinalIncumbentGenerationThrowsOnFirstGenerationAsIncumbentGreaterThanLastGenerationAsIncumbent()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => this._writer.LogFinalIncumbentGeneration(
                    1,
                    this._testGenomeResults,
                    2,
                    1,
                    true));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinalIncumbentGeneration"/> throws an 
        /// <see cref="InvalidOperationException"/> if the first and last generation as incumbent is provided as null and the fittest incumbent genome does not equal the default genome.
        /// </summary>
        [Fact]
        public void LogFinalIncumbentGenerationThrowsOnNullFirstAndLastGenerationAsIncumbentIfFittestIncumbentGenomeDoesNotEqualDefaultGenome()
        {
            Assert.Throws<InvalidOperationException>(
                () => this._writer.LogFinalIncumbentGeneration(
                    1,
                    this._testGenomeResults,
                    null,
                    null,
                    false));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> and <see cref="LogWriter{I,R}.LogFinalIncumbentGeneration"/> throw a
        /// <see cref="DirectoryNotFoundException"/> if called with a log file path to a non-existing directory.
        /// </summary>
        /// <param name="useLogFinishedGeneration">Whether to use <see cref="LogWriter{I,R}.LogFinishedGeneration"/>.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LogFinishedGenerationAndLogFinalIncumbentGenerationThrowForUnknownDirectory(bool useLogFinishedGeneration)
        {
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetLogFilePath("foo/bar.txt")
                .Build(1);
            var writer = new LogWriter<InstanceFile, RuntimeResult>(this._parameterTree, configuration);

            if (useLogFinishedGeneration)
            {
                Assert.Throws<DirectoryNotFoundException>(
                    () => writer.LogFinishedGeneration(1, 1, this._testGenomeResults, true));
            }
            else
            {
                Assert.Throws<DirectoryNotFoundException>(
                    () => writer.LogFinalIncumbentGeneration(1, this._testGenomeResults, 1, 2, true));
            }
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> logs the finished generation to file.
        /// </summary>
        [Fact]
        public void LogFinishedGenerationLogsGenerationCorrectly()
        {
            this._writer.LogFinishedGeneration(3, 1, this._testGenomeResults, true);

            var linesInFile = File.ReadLines(LogWriterTest.LogFilePath);
            Assert.Equal(
                "Finished generation 3 / 24",
                linesInFile.First());
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinalIncumbentGeneration"/> logs the finished generation to file.
        /// </summary>
        [Fact]
        public void LogFinalIncumbentGenerationLogsGenerationCorrectly()
        {
            this._writer.LogFinalIncumbentGeneration(1, this._testGenomeResults, 1, 2, true);

            var linesInFile = File.ReadLines(LogWriterTest.LogFilePath);
            Assert.Equal(
                "Finished final incumbent generation",
                linesInFile.First());
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> and <see cref="LogWriter{I,R}.LogFinalIncumbentGeneration"/> log the total number of evaluations if an
        /// evaluation limit exists.
        /// </summary>
        /// <param name="useLogFinishedGeneration">Whether to use <see cref="LogWriter{I,R}.LogFinishedGeneration"/>.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LogFinishedGenerationAndLogFinalIncumbentGenerationLogTotalNumberOfEvaluationsIfDesired(bool useLogFinishedGeneration)
        {
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetLogFilePath(LogWriterTest.LogFilePath)
                .SetEvaluationLimit(25)
                .Build(1);

            var writer = new LogWriter<InstanceFile, RuntimeResult>(this._parameterTree, configuration);

            if (useLogFinishedGeneration)
            {
                writer.LogFinishedGeneration(1, 3, this._testGenomeResults, true);
            }
            else
            {
                writer.LogFinalIncumbentGeneration(3, this._testGenomeResults, 1, 2, true);
            }

            var linesInFile = File.ReadLines(LogWriterTest.LogFilePath);
            Assert.Equal(
                "Evaluations: 3 / 25",
                linesInFile.ElementAt(1));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> and <see cref="LogWriter{I,R}.LogFinalIncumbentGeneration"/> do not log the total number of evaluations
        /// if no evaluation limit exists.
        /// </summary>
        /// <param name="useLogFinishedGeneration">Whether to use <see cref="LogWriter{I,R}.LogFinishedGeneration"/>.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LogFinishedGenerationAndLogFinalIncumbentGenerationDoNotLogTotalNumberOfEvaluationsIfNotDesired(bool useLogFinishedGeneration)
        {
            if (useLogFinishedGeneration)
            {
                this._writer.LogFinishedGeneration(1, 3, this._testGenomeResults, true);
            }
            else
            {
                this._writer.LogFinalIncumbentGeneration(3, this._testGenomeResults, 1, 2, true);
            }

            var linesInFile = File.ReadLines(LogWriterTest.LogFilePath);
            Assert.NotEqual(
                "Evaluations: 3 / 25",
                linesInFile.ElementAt(1));
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> and <see cref="LogWriter{I,R}.LogFinalIncumbentGeneration"/> log the elapsed time to file.
        /// </summary>
        /// <param name="useLogFinishedGeneration">Whether to use <see cref="LogWriter{I,R}.LogFinishedGeneration"/>.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LogFinishedGenerationAndLogFinalIncumbentGenerationLogElapsedTimeCorrectly(bool useLogFinishedGeneration)
        {
            Thread.Sleep(1000);

            if (useLogFinishedGeneration)
            {
                this._writer.LogFinishedGeneration(1, 1, this._testGenomeResults, true);
            }
            else
            {
                this._writer.LogFinalIncumbentGeneration(1, this._testGenomeResults, 1, 2, true);
            }

            var linesInFile = File.ReadLines(LogWriterTest.LogFilePath);
            var passedTime = DateTime.Now.ToUniversalTime() - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            var loggedTime = TimeSpan.Parse(linesInFile.ElementAt(1).Split(' ').Last());

            Assert.True(Math.Abs(passedTime.TotalSeconds - loggedTime.TotalSeconds) < passedTime.TotalSeconds * 0.05);
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinishedGeneration"/> logs the given information about the fittest genome to file.
        /// </summary>
        [Fact]
        public void LogFinishedGenerationLogsGenomeInformationCorrectly()
        {
            this._writer.LogFinishedGeneration(1, 1, this._testGenomeResults, true);

            // Skip first two lines as they do not describe the genome.
            var relevantLinesInFile = File.ReadLines(LogWriterTest.LogFilePath).Skip(2).ToList();
            relevantLinesInFile.Count.ShouldBe(8);
            Assert.Equal($"Fittest genome's age: {this._testGenomeResults.Genome.Age}", relevantLinesInFile[0]);
            Assert.Equal("Fittest genome is default genome: True", relevantLinesInFile[1]);
            Assert.Equal("Fittest genome according to last tournament:", relevantLinesInFile[2]);
            Assert.Equal(
                $"\tdecision: {this._testGenomeResults.Genome.CreateMutableGenome().GetGeneValue("decision")}",
                relevantLinesInFile[3]);
            Assert.Equal(
                $"\tb: {this._testGenomeResults.Genome.CreateMutableGenome().GetGeneValue("b")}",
                relevantLinesInFile[4]);
            // Inactive parameter 'a' should not be printed.
            Assert.Equal(
                "Fittest genome's results on instances so far:",
                relevantLinesInFile[5]);
            Assert.Equal(
                FormattableString.Invariant($"\ta:\t{TimeSpan.FromMilliseconds(42):G}"),
                relevantLinesInFile[6]);
            Assert.Equal(
                FormattableString.Invariant($"\tfoo/bar:\tCancelled after {TimeSpan.FromMilliseconds(11):G}"),
                relevantLinesInFile[7]);
        }

        /// <summary>
        /// Checks that <see cref="LogWriter{I,R}.LogFinalIncumbentGeneration"/> logs the given information about the fittest genome to file.
        /// </summary>
        [Fact]
        public void LogFinalIncumbentGenerationLogsGenomeInformationCorrectly()
        {
            this._writer.LogFinalIncumbentGeneration(1, this._testGenomeResults, null, null, true);

            // Skip first two lines as they do not describe the genome.
            var relevantLinesInFile = File.ReadLines(LogWriterTest.LogFilePath).Skip(2).ToList();
            relevantLinesInFile.Count.ShouldBe(9);
            Assert.Equal("Fittest genome's first generation as incumbent: none", relevantLinesInFile[0]);
            Assert.Equal("Fittest genome's last generation as incumbent: none", relevantLinesInFile[1]);
            Assert.Equal("Fittest genome is default genome: True", relevantLinesInFile[2]);
            Assert.Equal("Fittest genome according to final incumbent generation:", relevantLinesInFile[3]);
            Assert.Equal(
                $"\tdecision: {this._testGenomeResults.Genome.CreateMutableGenome().GetGeneValue("decision")}",
                relevantLinesInFile[4]);
            Assert.Equal(
                $"\tb: {this._testGenomeResults.Genome.CreateMutableGenome().GetGeneValue("b")}",
                relevantLinesInFile[5]);
            // Inactive parameter 'a' should not be printed.
            Assert.Equal(
                "Fittest genome's results on instances:",
                relevantLinesInFile[6]);
            Assert.Equal(
                FormattableString.Invariant($"\ta:\t{TimeSpan.FromMilliseconds(42):G}"),
                relevantLinesInFile[7]);
            Assert.Equal(
                FormattableString.Invariant($"\tfoo/bar:\tCancelled after {TimeSpan.FromMilliseconds(11):G}"),
                relevantLinesInFile[8]);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a parameter tree which root is an OR node "decision".
        /// If that is true, a value node "a" is evaluated, otherwise a value node "b".
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