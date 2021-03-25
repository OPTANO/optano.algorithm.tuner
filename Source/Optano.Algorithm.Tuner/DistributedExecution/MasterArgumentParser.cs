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

namespace Optano.Algorithm.Tuner.DistributedExecution
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    using NDesk.Options;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration;

    /// <summary>
    /// Utility class for parsing command line arguments.
    /// </summary>
    public class MasterArgumentParser : HelpSupportingArgumentParser<AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder>
    {
        #region Fields

        /// <summary>
        /// The dictionary of secondary <see cref="IConfigurationParser"/>s that are applied in a second parameter parsing.
        /// </summary>
        private readonly Dictionary<string, IConfigurationParser> _parsers = new Dictionary<string, IConfigurationParser>();

        /// <summary>
        /// A value indicating whether the run should be started in the state stored in a status dump file.
        /// </summary>
        private bool _startFromExistingStatus = false;

        /// <summary>
        /// If specified by parsed arguments, this variable contains the maximum number of parallel evaluations.
        /// </summary>
        private int? _maximumNumberParallelEvaluations;

        /// <summary>
        /// If specified by parsed arguments, this variable contains a path to a folder containing training instances.
        /// </summary>
        private string _pathToTrainingInstanceFolder;

        /// <summary>
        /// If specified by parsed arguments, this variable contains a path to a folder containing test instances.
        /// </summary>
        private string _pathToTestInstanceFolder;

        /// <summary>
        /// If specified by parsed arguments, this variable contains the own host name.
        /// </summary>
        private string _ownHostName;

        /// <summary>
        /// If specified by parsed arguments, this variable contains the desired port for the cluster seed.
        /// </summary>
        private int _port = 8081;

        /// <summary>
        /// If specified by parsed arguments, this variable contains the path to the status file directory.
        /// </summary>
        private string _statusFileDirectory =
            AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultStatusFileDirectory;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MasterArgumentParser" /> class.
        /// </summary>
        public MasterArgumentParser()
        {
            ProcessUtils.SetDefaultCultureInfo(CultureInfo.InvariantCulture);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets a value indicating whether the run should be started in the state stored in a status dump file.
        /// </summary>
        public bool StartFromExistingStatus
        {
            get
            {
                this.ThrowExceptionIfNoPreprocessingHasBeenDone();
                return this._startFromExistingStatus;
            }
        }

        /// <summary>
        /// Gets the maximum number of parallel evaluations which has been specified by the parsed arguments.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called before <see cref="ParseArguments(string[])" />
        /// has been executed.
        /// </exception>
        public int MaximumNumberParallelEvaluations
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return this.CheckAndGetMaximumNumberParallelEvaluations();
            }
        }

        /// <summary>
        /// Gets the path to a folder containing training instances which has been specified by the parsed arguments.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called before <see cref="ParseArguments(string[])" />
        /// has been executed.
        /// </exception>
        public string PathToTrainingInstanceFolder
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return this._pathToTrainingInstanceFolder;
            }
        }

        /// <summary>
        /// Gets the path to a folder containing test instances which has been specified by the parsed arguments.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called before <see cref="ParseArguments(string[])" />
        /// has been executed.
        /// </exception>
        public string PathToTestInstanceFolder
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return this._pathToTestInstanceFolder;
            }
        }

        /// <summary>
        /// Gets the host name that Akka listens to.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called before <see cref="ParseArguments(string[])" />
        /// has been executed.
        /// </exception>
        public string OwnHostName
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return this._ownHostName;
            }
        }

        /// <summary>
        /// Gets the desired port for the cluster seed.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called before <see cref="ParseArguments(string[])" />
        /// has been executed.
        /// </exception>
        public int Port
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return this._port;
            }
        }

        /// <summary>
        /// Gets the status file directory.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called before <see cref="ParseArguments(string[])" />
        /// has been executed.
        /// </exception>
        public string StatusFileDirectory
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return this._statusFileDirectory;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Parses the provided arguments.
        /// </summary>
        /// <param name="args">Arguments to parse.</param>
        /// <exception cref="OptionException">Thrown if required parameters have not been set.</exception>
        /// <exception cref="AggregateException">Thrown if one or more arguments could not be interpreted.</exception>
        public override void ParseArguments(string[] args)
        {
            // First check for options influencing which other options are possible.
            IEnumerable<string> remainingArguments = this.CreatePreprocessingOptionSet(true).Parse(args);
            this.FinishedPreProcessing();

            // Don't verify further if help was requested.
            if (this.HelpTextRequested)
            {
                return;
            }

            // Otherwise parse remaining arguments.
            remainingArguments = this.CreateBaseOptionSet().Parse(remainingArguments);

            // always parse all options. if --continue is used with changes to non-technical parameters, a validation in the master will fail, where old + new config are checked for "IsCompatible".
            remainingArguments = this.CreateAdditionalOptionsForNewTunings().Parse(remainingArguments);

            // Let additional parsers go through the arguments, too.
            this.AddConfigurationParser(
                RegressionForestArgumentParser.Identifier,
                new RegressionForestArgumentParser());

            foreach (var parserDefinition in this._parsers)
            {
                var detailParser = parserDefinition.Value;
                detailParser.ParseArguments(remainingArguments.ToArray());
                this.InternalConfigurationBuilder.AddDetailedConfigurationBuilder(parserDefinition.Key, detailParser.ConfigurationBuilder);
                remainingArguments = detailParser.AdditionalArguments;
            }

            this.FinishedParsing();

            // Now, all arguments should have been resolved.
            if (!this.AllowAdditionalArguments && remainingArguments.Any())
            {
                throw new AggregateException(
                    remainingArguments.Select(
                        arg => new OptionException(
                            $"Could not resolve '{arg}'. Maybe you made a typo or specified a parameter not allowed with the --continue option.",
                            arg)));
            }

            // Finally, check for required arguments.
            this.CheckAndGetMaximumNumberParallelEvaluations();
        }

        /// <inheritdoc />
        public override void PrintHelp(bool printHelpParameter)
        {
            var helperTextBuilder = new StringBuilder();
            helperTextBuilder.AppendLine("Arguments for master:");

            var textWriter = new StringWriter(helperTextBuilder);
            this.CreatePreprocessingOptionSet(printHelpParameter).WriteOptionDescriptions(textWriter);
            this.CreateBaseOptionSet().WriteOptionDescriptions(textWriter);
            helperTextBuilder.AppendLine("\nAdditional options for the master if a new tuning is started (i.e. --continue not provided):");
            this.CreateAdditionalOptionsForNewTunings().WriteOptionDescriptions(textWriter);
            helperTextBuilder.AppendLine("\nThe maximum number of parallel evaluations per node (i.e. --maxParallelEvaluations) must be provided.");

            Console.WriteLine(helperTextBuilder.ToString());

            // Go through additional parsers which might be relevant.
            this.AddConfigurationParser(
                RegressionForestArgumentParser.Identifier,
                new RegressionForestArgumentParser());
            this.AddConfigurationParser(
                DifferentialEvolutionStrategyArgumentParser.Identifier,
                new DifferentialEvolutionStrategyArgumentParser());
            this.AddConfigurationParser(
                CovarianceMatrixAdaptationStrategyArgumentParser.Identifier,
                new CovarianceMatrixAdaptationStrategyArgumentParser());
            foreach (var parser in this._parsers)
            {
                if (parser.Value is HelpSupportingArgumentParserBase helpfulParser)
                {
                    helpfulParser.PrintHelp(false);
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates an <see cref="OptionSet" /> containing all options important for starting a new tuning, but
        /// not for continuing a tuning.
        /// </summary>
        /// <returns>The created <see cref="OptionSet" />.</returns>
        protected OptionSet CreateAdditionalOptionsForNewTunings()
        {
            var options = new OptionSet();

            this.AddTuningScaleOptions(options);
            this.AddTuningAlgorithmOptions(options);
            this.AddGeneticAlgorithmOptions(options);
            this.AddTargetAlgorithmSpecificOptions(options);
            this.AddModelBasedCrossoverOptions(options);

            return options;
        }

        /// <summary>
        /// Checks <see cref="_maximumNumberParallelEvaluations"/> for null and returns it.
        /// </summary>
        /// <returns>The maximum number of parallel evaluations.</returns>
        private int CheckAndGetMaximumNumberParallelEvaluations()
        {
            if (this._maximumNumberParallelEvaluations == null)
            {
                throw new OptionException(
                    "The maximum number of parallel evaluations per node must be provided.",
                    "maxParallelEvaluations");
            }

            return (int)this._maximumNumberParallelEvaluations;
        }

        /// <summary>
        /// Creates an <see cref="OptionSet"/> containing all options that somehow influence which other options can be
        /// set.
        /// </summary>
        /// <param name="useHelpSupportOptionSet"><c>true</c>, to use <see cref="HelpSupportingArgumentParserBase.CreateOptionSet"/>.</param>
        /// <returns>The created <see cref="OptionSet"/>.</returns>
        private OptionSet CreatePreprocessingOptionSet(bool useHelpSupportOptionSet)
        {
            var options = useHelpSupportOptionSet ? base.CreateOptionSet() : new OptionSet();
            options.Add(
                "continue",
                () => "Add if this OPTANO Algorithm Tuner instance should start with the state stored in a status file directory.",
                c => this._startFromExistingStatus = true);
            return options;
        }

        /// <summary>
        /// Creates an <see cref="OptionSet"/> containing all options that can always be specified.
        /// </summary>
        /// <returns>The created <see cref="OptionSet"/>.</returns>
        private OptionSet CreateBaseOptionSet()
        {
            var options = new OptionSet
                              {
                                  {
                                      "maxParallelEvaluations=",
                                      () =>
                                          "The maximum {NUMBER} of parallel target algorithm evaluations per node.\nThis parameter must be specified.\nIt must be an integer.",
                                      (int maximumNumberParallelEvaluations) =>
                                          {
                                              if (maximumNumberParallelEvaluations <= 0)
                                              {
                                                  throw new OptionException(
                                                      $"At least one evaluation must be able to run at a time, but {maximumNumberParallelEvaluations} was given at the maximum number of parallel evaluations.",
                                                      "maxParallelEvaluations");
                                              }

                                              if (maximumNumberParallelEvaluations > Environment.ProcessorCount)
                                              {
                                                  LoggingHelper.WriteLine(
                                                      VerbosityLevel.Warn,
                                                      $"Warning: You specified {maximumNumberParallelEvaluations} parallel evaluations, but only have {Environment.ProcessorCount} processors. Processes may fight for resources.");
                                              }

                                              this.InternalConfigurationBuilder.SetMaximumNumberParallelEvaluations(maximumNumberParallelEvaluations);
                                              this._maximumNumberParallelEvaluations = maximumNumberParallelEvaluations;
                                          }
                                  },
                                  {
                                      "maxParallelThreads=",
                                      () =>
                                          "The maximum {NUMBER} of parallel threads per node.\nIf not specified, maxParallelEvaluations is used.\nThis must be an integer.",
                                      (int p) => { this.InternalConfigurationBuilder.SetMaximumNumberParallelThreads(p); }
                                  },
                              };

            this.AddAddressOptions(options);
            this.AddInstanceFolderOptions(options);
            this.AddEvaluationLimitOption(options);
            this.AddLoggingOptions(options);
            this.AddFaultToleranceOptions(options);

            return options;
        }

        /// <summary>
        /// Adds the address options to the provided <see cref="OptionSet"/>.
        /// </summary>
        /// <param name="options">The <see cref="OptionSet" /> to extend.</param>
        private void AddAddressOptions(OptionSet options)
        {
            options.Add(
                "ownHostName=",
                () =>
                    "The address that the master listens for workers that try to connect. Default: FQDN. Note: On some systems the FQDN cannot be resolved on the fly. In that case, please provide the FQDN or an IP address.",
                (string hostName) => this._ownHostName = hostName);
            options.Add(
                "port=",
                () =>
                    "The port {NUMBER} on which the master listens for worker connections. Must be identical for master and respective workers, but different for different parallel runs.\nDefault is 8081.\nThis must be an integer.",
                (int p) => this._port = p);
        }

        /// <summary>
        /// Adds the options about instance folders to the provided <see cref="OptionSet"/>.
        /// </summary>
        /// <param name="options">The <see cref="OptionSet" /> to extend.</param>
        private void AddInstanceFolderOptions(OptionSet options)
        {
            options.Add(
                "trainingInstanceFolder=",
                () => "The complete {PATH} to the folder containing training instances.",
                path => this._pathToTrainingInstanceFolder = path);
            options.Add(
                "testInstanceFolder=",
                () => "The complete {PATH} to the folder containing test instances.",
                path => this._pathToTestInstanceFolder = path);
        }

        /// <summary>
        /// Adds the logging options to the provided <see cref="OptionSet"/>.
        /// </summary>
        /// <param name="options">The <see cref="OptionSet" /> to extend.</param>
        private void AddLoggingOptions(OptionSet options)
        {
            options.Add(
                "v|verbose=",
                () =>
                    "The verbosity level. 0 only prints warnings, 1 regularly prints some status information, 2 prints more detailed information, e.g. calls to the target mechanism, and 3 is for debugging.\nDefault is 1.\nMust be one of 0, 1, 2, 3.\n",
                (VerbosityLevel level) => this.InternalConfigurationBuilder.SetVerbosity(level));
            options.Add(
                "statusFileDir=",
                () => "The {PATH} to a status file directory to write and read status dumps.\nDefault is a folder 'status' in the current directory.",
                f =>
                    {
                        this.InternalConfigurationBuilder.SetStatusFileDirectory(f);
                        this._statusFileDirectory = f;
                    });
            options.Add(
                "zipOldStatus=",
                () => "Whether to zip old status files. Otherwise, old status files are overwritten.\nDefault is false.",
                (bool zip) => this.InternalConfigurationBuilder.SetZipOldStatusFiles(zip));
            options.Add(
                "logFile=",
                () => "The {PATH} where the log file should be written to.\nDefault is a file 'tunerLog.txt' in the current directory.",
                f => this.InternalConfigurationBuilder.SetLogFilePath(f));
            options.Add(
                "trackConvergenceBehavior=",
                () => "If this option is enabled, the convergence behavior is evaluated and logged.\nDefault is false.\nMust be a boolean value",
                (bool b) => this.InternalConfigurationBuilder.SetTrackConvergenceBehavior(b));
            options.Add(
                "scoreGenerationHistory",
                () =>
                    "Add if the generation history logged at the end of the tuning should include average scores on the complete instance sets. Leads to additional evaluations after the most promising parameterization is printed.\n Default is false.",
                score => this.InternalConfigurationBuilder.SetScoreGenerationHistory(true));
            options.Add(
                "trainModel",
                () => "Add if a performance model should be trained even if genetic engineering and sexual selection are turned off.",
                tm => this.InternalConfigurationBuilder.SetTrainModel(true));
        }

        /// <summary>
        /// Adds the evaluation limit option to the provided <see cref="OptionSet"/>.
        /// </summary>
        /// <param name="options">The <see cref="OptionSet" /> to extend.</param>
        private void AddEvaluationLimitOption(OptionSet options)
        {
            options.Add(
                "evaluationLimit=",
                () =>
                    $"A maximum number of (configuration - instance) evaluations after which the program terminates.\nDefault is {int.MaxValue}.\nMust be an integer.",
                (int l) => this.InternalConfigurationBuilder.SetEvaluationLimit(l));
        }

        /// <summary>
        /// Adds the fault tolerance options to the provided <see cref="OptionSet"/>.
        /// </summary>
        /// <param name="options">The <see cref="OptionSet" /> to extend.</param>
        private void AddFaultToleranceOptions(OptionSet options)
        {
            options.Add(
                "faultTolerance=",
                () =>
                    "Maximum number of consecutive failures in an evaluation before OPTANO Algorithm Tuner is stopped.\nDefault is 3.\nThis must be an integer.",
                (int t) => this.InternalConfigurationBuilder.SetMaximumNumberConsecutiveFailuresPerEvaluation(t));
            options.Add(
                "maxRepair=",
                () =>
                    "The maximum {NUMBER} of attempts to repair a genome if it is invalid after crossover or mutation.\nDefault is 20.\nMust be an integer.",
                (int n) => this.InternalConfigurationBuilder.SetMaxRepairAttempts(n));
            options.Add(
                "strictCompatibilityCheck=",
                () =>
                    "Option to turn off / on the continuity compatibility check between the current and old configuration in case of a continued run. Use with care.\nDefault is 'true'.\nMust be a boolean.",
                (bool c) => this.InternalConfigurationBuilder.SetStrictCompatibilityCheck(c));
        }

        /// <summary>
        /// Adds the tuning scale options important for starting a new tuning to the provided
        /// <see cref="OptionSet"/>.
        /// </summary>
        /// <param name="options">The <see cref="OptionSet" /> to extend.</param>
        private void AddTuningScaleOptions(OptionSet options)
        {
            options.Add(
                "p|popSize=",
                () => "The {POPULATION SIZE} (competitive + non-competitive).\nDefault is 128.\nThis must be an integer.",
                (int p) => this.InternalConfigurationBuilder.SetPopulationSize(p));
            options.Add(
                "g|numGens=",
                () => "The number of {GENERATIONS} to execute.\nDefault is 100.\nThis must be an integer.",
                (int g) => this.InternalConfigurationBuilder.SetGenerations(g));
            options.Add(
                "goalGen=",
                () =>
                    "The 0-indexed {GENERATION} at which the maximum number instances per genome evaluation will be reached.\nDefault is 74.\nThis must be an integer.",
                (int gg) => this.InternalConfigurationBuilder.SetGoalGeneration(gg));
        }

        /// <summary>
        /// Adds the tuning algorithm options to the provided <see cref="OptionSet"/>.
        /// </summary>
        /// <param name="options">The <see cref="OptionSet" /> to extend.</param>
        private void AddTuningAlgorithmOptions(OptionSet options)
        {
            options.Add(
                "jade",
                () => $"Adds the differential evolution variant JADE as continuous optimization method to combine GGA(++) with.",
                s =>
                    {
                        this.InternalConfigurationBuilder.SetContinuousOptimizationMethod(
                            ContinuousOptimizationMethod.Jade);
                        this.AddConfigurationParser(
                            DifferentialEvolutionStrategyArgumentParser.Identifier,
                            new DifferentialEvolutionStrategyArgumentParser());
                    });
            options.Add(
                "cmaEs",
                () => "Adds CMA-ES as continuous optimization method to combine GGA(++) with.",
                s =>
                    {
                        this.InternalConfigurationBuilder.SetContinuousOptimizationMethod(
                            ContinuousOptimizationMethod.CmaEs);
                        this.AddConfigurationParser(
                            CovarianceMatrixAdaptationStrategyArgumentParser.Identifier,
                            new CovarianceMatrixAdaptationStrategyArgumentParser());
                    });
            options.Add(
                "maxGenerationsPerGgaPhase=",
                () =>
                    $"The maximum number of generations per GGA phase.\nDefault is {AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultMaximumNumberGgaGenerations}.\nThis must be a non-negative integer.",
                (int g) => this.InternalConfigurationBuilder.SetMaximumNumberGgaGenerations(g));
            options.Add(
                "maxGgaGenerationsWithSameIncumbent=",
                () =>
                    $"The maximum number of consecutive GGA generations which do not find a new incumbent.\nDefault is {AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultMaximumNumberGgaGenerations}.\nThis must be an integer >= 1.",
                (int g) => this.InternalConfigurationBuilder.SetMaximumNumberGgaGenerationsWithSameIncumbent(g));
        }

        /// <summary>
        /// Adds the genetic algorithm options important for starting a new tuning to the provided
        /// <see cref="OptionSet"/>.
        /// </summary>
        /// <param name="options">The <see cref="OptionSet" /> to extend.</param>
        private void AddGeneticAlgorithmOptions(OptionSet options)
        {
            options.Add(
                "s|miniTournamentSize=",
                () => "The maximum {NUMBER} of participants per mini tournament.\nDefault is 8.\nThis must be an integer.",
                (int s) => this.InternalConfigurationBuilder.SetMaximumMiniTournamentSize(s));
            options.Add(
                "maxGenomeAge=",
                () => "The number of generations a genome survives.\nDefault is 3.\nThis must be an integer.",
                (int a) => this.InternalConfigurationBuilder.SetMaxGenomeAge(a));
            options.Add(
                "w|winnerPercentage=",
                () => "The {PERCENTAGE} of winners per mini tournament.\nDefault is 0.125.\nThis must be a double.",
                (double w) => this.InternalConfigurationBuilder.SetTournamentWinnerPercentage(w));
            options.Add(
                "enableSexualSelection=",
                () =>
                    "Set a value indicating whether an attractiveness measure should be considered during the selection of non-competitive mates. The attractiveness of a genome refers to the rank that is predicted for it by the GeneticEngineering's random forest.\nDefault is 'false'.\nMust be a boolean.",
                (bool s) => this.InternalConfigurationBuilder.SetEnableSexualSelection(s));
            options.Add(
                "m|mutationRate=",
                () =>
                    $"The probability that a parameter is mutated.\nDefault is {AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultMutationRate}.\nThis must be a double.",
                (double m) => this.InternalConfigurationBuilder.SetMutationRate(m));
            options.Add(
                "mutationVariance=",
                () =>
                    "The {PERCENTAGE} of the variable's domain that is used to determine the variance for Gaussian mutation.\nDefault is 0.1.\nThis must be a double.",
                (double v) => this.InternalConfigurationBuilder.SetMutationVariancePercentage(v));
            options.Add(
                "populationMutantRatio=",
                () =>
                    $"Sets the ratio of the non-competitive population that gets replaced by random mutants after every generation.\nDefault is {AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultPopulationMutantRatio}. Value is only used if --engineeredProportion > 0. \nMust be a double.",
                (double mutantRatio) => this.InternalConfigurationBuilder.SetPopulationMutantRatio(mutantRatio));
            options.Add(
                "crossoverSwitchProbability=",
                () =>
                    "The {PROBABILITY} that we switch between parents when doing a crossover and deciding on the value of a parameter that has different values for both parents and has a parent parameter in the parameter tree which also has different values for both parents.\nDefault is 0.1.\nThis must be a double.",
                (double p) => this.InternalConfigurationBuilder.SetCrossoverSwitchProbability(p));
        }

        /// <summary>
        /// Adds the model based crossover options important for starting a new tuning to the provided
        /// <see cref="OptionSet"/>.
        /// </summary>
        /// <param name="options">The <see cref="OptionSet" /> to extend.</param>
        private void AddModelBasedCrossoverOptions(OptionSet options)
        {
            options.Add(
                "engineeredProportion=",
                () => "The proportion of offspring that should be genetically engineered.\nDefault is 0.\nMust be in the range of [0, 1].",
                (double p) => this.InternalConfigurationBuilder.SetEngineeredProportion(p));
            options.Add(
                "startIterationEngineering=",
                () =>
                    "Sets the iteration number in which the genetic engineering should be incorporated in the tuning.\nDefault is 3.\nMust be an integer.",
                (int i) => this.InternalConfigurationBuilder.SetStartEngineeringAtIteration(i));
            options.Add(
                "targetSampleSize=",
                () =>
                    $"Sets the number of random samples to generate per reachable leaf during GeneticEngineering. \nDefault is {AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultTargetSamplingSize}. \nMust be an integer larger than 0.",
                (int s) => this.InternalConfigurationBuilder.SetTargetSampleSize(s));
            options.Add(
                "distanceMetric=",
                () =>
                    "Sets the distance metric to use during genetic engineering.\nDefault: HammingDistance.\nMust be a member of the Configuration.DistanceMetric enum: {HammingDistance, L1Average}.",
                (string m) => this.InternalConfigurationBuilder.SetDistanceMetric(m));
            options.Add(
                "maxRanksCompensatedByDistance=",
                () =>
                    "Sets the influence factor for the 'distance' between a potential offspring and the existing population when scoring potential offspring. \nDefault is 1.6.\nMust be a double larger or equal than 0.",
                (double c) => this.InternalConfigurationBuilder.SetMaxRanksCompensatedByDistance(c));
            options.Add(
                "featureSubsetRatioForDistance=",
                () =>
                    "Distances between Genomes during GeneticEngineering are only computed over given percentage of Genome Features, selected at random.\nDefault is 0.3\nMust be a double in range [0, 1].",
                (double r) => this.InternalConfigurationBuilder.SetFeatureSubsetRatioForDistance(r));
            options.Add(
                "hammingDistanceRelativeThreshold=",
                () =>
                    "Sets the relative threshold above which two compared features are considered to be different. Used during GeneticEngineering. \n Default is 0.01.\nMust be in the range of [0, 1].",
                (double p) => this.InternalConfigurationBuilder.SetHammingDistanceRelativeThreshold(p));
            options.Add(
                "crossoverProbabilityCompetitive=",
                () =>
                    "Sets the probability with which a non-fixed parameter will be selected from the <c>competitive</c> genome during the targeted sampling of GeneticEngineering.\n Default is 0.5.\nMust be in the range of [0, 1].",
                (double p) => this.InternalConfigurationBuilder.SetCrossoverProbabilityCompetitive(p));
            options.Add(
                "topPerformerThreshold=",
                () =>
                    "Sets the proportion of genomes that are considered to be 'top performers' during model based approach.\nDefault is 0.1\nMust be in the range of [0, 1].",
                (double p) => this.InternalConfigurationBuilder.SetTopPerformerThreshold(p));
        }

        /// <summary>
        /// Adds the target algorithm specific options important for starting a new tuning to the provided
        /// <see cref="OptionSet"/>.
        /// </summary>
        /// <param name="options">The <see cref="OptionSet" /> to extend.</param>
        private void AddTargetAlgorithmSpecificOptions(OptionSet options)
        {
            options.Add(
                "i|instanceNumbers=",
                () => "The minimum and maximum number of instances per evaluation, given as integers.\nDefault is 5 100.",
                (int min, int max) => this.InternalConfigurationBuilder.SetInstanceNumbers(min, max));
            options.Add(
                "enableRacing=",
                () =>
                    $"Value indicating whether racing should be enabled.\nDefault is {AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultEnableRacing}.\nThis must be a boolean value.",
                (bool r) => this.InternalConfigurationBuilder.SetEnableRacing(r));
            options.Add(
                "t|cpuTimeout=",
                () =>
                    $"The CPU timeout per target algorithm run in {{SECONDS}}.\nDefault is {TimeSpan.FromMilliseconds(int.MaxValue).TotalSeconds:0} seconds.\nThis must be a double.",
                (double t) => this.InternalConfigurationBuilder.SetCpuTimeout(TimeSpan.FromSeconds(t)));
            options.Add(
                "addDefaultGenome=",
                () =>
                    "If set to true, a genome that uses the target algorithm's default values (if specified), is added to the competitive population when the tuning is started.",
                (bool b) => this.InternalConfigurationBuilder.SetAddDefaultGenome(b));
        }

        /// <summary>
        /// Adds an additional <see cref="IConfigurationParser"/> to use in a second round of parsing if
        /// <paramref name="key"/> is not used yet.
        /// </summary>
        /// <param name="key">The key to use.</param>
        /// <param name="parser"><see cref="IConfigurationParser"/> to add.</param>
        private void AddConfigurationParser(string key, IConfigurationParser parser)
        {
            if (!this._parsers.ContainsKey(key))
            {
                this._parsers.Add(key, parser);
            }
        }

        #endregion
    }
}