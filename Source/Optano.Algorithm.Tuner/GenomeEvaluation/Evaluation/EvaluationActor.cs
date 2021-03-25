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
    using System.Threading;
    using System.Threading.Tasks;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// The evaluation actor is responsible for evaluating single genome instance pairs.
    /// </summary>
    /// <typeparam name="TTargetAlgorithm">The target algorithm type.</typeparam>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    public class EvaluationActor<TTargetAlgorithm, TInstance, TResult> : ReceiveActor, ILogReceive
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult> where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
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
        /// The target algorithm that was configured using the current genome.
        /// </summary>
        private TTargetAlgorithm _configuredTargetAlgorithm;

        /// <summary>
        /// Actor that issued the current evaluation.
        /// </summary>
        private IActorRef _currentEvaluationIssuer;

        /// <summary>
        /// Genome that currently gets evaluated.
        /// </summary>
        private GenomeInstancePair<TInstance> _currentGenomeInstancePair;

        /// <summary>
        /// The evaluation cancellation token source.
        /// </summary>
        private CancellationTokenSource _evaluationCancellationTokenSource;

        /// <summary>
        /// The number of evaluation attempts.
        /// </summary>
        private int _evaluationTries;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EvaluationActor{TTargetAlgorithm, TInstance, TResult}" /> class.
        /// </summary>
        /// <param name="targetAlgorithmFactory">The target algorithm factory.</param>
        /// <param name="configuration">The algorithm tuner configuration.</param>
        /// <param name="parameterTree">The parameter tree.</param>
        /// <param name="generationEvaluationActor">The generation evaluation actor.</param>
        public EvaluationActor(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            AlgorithmTunerConfiguration configuration,
            ParameterTree parameterTree,
            IActorRef generationEvaluationActor)
        {
            LoggingHelper.WriteLine(VerbosityLevel.Info, $"Starting new evaluation actor! Address: {this.Self}");

            this._targetAlgorithmFactory = targetAlgorithmFactory ?? throw new ArgumentNullException(nameof(targetAlgorithmFactory));
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._parameterTree = parameterTree ?? throw new ArgumentNullException(nameof(parameterTree));

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
            this._evaluationCancellationTokenSource?.Cancel();
            this._evaluationCancellationTokenSource?.Dispose();
            this.DisposeTargetAlgorithm();

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
            this.Receive<GenomeInstancePair<TInstance>>(
                evaluation =>
                    {
                        this._currentGenomeInstancePair = evaluation;
                        this._currentEvaluationIssuer = this.Sender;
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
            // ignore all messages
            this.Receive<EvaluationResult<TInstance, TResult>>(
                r =>
                    {
                        this.DisposeTargetAlgorithm();
                        this.Become(this.Ready);
                        this._currentEvaluationIssuer.Tell(r);
                    });

            this.Receive<Faulted<TInstance>>(
                f =>
                    {
                        if (Interlocked.Increment(ref this._evaluationTries) > this._configuration.MaximumNumberConsecutiveFailuresPerEvaluation)
                        {
                            LoggingHelper.WriteLine(
                                VerbosityLevel.Warn,
                                $"Genome {this._currentGenomeInstancePair.Genome.ToFilteredGeneString(this._parameterTree)} does not work with instance {this._currentGenomeInstancePair.Instance}: {f.Reason}.");
                            var ex = f.Reason ?? (Exception)new TaskCanceledException(
                                         $"Aborting evaluation of {this._currentGenomeInstancePair.Genome} on {this._currentGenomeInstancePair.Instance}, since it failed {this._configuration.MaximumNumberConsecutiveFailuresPerEvaluation} times.");

                            this.Become(this.Ready);
                            this.Sender.Tell(new Status.Failure(ex));
                            return;
                        }

                        LoggingHelper.WriteLine(
                            VerbosityLevel.Debug,
                            $"Evaluating {this._currentGenomeInstancePair.Genome.ToFilteredGeneString(this._parameterTree)} on {this._currentGenomeInstancePair.Instance} failed for the {this._evaluationTries} time. Reason: {f.Reason}. Trying again.");
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
                $"Starting target algorithm run with configuration {this._currentGenomeInstancePair.Genome.ToFilteredGeneString(this._parameterTree)} on instance {this._currentGenomeInstancePair.Instance}.");

            // Set the CPU timeout.
            this._evaluationCancellationTokenSource = new CancellationTokenSource(this._configuration.CpuTimeout);

            // Start the target algorithm run.
            this._configuredTargetAlgorithm.Run(this._currentGenomeInstancePair.Instance, this._evaluationCancellationTokenSource.Token)
                .ContinueWith<object>(
                    finishedTask =>
                        {
                            if (finishedTask.IsCanceled)
                            {
                                var cancellationResult = ResultBase<TResult>.CreateCancelledResult(this._configuration.CpuTimeout);
                                return new EvaluationResult<TInstance, TResult>(this._currentGenomeInstancePair, cancellationResult);
                            }

                            if (finishedTask.IsFaulted)
                            {
                                return new Faulted<TInstance>(this._currentGenomeInstancePair, finishedTask.Exception);
                            }

                            return new EvaluationResult<TInstance, TResult>(this._currentGenomeInstancePair, finishedTask.Result);
                        })
                .PipeTo(this.Self, sender: this.Sender);
        }

        /// <summary>
        /// Configures the current target algorithm.
        /// </summary>
        private void ConfigureTargetAlgorithm()
        {
            this._configuredTargetAlgorithm =
                this._targetAlgorithmFactory.ConfigureTargetAlgorithm(this._currentGenomeInstancePair.Genome.GetFilteredGenes(this._parameterTree));
        }

        /// <summary>
        /// Disposes the current target algorithm.
        /// </summary>
        private void DisposeTargetAlgorithm()
        {
            if (this._configuredTargetAlgorithm is IDisposable disposableTargetAlgorithm)
            {
                disposableTargetAlgorithm.Dispose();
            }
        }

        #endregion
    }
}