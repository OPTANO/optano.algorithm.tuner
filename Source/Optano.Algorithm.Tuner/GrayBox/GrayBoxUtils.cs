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

namespace Optano.Algorithm.Tuner.GrayBox
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.GrayBox.CsvRecorder;
    using Optano.Algorithm.Tuner.GrayBox.DataRecordTypes;
    using Optano.Algorithm.Tuner.GrayBox.PostTuningRunner;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning.GenomeRepresentation;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    using SharpLearning.Containers.Matrices;
    using SharpLearning.RandomForest.Models;

    using TinyCsvParser;

    /// <summary>
    /// Contains useful methods for gray box tuning.
    /// </summary>
    public static class GrayBoxUtils
    {
        #region Public properties

        /// <summary>
        /// Gets the data log file name regex.
        /// </summary>
        public static Regex DataLogFileNameRegex => new Regex(@"dataLog_generation_(-?[0-9]+)_process_([0-9]+)_id_([0-9]+)_.*\.csv");

        /// <summary>
        /// Gets the data recorder delimiter.
        /// </summary>
        public static char DataRecorderDelimiter => ';';

        /// <summary>
        /// Gets the gray box label of timeout class.
        /// </summary>
        public static int GrayBoxLabelOfTimeouts => 1;

        /// <summary>
        /// Gets the gray box label of non timeout class.
        /// </summary>
        public static int GrayBoxLabelOfNonTimeouts => 0;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns the data log file name, which matches the <see cref="DataLogFileNameRegex"/>.
        /// </summary>
        /// <param name="generationId">The generation id.</param>
        /// <param name="processId">The process id.</param>
        /// <param name="actorId">The actor id.</param>
        /// <param name="targetAlgorithmStatus">The target algorithm status.</param>
        /// <returns>The data log file name.</returns>
        public static string GetDataLogFileName(int generationId, int processId, int actorId, TargetAlgorithmStatus targetAlgorithmStatus)
        {
            return $"dataLog_generation_{generationId}_process_{processId}_id_{actorId}_{targetAlgorithmStatus}.csv";
        }

        /// <summary>
        /// Exports the generation composition to the corresponding files.
        /// </summary>
        /// <typeparam name="TInstance">The instance type.</typeparam>
        /// <param name="listOfCurrentInstances"> The list of current instances.</param>
        /// <param name="listOfCurrentGenomes"> The list of current genomes.</param>
        /// <param name="generationInstanceCompositionFile"> The file to write the generation instance composition to.</param>
        /// <param name="generationGenomeCompositionFile"> The file to write the generation genome composition to.</param>
        public static void ExportGenerationComposition<TInstance>(
            IEnumerable<TInstance> listOfCurrentInstances,
            IEnumerable<GenomeDoubleRepresentation> listOfCurrentGenomes,
            FileInfo generationInstanceCompositionFile,
            FileInfo generationGenomeCompositionFile)
            where TInstance : InstanceBase
        {
            if (listOfCurrentInstances == null)
            {
                throw new ArgumentNullException(nameof(listOfCurrentInstances));
            }

            if (listOfCurrentGenomes == null)
            {
                throw new ArgumentNullException(nameof(listOfCurrentGenomes));
            }

            var instanceLine = GrayBoxUtils.GetAndCheckLine(
                listOfCurrentInstances.Select(instance => instance.ToId()).ToArray(),
                GrayBoxUtils.DataRecorderDelimiter);
            var genomeLine = GrayBoxUtils.GetAndCheckLine(
                listOfCurrentGenomes.Select(genome => genome.ToGenomeIdentifierStringRepresentation()).ToArray(),
                GrayBoxUtils.DataRecorderDelimiter);

            File.AppendAllText(
                generationInstanceCompositionFile.FullName,
                $"{instanceLine}{Environment.NewLine}");

            File.AppendAllText(
                generationGenomeCompositionFile.FullName,
                $"{genomeLine}{Environment.NewLine}");
        }

        /// <summary>
        /// Checks the given line elements for the given delimiter and returns the line.
        /// </summary>
        /// <param name="lineElements">The elements to form a line.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <returns>The line.</returns>
        public static string GetAndCheckLine(string[] lineElements, char delimiter)
        {
            foreach (var element in lineElements)
            {
                if (element.Contains(delimiter))
                {
                    throw new CsvDelimiterException($"The element {element} should not contain the delimiter {delimiter}.");
                }
            }

            return string.Join(delimiter, lineElements);
        }

        /// <summary>
        /// Moves the old data log files to another directory.
        /// </summary>
        /// <param name="dataRecordDirectoryPath">The path to the data record directory.</param>
        /// <param name="targetDirectoryPath">The path to the target directory.</param>
        /// <param name="tuningStartsFromExistingStatus">Bool, indicating whether the tuning starts from an existing status.</param>
        /// <param name="currentGeneration">The current generation.</param>
        public static void MoveOldDataLogFiles(
            string dataRecordDirectoryPath,
            string targetDirectoryPath,
            bool tuningStartsFromExistingStatus,
            int currentGeneration)
        {
            if (string.IsNullOrEmpty(targetDirectoryPath))
            {
                throw new ArgumentException("The path to the data record directory cannot be empty!");
            }

            if (string.IsNullOrEmpty(targetDirectoryPath))
            {
                throw new ArgumentException("The path to the target directory cannot be empty!");
            }

            if (!Directory.Exists(dataRecordDirectoryPath))
            {
                return;
            }

            var dataLogFiles = GrayBoxUtils.GetAllDataLogFilesInDirectory(dataRecordDirectoryPath);

            if (dataLogFiles.Count == 0)
            {
                return;
            }

            foreach (var dataLogFile in dataLogFiles)
            {
                // If tuning starts from existing status, move only data log files, which are from future generations or post tuning runs!
                if (tuningStartsFromExistingStatus)
                {
                    if (!GrayBoxUtils.TryToGetGenerationIdFromDataLogFileName(dataLogFile.Name, out var generation))
                    {
                        continue;
                    }

                    if (generation >= 0 && generation <= currentGeneration)
                    {
                        continue;
                    }
                }

                GrayBoxUtils.TryToMoveFile(dataLogFile, new FileInfo(Path.Combine(targetDirectoryPath, dataLogFile.Name)));
            }
        }

        /// <summary>
        /// Tries to get the generation id from the data log file name.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="generation">The generation id.</param>
        /// <returns>True, if successful.</returns>
        public static bool TryToGetGenerationIdFromDataLogFileName(string fileName, out int generation)
        {
            generation = 0;

            if (!GrayBoxUtils.DataLogFileNameRegex.IsMatch(fileName))
            {
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"The name of the file {fileName} does not match the regex {GrayBoxUtils.DataLogFileNameRegex}.");
                return false;
            }

            var generationString = GrayBoxUtils.DataLogFileNameRegex.Match(fileName).Result("$1");

            if (!int.TryParse(generationString, out generation))
            {
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"The name of the file {fileName} matches the regex {GrayBoxUtils.DataLogFileNameRegex}, but does not contain the generation id as integer.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Moves a file with overwriting in a try catch block.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="targetFile">The target file.</param>
        [SuppressMessage(
            "NDepend",
            "ND2016:MethodsPrefixedWithTryShouldReturnABoolean",
            Justification = "This method writes warning instead.")]
        public static void TryToMoveFile(FileInfo sourceFile, FileInfo targetFile)
        {
            if (!File.Exists(sourceFile.FullName))
            {
                return;
            }

            Directory.CreateDirectory(targetFile.DirectoryName!);

            try
            {
                if (File.Exists(targetFile.FullName))
                {
                    File.Delete(targetFile.FullName);
                }

                File.Move(sourceFile.FullName, targetFile.FullName);
                LoggingHelper.WriteLine(VerbosityLevel.Warn, $"Moved existing {sourceFile.FullName} to {targetFile.FullName}.");
            }
            catch
            {
                LoggingHelper.WriteLine(VerbosityLevel.Warn, $"Cannot move existing {sourceFile.FullName} to {targetFile.FullName}.");
            }
        }

        /// <summary>
        /// Tries to read the generation instance or genome composition from a given file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="generationComposition">The generation instance or genome composition.</param>
        /// <returns>True, if successful and file is not empty.</returns>
        public static bool TryToReadGenerationCompositionFromFile(FileInfo file, out List<List<string>> generationComposition)
        {
            try
            {
                generationComposition = File.ReadAllLines(file.FullName).Select(line => line.Split(GrayBoxUtils.DataRecorderDelimiter).ToList())
                    .ToList();
                return generationComposition.Any();
            }
            catch
            {
                generationComposition = null;
                return false;
            }
        }

        /// <summary>
        /// Converts the generation instance composition from a list of strings to a list of instances.
        /// </summary>
        /// <param name="generationInstanceComposition">The generation instance composition.</param>
        /// <param name="targetAlgorithmFactory">The <see cref="ITargetAlgorithmFactory{TTargetAlgorithm, TInstance, TResult}"/>.</param>
        /// <returns>The converted generation instance composition and its corresponding instance string dictionary.</returns>
        /// <typeparam name="TTargetAlgorithm">The target algorithm type.</typeparam>
        /// <typeparam name="TInstance">The instance type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        public static (List<List<TInstance>> instancesPerGeneration, Dictionary<TInstance, string> instanceToId)
            ConvertGenerationInstanceComposition<TTargetAlgorithm, TInstance, TResult>(
                List<List<string>> generationInstanceComposition,
                ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory)
            where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult>
            where TInstance : InstanceBase
            where TResult : ResultBase<TResult>, new()
        {
            var stringInstanceDictionary = new Dictionary<string, TInstance>();
            var convertedGenerationInstanceComposition = new List<List<TInstance>>();

            foreach (var generation in generationInstanceComposition)
            {
                var currentConvertedGenerationInstanceComposition = new List<TInstance>();
                foreach (var element in generation)
                {
                    // If string is already in dictionary, update the converted generation instance composition.
                    if (stringInstanceDictionary.TryGetValue(element, out var knownInstance))
                    {
                        currentConvertedGenerationInstanceComposition.Add(knownInstance);
                    }
                    // Else, transform string to instance and update both, the dictionary and the converted generation instance composition.
                    else
                    {
                        if (!targetAlgorithmFactory.TryToGetInstanceFromInstanceId(element, out var newInstance))
                        {
                            throw new ArgumentException($"Cannot convert given instance id {element} to valid instance.");
                        }

                        stringInstanceDictionary.Add(element, newInstance);
                        currentConvertedGenerationInstanceComposition.Add(newInstance);
                    }
                }

                convertedGenerationInstanceComposition.Add(currentConvertedGenerationInstanceComposition);
            }

            var instanceStringDictionary = stringInstanceDictionary.ToDictionary(x => x.Value, x => x.Key);

            return (convertedGenerationInstanceComposition, instanceStringDictionary);
        }

        /// <summary>
        /// Converts the generation genome composition from a list of strings to a list of <see cref="ImmutableGenome"/>s.
        /// </summary>
        /// <param name="generationGenomeComposition">The generation genome composition.</param>
        /// <param name="parameterTree">The <see cref="ParameterTree"/>.</param>
        /// <returns>The converted generation genome composition and its corresponding genome string dictionary.</returns>
        public static (List<List<ImmutableGenome>> genomesPerGeneration, Dictionary<ImmutableGenome, string> genomeToId)
            ConvertGenerationGenomeComposition(List<List<string>> generationGenomeComposition, ParameterTree parameterTree)
        {
            var stringGenomeDictionary = new Dictionary<string, ImmutableGenome>();
            var convertedGenerationGenomeComposition = new List<List<ImmutableGenome>>();
            var genomeTransformation = new GenomeTransformation<CategoricalBinaryEncoding>(parameterTree);

            foreach (var generation in generationGenomeComposition)
            {
                var currentConvertedGenerationGenomeComposition = new List<ImmutableGenome>();
                foreach (var element in generation)
                {
                    // If string is already in dictionary, update the converted generation genome composition.
                    if (stringGenomeDictionary.TryGetValue(element, out var knownImmutableGenome))
                    {
                        currentConvertedGenerationGenomeComposition.Add(knownImmutableGenome);
                    }
                    // Else, transform string to immutable genome and update both, the dictionary and the converted generation genome composition.
                    else
                    {
                        var genomeDoubleRepresentation =
                            GenomeDoubleRepresentation.GetGenomeDoubleRepresentationFromGenomeIdentifierStringRepresentation(element);
                        var genome = genomeTransformation.ConvertBack(genomeDoubleRepresentation);
                        var newImmutableGenome = new ImmutableGenome(genome);
                        stringGenomeDictionary.Add(element, newImmutableGenome);
                        currentConvertedGenerationGenomeComposition.Add(newImmutableGenome);
                    }
                }

                convertedGenerationGenomeComposition.Add(currentConvertedGenerationGenomeComposition);
            }

            var genomeStringDictionary = stringGenomeDictionary.ToDictionary(x => x.Value, x => x.Key, ImmutableGenome.GenomeComparer);

            return (convertedGenerationGenomeComposition, genomeStringDictionary);
        }

        /// <summary>
        /// Tries to train the gray box random forest.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="trainDataRecords">The list of <see cref="DataRecord{TResult}"/>s, used for training..</param>
        /// <param name="customGrayBoxMethods">The <see cref="ICustomGrayBoxMethods{TResult}"/>.</param>
        /// <param name="currentGeneration">The current generation.</param>
        /// <param name="numberOfThreads">The number of threads.</param>
        /// <param name="grayBoxRandomForest">The gray box random forest.</param>
        /// <returns>True, if successful.</returns>
        public static bool TryToTrainGrayBoxRandomForest<TResult>(
            List<DataRecord<TResult>> trainDataRecords,
            ICustomGrayBoxMethods<TResult> customGrayBoxMethods,
            int currentGeneration,
            int numberOfThreads,
            out ClassificationForestModel grayBoxRandomForest)
            where TResult : ResultBase<TResult>, new()
        {
            if (customGrayBoxMethods == null)
            {
                throw new ArgumentNullException(nameof(customGrayBoxMethods));
            }

            if (trainDataRecords == null)
            {
                throw new ArgumentNullException(nameof(trainDataRecords));
            }

            if (!trainDataRecords.Any())
            {
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Cannot train gray box random forest before generation {currentGeneration}, because the list of training data is empty.");
                grayBoxRandomForest = null;
                return false;
            }

            var trainDataObservations = GrayBoxUtils.CreateF64MatrixFromDataRecords(trainDataRecords, customGrayBoxMethods);
            var trainDataLabels = trainDataRecords.Select(record => (double)record.GrayBoxLabel).ToArray();
            var learner = new BalancedBinaryClassificationRandomForestLearner(numberOfThreads);

            var timer = Stopwatch.StartNew();

            try
            {
                grayBoxRandomForest = learner.Learn(trainDataObservations, trainDataLabels);
            }
            catch (Exception exception)
            {
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Cannot train gray box random forest before generation {currentGeneration}, because: {exception.Message}");
                grayBoxRandomForest = null;
                return false;
            }

            timer.Stop();
            LoggingHelper.WriteLine(
                VerbosityLevel.Info,
                $"The training of the gray box random forest before generation {currentGeneration} took {timer.Elapsed.TotalSeconds} seconds.");
            return true;
        }

        /// <summary>
        /// Creates a <see cref="F64Matrix"/> from a list of <see cref="DataRecord{TResult}"/>s.
        /// </summary>
        /// <param name="dataRecords">The list of <see cref="DataRecord{TResult}"/>s.</param>
        /// <param name="customGrayBoxMethods">The <see cref="ICustomGrayBoxMethods{TResult}"/>.</param>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>The <see cref="F64Matrix"/>.</returns>
        public static F64Matrix CreateF64MatrixFromDataRecords<TResult>(
            IReadOnlyList<DataRecord<TResult>> dataRecords,
            ICustomGrayBoxMethods<TResult> customGrayBoxMethods)
            where TResult : ResultBase<TResult>, new()
        {
            if (customGrayBoxMethods == null)
            {
                throw new ArgumentNullException(nameof(customGrayBoxMethods));
            }

            if (dataRecords == null)
            {
                throw new ArgumentNullException(nameof(dataRecords));
            }

            if (!dataRecords.Any())
            {
                throw new ArgumentException("The list of data records cannot be empty!");
            }

            var numberOfRows = dataRecords.Count;
            var numberOfColumns = customGrayBoxMethods.GetGrayBoxFeaturesFromDataRecord(dataRecords[0]).Length;
            return new F64Matrix(
                dataRecords.SelectMany(customGrayBoxMethods.GetGrayBoxFeaturesFromDataRecord).ToArray(),
                numberOfRows,
                numberOfColumns);
        }

        /// <summary>
        /// Gets the scaled feature importance from the given <see cref="ClassificationForestModel"/>.
        /// </summary>
        /// <param name="model">The <see cref="ClassificationForestModel"/>.</param>
        /// <returns>The scaled feature importance.</returns>
        public static double[] GetScaledFeatureImportance(ClassificationForestModel model)
        {
            var rawFeatureImportance = model.GetRawVariableImportance();
            var sumOfRawFeatureImportance = rawFeatureImportance.Sum();
            var scaledFeatureImportance = rawFeatureImportance.Select(value => value / sumOfRawFeatureImportance).ToArray();
            return scaledFeatureImportance;
        }

        /// <summary>
        /// Tries to read a list of <see cref="DataRecord{TResult}"/>s from all data log files in a given directory,
        /// while skipping data log files, containing <see cref="TargetAlgorithmStatus.CancelledByGrayBox"/> target algorithm runs.
        /// </summary>
        /// <param name="targetAlgorithmFactory">The target algorithm factory.</param>
        /// <param name="pathToDirectory">The path to the directory.</param>
        /// <param name="startGeneration">The start generation.</param>
        /// <param name="endGeneration">The end generation.</param>
        /// <param name="allDataRecords">The list of <see cref="DataRecord{TResult}"/>s.</param>
        /// <typeparam name="TTargetAlgorithm">The target algorithm type.</typeparam>
        /// <typeparam name="TInstance">The instance type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>True, if successful and at least one file contains valid data records, which are not <see cref="TargetAlgorithmStatus.CancelledByGrayBox"/>.</returns>
        public static bool TryToReadDataRecordsFromDirectory<TTargetAlgorithm, TInstance, TResult>(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            string pathToDirectory,
            int startGeneration,
            int endGeneration,
            out List<DataRecord<TResult>> allDataRecords)
            where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult>
            where TInstance : InstanceBase
            where TResult : ResultBase<TResult>, new()
        {
            allDataRecords = new List<DataRecord<TResult>>();

            var timer = Stopwatch.StartNew();

            if (!Directory.Exists(pathToDirectory))
            {
                return false;
            }

            var dataLogFiles = GrayBoxUtils.GetAllDataLogFilesInDirectory(pathToDirectory);
            if (dataLogFiles.Count == 0)
            {
                return false;
            }

            foreach (var dataLogFile in dataLogFiles)
            {
                if (dataLogFile.Name.Contains(TargetAlgorithmStatus.CancelledByGrayBox.ToString()))
                {
                    continue;
                }

                if (!GrayBoxUtils.TryToGetGenerationIdFromDataLogFileName(dataLogFile.Name, out var generation))
                {
                    continue;
                }

                if (generation < startGeneration || generation > endGeneration)
                {
                    continue;
                }

                if (GrayBoxUtils.TryToReadDataRecordsFromFile<TTargetAlgorithm, TInstance, TResult>(
                    targetAlgorithmFactory,
                    dataLogFile,
                    out var currentDataRecords))
                {
                    allDataRecords.AddRange(
                        currentDataRecords.Where(
                            record => record.TunerDataRecord.FinalResult.TargetAlgorithmStatus != TargetAlgorithmStatus.CancelledByGrayBox));
                }
                else
                {
                    LoggingHelper.WriteLine(VerbosityLevel.Warn, $"Cannot read data records from {dataLogFile.FullName}!");
                }
            }

            timer.Stop();
            LoggingHelper.WriteLine(
                VerbosityLevel.Info,
                $"Reading in data log files took {timer.Elapsed.TotalSeconds} seconds.");
            return allDataRecords.Any();
        }

        /// <summary>
        /// Tries to read a list of <see cref="DataRecord{TResult}"/>s from a given file.
        /// </summary>
        /// <param name="targetAlgorithmFactory">The target algorithm factory.</param>
        /// <param name="file">The file.</param>
        /// <param name="dataRecords">The list of <see cref="DataRecord{TResult}"/>s.</param>
        /// <typeparam name="TTargetAlgorithm">The target algorithm type.</typeparam>
        /// <typeparam name="TInstance">The instance type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>True, if successful and file is not empty.</returns>
        public static bool TryToReadDataRecordsFromFile<TTargetAlgorithm, TInstance, TResult>(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            FileInfo file,
            out IReadOnlyList<DataRecord<TResult>> dataRecords)
            where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult>
            where TInstance : InstanceBase
            where TResult : ResultBase<TResult>, new()
        {
            dataRecords = null;

            // Get and check header.
            var header = File.ReadLines(file.FullName).First().Split(GrayBoxUtils.DataRecorderDelimiter).ToArray();
            if (!GrayBoxUtils.TryToGetHeaderPartitionOfDataRecord<TResult>(
                    header,
                    out var genomeHeader,
                    out var adapterFeatureHeader,
                    out var numberOfResultColumns))
            {
                return false;
            }

            var csvParserOptions = new CsvParserOptions(true, GrayBoxUtils.DataRecorderDelimiter);
            var csvMapper = new DataRecordCsvMapping<TTargetAlgorithm, TInstance, TResult>(
                targetAlgorithmFactory,
                genomeHeader,
                adapterFeatureHeader,
                numberOfResultColumns);
            var csvParser = new CsvParser<DataRecord<TResult>>(csvParserOptions, csvMapper);

            var results = csvParser
                .ReadFromFile(file.FullName, Encoding.ASCII).ToArray();

            if (results.Any(x => x.IsValid == false))
            {
                return false;
            }

            dataRecords = results.Select(x => x.Result).ToArray();
            return dataRecords.Any();
        }

        /// <summary>
        /// Tries to get the header partition as expected from <see cref="TunerDataRecord{TResult}"/> and <see cref="AdapterDataRecord{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="header">The header.</param>
        /// <param name="genomeHeader">The genome header.</param>
        /// <param name="adapterFeaturesHeader">The adapter features header.</param>
        /// <param name="numberOfResultColumns">The number of result columns.</param>
        /// <returns>True, if successful.</returns>
        public static bool TryToGetHeaderPartitionOfDataRecord<TResult>(
            string[] header,
            out string[] genomeHeader,
            out string[] adapterFeaturesHeader,
            out int numberOfResultColumns)
            where TResult : ResultBase<TResult>, new()
        {
            genomeHeader = null;
            adapterFeaturesHeader = null;
            numberOfResultColumns = 0;

            genomeHeader = GrayBoxUtils.GetSubHeaderWithoutPrefix(header, TunerDataRecord<TResult>.GenomeHeaderPrefix);
            adapterFeaturesHeader = GrayBoxUtils.GetSubHeaderWithoutPrefix(header, AdapterDataRecord<TResult>.AdapterFeaturesHeaderPrefix);

            var numberOfFinalResultColumns = header.Count(column => column.StartsWith(TunerDataRecord<TResult>.FinalResultHeaderPrefix));
            var numberOfCurrentGrayBoxResultColumns =
                header.Count(column => column.StartsWith(AdapterDataRecord<TResult>.CurrentGrayBoxResultHeaderPrefix));

            if (numberOfFinalResultColumns != numberOfCurrentGrayBoxResultColumns)
            {
                return false;
            }

            numberOfResultColumns = numberOfFinalResultColumns;

            return GrayBoxUtils.CheckHeaderLengthAndOrder<TResult>(header, genomeHeader, adapterFeaturesHeader, numberOfResultColumns);
        }

        /// <summary>
        /// Tries to read a list of <see cref="GenomeInstancePairStringRepresentation"/>s from a given file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="listOfGenomeInstancePairs">The list of <see cref="GenomeInstancePairStringRepresentation"/>s.</param>
        /// <returns>True, if successful and file is not empty.</returns>
        public static bool TryToReadGenomeInstancePairsFromFile(
            FileInfo file,
            out List<GenomeInstancePairStringRepresentation> listOfGenomeInstancePairs)
        {
            listOfGenomeInstancePairs = null;

            try
            {
                var allLines = File.ReadAllLines(file.FullName).Select(line => line.Split(GrayBoxUtils.DataRecorderDelimiter)).ToList();

                if (!allLines.Any())
                {
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Warn,
                        $"Cannot read genome instance pairs from {file.FullName}, because the file is empty!");
                    return false;
                }

                if (!allLines[0].SequenceEqual(GenomeInstancePairStringRepresentation.GetHeader()))
                {
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Warn,
                        $"Cannot read genome instance pairs from {file.FullName}, because the file header is not matching!");
                    return false;
                }

                if (allLines.Count == 1)
                {
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Warn,
                        $"Cannot read genome instance pairs from {file.FullName}, because the file contains only the header!");
                    return false;
                }

                if (allLines.Any(line => line.Length != 2))
                {
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Warn,
                        $"Cannot read genome instance pairs from {file.FullName}, because the file contains lines, containing not exactly two columns!");
                    return false;
                }

                listOfGenomeInstancePairs = allLines
                    .Skip(1)
                    .Select(line => new GenomeInstancePairStringRepresentation(line[0], line[1]))
                    .ToList();

                return listOfGenomeInstancePairs.Any();
            }
            catch (Exception exception)
            {
                LoggingHelper.WriteLine(VerbosityLevel.Warn, $"Cannot read genome instance pairs from {file.FullName}, because: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates the post tuning configuration.
        /// </summary>
        /// <param name="postTuningConfiguration">The post tuning configuration.</param>
        public static void ValidatePostTuningConfiguration(PostTuningConfiguration postTuningConfiguration)
        {
            if (postTuningConfiguration == null)
            {
                throw new ArgumentNullException(nameof(postTuningConfiguration));
            }
        }

        /// <summary>
        /// Validates the additional post tuning parameters.
        /// </summary>
        /// <typeparam name="TGrayBoxTargetAlgorithm">The gray box target algorithm type.</typeparam>
        /// <typeparam name="TInstance">The instance type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="tunerConfiguration">The tuner configuration.</param>
        /// <param name="targetAlgorithmFactory">The target algorithm factory.</param>
        /// <param name="parameterTree">The parameter tree.</param>
        public static void ValidateAdditionalPostTuningParameters<TGrayBoxTargetAlgorithm, TInstance, TResult>(
            AlgorithmTunerConfiguration tunerConfiguration,
            ITargetAlgorithmFactory<TGrayBoxTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            ParameterTree parameterTree)
            where TGrayBoxTargetAlgorithm : IGrayBoxTargetAlgorithm<TInstance, TResult>
            where TInstance : InstanceBase
            where TResult : ResultBase<TResult>, new()
        {
            if (tunerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(tunerConfiguration));
            }

            if (!tunerConfiguration.EnableDataRecording)
            {
                throw new InvalidOperationException(
                    "You cannot initialize the post tuning runner without data recording behaviour. Please set --enableDataRecording=true to enable data recording behaviour.");
            }

            if (tunerConfiguration.EnableGrayBox)
            {
                throw new InvalidOperationException(
                    "You cannot initialize the post tuning runner with gray box tuning behaviour. Please set --enableGrayBox=false to disable gray box tuning behaviour.");
            }

            if (targetAlgorithmFactory == null)
            {
                throw new ArgumentNullException(nameof(targetAlgorithmFactory));
            }

            if (parameterTree == null)
            {
                throw new ArgumentNullException(nameof(parameterTree));
            }

            if (!parameterTree.ContainsParameters())
            {
                throw new ArgumentException("Specified parameter tree without parameters.", nameof(parameterTree));
            }

            if (!parameterTree.IdentifiersAreUnique())
            {
                throw new ArgumentException("Specified parameter tree contained duplicate identifiers.", nameof(parameterTree));
            }
        }

        /// <summary>
        /// Checks the gray box confidence threshold.
        /// </summary>
        /// <param name="grayBoxConfidenceThreshold">The gray box confidence threshold.</param>
        public static void CheckGrayBoxConfidenceThreshold(double grayBoxConfidenceThreshold)
        {
            if (grayBoxConfidenceThreshold < 0 || grayBoxConfidenceThreshold > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(grayBoxConfidenceThreshold),
                    $"The gray box confidence threshold needs to be in [0,1], but {grayBoxConfidenceThreshold} was provided.");
            }
        }

        /// <summary>
        /// Checks the gray box start time point.
        /// </summary>
        /// <param name="grayBoxStartTimePoint">The gray box start time point.</param>
        public static void CheckGrayBoxStartTimePoint(TimeSpan grayBoxStartTimePoint)
        {
            if (grayBoxStartTimePoint <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(grayBoxStartTimePoint),
                    $"The gray box start time point must be positive, but {grayBoxStartTimePoint.TotalSeconds} seconds was provided.");
            }

            if (grayBoxStartTimePoint.TotalSeconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(grayBoxStartTimePoint),
                    $"The gray box start time point must be less than {int.MaxValue} seconds.");
            }
        }

        /// <summary>
        /// Checks the gray box start generation.
        /// </summary>
        /// <param name="grayBoxStartGeneration">The gray box start generation.</param>
        public static void CheckGrayBoxStartGeneration(int grayBoxStartGeneration)
        {
            if (grayBoxStartGeneration <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(grayBoxStartGeneration),
                    $"The gray box start generation should always be positive, but {grayBoxStartGeneration} was provided.");
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets all data log files in the given directory.
        /// </summary>
        /// <param name="directoryPath">The path to the directory.</param>
        /// <returns>The data log files.</returns>
        private static List<FileInfo> GetAllDataLogFilesInDirectory(string directoryPath)
        {
            return Directory.GetFiles(directoryPath)
                .Select(file => new FileInfo(file))
                .Where(file => GrayBoxUtils.DataLogFileNameRegex.IsMatch(file.Name))
                .ToList();
        }

        /// <summary>
        /// Checks the header length and order, given by the sub headers.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="header">The header.</param>
        /// <param name="genomeHeader">The genome sub header.</param>
        /// <param name="adapterFeaturesHeader">The adapter features sub header.</param>
        /// <param name="numberOfResultColumns">The number of result columns.</param>
        /// <returns>True, if header is valid.</returns>
        private static bool CheckHeaderLengthAndOrder<TResult>(
            string[] header,
            string[] genomeHeader,
            string[] adapterFeaturesHeader,
            int numberOfResultColumns)
            where TResult : ResultBase<TResult>, new()
        {
            var genomePrefixHeader = Enumerable.Repeat(
                TunerDataRecord<TResult>.GenomeHeaderPrefix,
                genomeHeader.Length);
            var adapterFeaturesPrefixHeader = Enumerable.Repeat(
                AdapterDataRecord<TResult>.AdapterFeaturesHeaderPrefix,
                adapterFeaturesHeader.Length);
            var finalResultPrefixHeader = Enumerable.Repeat(
                TunerDataRecord<TResult>.FinalResultHeaderPrefix,
                numberOfResultColumns);
            var currentGrayBoxResultPrefixHeader = Enumerable.Repeat(
                AdapterDataRecord<TResult>.CurrentGrayBoxResultHeaderPrefix,
                numberOfResultColumns);

            var completePrefixHeader = TunerDataRecord<TResult>.OtherHeader
                .Concat(genomePrefixHeader)
                .Concat(finalResultPrefixHeader)
                .Concat(AdapterDataRecord<TResult>.OtherHeader)
                .Concat(adapterFeaturesPrefixHeader)
                .Concat(currentGrayBoxResultPrefixHeader)
                .ToArray();

            if (header.Length != completePrefixHeader.Length)
            {
                return false;
            }

            return !header.Where((name, index) => !name.StartsWith(completePrefixHeader[index])).Any();
        }

        /// <summary>
        /// Gets the sub header, matching the given prefix, without the given prefix.
        /// </summary>
        /// <param name="header">The complete header.</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns>The sub header without prefix.</returns>
        private static string[] GetSubHeaderWithoutPrefix(string[] header, string prefix)
        {
            return header.Where(column => column.StartsWith(prefix)).Select(name => name.Substring(prefix.Length)).ToArray();
        }

        #endregion
    }
}
