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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.GrayBox;
    using Optano.Algorithm.Tuner.GrayBox.DataRecordTypes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.Tuning;

    using SharpLearning.RandomForest.Models;

    /// <summary>
    /// The evaluation actor is responsible for evaluating single genome instance pairs.
    /// </summary>
    /// <typeparam name="TTargetAlgorithm">The target algorithm type.</typeparam>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    public class EvaluationActor<TTargetAlgorithm, TInstance, TResult> : ReceiveActor, ILogReceive
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult> where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Static Fields

        /// <summary>
        /// The id counter.
        /// </summary>
        [SuppressMessage(
            "NDepend",
            "ND1210:DontAssignStaticFieldsFromInstanceMethods",
            Justification = "This static field needs to be incremented in the constructor.")]
        // ReSharper disable once StaticMemberInGenericType
        private static int idCounter = -1;

        #endregion

        #region Fields

        /// <summary>
        /// Algorithm tuner configuration parameters.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _configuration;

        /// <summary>
        /// The parameter tree, needed to identify active genes that have to be passed to the target algorithm.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// An object producing configured target algorithms to run with the genomes.
        /// </summary>
        private readonly ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> _targetAlgorithmFactory;

        /// <summary>
        /// The id of the current <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </summary>
        private readonly int _id;

        /// <summary>
        /// The <see cref="ICustomGrayBoxMethods{TResult}"/>.
        /// </summary>
        private readonly ICustomGrayBoxMethods<TResult> _customGrayBoxMethods;

        /// <summary>
        /// The target algorithm that was configured using the current genome.
        /// </summary>
        private TTargetAlgorithm _configuredTargetAlgorithm;

        /// <summary>
        /// The current evaluation.
        /// </summary>
        private GenomeInstancePairEvaluation<TInstance> _currentEvaluation;

        /// <summary>
        /// The generation of the last gray box random forest deserialization try.
        /// This field is used to ensure that it is only tried once per generation per evaluation actor to deserialize the gray box random forest from file.
        /// </summary>
        private int _generationOfLastGrayBoxRandomForestDeserializationTry = -1;

        /// <summary>
        /// The gray box random forest.
        /// </summary>
        private ClassificationForestModel _grayBoxRandomForest;

        /// <summary>
        /// The gray box handler.
        /// </summary>
        private GrayBoxHandler<TInstance, TResult> _grayBoxHandler;

        /// <summary>
        /// The evaluation cancellation token source.
        /// </summary>
        private CancellationTokenSource _evaluationCancellationTokenSource;

        /// <summary>
        /// The number of evaluation attempts.
        /// </summary>
        private int _evaluationTries;

        /// <summary>
        /// Boolean, indicating whether gray box random forest was successfully deserialized in the current generation.
        /// </summary>
        private bool _successfullyDeserializedGrayBoxRandomForest = false;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EvaluationActor{TTargetAlgorithm, TInstance, TResult}" /> class.
        /// </summary>
        /// <param name="targetAlgorithmFactory">The target algorithm factory.</param>
        /// <param name="configuration">The algorithm tuner configuration.</param>
        /// <param name="parameterTree">The parameter tree.</param>
        /// <param name="customGrayBoxMethods">The <see cref="ICustomGrayBoxMethods{TResult}"/>.</param>
        /// <param name="generationEvaluationActor">The generation evaluation actor.</param>
        public EvaluationActor(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            AlgorithmTunerConfiguration configuration,
            ParameterTree parameterTree,
            ICustomGrayBoxMethods<TResult> customGrayBoxMethods,
            IActorRef generationEvaluationActor)
        {
            LoggingHelper.WriteLine(VerbosityLevel.Info, $"Starting new evaluation actor! Address: {this.Self}");

            this._targetAlgorithmFactory = targetAlgorithmFactory ?? throw new ArgumentNullException(nameof(targetAlgorithmFactory));
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._parameterTree = parameterTree ?? throw new ArgumentNullException(nameof(parameterTree));

            if (generationEvaluationActor == null)
            {
                throw new ArgumentNullException(nameof(generationEvaluationActor));
            }

            // No need to check for null. Might be null by purpose.
            this._customGrayBoxMethods = customGrayBoxMethods;

            this._id = Interlocked.Increment(ref EvaluationActor<TTargetAlgorithm, TInstance, TResult>.idCounter);

            this.Become(this.Ready);
            generationEvaluationActor.Tell(new HelloWorld());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets called asynchronously after 'actor.stop()' is invoked.
        /// Makes sure to send a cancellation notice to all remaining evaluation tasks.
        /// </summary>
        protected override void PostStop()
        {
            LoggingHelper.WriteLine(VerbosityLevel.Info, $"Stopping evaluation actor! Address: {this.Self}");

            this._evaluationCancellationTokenSource?.Cancel();
            this._evaluationCancellationTokenSource?.Dispose();
            this.DisposeTargetAlgorithmAndGrayBoxHandler();

            base.PostStop();
        }

        /// <summary>
        /// Actor is ready to process polls sent to it from external senders.
        /// </summary>
        private void Ready()
        {
            this.Receive<Poll>(
                message =>
                    {
                        this.Become(this.WaitingForEvaluation);
                        this.Sender.Tell(new Accept());
                    });
        }

        /// <summary>
        /// Waiting for evaluation state.
        /// </summary>
        private void WaitingForEvaluation()
        {
            this.Receive<GenomeInstancePairEvaluation<TInstance>>(
                evaluation =>
                    {
                        this._currentEvaluation = evaluation;
                        this._evaluationTries = 0;
                        this.ConfigureTargetAlgorithm();
                        this.Become(this.Evaluating);
                        this.StartEvaluation();
                    });

            this.Receive<NoJob>(
                noJob => { this.Become(this.Ready); });
        }

        /// <summary>
        /// Actor has spawned a target algorithm run and is waiting for it to complete.
        /// </summary>
        private void Evaluating()
        {
            this.Receive<EvaluationResult<TInstance, TResult>>(
                result =>
                    {
                        this.DisposeTargetAlgorithmAndGrayBoxHandler();
                        this.Become(this.Ready);
                        this.Sender.Tell(result);
                    });

            this.Receive<Faulted<TInstance>>(
                faulted =>
                    {
                        if (Interlocked.Increment(ref this._evaluationTries) > this._configuration.MaximumNumberConsecutiveFailuresPerEvaluation)
                        {
                            LoggingHelper.WriteLine(
                                VerbosityLevel.Warn,
                                $"Genome {this._currentEvaluation.GenomeInstancePair.Genome.ToFilteredGeneString(this._parameterTree)} does not work with instance {this._currentEvaluation.GenomeInstancePair.Instance}: {faulted.Reason}.");
                            var exception = faulted.Reason ?? new TaskCanceledException(
                                                $"Aborting evaluation of {this._currentEvaluation.GenomeInstancePair.Genome} on {this._currentEvaluation.GenomeInstancePair.Instance}, since it failed {this._configuration.MaximumNumberConsecutiveFailuresPerEvaluation} times.");
                            this.DisposeTargetAlgorithmAndGrayBoxHandler();
                            this.Become(this.Ready);
                            this.Sender.Tell(new Status.Failure(exception));
                            return;
                        }

                        LoggingHelper.WriteLine(
                            VerbosityLevel.Debug,
                            $"Evaluating {this._currentEvaluation.GenomeInstancePair.Genome.ToFilteredGeneString(this._parameterTree)} on {this._currentEvaluation.GenomeInstancePair.Instance} failed for the {this._evaluationTries} time. Reason: {faulted.Reason}. Trying again.");
                        this.StartEvaluation();
                    });
        }

        /// <summary>
        /// Starts an evaluation of the current genome instance pair.
        /// </summary>
        private void StartEvaluation()
        {
            LoggingHelper.WriteLine(
                VerbosityLevel.Trace,
                $"Starting target algorithm run with configuration {this._currentEvaluation.GenomeInstancePair.Genome.ToFilteredGeneString(this._parameterTree)} on instance {this._currentEvaluation.GenomeInstancePair.Instance}.");

            if (this._configuration.EnableDataRecording)
            {
                if (this._currentEvaluation.UseGrayBoxInEvaluation)
                {
                    this.UpdateGrayBoxRandomForest();
                }

                this.ConfigureGrayBoxHandler();
            }

            // Set the CPU timeout.
            this._evaluationCancellationTokenSource = new CancellationTokenSource(this._configuration.CpuTimeout);

            // Start the target algorithm run.
            this._configuredTargetAlgorithm.Run(this._currentEvaluation.GenomeInstancePair.Instance, this._evaluationCancellationTokenSource.Token)
                .ContinueWith<object>(
                    finishedTask =>
                        {
                            if (finishedTask.IsFaulted)
                            {
                                return new Faulted<TInstance>(this._currentEvaluation.GenomeInstancePair, finishedTask.Exception);
                            }

                            var result = finishedTask.IsCanceled
                                             ? ResultBase<TResult>.CreateCancelledResult(this._configuration.CpuTimeout)
                                             : finishedTask.Result;

                            if (this._configuration.EnableDataRecording)
                            {
                                this._grayBoxHandler.WriteListOfDataRecordsToFile(result);
                            }

                            return new EvaluationResult<TInstance, TResult>(this._currentEvaluation.GenomeInstancePair, result);
                        })
                .PipeTo(this.Self, sender: this.Sender);
        }

        /// <summary>
        /// Configures the current target algorithm.
        /// </summary>
        private void ConfigureTargetAlgorithm()
        {
            this._configuredTargetAlgorithm =
                this._targetAlgorithmFactory.ConfigureTargetAlgorithm(
                    this._currentEvaluation.GenomeInstancePair.Genome.GetFilteredGenes(this._parameterTree));
        }

        /// <summary>
        /// Configures the current gray box handler.
        /// </summary>
        private void ConfigureGrayBoxHandler()
        {
            var genomeTransformation = new GenomeTransformation<CategoricalBinaryEncoding>(this._parameterTree);
            var genomeDoubleRepresentation =
                genomeTransformation.ConvertGenomeToArray(this._currentEvaluation.GenomeInstancePair.Genome.CreateMutableGenome());

            var tunerDataRecord = new TunerDataRecord<TResult>(
                NetworkUtils.GetFullyQualifiedDomainName(),
                this._currentEvaluation.GenerationId,
                this._currentEvaluation.TournamentId,
                this._currentEvaluation.GenomeInstancePair.Instance.ToId(),
                double.NaN,
                genomeTransformation.GetConverterColumnHeader(),
                genomeDoubleRepresentation,
                null);

            this._grayBoxHandler = new GrayBoxHandler<TInstance, TResult>(
                this._configuration,
                this._configuredTargetAlgorithm as IGrayBoxTargetAlgorithm<TInstance, TResult>,
                this._id,
                tunerDataRecord,
                this._currentEvaluation.UseGrayBoxInEvaluation && this._successfullyDeserializedGrayBoxRandomForest,
                this._customGrayBoxMethods,
                this._grayBoxRandomForest);
        }

        /// <summary>
        /// Disposes the current target algorithm and gray box handler.
        /// </summary>
        private void DisposeTargetAlgorithmAndGrayBoxHandler()
        {
            if (this._configuredTargetAlgorithm is IDisposable disposableTargetAlgorithm)
            {
                disposableTargetAlgorithm.Dispose();
            }

            this._grayBoxHandler?.Dispose();
        }

        /// <summary>
        /// Updates the gray box random forest.
        /// </summary>
        private void UpdateGrayBoxRandomForest()
        {
            // Try to update gray box random forest only once per generation per evaluation actor.
            if (this._currentEvaluation.GenerationId == this._generationOfLastGrayBoxRandomForestDeserializationTry)
            {
                return;
            }

            this._generationOfLastGrayBoxRandomForestDeserializationTry = this._currentEvaluation.GenerationId;
            this._successfullyDeserializedGrayBoxRandomForest = this.TryToDeserializeGrayBoxRandomForest(out this._grayBoxRandomForest);

            if (!this._successfullyDeserializedGrayBoxRandomForest)
            {
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Disable desired gray box tuning in generation {this._currentEvaluation.GenerationId} on evaluation actor with address {this.Self}!");
            }
        }

        /// <summary>
        /// Tries to deserialize the gray box random forest. This method is the counterpart of TryToSerializeGrayBoxRandomForest in <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </summary>
        /// <param name="grayBoxRandomForest">The gray box random forest.</param>
        /// <returns>True, if successful.</returns>
        private bool TryToDeserializeGrayBoxRandomForest(out ClassificationForestModel grayBoxRandomForest)
        {
            try
            {
                var timer = Stopwatch.StartNew();
                using (var file = File.OpenRead(this._configuration.GrayBoxRandomForestFile.FullName))
                {
                    grayBoxRandomForest = new Hyperion.Serializer().Deserialize<ClassificationForestModel>(file);
                }

                timer.Stop();

                if (grayBoxRandomForest != null)
                {
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Info,
                        $"The deserialization of the gray box random forest before generation {this._currentEvaluation.GenerationId} on evaluation actor with address {this.Self} took {timer.Elapsed.TotalSeconds} seconds.");
                    return true;
                }

                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Cannot deserialize gray box random forest before generation {this._currentEvaluation.GenerationId} on evaluation actor with address {this.Self}, because it is null.");
                return false;
            }
            catch (Exception exception)
            {
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Cannot deserialize gray box random forest before generation {this._currentEvaluation.GenerationId} on evaluation actor with address {this.Self}, because: {exception.Message}");
                grayBoxRandomForest = null;
                return false;
            }
        }

        #endregion
    }
}