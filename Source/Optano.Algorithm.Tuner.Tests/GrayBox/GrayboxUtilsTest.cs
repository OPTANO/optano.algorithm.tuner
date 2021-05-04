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

namespace Optano.Algorithm.Tuner.Tests.GrayBox
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Optano.Algorithm.Tuner.GrayBox;
    using Optano.Algorithm.Tuner.GrayBox.CsvRecorder;
    using Optano.Algorithm.Tuner.GrayBox.DataRecordTypes;
    using Optano.Algorithm.Tuner.MachineLearning.GenomeRepresentation;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.Tests.GrayBox.DataRecordTypes;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GrayBoxUtils"/> class.
    /// </summary>
    public class GrayBoxUtilsTest : IDisposable
    {
        #region Constants

        /// <summary>
        /// The path to the real test data directory.
        /// </summary>
        private const string PathToRealTestDataDirectory = @"GrayBox/TestData";

        #endregion

        #region Static Fields

        /// <summary>
        /// The broken data log, used in tests.
        /// </summary>
        private static readonly FileInfo BrokenDataLogFile =
            new FileInfo(Path.Combine(GrayBoxUtilsTest.PathToRealTestDataDirectory, "brokenDataLog.csv"));

        /// <summary>
        /// The valid data log, used in tests.
        /// </summary>
        private static readonly FileInfo ValidDataLogFile = new FileInfo(
            Path.Combine(GrayBoxUtilsTest.PathToRealTestDataDirectory, "dataLog_generation_0_process_123_id_0_Finished.csv"));

        /// <summary>
        /// The file, containing some genome instance pairs, used in tests.
        /// </summary>
        private static readonly FileInfo GenomeInstancePairsFile =
            new FileInfo(Path.Combine(GrayBoxUtilsTest.PathToRealTestDataDirectory, "genomeInstancePairs.csv"));

        /// <summary>
        /// The file, containing the generation genome composition, used in tests.
        /// </summary>
        private static readonly FileInfo GenerationGenomeCompositionFile =
            new FileInfo(Path.Combine(GrayBoxUtilsTest.PathToRealTestDataDirectory, "generationGenomeComposition.csv"));

        /// <summary>
        /// The file, containing the generation instance composition, used in tests.
        /// </summary>
        private static readonly FileInfo GenerationInstanceCompositionFile =
            new FileInfo(Path.Combine(GrayBoxUtilsTest.PathToRealTestDataDirectory, "generationInstanceComposition.csv"));

        /// <summary>
        /// The dummy test data record directory.
        /// </summary>
        private static readonly DirectoryInfo DummyTestDataRecordDirectory =
            new DirectoryInfo(PathUtils.GetAbsolutePathFromCurrentDirectory("TestDataRecordDirectory"));

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GrayBoxUtilsTest"/> class.
        /// Behaves like the [TestInitialize] of MSTest framework.
        /// </summary>
        public GrayBoxUtilsTest()
        {
            GrayBoxUtilsTest.DummyTestDataRecordDirectory.Create();
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public void Dispose()
        {
            if (Directory.Exists(GrayBoxUtilsTest.DummyTestDataRecordDirectory.FullName))
            {
                GrayBoxUtilsTest.DummyTestDataRecordDirectory.Delete(recursive: true);
            }
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.GetAndCheckLine"/> throws a <see cref="CsvDelimiterException"/>, if a line element contains the delimiter.
        /// </summary>
        [Fact]
        public void GetAndCheckLineThrowsIfDelimiterInElement()
        {
            var lineElements = new[]
                                   {
                                       "ele,ment",
                                       "element",
                                   };
            Assert.Throws<CsvDelimiterException>(() => GrayBoxUtils.GetAndCheckLine(lineElements, ','));
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.TryToGetGenerationIdFromDataLogFileName"/> returns the correct output.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="correctBoolean">The correct boolean.</param>
        /// <param name="correctGeneration">The correct generation.</param>
        [Theory]
        [InlineData("dataLog_generation_-100_process_0_id_0_Finished_1234.csv", true, -100)]
        [InlineData("dataLog_generation_100_process_0_id_0_Finished_1234.csv", true, 100)]
        [InlineData("dataLog_generation_-100_process_0_id_0_Finished.csv", true, -100)]
        [InlineData("dataLog_generation_100_process_0_id_0_Finished.csv", true, 100)]
        [InlineData("dataLog_generation_x_process_0_id_0_Finished.csv", false, 0)]
        public void TryToGetGenerationIdFromDataLogFileNameReturnsCorrectOutput(string fileName, bool correctBoolean, int correctGeneration)
        {
            var boolean = GrayBoxUtils.TryToGetGenerationIdFromDataLogFileName(fileName, out var generation);
            boolean.ShouldBe(correctBoolean);
            generation.ShouldBe(correctGeneration);
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.MoveOldDataLogFiles"/> moves the correct files.
        /// </summary>
        /// <param name="tuningStartsFromExistingStatus">Bool, indicating whether the tuning starts from an existing status.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MoveOldDataLogFilesMovesCorrectFiles(bool tuningStartsFromExistingStatus)
        {
            // Declare target directory.
            var pathToTargetDirectory = Path.Combine(GrayBoxUtilsTest.DummyTestDataRecordDirectory.FullName, "OldDataLogFiles");
            Directory.Exists(pathToTargetDirectory).ShouldBeFalse();

            // Create files.
            var postTuningDataLogFile =
                new FileInfo(
                    Path.Combine(GrayBoxUtilsTest.DummyTestDataRecordDirectory.FullName, "dataLog_generation_-1_process_1_id_0_Finished.csv"));
            var pastDataLogFile = new FileInfo(
                Path.Combine(GrayBoxUtilsTest.DummyTestDataRecordDirectory.FullName, "dataLog_generation_0_process_1_id_0_Finished.csv"));
            var futureDataLogFile =
                new FileInfo(
                    Path.Combine(GrayBoxUtilsTest.DummyTestDataRecordDirectory.FullName, "dataLog_generation_1_process_1_id_0_Finished.csv"));
            var brokenDataLogFile = new FileInfo(Path.Combine(GrayBoxUtilsTest.DummyTestDataRecordDirectory.FullName, "dataLog.csv"));
            GrayBoxUtilsTest.CreateFileAndCloseIt(postTuningDataLogFile.FullName);
            GrayBoxUtilsTest.CreateFileAndCloseIt(pastDataLogFile.FullName);
            GrayBoxUtilsTest.CreateFileAndCloseIt(futureDataLogFile.FullName);
            GrayBoxUtilsTest.CreateFileAndCloseIt(brokenDataLogFile.FullName);

            // Move old data log files to target directory.
            GrayBoxUtils.MoveOldDataLogFiles(
                GrayBoxUtilsTest.DummyTestDataRecordDirectory.FullName,
                pathToTargetDirectory,
                tuningStartsFromExistingStatus,
                0);

            // Check the data record directory.
            File.Exists(postTuningDataLogFile.FullName).ShouldBeFalse();
            File.Exists(futureDataLogFile.FullName).ShouldBeFalse();
            File.Exists(brokenDataLogFile.FullName).ShouldBeTrue();
            if (tuningStartsFromExistingStatus)
            {
                File.Exists(pastDataLogFile.FullName).ShouldBeTrue();
            }
            else
            {
                File.Exists(pastDataLogFile.FullName).ShouldBeFalse();
            }

            // Check the target directory.
            Directory.Exists(pathToTargetDirectory).ShouldBeTrue();
            File.Exists(Path.Combine(pathToTargetDirectory, postTuningDataLogFile.Name)).ShouldBeTrue();
            File.Exists(Path.Combine(pathToTargetDirectory, futureDataLogFile.Name)).ShouldBeTrue();
            File.Exists(Path.Combine(pathToTargetDirectory, brokenDataLogFile.Name)).ShouldBeFalse();
            if (tuningStartsFromExistingStatus)
            {
                File.Exists(Path.Combine(pathToTargetDirectory, pastDataLogFile.Name)).ShouldBeFalse();
            }
            else
            {
                File.Exists(Path.Combine(pathToTargetDirectory, pastDataLogFile.Name)).ShouldBeTrue();
            }
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.ExportGenerationComposition{TInstance}"/> writes correct lines.
        /// </summary>
        [Fact]
        public void ExportGenerationCompositionWritesCorrectLines()
        {
            const int TotalNumberOfLines = 3;

            var listOfGenomes = new List<GenomeDoubleRepresentation>
                                    { (GenomeDoubleRepresentation)new[] { 1.0d, 2.0d }, (GenomeDoubleRepresentation)new[] { 3.0d, 4.0d } };
            var listOfInstances = new List<TestInstance> { new TestInstance("TestInstance_1"), new TestInstance("TestInstance_2") };

            var generationGenomeCompositionFile = new FileInfo(
                Path.Combine(GrayBoxUtilsTest.DummyTestDataRecordDirectory.FullName, "generationGenomeComposition.csv"));
            var generationInstanceCompositionFile = new FileInfo(
                Path.Combine(GrayBoxUtilsTest.DummyTestDataRecordDirectory.FullName, "generationInstanceComposition.csv"));
            File.Exists(generationGenomeCompositionFile.FullName).ShouldBeFalse();
            File.Exists(generationInstanceCompositionFile.FullName).ShouldBeFalse();

            for (var outerIndex = 0; outerIndex < TotalNumberOfLines; outerIndex++)
            {
                GrayBoxUtils.ExportGenerationComposition(
                    listOfInstances,
                    listOfGenomes,
                    generationInstanceCompositionFile,
                    generationGenomeCompositionFile);

                File.Exists(generationInstanceCompositionFile.FullName).ShouldBeTrue();
                File.Exists(generationGenomeCompositionFile.FullName).ShouldBeTrue();

                var linesInGenomeFile = File.ReadLines(generationGenomeCompositionFile.FullName).ToList();
                Assert.Equal(outerIndex + 1, linesInGenomeFile.Count());
                var linesInInstanceFile = File.ReadLines(generationInstanceCompositionFile.FullName).ToList();
                Assert.Equal(outerIndex + 1, linesInInstanceFile.Count());

                for (var innerIndex = 0; innerIndex < outerIndex; innerIndex++)
                {
                    Assert.Equal(
                        "[1.0,2.0];[3.0,4.0]",
                        linesInGenomeFile.ElementAt(innerIndex));
                    Assert.Equal(
                        "TestInstance_1;TestInstance_2",
                        linesInInstanceFile.ElementAt(innerIndex));
                }
            }
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.TryToMoveFile"/> moves the file, if existing.
        /// </summary>
        [Fact]
        public void TryToMoveFileMovesFile()
        {
            GrayBoxUtilsTest.DummyTestDataRecordDirectory.GetFiles().Length.ShouldBe(0);

            var file = new FileInfo(Path.Combine(GrayBoxUtilsTest.DummyTestDataRecordDirectory.FullName, "file.csv"));
            var targetFile = new FileInfo(Path.Combine(GrayBoxUtilsTest.DummyTestDataRecordDirectory.FullName, "targetFile.csv"));
            GrayBoxUtilsTest.CreateFileAndCloseIt(file.FullName);

            GrayBoxUtilsTest.DummyTestDataRecordDirectory.GetFiles().Length.ShouldBe(1);
            File.Exists(file.FullName).ShouldBeTrue();
            File.Exists(targetFile.FullName).ShouldBeFalse();

            GrayBoxUtils.TryToMoveFile(file, targetFile);

            GrayBoxUtilsTest.DummyTestDataRecordDirectory.GetFiles().Length.ShouldBe(1);
            File.Exists(file.FullName).ShouldBeFalse();
            File.Exists(targetFile.FullName).ShouldBeTrue();
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.TryToReadGenerationCompositionFromFile"/> returns the correct generation instance composition.
        /// </summary>
        [Fact]
        public void TryToReadGenerationInstanceCompositionFromFileReturnsCorrectOutput()
        {
            GrayBoxUtils.TryToReadGenerationCompositionFromFile(
                GrayBoxUtilsTest.GenerationInstanceCompositionFile,
                out var generationInstanceComposition).ShouldBeTrue();

            // Check counts.
            generationInstanceComposition.Count.ShouldBe(3);
            generationInstanceComposition[0].Count.ShouldBe(5);
            generationInstanceComposition[1].Count.ShouldBe(46);
            generationInstanceComposition[2].Count.ShouldBe(50);

            // Check some elements.
            generationInstanceComposition[0][0].ShouldBe("instance_0197.mps_361709742");
            generationInstanceComposition[1][10].ShouldBe("instance_0197.mps_269548474");
            generationInstanceComposition[2][49].ShouldBe("instance_0152.mps_269548474");
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.TryToReadGenerationCompositionFromFile"/> returns the correct generation genome composition.
        /// </summary>
        [Fact]
        public void TryToReadGenerationGenomeCompositionFromFileReturnsCorrectOutput()
        {
            GrayBoxUtils.TryToReadGenerationCompositionFromFile(
                GrayBoxUtilsTest.GenerationGenomeCompositionFile,
                out var generationGenomeComposition).ShouldBeTrue();

            // Check counts.
            generationGenomeComposition.Count.ShouldBe(3);
            generationGenomeComposition[0].Count.ShouldBe(9);
            generationGenomeComposition[1].Count.ShouldBe(9);
            generationGenomeComposition[2].Count.ShouldBe(9);

            // Check some elements.
            generationGenomeComposition[0][0].ShouldBe("[0.0,0.0]");
            generationGenomeComposition[1][2].ShouldBe("[0.0,0.0]");
            generationGenomeComposition[2][8].ShouldBe("[1.0,1.0]");
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.ConvertGenerationGenomeComposition"/> returns the correct output.
        /// </summary>
        [Fact]
        public void ConvertGenerationGenomeCompositionReturnsCorrectOutput()
        {
            var parameterTree = GrayBoxUtilsTest.CreateDummyParameterTree();

            // Read generation genome composition from file and convert it.
            GrayBoxUtils.TryToReadGenerationCompositionFromFile(
                GrayBoxUtilsTest.GenerationGenomeCompositionFile,
                out var generationGenomeComposition).ShouldBeTrue();
            var (convertedGenerationGenomeComposition, genomeStringDictionary) =
                GrayBoxUtils.ConvertGenerationGenomeComposition(generationGenomeComposition, parameterTree);

            // Check counts.
            genomeStringDictionary.Count.ShouldBe(9);
            convertedGenerationGenomeComposition.Count.ShouldBe(3);
            convertedGenerationGenomeComposition[0].Count.ShouldBe(9);
            convertedGenerationGenomeComposition[1].Count.ShouldBe(9);
            convertedGenerationGenomeComposition[2].Count.ShouldBe(9);

            // Check all elements.
            var flatenGenerationGenomeComposition = generationGenomeComposition.SelectMany(x => x).ToList();
            var flatenConvertedGenerationGenomeComposition = convertedGenerationGenomeComposition.SelectMany(x => x).ToList();
            for (var index = 0; index < flatenConvertedGenerationGenomeComposition.Count; index++)
            {
                genomeStringDictionary[flatenConvertedGenerationGenomeComposition[index]].ShouldBe(flatenGenerationGenomeComposition[index]);
            }
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.ConvertGenerationInstanceComposition{A, I, R}"/> returns the correct output.
        /// </summary>
        [Fact]
        public void ConvertGenerationInstanceCompositionReturnsCorrectOutput()
        {
            var targetAlgorithmFactory =
                new TargetAlgorithmFactoryTest.DummyTargetAlgorithmFactory<
                        TargetAlgorithmFactoryTest.DummyTargetAlgorithm<InstanceSeedFile, TestResult>, InstanceSeedFile, TestResult>() as
                    ITargetAlgorithmFactory<TargetAlgorithmFactoryTest.DummyTargetAlgorithm<InstanceSeedFile, TestResult>, InstanceSeedFile,
                        TestResult>;

            // Read generation instance composition from file and convert it.
            GrayBoxUtils.TryToReadGenerationCompositionFromFile(
                GrayBoxUtilsTest.GenerationInstanceCompositionFile,
                out var generationInstanceComposition).ShouldBeTrue();
            var (convertedGenerationInstanceComposition, instanceStringDictionary) =
                GrayBoxUtils.ConvertGenerationInstanceComposition(generationInstanceComposition, targetAlgorithmFactory);

            // Check counts.
            instanceStringDictionary.Count.ShouldBe(50);
            convertedGenerationInstanceComposition.Count.ShouldBe(3);
            convertedGenerationInstanceComposition[0].Count.ShouldBe(5);
            convertedGenerationInstanceComposition[1].Count.ShouldBe(46);
            convertedGenerationInstanceComposition[2].Count.ShouldBe(50);

            // Check all elements.
            var flatenGenerationInstanceComposition = generationInstanceComposition.SelectMany(x => x).ToList();
            var flatenConvertedGenerationInstanceComposition = convertedGenerationInstanceComposition.SelectMany(x => x).ToList();
            for (var index = 0; index < flatenConvertedGenerationInstanceComposition.Count; index++)
            {
                instanceStringDictionary[flatenConvertedGenerationInstanceComposition[index]].ShouldBe(flatenGenerationInstanceComposition[index]);
            }
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.TryToGetHeaderPartitionOfDataRecord{TResult}"/> returns the correct output.
        /// </summary>
        [Fact]
        public void TryToGetHeaderPartitionReturnsCorrectOutput()
        {
            var header = new[]
                             {
                                 "NodeID",
                                 "GenerationID",
                                 "TournamentID",
                                 "RunID",
                                 "InstanceID",
                                 "GenomeID",
                                 "GrayBoxConfidence",
                                 "Genome_GenomeFeature_1",
                                 "Genome_GenomeFeature_2",
                                 "Genome_GenomeFeature_3",
                                 "Genome_GenomeFeature_4",
                                 "FinalResult_TargetAlgorithmStatus",
                                 "FinalResult_Runtime",
                                 "TargetAlgorithmName",
                                 "TargetAlgorithmStatus",
                                 "ExpendedCpuTime",
                                 "ExpendedWallClockTime",
                                 "TimeStamp",
                                 "AdapterFeature_Feature_1",
                                 "AdapterFeature_Feature_2",
                                 "AdapterFeature_Feature_3",
                                 "CurrentGrayBoxResult_TargetAlgorithmStatus",
                                 "CurrentGrayBoxResult_Runtime",
                             };

            GrayBoxUtils.TryToGetHeaderPartitionOfDataRecord<RuntimeResult>(
                header,
                out var genomeHeader,
                out var adapterFeatureHeader,
                out var numberOfResultColumns).ShouldBeTrue();

            genomeHeader.ShouldBe(
                new[]
                    {
                        "GenomeFeature_1",
                        "GenomeFeature_2",
                        "GenomeFeature_3",
                        "GenomeFeature_4",
                    });

            adapterFeatureHeader.ShouldBe(
                new[]
                    {
                        "Feature_1",
                        "Feature_2",
                        "Feature_3",
                    });

            numberOfResultColumns.ShouldBe(2);
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.TryToGetHeaderPartitionOfDataRecord{TResult}"/> returns false for broken header.
        /// </summary>
        [Fact]
        public void TryToGetHeaderPartitionReturnsFalseForBrokenHeader()
        {
            var header = new[]
                             {
                                 "NodeID",
                                 "GenerationID",
                                 "TournamentID",
                                 "RunID",
                                 "InstanceID",
                                 "GenomeID",
                                 "GrayBoxConfidence",
                                 "Genome_GenomeFeature_1",
                                 // Add broken line.
                                 "Gen_GenomeFeature_2",
                                 "Genome_GenomeFeature_3",
                                 "Genome_GenomeFeature_4",
                                 "FinalResult_TargetAlgorithmStatus",
                                 "FinalResult_Runtime",
                                 "TargetAlgorithmName",
                                 "TargetAlgorithmStatus",
                                 "ExpendedCpuTime",
                                 "ExpendedWallClockTime",
                                 "TimeStamp",
                                 "AdapterFeature_Feature_1",
                                 "AdapterFeature_Feature_2",
                                 "AdapterFeature_Feature_3",
                                 "CurrentGrayBoxResult_TargetAlgorithmStatus",
                                 "CurrentGrayBoxResult_Runtime",
                             };

            GrayBoxUtils.TryToGetHeaderPartitionOfDataRecord<RuntimeResult>(
                header,
                out var genomeHeader,
                out var adapterFeatureHeader,
                out var numberOfResultColumns).ShouldBeFalse();
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.TryToReadDataRecordsFromFile{TTargetAlgorithm, TInstance, TResult}"/> works for valid files.
        /// </summary>
        [Fact]
        public void TryToReadDataRecordsFromFileWorksForValidFiles()
        {
            var targetAlgorithmFactory =
                new TargetAlgorithmFactoryTest.DummyTargetAlgorithmFactory<
                    TargetAlgorithmFactoryTest.DummyTargetAlgorithm<TestInstance, RuntimeResult>, TestInstance, RuntimeResult>();
            GrayBoxUtils.TryToReadDataRecordsFromFile(targetAlgorithmFactory, GrayBoxUtilsTest.ValidDataLogFile, out var dataRecords).ShouldBeTrue();

            // Check count.
            dataRecords.Count.ShouldBe(1);

            // Check all values.
            GrayBoxUtilsTest.CheckDataRecordValues(dataRecords[0]);
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.TryToReadDataRecordsFromFile{TTargetAlgorithm, TInstance, TResult}"/> returns false for broken files.
        /// </summary>
        [Fact]
        public void TryToReadDataRecordsFromFileReturnsFalseForBrokenFiles()
        {
            var targetAlgorithmFactory =
                new TargetAlgorithmFactoryTest.DummyTargetAlgorithmFactory<
                    TargetAlgorithmFactoryTest.DummyTargetAlgorithm<TestInstance, RuntimeResult>, TestInstance, RuntimeResult>();
            GrayBoxUtils.TryToReadDataRecordsFromFile(targetAlgorithmFactory, GrayBoxUtilsTest.BrokenDataLogFile, out var dataRecords)
                .ShouldBeFalse();
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.TryToReadDataRecordsFromDirectory{TTargetAlgorithm, TInstance, TResult}"/> reads the correct data records.
        /// </summary>
        [Fact]
        public void TryToReadDataRecordsFromDirectoryReturnsCorrectOutput()
        {
            var targetAlgorithmFactory =
                new TargetAlgorithmFactoryTest.DummyTargetAlgorithmFactory<
                    TargetAlgorithmFactoryTest.DummyTargetAlgorithm<TestInstance, RuntimeResult>, TestInstance, RuntimeResult>();
            GrayBoxUtils.TryToReadDataRecordsFromDirectory(
                    targetAlgorithmFactory,
                    GrayBoxUtilsTest.PathToRealTestDataDirectory,
                    0,
                    1,
                    out var dataRecords)
                .ShouldBeTrue();

            // Check count.
            dataRecords.Count.ShouldBe(2);

            // Check all values.
            foreach (var dataRecord in dataRecords)
            {
                GrayBoxUtilsTest.CheckDataRecordValues(dataRecord);
            }
        }

        /// <summary>
        /// Checks, that <see cref="GrayBoxUtils.TryToReadGenomeInstancePairsFromFile"/> reads the correct pairs.
        /// </summary>
        [Fact]
        public void TryToReadGenomeInstancePairsFromFileReadsCorrectPairs()
        {
            var correctPairs = new List<GenomeInstancePairStringRepresentation>
                                   {
                                       new GenomeInstancePairStringRepresentation("[5.0,12.0]", "Instance_1"),
                                       new GenomeInstancePairStringRepresentation("[5.0,12.0]", "Instance_2"),
                                       new GenomeInstancePairStringRepresentation("[15.0,22.0]", "Instance_2"),
                                   };

            GrayBoxUtils.TryToReadGenomeInstancePairsFromFile(GrayBoxUtilsTest.GenomeInstancePairsFile, out var readPairs)
                .ShouldBeTrue();
            Assert.Equal(3, readPairs.Count);
            Assert.Equal(correctPairs, readPairs);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a file and closes it afterwards.
        /// </summary>
        /// <param name="path">The path.</param>
        private static void CreateFileAndCloseIt(string path)
        {
            var file = File.Create(path);
            file.Close();
        }

        /// <summary>
        /// Checks the given data record values.
        /// </summary>
        /// <param name="dataRecord">The data record.</param>
        private static void CheckDataRecordValues(DataRecord<RuntimeResult> dataRecord)
        {
            dataRecord.GenomeInstancePair.ShouldBe(DataRecordTest.DataRecord.GenomeInstancePair);
            dataRecord.GrayBoxLabel.ShouldBe(DataRecordTest.DataRecord.GrayBoxLabel);
            dataRecord.IsCancelledByGrayBoxDuringGrayBoxSimulation
                .ShouldBe(DataRecordTest.DataRecord.IsCancelledByGrayBoxDuringGrayBoxSimulation);
            dataRecord.TunerDataRecord.NodeId.ShouldBe(DataRecordTest.DataRecord.TunerDataRecord.NodeId);
            dataRecord.TunerDataRecord.GenerationId.ShouldBe(DataRecordTest.DataRecord.TunerDataRecord.GenerationId);
            dataRecord.TunerDataRecord.TournamentId.ShouldBe(DataRecordTest.DataRecord.TunerDataRecord.TournamentId);
            dataRecord.TunerDataRecord.RunId.ShouldBe(DataRecordTest.DataRecord.TunerDataRecord.RunId);
            dataRecord.TunerDataRecord.InstanceId.ShouldBe(DataRecordTest.DataRecord.TunerDataRecord.InstanceId);
            dataRecord.TunerDataRecord.GenomeId.ShouldBe(DataRecordTest.DataRecord.TunerDataRecord.GenomeId);
            dataRecord.TunerDataRecord.GrayBoxConfidence.ShouldBe(DataRecordTest.DataRecord.TunerDataRecord.GrayBoxConfidence);
            dataRecord.TunerDataRecord.Genome.ShouldBe(DataRecordTest.DataRecord.TunerDataRecord.Genome);
            dataRecord.TunerDataRecord.FinalResult.TargetAlgorithmStatus
                .ShouldBe(DataRecordTest.DataRecord.TunerDataRecord.FinalResult.TargetAlgorithmStatus);
            dataRecord.TunerDataRecord.FinalResult.Runtime.ShouldBe(DataRecordTest.DataRecord.TunerDataRecord.FinalResult.Runtime);
            dataRecord.TunerDataRecord.FinalResult.IsCancelled.ShouldBe(DataRecordTest.DataRecord.TunerDataRecord.FinalResult.IsCancelled);
            dataRecord.AdapterDataRecord.TargetAlgorithmName.ShouldBe(DataRecordTest.DataRecord.AdapterDataRecord.TargetAlgorithmName);
            dataRecord.AdapterDataRecord.TargetAlgorithmStatus.ShouldBe(DataRecordTest.DataRecord.AdapterDataRecord.TargetAlgorithmStatus);
            dataRecord.AdapterDataRecord.ExpendedCpuTime.ShouldBe(DataRecordTest.DataRecord.AdapterDataRecord.ExpendedCpuTime);
            dataRecord.AdapterDataRecord.ExpendedWallClockTime.ShouldBe(DataRecordTest.DataRecord.AdapterDataRecord.ExpendedWallClockTime);
            dataRecord.AdapterDataRecord.TimeStamp.ShouldBe(DataRecordTest.DataRecord.AdapterDataRecord.TimeStamp);
            dataRecord.AdapterDataRecord.AdapterFeatures.SequenceEqual(DataRecordTest.DataRecord.AdapterDataRecord.AdapterFeatures)
                .ShouldBeTrue();
            dataRecord.AdapterDataRecord.CurrentGrayBoxResult.TargetAlgorithmStatus
                .ShouldBe(DataRecordTest.DataRecord.AdapterDataRecord.CurrentGrayBoxResult.TargetAlgorithmStatus);
            dataRecord.AdapterDataRecord.CurrentGrayBoxResult.Runtime
                .ShouldBe(DataRecordTest.DataRecord.AdapterDataRecord.CurrentGrayBoxResult.Runtime);
            dataRecord.AdapterDataRecord.CurrentGrayBoxResult.IsCancelled
                .ShouldBe(DataRecordTest.DataRecord.AdapterDataRecord.CurrentGrayBoxResult.IsCancelled);
        }

        /// <summary>
        /// Creates a dummy <see cref="ParameterTree"/>.
        /// </summary>
        /// <returns>The <see cref="ParameterTree"/>.</returns>
        private static ParameterTree CreateDummyParameterTree()
        {
            var firstNode = new ValueNode<double>(
                "FirstValue",
                new ContinuousDomain(minimum: 0, maximum: 1));
            var secondNode = new ValueNode<double>(
                "SecondValue",
                new ContinuousDomain(minimum: 0, maximum: 1));

            var rootNode = new AndNode();
            rootNode.AddChild(firstNode);
            rootNode.AddChild(secondNode);

            var parameterTree = new ParameterTree(rootNode);
            return parameterTree;
        }

        #endregion
    }
}