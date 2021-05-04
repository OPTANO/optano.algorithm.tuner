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

namespace Optano.Algorithm.Tuner.GrayBox.PostTuningRunner
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GrayBox.DataRecordTypes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning.GenomeRepresentation;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Handles a single post tuning run of the provided target algorithm for data recording purposes.
    /// </summary>
    /// <typeparam name="TGrayBoxTargetAlgorithm">The gray box target algorithm type.</typeparam>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    internal class SinglePostTuningRunner<TGrayBoxTargetAlgorithm, TInstance, TResult> : IDisposable
        where TGrayBoxTargetAlgorithm : IGrayBoxTargetAlgorithm<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// The <see cref="AlgorithmTunerConfiguration"/>.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _tunerConfiguration;

        /// <summary>
        /// The instance.
        /// </summary>
        private readonly TInstance _instance;

        /// <summary>
        /// The configured <see cref="ITargetAlgorithm{TInstance,TResult}"/>.
        /// </summary>
        private TGrayBoxTargetAlgorithm _configuredTargetAlgorithm;

        /// <summary>
        /// The gray box handler.
        /// </summary>
        private GrayBoxHandler<TInstance, TResult> _grayBoxHandler;

        /// <summary>
        /// The evaluation cancellation token source.
        /// </summary>
        private CancellationTokenSource _evaluationCancellationTokenSource;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SinglePostTuningRunner{TTargetAlgorithm,TInstance,TResult}"/> class.
        /// </summary>
        /// <param name="tunerConfiguration">The tuner configuration.</param>
        /// <param name="targetAlgorithmFactory">The target algorithm factory.</param>
        /// <param name="parameterTree">The parameter tree.</param>
        /// <param name="postTuningGenomeInstancePair">The post tuning genome instance pair..</param>
        /// <param name="indexOfDesiredPostTuningRun">The index of the desired post tuning run.</param>
        internal SinglePostTuningRunner(
            AlgorithmTunerConfiguration tunerConfiguration,
            ITargetAlgorithmFactory<TGrayBoxTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            ParameterTree parameterTree,
            GenomeInstancePairStringRepresentation postTuningGenomeInstancePair,
            int indexOfDesiredPostTuningRun)
        {
            GrayBoxUtils.ValidateAdditionalPostTuningParameters(tunerConfiguration, targetAlgorithmFactory, parameterTree);

            this._tunerConfiguration = tunerConfiguration;

            this.ConfigureTargetAlgorithmAndGrayBoxHandler(
                targetAlgorithmFactory,
                parameterTree,
                indexOfDesiredPostTuningRun,
                postTuningGenomeInstancePair);

            if (!targetAlgorithmFactory.TryToGetInstanceFromInstanceId(postTuningGenomeInstancePair.Instance, out this._instance))
            {
                throw new ArgumentException($"Cannot convert given instance id {postTuningGenomeInstancePair.Instance} to valid instance.");
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public void Dispose()
        {
            this._evaluationCancellationTokenSource?.Cancel();
            this._evaluationCancellationTokenSource?.Dispose();
            this.DisposeTargetAlgorithmAndGrayBoxHandler();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts the desired single post tuning run.
        /// </summary>
        internal void ExecuteSinglePostTuningRun()
        {
            this._evaluationCancellationTokenSource = new CancellationTokenSource(this._tunerConfiguration.CpuTimeout);
            using var runnerTask = this._configuredTargetAlgorithm.Run(this._instance, this._evaluationCancellationTokenSource.Token);

            try
            {
                // ReSharper disable once MethodSupportsCancellation
                runnerTask.Wait();
            }
            catch (Exception exception)
            {
                LoggingHelper.WriteLine(VerbosityLevel.Debug, exception.ToString());
            }

            if (runnerTask.IsFaulted)
            {
                throw new TaskCanceledException($"{runnerTask.Exception?.Message ?? "Undefined exception!"}");
            }

            var result = runnerTask.IsCanceled ? ResultBase<TResult>.CreateCancelledResult(this._tunerConfiguration.CpuTimeout) : runnerTask.Result;
            this._grayBoxHandler.WriteListOfDataRecordsToFile(result);

            this.DisposeTargetAlgorithmAndGrayBoxHandler();
        }

        /// <summary>
        /// Configures the current target algorithm and gray box handler.
        /// </summary>
        /// <param name="targetAlgorithmFactory">The <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/>.</param>
        /// <param name="parameterTree">The <see cref="ParameterTree"/>.</param>
        /// <param name="indexOfDesiredPostTuningRun">The index of the desired post tuning run.</param>
        /// <param name="postTuningGenomeInstancePair">The post tuning genome instance pair.</param>
        private void ConfigureTargetAlgorithmAndGrayBoxHandler(
            ITargetAlgorithmFactory<TGrayBoxTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            ParameterTree parameterTree,
            int indexOfDesiredPostTuningRun,
            GenomeInstancePairStringRepresentation postTuningGenomeInstancePair)
        {
            var genomeDoubleRepresentation =
                GenomeDoubleRepresentation.GetGenomeDoubleRepresentationFromGenomeIdentifierStringRepresentation(postTuningGenomeInstancePair.Genome);
            var genomeTransformation = new GenomeTransformation<CategoricalBinaryEncoding>(parameterTree);
            var genome = genomeTransformation.ConvertBack(genomeDoubleRepresentation);
            var runnerDictionary = genome.GetFilteredGenes(parameterTree);
            this._configuredTargetAlgorithm = targetAlgorithmFactory.ConfigureTargetAlgorithm(runnerDictionary);

            // Set generationId and tournamentId to -1, since this is a post tuning run.
            var tunerDataRecord = new TunerDataRecord<TResult>(
                NetworkUtils.GetFullyQualifiedDomainName(),
                -1,
                -1,
                postTuningGenomeInstancePair.Instance,
                double.NaN,
                genomeTransformation.GetConverterColumnHeader(),
                genomeDoubleRepresentation,
                null);

            this._grayBoxHandler = new GrayBoxHandler<TInstance, TResult>(
                this._tunerConfiguration,
                this._configuredTargetAlgorithm,
                indexOfDesiredPostTuningRun,
                tunerDataRecord,
                false,
                null,
                null);
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

        #endregion
    }
}