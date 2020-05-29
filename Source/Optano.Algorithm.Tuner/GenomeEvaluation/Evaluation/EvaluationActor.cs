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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Communication;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Actor responsible for conducting single genome - instance evaluations.
    /// </summary>
    /// <typeparam name="TTargetAlgorithm">
    /// The algorithm that is tuned.
    /// </typeparam>
    /// <typeparam name="TInstance">
    /// Type of instance the target algorithm is able to process.
    /// Must be a subtype of <see cref="InstanceBase"/>.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// Type of result of a target algorithm run.
    /// Must be a subtype of <see cref="ResultBase{TResultType}"/>.
    /// </typeparam>
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
        /// Reference to the actor which is responsible for storing all evaluation results that have been observed so
        /// far.
        /// </summary>
        private readonly IActorRef _resultStorageActor;

        /// <summary>
        /// An object producing configured target algorithms to run with the genomes.
        /// </summary>
        private readonly ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> _targetAlgorithmFactory;

        /// <summary>
        /// The current evaluation progress.
        /// </summary>
        private readonly GenomeEvaluationProgress<TInstance, TResult> _currentEvaluationProgress;

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
        private ImmutableGenome _currentGenome;

        /// <summary>
        /// The <see cref="CancellationTokenSource" /> used for the most recent evaluation.
        /// </summary>
        private CancellationTokenSource _evaluationCancellationTokenSource;

        /// <summary>
        /// All instances that have to be tested for an evaluation.
        /// </summary>
        private List<TInstance> _instancesForEvaluation;

        /// <summary>
        /// Timeout for the sum of all target runs using the configured algorithm.
        /// </summary>
        private TimeSpan _totalEvaluationTimeout = TimeSpan.MaxValue;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EvaluationActor{TTargetAlgorithm, TInstance, TResult}" /> class.
        /// </summary>
        /// <param name="targetAlgorithmFactory">Produces configured target algorithms to run with the genomes.</param>
        /// <param name="resultStorageActor">
        /// Actor which is responsible for storing all evaluation results that have
        /// been observed so far.
        /// </param>
        /// <param name="configuration">Algorithm tuner configuration parameters.</param>
        /// <param name="parameterTree">Specifies parameters and their relationships.</param>
        public EvaluationActor(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            IActorRef resultStorageActor,
            AlgorithmTunerConfiguration configuration,
            ParameterTree parameterTree)
        {
            this._targetAlgorithmFactory = targetAlgorithmFactory ?? throw new ArgumentNullException(nameof(targetAlgorithmFactory));
            this._resultStorageActor = resultStorageActor ?? throw new ArgumentNullException(nameof(resultStorageActor));
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._parameterTree = parameterTree ?? throw new ArgumentNullException(nameof(parameterTree));

            this._currentEvaluationProgress = new GenomeEvaluationProgress<TInstance, TResult>();

            // Start in wait for instances state.
            this.WaitForInstances();

            // Finally, volunteer for work.
            UntypedActor.Context.System.ActorSelection($"/*/{AkkaNames.GenomeSorter}").Tell(new Accept());
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

            base.PostStop();
        }

        /// <summary>
        /// Actor is ignorant about which instances to evaluate genomes on.
        /// </summary>
        private void WaitForInstances()
        {
            // If a poll comes in, decline and ask for configuration first.
            this.Receive<Poll>(
                poll =>
                    {
                        this.Sender.Tell(new Decline());
                        this.Sender.Tell(new InstancesRequest());
                    });

            // Timeout can be updated.
            this.Receive<UpdateTimeout>(update => this.HandleTimeoutUpdate(update));

            // Timeout can be reset.
            this.Receive<ResetTimeout>(reset => this._totalEvaluationTimeout = TimeSpan.MaxValue);

            // Instances can be updated.
            this.Receive<ClearInstances>(update => this._instancesForEvaluation = new List<TInstance>());
            this.Receive<AddInstances<TInstance>>(update => this._instancesForEvaluation.AddRange(update.Instances));

            // If an instance update finishes successfully, change to ready state.
            this.Receive<InstanceUpdateFinished>(
                update => this.Become(this.Ready),
                update => this._instancesForEvaluation.Count == update.ExpectedInstanceCount);

            // Else request a new instance specification.
            this.Receive<InstanceUpdateFinished>(
                update =>
                    {
                        this.Sender.Tell(new InstancesRequest());
                        LoggingHelper.WriteLine(
                            VerbosityLevel.Warn,
                            $"Request instances again because we received {this._instancesForEvaluation.Count} instead of {update.ExpectedInstanceCount} instances from {this.Sender}.");
                    },
                update => this._instancesForEvaluation.Count != update.ExpectedInstanceCount);
        }

        /// <summary>
        /// Actor is ready to process evaluation tasks sent to it from external senders.
        /// </summary>
        private void Ready()
        {
            // Polls are accepted.
            this.Receive<Poll>(poll => this.Sender.Tell(new Accept()));

            // Timeout can be updated.
            this.Receive<UpdateTimeout>(update => this.HandleTimeoutUpdate(update));

            // Timeout can be reset.
            this.Receive<ResetTimeout>(reset => this._totalEvaluationTimeout = TimeSpan.MaxValue);

            // Instances updates can be started.
            this.Receive<ClearInstances>(
                update =>
                    {
                        this._instancesForEvaluation = new List<TInstance>();
                        this.Become(this.WaitForInstances);
                    });

            // Instance updates without earlier reset indicate missing messages.
            this.Receive<AddInstances<TInstance>>(
                update =>
                    {
                        this.Sender.Tell(new InstancesRequest());
                        this.Become(this.WaitForInstances);
                    });
            this.Receive<InstanceUpdateFinished>(
                update =>
                    {
                        this.Sender.Tell(new InstancesRequest());
                        this.Become(this.WaitForInstances);
                    });

            // Switch to reading from storage state if receiving a genome evaluation.
            this.Receive<GenomeEvaluation>(
                evaluation =>
                    {
                        this._currentEvaluationIssuer = this.Sender;
                        this.BecomeReadingFromStorage(evaluation);
                    });
        }

        /// <summary>
        /// Actor has accepted a <see cref="GenomeEvaluation" /> and migrates to the <see cref="ReadingFromStorage" />
        /// state.
        /// </summary>
        /// <param name="evaluation">The accepted <see cref="GenomeEvaluation" />.</param>
        private void BecomeReadingFromStorage(GenomeEvaluation evaluation)
        {
            // Configure target algorithm.
            this._currentGenome = evaluation.Genome;
            this._configuredTargetAlgorithm = this._targetAlgorithmFactory.ConfigureTargetAlgorithm(
                this._currentGenome.GetFilteredGenes(this._parameterTree));

            // Initialize evaluation progress
            this._currentEvaluationProgress.Initialize(evaluation, this._instancesForEvaluation);

            // Change state to reading from storage.
            this.Become(this.ReadingFromStorage);

            // Start working.
            this.WorkOnNextInstance();
        }

        /// <summary>
        /// Actor is currently trying to read a result from storage.
        /// </summary>
        private void ReadingFromStorage()
        {
            // Polls are declined.
            this.Receive<Poll>(poll => this.Sender.Tell(new Decline()));

            // Timeout can be updated.
            this.Receive<UpdateTimeout>(update => this.HandleTimeoutUpdate(update));

            // Received a result from storage: Remember it and continue working. 
            this.Receive<ResultMessage<TInstance, TResult>>(
                resultMessage =>
                    {
                        this._currentEvaluationProgress.AddResult(resultMessage.RunResult);
                        this.WorkOnNextInstance();
                    });

            // Received a storage miss: Switch state and start running the target algorithm.
            this.Receive<StorageMiss<TInstance>>(
                storageMiss =>
                    {
                        this.Become(this.Evaluating);
                        this.StartEvaluation(storageMiss.Instance);
                    });
        }

        /// <summary>
        /// Actor has spawned a target algorithm run and is waiting for it to complete.
        /// </summary>
        private void Evaluating()
        {
            // Polls are declined.
            this.Receive<Poll>(poll => this.Sender.Tell(new Decline()));

            // Timeout can be updated.
            this.Receive<UpdateTimeout>(update => this.HandleTimeoutUpdate(update));

            // Received a result: Add to storage, remember it yourself, change state to reading from storage,
            // and continue working.
            this.Receive<ResultMessage<TInstance, TResult>>(
                resultMessage =>
                    {
                        LoggingHelper.WriteLine(
                            VerbosityLevel.Trace,
                            $"Target algorithm run of configuration {resultMessage.Genome.ToFilteredGeneString(this._parameterTree)} on instance {resultMessage.Instance} returned with result {resultMessage.RunResult}.");

                        this._resultStorageActor.Tell(resultMessage);
                        this._currentEvaluationProgress.AddResult(resultMessage.RunResult);

                        this.Become(this.ReadingFromStorage);
                        this.WorkOnNextInstance();
                    });

            // Received a message that the last evaluation did not work: If it hasn't happened too often yet, 
            // try again; otherwise, print information and throw an exception for the mini tournament actor to 
            // handle. 
            this.Receive<Faulted<TInstance>>(
                faultMessage =>
                    {
                        // Update the number of faulted evaluations for the instance.
                        var numberFaultedEvaluationsForInstance = this._currentEvaluationProgress.AddFaultedEvaluation(faultMessage.Instance);

                        // If it is small enough, just try again.
                        if (numberFaultedEvaluationsForInstance <= this._configuration.MaximumNumberConsecutiveFailuresPerEvaluation)
                        {
                            LoggingHelper.WriteLine(
                                VerbosityLevel.Debug,
                                $"Evaluating {faultMessage.Genome.ToFilteredGeneString(this._parameterTree)} on {faultMessage.Instance} failed for the {numberFaultedEvaluationsForInstance} time. Reason: {faultMessage.Exception}. Trying again.");
                            this.StartEvaluation(faultMessage.Instance);
                        }

                        // Otherwise, throw an exception.
                        else
                        {
                            LoggingHelper.WriteLine(
                                VerbosityLevel.Warn,
                                $"Genome {this._currentGenome.ToFilteredGeneString(this._parameterTree)} does not work with instance {faultMessage.Instance}: {faultMessage.Exception}.");
                            throw faultMessage.Exception;
                        }
                    });
        }

        /// <summary>
        /// Actor has finished all evaluations and migrates to <see cref="Ready" /> state.
        /// </summary>
        private void BecomeReady()
        {
            // Inform evaluation issuer about results.
            foreach (var message in this._currentEvaluationProgress.CreateEvaluationResultMessages())
            {
                this._currentEvaluationIssuer.Tell(message);
            }

            // Dispose of target algorithm if it is disposable.
            var targetAlgorithmToDispose = this._configuredTargetAlgorithm as IDisposable;
            targetAlgorithmToDispose?.Dispose();

            // Reset evaluation specific fields.
            this._currentEvaluationIssuer = null;
            this._currentEvaluationProgress.Reset();

            // Change state to ready.
            this.Become(this.Ready);
        }

        /// <summary>
        /// Looks for next open instance and starts working on it. If there is no next instance or the timeout is
        /// reached, working is stopped and a switch to <see cref="Ready" /> state is prompted.
        /// </summary>
        private void WorkOnNextInstance()
        {
            // Check status:
            if (this._currentEvaluationProgress.HasOpenEvaluations && !this.ExceededTimeout())
            {
                this.NextStorageRequest();
            }
            else
            {
                this.BecomeReady();
            }
        }

        /// <summary>
        /// Checks whether the mini tournament timeout has been exceeded.
        /// </summary>
        /// <returns>
        /// Whether the timeout has been exceeded.
        /// Always false if <see cref="AlgorithmTunerConfiguration.EnableRacing" /> is false.
        /// </returns>
        private bool ExceededTimeout()
        {
            if (!this._configuration.EnableRacing)
            {
                return false;
            }

            return this._currentEvaluationProgress.TotalRunTime > this._totalEvaluationTimeout;
        }

        /// <summary>
        /// Issues a storage request for the next open instance.
        /// </summary>
        private void NextStorageRequest()
        {
            // Pop an instance.
            var currentInstance = this._currentEvaluationProgress.PopOpenInstance();

            // Try to look it up in storage.
            var resultRequest = new ResultRequest<TInstance>(this._currentGenome, currentInstance);
            this._resultStorageActor.Tell(resultRequest);
        }

        /// <summary>
        /// Starts an evaluation on the given instance.
        /// </summary>
        /// <param name="instance">The instance to run the configured target algorithm on.</param>
        private void StartEvaluation(TInstance instance)
        {
            LoggingHelper.WriteLine(
                VerbosityLevel.Trace,
                $"Starting target algorithm run with configuration {this._currentGenome.ToFilteredGeneString(this._parameterTree)} on instance {instance}.");

            // Set a CPU timeout.
            this._evaluationCancellationTokenSource = new CancellationTokenSource(this._configuration.CpuTimeout);

            // Start the target algorithm run.
            this._configuredTargetAlgorithm.Run(instance, this._evaluationCancellationTokenSource.Token)
                .ContinueWith<object>(
                    finishedTask =>
                        {
                            if (finishedTask.IsCanceled)
                            {
                                var cancellationResult = ResultBase<TResult>.CreateCancelledResult(this._configuration.CpuTimeout);
                                return new ResultMessage<TInstance, TResult>(this._currentGenome, instance, cancellationResult);
                            }

                            if (finishedTask.IsFaulted)
                            {
                                return new Faulted<TInstance>(this._currentGenome, instance, finishedTask.Exception);
                            }

                            return new ResultMessage<TInstance, TResult>(this._currentGenome, instance, finishedTask.Result);
                        })
                .PipeTo(this.Self);
        }

        /// <summary>
        /// Updates the <see cref="_totalEvaluationTimeout" />.
        /// </summary>
        /// <param name="update">Message that caused the update.</param>
        private void HandleTimeoutUpdate(UpdateTimeout update)
        {
            this._totalEvaluationTimeout = TimeSpanUtil.Min(update.Timeout, this._totalEvaluationTimeout);
        }

        #endregion
    }
}