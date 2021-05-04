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

namespace Optano.Algorithm.Tuner.GrayBox.GrayBoxSimulation
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.GrayBox.CsvRecorder;
    using Optano.Algorithm.Tuner.GrayBox.DataRecordTypes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    using SharpLearning.RandomForest.Models;

    /// <summary>
    /// Contains methods for a simulation of the gray box tuning.
    /// </summary>
    /// <typeparam name="TTargetAlgorithm">The target algorithm type.</typeparam>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class GrayBoxSimulation<TTargetAlgorithm, TInstance, TResult> : IDisposable
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// The <see cref="AlgorithmTunerConfiguration"/>.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _configuration;

        /// <summary>
        /// The <see cref="ICustomGrayBoxMethods{TResult}"/>.
        /// </summary>
        private readonly ICustomGrayBoxMethods<TResult> _customGrayBoxMethods;

        /// <summary>
        /// The <see cref="IRunEvaluator{TInstance,TResult}"/>.
        /// </summary>
        private readonly IRunEvaluator<TInstance, TResult> _runEvaluator;

        /// <summary>
        /// The <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </summary>
        private readonly ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> _targetAlgorithmFactory;

        /// <summary>
        /// The <see cref="ParameterTree"/>.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// The log file directory.
        /// </summary>
        private readonly DirectoryInfo _logFileDirectory;

        /// <summary>
        /// The list of all <see cref="DataRecord{TResult}"/>s.
        /// </summary>
        private readonly List<DataRecord<TResult>> _allDataRecords;

        /// <summary>
        /// The generation instance composition.
        /// </summary>
        private List<List<TInstance>> _generationInstanceComposition;

        /// <summary>
        /// The generation genome composition.
        /// </summary>
        private List<List<ImmutableGenome>> _generationGenomeComposition;

        /// <summary>
        /// The prediction dictionary.
        /// </summary>
        private Dictionary<GenomeInstancePairStringRepresentation, GrayBoxSimulationResultPair<TResult>> _predictionDictionary;

        /// <summary>
        /// The data dictionary.
        /// </summary>
        private Dictionary<GenomeInstancePairStringRepresentation, List<DataRecord<TResult>>> _dataDictionary;

        /// <summary>
        /// The dictionary, which translates an <see cref="ImmutableGenome"/> in its string representation.
        /// </summary>
        private Dictionary<ImmutableGenome, string> _genomeStringDictionary;

        /// <summary>
        /// The dictionary, which translates an instance in its string representation.
        /// </summary>
        private Dictionary<TInstance, string> _instanceStringDictionary;

        /// <summary>
        /// The current <see cref="GrayBoxSimulationConfiguration"/>.
        /// </summary>
        private GrayBoxSimulationConfiguration _simulationConfiguration;

        /// <summary>
        /// The feature importance recorder.
        /// </summary>
        private StringArrayRecorder _featureImportanceRecorder;

        /// <summary>
        /// The prediction scores recorder.
        /// </summary>
        private StringArrayRecorder _predictionScoresRecorder;

        /// <summary>
        /// The simulation scores recorder.
        /// </summary>
        private StringArrayRecorder _tuningScoresRecorder;

        /// <summary>
        /// The instance count recorder.
        /// </summary>
        private StringArrayRecorder _instanceCountRecorder;

        /// <summary>
        /// Gets the current positive train data count.
        /// </summary>
        private int _positiveTrainDataCount;

        /// <summary>
        /// Gets the current negative train data count.
        /// </summary>
        private int _negativeTrainDataCount;

        /// <summary>
        /// The list of black box evaluation run times in the current generation.
        /// </summary>
        private List<TimeSpan> _listOfBlackBoxEvaluationRunTimesInCurrentGeneration;

        /// <summary>
        /// The list of gray box evaluation run times in the current generation.
        /// </summary>
        private List<TimeSpan> _listOfGrayBoxEvaluationRunTimesInCurrentGeneration;

        /// <summary>
        /// The bag of percentage of tournament winner changes per tournament.
        /// </summary>
        private ConcurrentBag<double> _bagOfPercentageOfTournamentWinnerChangesPerTournament;

        /// <summary>
        /// The bag of adapted WS coefficients per tournament.
        /// </summary>
        private ConcurrentBag<double> _bagOfAdaptedWsCoefficientsPerTournament;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GrayBoxSimulation{TTargetAlgorithm, TInstance, TResult}" /> class.
        /// </summary>
        /// <param name="configuration">The <see cref="AlgorithmTunerConfiguration"/>.</param>
        /// <param name="targetAlgorithmFactory">The <see cref="ITargetAlgorithmFactory{TTargetAlgorithm, TInstance, TResult}"/>.</param>
        /// <param name="customGrayBoxMethods">The <see cref="ICustomGrayBoxMethods{TResult}"/>.</param>
        /// <param name="runEvaluator">The <see cref="IRunEvaluator{TInstance, TResult}"/>.</param>
        /// <param name="parameterTree">The <see cref="ParameterTree"/>.</param>
        public GrayBoxSimulation(
            AlgorithmTunerConfiguration configuration,
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            ICustomGrayBoxMethods<TResult> customGrayBoxMethods,
            IRunEvaluator<TInstance, TResult> runEvaluator,
            ParameterTree parameterTree)
        {
            ProcessUtils.SetDefaultCultureInfo(CultureInfo.InvariantCulture);

            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._customGrayBoxMethods = customGrayBoxMethods ?? throw new ArgumentNullException(nameof(customGrayBoxMethods));
            this._runEvaluator = runEvaluator ?? throw new ArgumentNullException(nameof(runEvaluator));
            this._targetAlgorithmFactory = targetAlgorithmFactory ?? throw new ArgumentNullException(nameof(targetAlgorithmFactory));
            this._parameterTree = parameterTree ?? throw new ArgumentNullException(nameof(parameterTree));

            this._logFileDirectory = new DirectoryInfo(Path.Combine(this._configuration.DataRecordDirectoryPath, "GrayBoxSimulationLogFiles"));
            Directory.CreateDirectory(this._logFileDirectory.FullName);

            LoggingHelper.Configure(
                Path.Combine(this._logFileDirectory.FullName, $"consoleOutput_GrayBoxSimulation_{ProcessUtils.GetCurrentProcessId()}.log"));
            LoggingHelper.ChangeConsoleLoggingLevel(configuration.Verbosity);
            LoggingHelper.WriteLine(VerbosityLevel.Info, "Reading in and preprocessing data for gray box simulation.");

            if (!GrayBoxUtils.TryToReadDataRecordsFromDirectory(
                    targetAlgorithmFactory,
                    configuration.DataRecordDirectoryPath,
                    0,
                    configuration.Generations - 1,
                    out this._allDataRecords))
            {
                throw new ArgumentException($"Cannot read data records from {configuration.DataRecordDirectoryPath}!");
            }

            this.ReadGenerationCompositionFiles();
            this.CreatePredictionDictionaryAndDataDictionary();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Gets the tournament statistics: The percentage of tournament winner changes and the adapted WS coefficient.
        /// </summary>
        /// <param name="blackBoxRanking">The gray box tournament winners.</param>
        /// <param name="grayBoxWinners">The black box tournament order.</param>
        /// <returns>The tournament statistics.</returns>
        public static (double percentageOfTournamentWinnerChanges, double adaptedWsCoefficient) GetTournamentStatistics(
            List<ImmutableGenome> blackBoxRanking,
            IReadOnlyCollection<ImmutableGenome> grayBoxWinners)
        {
            if (!blackBoxRanking.Any())
            {
                throw new ArgumentException($"The black box ranking cannot be empty.");
            }

            if (!grayBoxWinners.Any())
            {
                throw new ArgumentException($"The list of gray box winners cannot be empty.");
            }

            var numberOfTournamentParticipants = blackBoxRanking.Count;
            var numberOfTournamentWinners = grayBoxWinners.Count;

            var tournamentWinnerChanges = 0D;
            var adaptedWsCoefficient = 0D;
            var currentGrayBoxRank = 0;

            foreach (var genome in grayBoxWinners)
            {
                currentGrayBoxRank++;
                var currentBlackBoxRank = blackBoxRanking.FindIndex(x => ImmutableGenome.GenomeComparer.Equals(x, genome)) + 1;

                if (currentBlackBoxRank <= 0)
                {
                    throw new ArgumentException(
                        $"The black box ranking does not contain the genome {genome}.");
                }

                if (currentBlackBoxRank > numberOfTournamentWinners)
                {
                    tournamentWinnerChanges++;
                }

                adaptedWsCoefficient += 1d / numberOfTournamentWinners
                                        * Math.Abs(currentGrayBoxRank - currentBlackBoxRank)
                                        / Math.Max(Math.Abs(currentGrayBoxRank - 1), Math.Abs(numberOfTournamentParticipants - currentGrayBoxRank));
            }

            var percentageOfTournamentWinnerChanges = tournamentWinnerChanges / numberOfTournamentWinners;

            return (percentageOfTournamentWinnerChanges, adaptedWsCoefficient);
        }

        /// <summary>
        /// Runs this instance of the <see cref="GrayBoxSimulation{TTargetAlgorithm,TInstance,TResult}"/> class.
        /// </summary>
        /// <param name="simulationConfiguration">The <see cref="GrayBoxSimulationConfiguration"/>.</param>
        public void Run(GrayBoxSimulationConfiguration simulationConfiguration)
        {
            this._simulationConfiguration = simulationConfiguration;

            this.InitializeLogFileRecorder();
            this.ResetPredictionDictionaryAndDataDictionary();

            Randomizer.Reset();
            Randomizer.Configure(simulationConfiguration.RandomSeed);

            LoggingHelper.WriteLine(VerbosityLevel.Info, "Starting new gray box simulation.");
            var timer = Stopwatch.StartNew();
            this.PerformPredictionSimulation();
            this.PerformTuningSimulation();
            LoggingHelper.WriteLine(VerbosityLevel.Info, $"Finished gray box simulation in {timer.Elapsed.TotalSeconds} seconds.");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this._allDataRecords.Clear();
            this._predictionDictionary.Clear();
            this._dataDictionary.Clear();
            this._generationInstanceComposition.Clear();
            this._generationGenomeComposition.Clear();
            this._instanceStringDictionary.Clear();
            this._genomeStringDictionary.Clear();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Validates that the genome instance pairs, given by the data records, contain all genome instance pairs, given by the generation composition.
        /// </summary>
        /// <param name="listOfDataRecords">The list of data records.</param>
        /// <param name="generationInstanceComposition">The generation instance composition.</param>
        /// <param name="generationGenomeComposition">The generation genome composition.</param>
        /// <returns>True, if validation was successful.</returns>
        private static bool ValidateGenomeInstancePairsOfListOfDataRecordsAndGenerationComposition(
            IReadOnlyList<DataRecord<TResult>> listOfDataRecords,
            IReadOnlyList<List<string>> generationInstanceComposition,
            IReadOnlyList<List<string>> generationGenomeComposition)
        {
            var compositionGenomeInstancePairs = new List<GenomeInstancePairStringRepresentation>();
            for (var generation = 0; generation < generationInstanceComposition.Count; generation++)
            {
                foreach (var genome in generationGenomeComposition[generation])
                {
                    foreach (var instance in generationInstanceComposition[generation])
                    {
                        compositionGenomeInstancePairs.Add(new GenomeInstancePairStringRepresentation(genome, instance));
                    }
                }
            }

            var dataRecordGenomeInstancePairs = listOfDataRecords
                .Where(record => record.AdapterDataRecord.TargetAlgorithmStatus != TargetAlgorithmStatus.Running)
                .Select(record => record.GenomeInstancePair).ToList();

            return !compositionGenomeInstancePairs.Except(dataRecordGenomeInstancePairs).Any();
        }

        /// <summary>
        /// Gets the current test data.
        /// </summary>
        /// <param name="testData">The test data.</param>
        /// <param name="timePoint">The current time point.</param>
        /// <returns>The current test data.</returns>
        private static IReadOnlyList<DataRecord<TResult>> GetCurrentTestData(
            IEnumerable<DataRecord<TResult>> testData,
            TimeSpan timePoint)
        {
            return testData
                // Drop records, which have been cancelled by gray box during simulation or have finished until current time point.
                .Where(
                    record => (record.IsCancelledByGrayBoxDuringGrayBoxSimulation == false)
                              && (timePoint < record.TunerDataRecord.FinalResult.Runtime))
                // Drop future records. Thereby floor runtime to seconds to avoid time-delayed problems.
                .Where(
                    record => timePoint >= TimeSpan.FromSeconds(Math.Floor(record.AdapterDataRecord.ExpendedWallClockTime.TotalMilliseconds / 1000)))
                // Return last entry per genome instance pair.
                .GroupBy(record => record.GenomeInstancePair)
                .Select(gipGroup => gipGroup.Last())
                .ToList();
        }

        /// <summary>
        /// Logs the total tuning scores.
        /// </summary>
        /// <param name="listOfTuningScores">The list of tuning scores.</param>
        private void LogTotalTuningScores(List<GrayBoxSimulationTuningScores> listOfTuningScores)
        {
            var totalNumberOfEvaluations = listOfTuningScores.Sum(score => score.NumberOfEvaluations);

            // Set generation to -1, since it gets overwritten by "total".
            var totalTuningScores = new GrayBoxSimulationTuningScores(
                -1,
                totalNumberOfEvaluations,
                TimeSpan.FromMilliseconds(
                    listOfTuningScores.Sum(score => score.AveragedBlackBoxEvaluationRuntime.TotalMilliseconds * score.NumberOfEvaluations)
                    / totalNumberOfEvaluations),
                TimeSpan.FromMilliseconds(
                    listOfTuningScores.Sum(score => score.AveragedGrayBoxEvaluationRuntime.TotalMilliseconds * score.NumberOfEvaluations)
                    / totalNumberOfEvaluations),
                listOfTuningScores.Average(score => score.AveragedPercentageOfTournamentWinnerChanges),
                listOfTuningScores.Average(score => score.AveragedAdaptedWsCoefficient));

            this._tuningScoresRecorder.WriteRow(totalTuningScores.ToStringArray("total"));
        }

        /// <summary>
        /// Gets the current <see cref="GrayBoxSimulationPredictionScores"/>.
        /// </summary>
        /// <param name="generation">The generation.</param>
        /// <param name="timePoint">The gray box start time point.</param>
        /// <param name="currentDataRecords">The current data records.</param>
        /// <returns>The current <see cref="GrayBoxSimulationPredictionScores"/>.</returns>
        private GrayBoxSimulationPredictionScores GetPredictionScores(
            int generation,
            TimeSpan timePoint,
            IEnumerable<DataRecord<TResult>> currentDataRecords)
        {
            var predictionScores = new GrayBoxSimulationPredictionScores(
                generation,
                timePoint,
                this._positiveTrainDataCount,
                this._negativeTrainDataCount);

            foreach (var record in currentDataRecords)
            {
                if (record.IsCancelledByGrayBoxDuringGrayBoxSimulation && record.GrayBoxLabel == GrayBoxUtils.GrayBoxLabelOfTimeouts)
                {
                    predictionScores.TruePositiveCount++;
                }

                if (record.IsCancelledByGrayBoxDuringGrayBoxSimulation && record.GrayBoxLabel == GrayBoxUtils.GrayBoxLabelOfNonTimeouts)
                {
                    predictionScores.FalsePositiveCount++;
                }

                if (!record.IsCancelledByGrayBoxDuringGrayBoxSimulation && record.GrayBoxLabel == GrayBoxUtils.GrayBoxLabelOfNonTimeouts)
                {
                    predictionScores.TrueNegativeCount++;
                }

                if (!record.IsCancelledByGrayBoxDuringGrayBoxSimulation && record.GrayBoxLabel == GrayBoxUtils.GrayBoxLabelOfTimeouts)
                {
                    predictionScores.FalseNegativeCount++;
                }
            }

            return predictionScores;
        }

        /// <summary>
        /// Reads the generation composition files.
        /// </summary>
        private void ReadGenerationCompositionFiles()
        {
            var timer = Stopwatch.StartNew();

            if (!GrayBoxUtils.TryToReadGenerationCompositionFromFile(
                    this._configuration.GenerationInstanceCompositionFile,
                    out var generationInstanceComposition))
            {
                throw new ArgumentException(
                    $"Cannot read generation instance composition from {this._configuration.GenerationInstanceCompositionFile.FullName}!");
            }

            if (!GrayBoxUtils.TryToReadGenerationCompositionFromFile(
                    this._configuration.GenerationGenomeCompositionFile,
                    out var generationGenomeComposition))
            {
                throw new ArgumentException(
                    $"Cannot read generation genome composition from {this._configuration.GenerationGenomeCompositionFile.FullName}!");
            }

            if (generationInstanceComposition.Count != generationGenomeComposition.Count)
            {
                throw new ArgumentException(
                    "The number of generations given by the generation instance composition does not equal the number of generations given by the generation genome composition.");
            }

            Debug.Assert(
                GrayBoxSimulation<TTargetAlgorithm, TInstance, TResult>.ValidateGenomeInstancePairsOfListOfDataRecordsAndGenerationComposition(
                    this._allDataRecords,
                    generationInstanceComposition,
                    generationGenomeComposition),
                "The genome instance pairs, given by the data records, do not contain all genome instance pairs, given by the generation composition.");

            var (convertedGenerationInstanceComposition, instanceStringDictionary) =
                GrayBoxUtils.ConvertGenerationInstanceComposition(
                    generationInstanceComposition,
                    this._targetAlgorithmFactory);

            var (convertedGenerationGenomeComposition, genomeStringDictionary) =
                GrayBoxUtils.ConvertGenerationGenomeComposition(
                    generationGenomeComposition,
                    this._parameterTree);

            this._generationInstanceComposition = convertedGenerationInstanceComposition;
            this._instanceStringDictionary = instanceStringDictionary;
            this._generationGenomeComposition = convertedGenerationGenomeComposition;
            this._genomeStringDictionary = genomeStringDictionary;

            timer.Stop();
            LoggingHelper.WriteLine(
                VerbosityLevel.Info,
                $"Reading in generation composition files took {timer.Elapsed.TotalSeconds} seconds.");
        }

        /// <summary>
        /// Creates the prediction dictionary and the data dictionary.
        /// </summary>
        private void CreatePredictionDictionaryAndDataDictionary()
        {
            var timer = Stopwatch.StartNew();

            // Get all finished runs, grouped by genome instance pair.
            var finishedRunsGroupedByGenomeInstancePair = this._allDataRecords
                .Where(record => record.AdapterDataRecord.TargetAlgorithmStatus != TargetAlgorithmStatus.Running)
                .GroupBy(record => record.GenomeInstancePair)
                .ToList();

            // Create prediction dictionary. For duplicated runs, unlikely arising from duplicated runs of the same genome instance pair, arbitrarily use last run.
            this._predictionDictionary = finishedRunsGroupedByGenomeInstancePair.Select(
                    gipGroup =>
                        new
                            {
                                gip = gipGroup.Key,
                                record = gipGroup.Last(),
                            })
                .ToDictionary(
                    run => run.gip,
                    run => new GrayBoxSimulationResultPair<TResult>(
                        run.record.TunerDataRecord.GenerationId,
                        run.record.TunerDataRecord.FinalResult,
                        run.record.TunerDataRecord.FinalResult));

            // Get all unique finished genome instance pairs.
            var uniqueFinishedGenomeInstancePairs = finishedRunsGroupedByGenomeInstancePair
                .Where(gipGroup => gipGroup.Count() == 1)
                .Select(gipGroup => gipGroup.Key).ToHashSet();

            // Create data dictionary.
            this._dataDictionary = this._allDataRecords
                .GroupBy(record => record.GenomeInstancePair)
                .Where(gipGroup => uniqueFinishedGenomeInstancePairs.Contains(gipGroup.Key))
                .ToDictionary(
                    gipGroup => gipGroup.Key,
                    gipGroup => gipGroup.OrderBy(gip => gip.AdapterDataRecord.ExpendedWallClockTime).ToList());

            timer.Stop();
            LoggingHelper.WriteLine(
                VerbosityLevel.Info,
                $"Preprocessing data for gray box simulation took {timer.Elapsed.TotalSeconds} seconds.");
        }

        /// <summary>
        /// Initializes all log file recorder.
        /// </summary>
        private void InitializeLogFileRecorder()
        {
            // Get log file suffix from current simulation configuration.
            var logFileSuffix =
                $"SG_{this._simulationConfiguration.GrayBoxStartGeneration}_ST_{this._simulationConfiguration.GrayBoxStartTimePoint.TotalMilliseconds:0}_CT_{this._simulationConfiguration.GrayBoxConfidenceThreshold:0.####}_ID_{ProcessUtils.GetCurrentProcessId()}";

            // Get feature importance header from first element in data dictionary.
            var featureImportanceHeader =
                new[] { "Generation", }.Concat(
                    this._customGrayBoxMethods.GetGrayBoxFeatureNamesFromDataRecord(this._dataDictionary.Values.First().First())).ToArray();

            // Define instance count header.
            var instanceCountHeader = new[]
                                          {
                                              "Instance",
                                              "CancelledBlackBoxRuns",
                                              "FinishedBlackBoxRuns",
                                              "CancelledGrayBoxRuns",
                                              "FinishedGrayBoxRuns",
                                          };

            // Initialize log file recorder.
            this._featureImportanceRecorder = new StringArrayRecorder(
                new FileInfo(Path.Combine(this._logFileDirectory.FullName, $"featureImportance_{logFileSuffix}.csv")),
                featureImportanceHeader,
                true);
            this._predictionScoresRecorder = new StringArrayRecorder(
                new FileInfo(Path.Combine(this._logFileDirectory.FullName, $"predictionScores_{logFileSuffix}.csv")),
                GrayBoxSimulationPredictionScores.GetHeader(),
                true);
            this._tuningScoresRecorder = new StringArrayRecorder(
                new FileInfo(Path.Combine(this._logFileDirectory.FullName, $"tuningScores_{logFileSuffix}.csv")),
                GrayBoxSimulationTuningScores.GetHeader(),
                true);
            this._instanceCountRecorder = new StringArrayRecorder(
                new FileInfo(Path.Combine(this._logFileDirectory.FullName, $"instanceCounts_{logFileSuffix}.csv")),
                instanceCountHeader,
                true);
        }

        /// <summary>
        /// Resets the prediction dictionary and the data dictionary.
        /// </summary>
        private void ResetPredictionDictionaryAndDataDictionary()
        {
            foreach (var key in this._predictionDictionary.Keys)
            {
                this._predictionDictionary[key].GrayBoxResult = this._predictionDictionary[key].BlackBoxResult;
                this._predictionDictionary[key].RuntimeUntilGrayBoxCancellation = this._predictionDictionary[key].GrayBoxResult.Runtime;
            }

            foreach (var key in this._dataDictionary.Keys)
            {
                this._dataDictionary[key].ForEach(record => record.IsCancelledByGrayBoxDuringGrayBoxSimulation = false);
            }
        }

        /// <summary>
        /// Simulates the predictions, used in <see cref="PerformTuningSimulation"/>.
        /// </summary>
        private void PerformPredictionSimulation()
        {
            LoggingHelper.WriteLine(VerbosityLevel.Info, "Starting prediction simulation of gray box simulation.");
            var outerTimer = Stopwatch.StartNew();

            for (var currentGeneration = this._simulationConfiguration.GrayBoxStartGeneration;
                 currentGeneration < this._configuration.Generations;
                 currentGeneration++)
            {
                var trainData = this.GetTrainDataAndUpdateCounts(currentGeneration);

                if (!GrayBoxUtils.TryToTrainGrayBoxRandomForest(
                        trainData,
                        this._customGrayBoxMethods,
                        currentGeneration,
                        this._configuration.MaximumNumberParallelThreads,
                        out var grayBoxRandomForest))
                {
                    continue;
                }

                this.LogFeatureImportance(currentGeneration, grayBoxRandomForest);

                var innerTimer = Stopwatch.StartNew();

                var testData = this.GetTestData(currentGeneration);
                this.SimulatePredictionsOfGrayBoxRandomForest(testData, grayBoxRandomForest, currentGeneration);

                this.LogTotalPredictionScores(testData, currentGeneration);

                innerTimer.Stop();
                LoggingHelper.WriteLine(
                    VerbosityLevel.Info,
                    $"The predictions of the gray box random forest for generation {currentGeneration} took {innerTimer.Elapsed.TotalSeconds} seconds.");
            }

            this.LogInstanceCounts();

            outerTimer.Stop();
            LoggingHelper.WriteLine(
                VerbosityLevel.Info,
                $"The prediction simulation of the gray box simulation took {outerTimer.Elapsed.TotalSeconds} seconds.");
        }

        /// <summary>
        /// Logs the instance counts.
        /// </summary>
        private void LogInstanceCounts()
        {
            var instanceCounts = this._predictionDictionary
                .Where(
                    kvp =>
                        (kvp.Value.GenerationID >= this._simulationConfiguration.GrayBoxStartGeneration
                         && kvp.Value.GenerationID < this._configuration.Generations))
                .GroupBy(kvp => kvp.Key.Instance)
                .Select(
                    instanceGroup => new[]
                                         {
                                             instanceGroup.Key,
                                             instanceGroup.Count(kvp => kvp.Value.BlackBoxResult.IsCancelled).ToString(),
                                             instanceGroup.Count(kvp => !kvp.Value.BlackBoxResult.IsCancelled).ToString(),
                                             instanceGroup.Count(kvp => kvp.Value.GrayBoxResult.IsCancelled).ToString(),
                                             instanceGroup.Count(kvp => !kvp.Value.GrayBoxResult.IsCancelled).ToString(),
                                         }).ToList();

            this._instanceCountRecorder.WriteRows(instanceCounts);
        }

        /// <summary>
        /// Logs the total prediction scores.
        /// </summary>
        /// <param name="testData">The test data.</param>
        /// <param name="currentGeneration">The current generation.</param>
        private void LogTotalPredictionScores(List<DataRecord<TResult>> testData, int currentGeneration)
        {
            var uniqueTestData = testData
                .GroupBy(record => record.GenomeInstancePair)
                .Select(gipGroup => gipGroup.Last())
                .ToList();

            // Set time point to -1, since it gets overwritten by "total".
            var totalPredictionScores = this.GetPredictionScores(
                currentGeneration,
                TimeSpan.FromSeconds(-1),
                uniqueTestData);

            this._predictionScoresRecorder.WriteRow(totalPredictionScores.ToStringArray("total"));
        }

        /// <summary>
        /// Simulates the predictions of the gray box random forest.
        /// </summary>
        /// <param name="testData">The test data.</param>
        /// <param name="grayBoxRandomForest">The gray box random forest.</param>
        /// <param name="currentGeneration">The current generation.</param>
        private void SimulatePredictionsOfGrayBoxRandomForest(
            List<DataRecord<TResult>> testData,
            ClassificationForestModel grayBoxRandomForest,
            int currentGeneration)
        {
            for (var currentTimePoint = TimeSpan.Zero;
                 currentTimePoint < this._configuration.CpuTimeout;
                 currentTimePoint += this._configuration.DataRecordUpdateInterval)
            {
                if (currentTimePoint < this._simulationConfiguration.GrayBoxStartTimePoint)
                {
                    continue;
                }

                var currentTestData =
                    GrayBoxSimulation<TTargetAlgorithm, TInstance, TResult>.GetCurrentTestData(testData, currentTimePoint);

                if (!currentTestData.Any())
                {
                    continue;
                }

                var predictions = this.GetPredictionsOfGrayBoxRandomForest(grayBoxRandomForest, currentTestData);
                this.UpdatePredictionDictionaryAndDataDictionary(predictions, currentTestData, currentTimePoint);

                var currentPredictionScores = this.GetPredictionScores(
                    currentGeneration,
                    currentTimePoint,
                    currentTestData);
                this._predictionScoresRecorder.WriteRow(currentPredictionScores.ToStringArray());
            }
        }

        /// <summary>
        /// Gets the predictions of the gray box random forest.
        /// </summary>
        /// <param name="grayBoxRandomForest">The gray box random forest.</param>
        /// <param name="currentTestData">The current test data.</param>
        /// <returns>The predictions.</returns>
        private int[] GetPredictionsOfGrayBoxRandomForest(
            ClassificationForestModel grayBoxRandomForest,
            IReadOnlyList<DataRecord<TResult>> currentTestData)
        {
            var currentTestDataObservations = GrayBoxUtils.CreateF64MatrixFromDataRecords(currentTestData, this._customGrayBoxMethods);
            var originalPredictions = grayBoxRandomForest.PredictProbability(currentTestDataObservations);

            // Use gray box confidence threshold to adjust predictions.
            var correctedPredictions = originalPredictions
                .Select(
                    prediction =>
                        prediction.Probabilities[GrayBoxUtils.GrayBoxLabelOfTimeouts]
                        >= this._simulationConfiguration.GrayBoxConfidenceThreshold
                            ? GrayBoxUtils.GrayBoxLabelOfTimeouts
                            : GrayBoxUtils.GrayBoxLabelOfNonTimeouts)
                .ToArray();

            return correctedPredictions;
        }

        /// <summary>
        /// Gets the test data.
        /// </summary>
        /// <param name="currentGeneration">The current generation.</param>
        /// <returns>The test data.</returns>
        private List<DataRecord<TResult>> GetTestData(int currentGeneration)
        {
            var testData = this._dataDictionary
                .SelectMany(gip => gip.Value)
                .Where(record => record.TunerDataRecord.GenerationId == currentGeneration)
                .ToList();
            return testData;
        }

        /// <summary>
        /// Gets the train data and update the train data counts.
        /// </summary>
        /// <param name="currentGeneration">The current generation.</param>
        /// <returns>The train data.</returns>
        private List<DataRecord<TResult>> GetTrainDataAndUpdateCounts(int currentGeneration)
        {
            // Use all past records, not cancelled by gray box, as training data.
            var trainData = this._dataDictionary
                .SelectMany(gip => gip.Value)
                .Where(
                    record => record.TunerDataRecord.GenerationId < currentGeneration
                              && record.IsCancelledByGrayBoxDuringGrayBoxSimulation == false)
                .ToList();

            this._positiveTrainDataCount = trainData.Count(x => x.GrayBoxLabel == GrayBoxUtils.GrayBoxLabelOfTimeouts);
            this._negativeTrainDataCount = trainData.Count(x => x.GrayBoxLabel == GrayBoxUtils.GrayBoxLabelOfNonTimeouts);

            return trainData;
        }

        /// <summary>
        /// Logs the feature importance.
        /// </summary>
        /// <param name="currentGeneration">The current generation.</param>
        /// <param name="grayBoxRandomForest">The gray box random forest.</param>
        private void LogFeatureImportance(int currentGeneration, ClassificationForestModel grayBoxRandomForest)
        {
            var featureImportanceEntry =
                new[] { (double)currentGeneration, }.Concat(
                    GrayBoxUtils.GetScaledFeatureImportance(grayBoxRandomForest)).Select(d => $"{d:0.######}").ToArray();

            this._featureImportanceRecorder.WriteRow(featureImportanceEntry);
        }

        /// <summary>
        /// Simulates the tuning, based on the predictions of <see cref="PerformPredictionSimulation"/>.
        /// </summary>
        private void PerformTuningSimulation()
        {
            LoggingHelper.WriteLine(VerbosityLevel.Info, "Starting tuning simulation of gray box simulation.");
            var timer = Stopwatch.StartNew();

            var listOfTuningScores = new List<GrayBoxSimulationTuningScores>();

            for (var currentGeneration = this._simulationConfiguration.GrayBoxStartGeneration;
                 currentGeneration < this._configuration.Generations;
                 currentGeneration++)
            {
                var currentGenomes = this._generationGenomeComposition[currentGeneration];
                this.CheckGenomeCountAndThrowOnError(currentGenomes.Count, currentGeneration);
                var currentInstances = this._generationInstanceComposition[currentGeneration];

                this.ResetTuningSimulationLists();

                var currentGenomeStatsPairs = this.GetGenomeStatsPairs(
                    currentGenomes,
                    currentInstances,
                    currentGeneration);
                var genomeToGenomeStatsPairs = currentGenomeStatsPairs.ToDictionary(
                    gsp => gsp.Genome,
                    gsp => gsp,
                    ImmutableGenome.GenomeComparer);

                var miniTournaments = this.DrawMiniTournaments(currentGenomes);
                this.EvaluateMiniTournaments(miniTournaments, genomeToGenomeStatsPairs);

                var currentTuningScores = this.GetCurrentTuningScores(currentGeneration);
                listOfTuningScores.Add(currentTuningScores);
                this._tuningScoresRecorder.WriteRow(currentTuningScores.ToStringArray());
            }

            this.LogTotalTuningScores(listOfTuningScores);

            timer.Stop();
            LoggingHelper.WriteLine(
                VerbosityLevel.Info,
                $"The tuning simulation of the gray box simulation took {timer.Elapsed.TotalSeconds} seconds.");
        }

        /// <summary>
        /// Gets the current tuning scores.
        /// </summary>
        /// <param name="currentGeneration">The current generation.</param>
        /// <returns>The current tuning scores.</returns>
        private GrayBoxSimulationTuningScores GetCurrentTuningScores(int currentGeneration)
        {
            var currentTuningScores = new GrayBoxSimulationTuningScores(
                currentGeneration,
                this._listOfBlackBoxEvaluationRunTimesInCurrentGeneration.Count,
                TimeSpan.FromMilliseconds(
                    this._listOfBlackBoxEvaluationRunTimesInCurrentGeneration.Average(runtime => runtime.TotalMilliseconds)),
                TimeSpan.FromMilliseconds(this._listOfGrayBoxEvaluationRunTimesInCurrentGeneration.Average(runtime => runtime.TotalMilliseconds)),
                this._bagOfPercentageOfTournamentWinnerChangesPerTournament.Average(),
                this._bagOfAdaptedWsCoefficientsPerTournament.Average());
            return currentTuningScores;
        }

        /// <summary>
        /// Evaluates the given mini tournaments.
        /// </summary>
        /// <param name="miniTournaments">The mini tournaments.</param>
        /// <param name="genomeToGenomeStatsPairs">The <see cref="ImmutableGenome"/> to <see cref="GrayBoxSimulationGenomeStatsPair{TInstance,TResult}"/> dictionary.</param>
        private void EvaluateMiniTournaments(
            List<ImmutableList<ImmutableGenome>> miniTournaments,
            Dictionary<ImmutableGenome, GrayBoxSimulationGenomeStatsPair<TInstance, TResult>> genomeToGenomeStatsPairs)
        {
            var partitionOptions = new ParallelOptions { MaxDegreeOfParallelism = this._configuration.MaximumNumberParallelThreads };
            var rangePartitioner = Partitioner.Create(miniTournaments, true);

            Parallel.ForEach(
                rangePartitioner,
                partitionOptions,
                (miniTournament, loopState) =>
                    {
                        var currentGenomeStatsPairs =
                            miniTournament.Select(participant => genomeToGenomeStatsPairs[participant]).ToList();

                        var blackBoxTournamentRanking = this._runEvaluator
                            .Sort(currentGenomeStatsPairs.Select(pair => pair.BlackBoxGenomeStats))
                            .Select(gs => gs.Genome).ToList();

                        var desiredNumberOfTournamentWinners =
                            (int)Math.Ceiling(miniTournament.Count * this._configuration.TournamentWinnerPercentage);

                        var grayBoxTournamentWinners = this._runEvaluator
                            .Sort(currentGenomeStatsPairs.Select(pair => pair.GrayBoxGenomeStats))
                            .Select(gs => gs.Genome).Take(desiredNumberOfTournamentWinners).ToList();

                        var (percentageOfTournamentWinnerChanges, adaptedWsCoefficient) =
                            GrayBoxSimulation<TTargetAlgorithm, TInstance, TResult>.GetTournamentStatistics(
                                blackBoxTournamentRanking,
                                grayBoxTournamentWinners);

                        this._bagOfPercentageOfTournamentWinnerChangesPerTournament.Add(percentageOfTournamentWinnerChanges);
                        this._bagOfAdaptedWsCoefficientsPerTournament.Add(adaptedWsCoefficient);
                    });
        }

        /// <summary>
        /// Draws the list of mini tournaments from the given list of genomes.
        /// </summary>
        /// <param name="genomes">The list of genomes.</param>
        /// <returns>The list of mini tournaments.</returns>
        private List<ImmutableList<ImmutableGenome>> DrawMiniTournaments(List<ImmutableGenome> genomes)
        {
            var miniTournaments = new List<ImmutableList<ImmutableGenome>>();
            for (var draw = 0; draw < this._simulationConfiguration.NumberOfDrawsPerGeneration; draw++)
            {
                miniTournaments.AddRange(
                    Randomizer.Instance.SplitIntoRandomBalancedSubsets(
                        genomes,
                        this._configuration.MaximumMiniTournamentSize).Select(mt => mt.ToImmutableList()));
            }

            return miniTournaments;
        }

        /// <summary>
        /// Resets the tuning simulation lists.
        /// </summary>
        private void ResetTuningSimulationLists()
        {
            this._listOfBlackBoxEvaluationRunTimesInCurrentGeneration = new List<TimeSpan>();
            this._listOfGrayBoxEvaluationRunTimesInCurrentGeneration = new List<TimeSpan>();
            this._bagOfPercentageOfTournamentWinnerChangesPerTournament = new ConcurrentBag<double>();
            this._bagOfAdaptedWsCoefficientsPerTournament = new ConcurrentBag<double>();
        }

        /// <summary>
        /// Checks the genome count and throws an <see cref="ArgumentException"/>, if faulty.
        /// </summary>
        /// <param name="currentGenomeCount">The current genome count.</param>
        /// <param name="currentGeneration">The current generation.</param>
        private void CheckGenomeCountAndThrowOnError(int currentGenomeCount, int currentGeneration)
        {
            if (currentGenomeCount != (int)Math.Floor((double)this._configuration.PopulationSize / 2D)
                && currentGenomeCount != (int)Math.Ceiling((double)this._configuration.PopulationSize / 2D))
            {
                throw new ArgumentException(
                    $"The number of genomes in generation {currentGeneration} of the gray box simulation does not equal the number of competitive genomes in the tuning.");
            }
        }

        /// <summary>
        /// Handles the predictions by updating the prediction dictionary and the data dictionary.
        /// </summary>
        /// <param name="predictions">The predictions.</param>
        /// <param name="currentTestData">The current test data.</param>
        /// <param name="predictionTimePoint">The current prediction time point.</param>
        private void UpdatePredictionDictionaryAndDataDictionary(
            IReadOnlyList<int> predictions,
            IReadOnlyList<DataRecord<TResult>> currentTestData,
            TimeSpan predictionTimePoint)
        {
            for (var index = 0; index < predictions.Count; index++)
            {
                // For all positive predictions ...
                if (predictions[index] != GrayBoxUtils.GrayBoxLabelOfTimeouts)
                {
                    continue;
                }

                var currentRecord = currentTestData[index];

                // ... set the corresponding gray box result ...
                this._predictionDictionary[currentRecord.GenomeInstancePair].GrayBoxResult = currentRecord.AdapterDataRecord.CurrentGrayBoxResult;
                this._predictionDictionary[currentRecord.GenomeInstancePair].RuntimeUntilGrayBoxCancellation = predictionTimePoint;

                // ... and the IsCancelledByGrayBoxDuringGrayBoxSimulation-Boolean.
                this._dataDictionary[currentRecord.GenomeInstancePair]
                    .ForEach(record => record.IsCancelledByGrayBoxDuringGrayBoxSimulation = true);
            }
        }

        /// <summary>
        /// Gets the <see cref="GrayBoxSimulationGenomeStatsPair{TInstance, TResult}"/>s of the given genomes on the given instances.
        /// </summary>
        /// <param name="genomes">The genomes.</param>
        /// <param name="instances">The instances.</param>
        /// <param name="currentGeneration">The current generation.</param>
        /// <returns>The <see cref="GrayBoxSimulationGenomeStatsPair{TInstance, TResult}"/>s.</returns>
        private List<GrayBoxSimulationGenomeStatsPair<TInstance, TResult>> GetGenomeStatsPairs(
            IEnumerable<ImmutableGenome> genomes,
            IReadOnlyList<TInstance> instances,
            int currentGeneration)
        {
            var listOfGenomeStatsPairs = new List<GrayBoxSimulationGenomeStatsPair<TInstance, TResult>>();

            foreach (var genome in genomes)
            {
                var genomeStringRepresentation = this._genomeStringDictionary[genome];
                var blackBoxResults = new Dictionary<TInstance, TResult>();
                var grayBoxResults = new Dictionary<TInstance, TResult>();

                foreach (var instance in instances)
                {
                    var currentGenomeInstancePair = new GenomeInstancePairStringRepresentation(
                        genomeStringRepresentation,
                        this._instanceStringDictionary[instance]);

                    if (!this._predictionDictionary.TryGetValue(currentGenomeInstancePair, out var resultPair))
                    {
                        throw new ArgumentException(
                            $"The prediction dictionary does not contain an entry for the following genome instance pair.{Environment.NewLine}{currentGenomeInstancePair}");
                    }

                    if (resultPair.GenerationID == currentGeneration)
                    {
                        this._listOfBlackBoxEvaluationRunTimesInCurrentGeneration.Add(resultPair.BlackBoxResult.Runtime);
                        this._listOfGrayBoxEvaluationRunTimesInCurrentGeneration.Add(resultPair.RuntimeUntilGrayBoxCancellation);
                    }

                    blackBoxResults.Add(instance, resultPair.BlackBoxResult);
                    grayBoxResults.Add(instance, resultPair.GrayBoxResult);
                }

                listOfGenomeStatsPairs.Add(
                    new GrayBoxSimulationGenomeStatsPair<TInstance, TResult>(
                        new GenomeStats<TInstance, TResult>(genome, blackBoxResults).ToImmutable(),
                        new GenomeStats<TInstance, TResult>(genome, grayBoxResults).ToImmutable()));
            }

            if (this._listOfBlackBoxEvaluationRunTimesInCurrentGeneration.Count != this._listOfGrayBoxEvaluationRunTimesInCurrentGeneration.Count)
            {
                throw new ArgumentException(
                    "The number of run times, given by the list of black box evaluation run times, does not equal the number of run times, given by the list of gray box evaluation run times.");
            }

            return listOfGenomeStatsPairs;
        }

        #endregion
    }
}