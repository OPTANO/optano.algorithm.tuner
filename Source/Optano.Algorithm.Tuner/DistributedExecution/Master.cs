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
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Akka.Configuration;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.MachineLearning.Prediction;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.Tracking;
    using Optano.Algorithm.Tuner.Tuning;

    /// <summary>
    /// The Master aka cluster seed node.
    /// </summary>
    /// <typeparam name="TTargetAlgorithm">
    /// The algorithm that should be tuned.
    /// </typeparam>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    /// <typeparam name="TLearnerModel">
    /// The machine learning model that trains the specified <typeparamref name="TPredictorModel"/>.
    /// </typeparam>
    /// <typeparam name="TPredictorModel">
    /// The ML model that predicts the performance for a given potential offspring.
    /// </typeparam>
    /// <typeparam name="TSamplingStrategy">
    /// The strategy that is used for aggregating the observed training data before training the <typeparamref name="TPredictorModel"/>.
    /// </typeparam>
    [SuppressMessage(
        "NDepend",
        "ND2003:AbstractBaseClassShouldBeSuffixedWithBase",
        Justification = "Class was made abstract so that a concretely typed implementation could be defined. This was not possible for a static class. For consistency, both classes should only be called \"Master\".")]
    public abstract class Master<TTargetAlgorithm, TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy>
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
        where TLearnerModel : IGenomeLearner<TPredictorModel, TSamplingStrategy>
        where TPredictorModel : IEnsemblePredictor<GenomePredictionTree>
        where TSamplingStrategy : IEnsembleSamplingStrategy<IGenomePredictor>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Master{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/> class.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Constructor mustn't be called. <see cref="Master{TTargetAlgorithm,TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/> should behave like a static class.
        /// Since there is no "static inheritance", we need to provide some _ctor.
        /// </exception>
        protected Master()
        {
            throw new InvalidOperationException("This class should never be initialized.");
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Runs the master.
        /// </summary>
        /// <param name="args">
        /// Arguments to configure the run, e.g. population size or port to use.
        /// </param>
        /// <param name="algorithmTunerBuilder">
        /// A function creating a <see cref="AlgorithmTuner{TTargetAlgorithm, TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}"/> instance.
        /// </param>
        /// <returns>
        /// The <see cref="Dictionary{String, IAllele}"/>, containing the best configuration.
        /// </returns>
        public static Dictionary<string, IAllele> Run(
            string[] args,
            Func<AlgorithmTunerConfiguration, string, string,
                AlgorithmTuner<TTargetAlgorithm, TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy>> algorithmTunerBuilder)
        {
            ProcessUtils.SetDefaultCultureInfo(CultureInfo.InvariantCulture);
            LoggingHelper.Configure($"consoleOutput_Master_{ProcessUtils.GetCurrentProcessId()}.log");
            Randomizer.Configure();

            var argsParser = new MasterArgumentParser();
            if (!ArgumentParserUtils.ParseArguments(argsParser, args))
            {
                return null;
            }

            var configuration = CreateAlgorithmTunerConfiguration(argsParser);

            LoggingHelper.ChangeConsoleLoggingLevel(configuration.Verbosity);
            LoggingHelper.WriteLine(VerbosityLevel.Info, $"Configuration:{Environment.NewLine}{configuration}");

            // Create status file directories.
            Directory.CreateDirectory(configuration.StatusFileDirectory);
            if (configuration.ZipOldStatusFiles)
            {
                Directory.CreateDirectory(configuration.ZippedStatusFileDirectory);
            }

            Dictionary<string, IAllele> bestParameters = null;
            // Run algorithm tuner.
            using (var runner = algorithmTunerBuilder.Invoke(
                configuration,
                argsParser.PathToTrainingInstanceFolder,
                argsParser.PathToTestInstanceFolder))
            {
                if (argsParser.StartFromExistingStatus)
                {
                    runner.UseStatusDump(Path.Combine(configuration.StatusFileDirectory, AlgorithmTunerConfiguration.FileName));
                }

                bestParameters = runner.Run();
                runner.CompleteAndExportGenerationHistory();
            }

            LoggingHelper.WriteLine(
                VerbosityLevel.Info,
                $"Best Configuration:{Environment.NewLine}{string.Join("Environment.NewLine", bestParameters.Select(keyValuePair => $"{keyValuePair.Key}: {keyValuePair.Value}"))}");

            return bestParameters;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a <see cref="AlgorithmTunerConfiguration"/> object depending on what the provided
        /// <see cref="MasterArgumentParser"/> has parsed.
        /// </summary>
        /// <param name="argsParser">
        /// <see cref="MasterArgumentParser"/> which has already parsed arguments.
        /// </param>
        /// <returns>
        /// The created <see cref="AlgorithmTunerConfiguration"/>.
        /// </returns>
        private static AlgorithmTunerConfiguration CreateAlgorithmTunerConfiguration(MasterArgumentParser argsParser)
        {
            var akkaConfiguration = CustomizeAkkaConfiguration(
                argsParser.OwnHostName,
                argsParser.Port,
                argsParser.ConfigurationBuilder.Verbosity >= VerbosityLevel.Trace,
                argsParser.MaximumNumberParallelEvaluations);
            var builderForParsedConfiguration =
                argsParser.ConfigurationBuilder.SetAkkaConfiguration(akkaConfiguration);

            // If the tuning is not a continued one, just use the parsed arguments.
            if (!argsParser.StartFromExistingStatus)
            {
                return builderForParsedConfiguration.Build();
            }

            // Else base the configuration on the one stored in the status file.
            var oldConfig = StatusBase.ReadFromFile<Status<TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy>>(
                    Path.Combine(argsParser.StatusFileDirectory, AlgorithmTunerConfiguration.FileName))
                .Configuration;
            return builderForParsedConfiguration.BuildWithFallback(oldConfig);
        }

        /// <summary>
        /// Adapts Akka.NET configuration to use the correct hostname and port.
        /// </summary>
        /// <param name="ownHostName">
        /// The own host name.
        /// </param>
        /// <param name="port">
        /// The port to use.
        /// </param>
        /// <param name="logOnDebugLevel">
        /// Whether logging should be done on debug level.
        /// </param>
        /// <param name="maximumNumberParallelEvaluations">The maximum number of parallel evaluations per node.</param>
        /// <returns>
        /// The adapted Akka.NET configuration.
        /// </returns>
        private static Config CustomizeAkkaConfiguration(string ownHostName, int port, bool logOnDebugLevel, int maximumNumberParallelEvaluations)
        {
            if (string.IsNullOrWhiteSpace(ownHostName))
            {
                ownHostName = NetworkUtils.GetFullyQualifiedDomainName();
            }

            LoggingHelper.WriteLine(VerbosityLevel.Debug, $"Master's own HostName: {ownHostName}");

            var logClusterEvents = logOnDebugLevel ? "on" : "off";

            var commonBaseConfig = ConfigurationFactory.FromResource(AkkaNames.CommonAkkaConfigFileName, Assembly.GetCallingAssembly());
            var config = ConfigurationFactory.ParseString(
                $@"
                akka
                {{
                    remote.dot-netty.tcp
                    {{
                        hostname={ownHostName}
                        port={port}
                        batching.enabled = false
                    }}
                    cluster.seed-nodes=[""akka.tcp://{AkkaNames.ActorSystemName}@{ownHostName}:{port}""]
                    cluster.log-info = {logClusterEvents}
					actor.deployment {{
                        /{AkkaNames.GenerationEvaluationActor}/{AkkaNames.EvaluationActorRouter} {{
                            router = broadcast-pool # routing strategy
                            cluster {{
                                max-nr-of-instances-per-node = {maximumNumberParallelEvaluations}
                                enabled = on
                                allow-local-routees = on
                            }}
                        }}
                    }}
                }}").WithFallback(commonBaseConfig);

            return logOnDebugLevel
                       ? config.WithFallback(ConfigurationFactory.FromResource(AkkaNames.ExtensiveAkkaLoggingFileName, Assembly.GetCallingAssembly()))
                       : config;
        }

        #endregion
    }
}