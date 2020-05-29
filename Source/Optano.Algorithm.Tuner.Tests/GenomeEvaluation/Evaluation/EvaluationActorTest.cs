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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Akka.Actor;
    using Akka.Configuration;
    using Akka.TestKit.Xunit2;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Communication;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupTwoName)]
    public class EvaluationActorTest : TestKit
    {
        #region Fields

        /// <summary>
        /// Reference to the actor which is responsible for storing all evaluation results that have been observed so
        /// far. Has to be initialized.
        /// </summary>
        private readonly IActorRef _resultStorageActorRef;

        /// <summary>
        /// An actor reference to the <see cref="EvaluationActor{A, I, R}"/> used in tests. Needs to be initialized.
        /// </summary>
        private readonly IActorRef _evaluationActorRef;

        /// <summary>
        /// An <see cref="ImmutableGenome"/> that adheres to the default parameter tree in <see cref="EvaluationActorPropsBuilder"/>.
        /// Needs to be initialized.
        /// </summary>
        private readonly ImmutableGenome _genome;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EvaluationActorTest"/> class.
        /// </summary>
        public EvaluationActorTest()
            : base(ConfigurationFactory.Load().WithFallback(TestKit.DefaultConfig))
        {
            TestUtils.InitializeLogger();
            // Initialize a genome adhering to the parameter tree.
            var genomeData = new Genome();
            genomeData.SetGene("value", new Allele<int>(1));
            this._genome = new ImmutableGenome(genomeData);

            // Initialize the actors.
            this._resultStorageActorRef = this.Sys.ActorOf(
                Props.Create(
                    () =>
                        new ResultStorageActor<TestInstance, TestResult>()),
                AkkaNames.ResultStorageActor);
            this._evaluationActorRef = this.Sys.ActorOf(
                props: new EvaluationActorPropsBuilder().Build(this._resultStorageActorRef),
                name: "EvaluationActor");
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that <see cref="EvaluationActor{A, I, R}"/>'s constructor throws an exception when called without
        /// a <see cref="TargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsErrorOnMissingTargetAlgorithmFactory()
        {
            // Expect an exception on initialization...
            this.EventFilter.Exception<ActorInitializationException>().ExpectOne(
                () =>
                    {
                        // ...when no target algorithm factory is provided.
                        var evaluationActorRef = this.Sys.ActorOf(
                            new EvaluationActorPropsBuilder()
                                .SetTargetAlgorithmFactory(null)
                                .Build(this._resultStorageActorRef));
                    });
        }

        /// <summary>
        /// Verifies that <see cref="EvaluationActor{A, I, R}"/>'s constructor throws an exception when called without
        /// an <see cref="IActorRef"/> to a result storage actor.
        /// </summary>
        [Fact]
        public void ConstructorThrowsErrorOnMissingResultStorageActor()
        {
            // Expect an exception on initialization...
            this.EventFilter.Exception<ActorInitializationException>().ExpectOne(
                () =>
                    {
                        // ...when no result storage actor is provided.
                        var evaluationActorRef = this.Sys.ActorOf(
                            new EvaluationActorPropsBuilder().Build(resultStorageActorRef: null));
                    });
        }

        /// <summary>
        /// Verifies that <see cref="EvaluationActor{A, I, R}"/>'s constructor throws an exception when called without
        /// a <see cref="AlgorithmTunerConfiguration"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsErrorOnMissingAlgorithmTunerConfiguration()
        {
            // Expect an exception on initialization...
            this.EventFilter.Exception<ActorInitializationException>().ExpectOne(
                () =>
                    {
                        // ...when no configuration is provided.
                        var evaluationActorRef = this.Sys.ActorOf(
                            new EvaluationActorPropsBuilder().SetConfiguration(null).Build(this._resultStorageActorRef));
                    });
        }

        /// <summary>
        /// Verifies that <see cref="EvaluationActor{A, I, R}"/>'s constructor throws an exception when called without
        /// a <see cref="ParameterTree"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsErrorOnMissingParameterTree()
        {
            // Expect an exception on initialization...
            this.EventFilter.Exception<ActorInitializationException>().ExpectOne(
                () =>
                    {
                        // ...when no parameter tree is provided.
                        var evaluationActorRef = this.Sys.ActorOf(
                            new EvaluationActorPropsBuilder().SetParameterTree(null).Build(this._resultStorageActorRef));
                    });
        }

        /// <summary>
        /// Checks that sending a <see cref="Poll"/> message results in acceptance directly after creating the actor.
        /// </summary>
        [Fact]
        public void PollsAreAcceptedInReadyState()
        {
            EvaluationActorTest.UpdateNumberInstances(this._evaluationActorRef, number: 1);

            this._evaluationActorRef.Tell(new Poll());
            this.ExpectMsg<Accept>();
        }

        /// <summary>
        /// Check that sending <see cref="ClearInstances"/> and <see cref="AddInstances{TInstance}"/> messages without
        /// any <see cref="InstanceUpdateFinished"/> message results in the actor waiting for the rest of the
        /// instances.
        /// </summary>
        [Fact]
        public void PollsAreDeclinedInWaitingForConfigurationState()
        {
            // Make sure we are in ready state.
            EvaluationActorTest.UpdateNumberInstances(this._evaluationActorRef, number: 1);
            this._evaluationActorRef.Tell(new Poll());
            this.ExpectMsg<Accept>();

            // Then send an incomplete instance update.
            var instances = new List<TestInstance> { new TestInstance("test") };
            var updateMessages = Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages.UpdateInstances.CreateInstanceUpdateMessages(
                instances.ToImmutableList());
            foreach (var message in updateMessages.Where(message => !(message is InstanceUpdateFinished)))
            {
                this._evaluationActorRef.Tell(message);
            }

            // Actor should now request instances on polls.
            this._evaluationActorRef.Tell(new Poll());
            this.ExpectMsg<Decline>();
            this.ExpectMsg<InstancesRequest>();
        }

        /// <summary>
        /// Checks that sending a <see cref="Poll"/> message after the <see cref="EvaluationActor{A, I, R}"/> started
        /// working by looking up a result in storage results in a declination.
        /// </summary>
        [Fact]
        public void PollsAreDeclinedWhenReadingFromStorage()
        {
            // Make sure storage requests take a while.
            var slowStorageMisser = this.Sys.ActorOf(
                props: Props.Create(() => new SlowStorageMisser()),
                name: "SlowStorageMisser");
            var evaluationActorRef = this.Sys.ActorOf(
                props: new EvaluationActorPropsBuilder().Build(resultStorageActorRef: slowStorageMisser),
                name: "SlowEvaluationActor");

            // Set some evaluation instances.
            EvaluationActorTest.UpdateInstances(evaluationActorRef, new List<TestInstance> { new TestInstance("test") });

            // Send evaluation command.
            evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));

            // Send poll.
            evaluationActorRef.Tell(new Poll());
            // Check it is declined.
            this.ExpectMsg<Decline>();
        }

        /// <summary>
        /// Checks that sending a <see cref="Poll"/> message when the <see cref="EvaluationActor{A, I, R}"/> is busy
        /// scheduling a target run results in a declination.
        /// </summary>
        [Fact]
        public void PollsAreDeclinedWhenEvaluating()
        {
            // Make sure target algorithm runs take a while.
            var runtime = TimeSpan.FromMilliseconds(150);
            var targetAlgorithmFactory = new TargetAlgorithmFactory<NoOperation, TestInstance, TestResult>(
                targetAlgorithmCreator: () => new NoOperation(runtime));
            var evaluationActorRef = this.Sys.ActorOf(
                new EvaluationActorPropsBuilder()
                    .SetTargetAlgorithmFactory(targetAlgorithmFactory)
                    .Build(this._resultStorageActorRef));

            // Set some evaluation instances.
            EvaluationActorTest.UpdateInstances(evaluationActorRef, new List<TestInstance> { new TestInstance("test") });

            // Send evaluation command.
            evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));

            // Wait a bit to leave reading from storage state.
            Task.Delay(runtime.Multiply(0.1)).Wait();

            // Send poll.
            evaluationActorRef.Tell(new Poll());
            // Check it is declined.
            this.ExpectMsg<Decline>();
        }

        /// <summary>
        /// Checks that no target algorithm run is used when the result is already cached in storage.
        /// </summary>
        [Fact]
        public void ResultIsTakenFromStorageIfCached()
        {
            // Add a result to storage.
            var instance = new TestInstance("known");
            var knownRuntime = TimeSpan.FromMilliseconds(230);
            this._resultStorageActorRef.Tell(
                new ResultMessage<TestInstance, TestResult>(this._genome, instance, new TestResult(knownRuntime)));

            // Make sure target algorithm runs take a while.
            var runtime = TimeSpan.FromMilliseconds(3000);
            var targetAlgorithmFactory = new TargetAlgorithmFactory<NoOperation, TestInstance, TestResult>(
                targetAlgorithmCreator: () => new NoOperation(runtime));
            var evaluationActorRef = this.Sys.ActorOf(
                new EvaluationActorPropsBuilder()
                    .SetTargetAlgorithmFactory(targetAlgorithmFactory)
                    .Build(this._resultStorageActorRef));

            // Set instances to look at for evaluation to only contain the known instance.
            EvaluationActorTest.UpdateInstances(evaluationActorRef, new List<TestInstance> { instance });

            // Ask for the genome we know the result for.
            evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));

            // Check result is correct and returned before the target algorithm has the chance to return.
            var result = this.ExpectMsg<PartialGenomeEvaluationResults<TestResult>>(
                duration: runtime.Multiply(0.5)).RunResults.Single();
            Assert.Equal(knownRuntime, result.Runtime);
        }

        /// <summary>
        /// Checks that a result is found even if the result is not cached in storage.
        /// </summary>
        [Fact]
        public void ResultIsObtainedInAnotherWayOnCacheMiss()
        {
            // Set some evaluation instance.
            var instance = new TestInstance("unknown");
            EvaluationActorTest.UpdateInstances(this._evaluationActorRef, new List<TestInstance> { instance });

            // Make sure the result is not cached.
            this._resultStorageActorRef.Tell(new ResultRequest<TestInstance>(this._genome, instance));
            this.ExpectMsg<StorageMiss<TestInstance>>();

            // Send evaluation command.
            this._evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));

            // Make sure a run result was obtained.
            var results = this.ExpectMsg<PartialGenomeEvaluationResults<TestResult>>();
            Assert.False(
                results.RunResults.IsEmpty,
                "No run result was found in evaluation results.");
        }

        /// <summary>
        /// Checks that a <see cref="GenomeEvaluation"/> which has no restrictions concerning timeout or CPU
        /// timeout results in result messages which contain a number of results equal to the
        /// number of instances specified by the last instance update.
        /// </summary>
        [Fact]
        public void GenomeGetsEvaluatedOnCorrectNumberOfInstances()
        {
            // Update evaluation instances to two instances.
            int numberOfInstances = 2;
            EvaluationActorTest.UpdateNumberInstances(this._evaluationActorRef, numberOfInstances);

            // Ask for an evaluation.
            this._evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));

            // Check that the correct number of results are returned.
            this.IgnoreMessages<PartialGenomeEvaluationResults<TestResult>>();
            var evaluationFinishedMessage = this.ExpectMsg<GenomeEvaluationFinished>();
            this.CheckForCorrectNumberOfEvaluations(evaluationFinishedMessage, numberOfInstances);
        }

        /// <summary>
        /// Check that sending <see cref="ClearInstances"/> and <see cref="InstanceUpdateFinished"/> in combination
        /// with a wrong number of <see cref="AddInstances{TInstance}"/> messages results in the actor requesting a
        /// resending of that information.
        /// </summary>
        [Fact]
        public void InconsistentInstanceUpdateProvokesNewInstancesRequest()
        {
            this._evaluationActorRef.Tell(new ClearInstances());
            this._evaluationActorRef.Tell(new InstanceUpdateFinished(expectedInstanceCount: 2));
            this.ExpectMsg<InstancesRequest>();
        }

        /// <summary>
        /// Check that sending an <see cref="AddInstances{TInstance}"/> message without any <see cref="ClearInstances"/>
        /// message results in the actor requesting a new update and transferring to the wait for instances state.
        /// </summary>
        [Fact]
        public void AddInstanceMessageWithoutClearMessageResultsInWaitForInstancesState()
        {
            // Make sure we are in ready state.
            EvaluationActorTest.UpdateNumberInstances(this._evaluationActorRef, number: 1);
            this._evaluationActorRef.Tell(new Poll());
            this.ExpectMsg<Accept>();

            // Then send an incomplete instance update.
            var instances = new List<TestInstance> { new TestInstance("test") };
            this._evaluationActorRef.Tell(new AddInstances<TestInstance>(instances));
            this.ExpectMsg<InstancesRequest>();

            // Actor should now request instances on polls.
            this._evaluationActorRef.Tell(new Poll());
            this.ExpectMsg<Decline>();
            this.ExpectMsg<InstancesRequest>();
        }

        /// <summary>
        /// Check that sending a <see cref="InstanceUpdateFinished"/> messages without any <see cref="ClearInstances"/>
        /// message results in the actor requesting a new update and transferring to the wait for instances state.
        /// </summary>
        [Fact]
        public void InstanceUpdateFinishedWithoutClearMessageResultsInWaitForInstancesState()
        {
            // Make sure we are in ready state.
            EvaluationActorTest.UpdateNumberInstances(this._evaluationActorRef, number: 1);
            this._evaluationActorRef.Tell(new Poll());
            this.ExpectMsg<Accept>();

            // Then send an incomplete instance update.
            this._evaluationActorRef.Tell(new InstanceUpdateFinished(0));
            this.ExpectMsg<InstancesRequest>();

            // Actor should now request instances on polls.
            this._evaluationActorRef.Tell(new Poll());
            this.ExpectMsg<Decline>();
            this.ExpectMsg<InstancesRequest>();
        }

        /// <summary>
        /// Checks that a <see cref="GenomeEvaluation"/> started after another one cancelled due to timeout results in
        /// evaluation result messages which contain a number of results equal to the number of
        /// instances specified by the last instance update.
        /// </summary>
        [Fact]
        public void GenomeGetsEvaluatedOnCorrectNumberOfInstancesAfterTimeout()
        {
            // Create an evaluation actor that uses a target algorithm which runs take a significant time.
            var runtime = TimeSpan.FromMilliseconds(15);
            var evaluationActorRef = this.CreateEvaluationActorWithLongRunningTargetAlgorithm(
                config: new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetEnableRacing(true)
                    .Build(maximumNumberParallelEvaluations: 1),
                runtime: runtime);

            // Set a timeout.
            var timeout = TimeSpan.FromMilliseconds(60);
            evaluationActorRef.Tell(new UpdateTimeout(timeout));

            // Update evaluation instances s. t. many runs have to be executed.
            int numberOfInstances = 6;
            EvaluationActorTest.UpdateNumberInstances(evaluationActorRef, numberOfInstances);

            // Make sure timeout would be detected before last instance.
            Assert.True(runtime.Multiply(numberOfInstances - 1) > timeout);

            // Complete one evaluation which will be cancelled due to timeout.
            this.IgnoreMessages<PartialGenomeEvaluationResults<TestResult>>();
            evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));
            this.ExpectMsg<GenomeEvaluationFinished>();

            // Ask for next one with less evaluation instances.
            numberOfInstances = 2;
            EvaluationActorTest.UpdateNumberInstances(evaluationActorRef, numberOfInstances);
            evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 1));

            // Check that the correct number of results are returned.
            this.IgnoreMessages<PartialGenomeEvaluationResults<TestResult>>();
            var evaluationFinishedMessage = this.ExpectMsg<GenomeEvaluationFinished>();
            this.CheckForCorrectNumberOfEvaluations(evaluationFinishedMessage, numberOfInstances);
        }

        /// <summary>
        /// Checks that a <see cref="GenomeEvaluationFinished"/> meesage send in response to a
        /// <see cref="GenomeEvaluation"/> message has the same evaluation ID.
        /// </summary>
        [Fact]
        public void ResultsHaveSameEvaluationIdAsRequest()
        {
            EvaluationActorTest.UpdateNumberInstances(this._evaluationActorRef, 1);

            // Ask for an evaluation with a specific ID.
            this._evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 12));

            // Check that the same ID is returned.
            this.IgnoreMessages<PartialGenomeEvaluationResults<TestResult>>();
            var evaluationFinishedMessage = this.ExpectMsg<GenomeEvaluationFinished>();
            Assert.Equal(
                12,
                evaluationFinishedMessage.EvaluationId);

            // Check that this also works for a second evaluation with different ID.
            this._evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 236));
            this.IgnoreMessages<PartialGenomeEvaluationResults<TestResult>>();
            evaluationFinishedMessage = this.ExpectMsg<GenomeEvaluationFinished>();
            Assert.Equal(
                236,
                evaluationFinishedMessage.EvaluationId);
        }

        /// <summary>
        /// Checks that <see cref="EvaluationActor{A, I, R}"/> disposes of target algorithms implementing
        /// <see cref="IDisposable"/> after evaluation is finished.
        /// </summary>
        [Fact]
        public void TargetAlgorithmGetsDisposedAtEndOfEvaluations()
        {
            // Create a target algorithm that implements IDisposable.
            DisposableNoOp disposableNoOp = new DisposableNoOp();

            // Create evaluation actor using it.
            var disposableFactory = new TargetAlgorithmFactory<NoOperation, TestInstance, TestResult>(
                targetAlgorithmCreator: () => disposableNoOp);
            var evaluationActorForDisposableNoOpRef = this.Sys.ActorOf(
                new EvaluationActorPropsBuilder()
                    .SetTargetAlgorithmFactory(disposableFactory)
                    .Build(this._resultStorageActorRef));

            // Make sure it's not disposed yet.
            Assert.False(disposableNoOp.HasBeenDisposed);

            // Set some evaluation instances.
            EvaluationActorTest.UpdateInstances(evaluationActorForDisposableNoOpRef, new List<TestInstance> { new TestInstance("test") });

            // Send evaluation command and wait until it's finished.
            evaluationActorForDisposableNoOpRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));
            this.IgnoreMessages<PartialGenomeEvaluationResults<TestResult>>();
            this.ExpectMsg<GenomeEvaluationFinished>();
            evaluationActorForDisposableNoOpRef.Tell(new Poll());
            this.ExpectMsg<Accept>();

            // Check target algo got disposed.
            Assert.True(disposableNoOp.HasBeenDisposed);
        }

        /// <summary>
        /// Checks that a <see cref="GenomeEvaluation"/> stops evaluating on new instances when the timeout specified
        /// via <see cref="UpdateTimeout"/> is reached.
        /// </summary>
        [Fact]
        public void InstanceEvaluationsAreStoppedOnTimeout()
        {
            // Create an evaluation actor that uses a target algorithm which runs take a significant time.
            var runtime = TimeSpan.FromMilliseconds(15);
            var evaluationActorRef = this.CreateEvaluationActorWithLongRunningTargetAlgorithm(
                config: new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetEnableRacing(true)
                    .Build(maximumNumberParallelEvaluations: 1),
                runtime: runtime);

            // Update evaluation instances s. t. many runs have to be executed.
            int numberOfInstances = 6;
            EvaluationActorTest.UpdateNumberInstances(evaluationActorRef, numberOfInstances);

            // Set a timeout.
            var timeout = TimeSpan.FromMilliseconds(60);
            evaluationActorRef.Tell(new UpdateTimeout(timeout));

            // Make sure timeout would be detected before last instance.
            Assert.True(runtime.Multiply(numberOfInstances - 1) > timeout);

            // Ask for evaluation.
            evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));

            // Check that instances have only been evaluated until timeout.
            this.IgnoreMessages<PartialGenomeEvaluationResults<TestResult>>();
            var evaluationFinishedMessage = this.ExpectMsg<GenomeEvaluationFinished>();
            this.CheckForCorrectNumberOfEvaluations(
                evaluationFinishedMessage,
                expectedEvaluations: 1 + (int)Math.Floor(timeout.TotalMilliseconds / runtime.TotalMilliseconds));
        }

        /// <summary>
        /// Checks that timeout is ignored if <see cref="AlgorithmTunerConfiguration.EnableRacing"/> is set to false.
        /// </summary>
        [Fact]
        public void TimeoutIsIgnoredWithoutRuntimeTuning()
        {
            // Create an evaluation actor that uses a target algorithm which runs for a significant time.
            // Also turn off runtime tuning.
            var runtime = TimeSpan.FromMilliseconds(15);
            var configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetEnableRacing(false)
                    .Build(maximumNumberParallelEvaluations: 1);
            var evaluationActorRef = this.CreateEvaluationActorWithLongRunningTargetAlgorithm(
                configuration,
                runtime: runtime);

            // Update evaluation instances s. t. many runs have to be executed.
            int numberOfInstances = 6;
            EvaluationActorTest.UpdateNumberInstances(evaluationActorRef, numberOfInstances);

            // Set a timeout.
            var timeout = TimeSpan.FromMilliseconds(60);
            evaluationActorRef.Tell(new UpdateTimeout(timeout));

            // Make sure timeout would be detected before last instance.
            Assert.True(runtime.Multiply(numberOfInstances - 1) > timeout);

            // Ask for evaluation.
            evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));

            // Check that instances have been evaluated even after timeout.
            this.IgnoreMessages<PartialGenomeEvaluationResults<TestResult>>();
            var evaluationFinishedMessage = this.ExpectMsg<GenomeEvaluationFinished>();
            this.CheckForCorrectNumberOfEvaluations(evaluationFinishedMessage, numberOfInstances);
        }

        /// <summary>
        /// Checks that a second <see cref="UpdateTimeout"/> message with an increased timeout does not actually
        /// increase the time allotted for the evaluation, i.e. evaluations are stopped for the smallest timeout that
        /// was communicated.
        /// </summary>
        [Fact]
        public void TimeoutCannotBeIncreased()
        {
            // Create an evaluation actor that uses a target algorithm with runs for a significant time.
            var runtime = TimeSpan.FromMilliseconds(15);
            var evaluationActorRef = this.CreateEvaluationActorWithLongRunningTargetAlgorithm(
                config: new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetEnableRacing(true)
                    .Build(maximumNumberParallelEvaluations: 1),
                runtime: runtime);

            // Update evaluation instances s. t. many runs have to be executed.
            int numberOfInstances = 6;
            EvaluationActorTest.UpdateNumberInstances(evaluationActorRef, numberOfInstances);

            // Set a first timeout.
            var timeout = TimeSpan.FromMilliseconds(60);
            evaluationActorRef.Tell(new UpdateTimeout(timeout));

            // Set a second, higher timeout afterwards.
            evaluationActorRef.Tell(new UpdateTimeout(TimeSpan.FromMilliseconds(10000)));

            // Make sure first timeout would be detected before last instance.
            Assert.True(runtime.Multiply(numberOfInstances - 1) > timeout);

            // Ask for evaluation.
            evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));

            // Check that instances have only been evaluated until the first timeout.
            this.IgnoreMessages<PartialGenomeEvaluationResults<TestResult>>();
            var evaluationFinishedMessage = this.ExpectMsg<GenomeEvaluationFinished>();
            this.CheckForCorrectNumberOfEvaluations(
                evaluationFinishedMessage,
                expectedEvaluations: 1 + (int)Math.Floor((double)(timeout.TotalMilliseconds / runtime.TotalMilliseconds)));
        }

        /// <summary>
        /// Checks that a <see cref="ResetTimeout"/> message sent while not evaluating a genome results in ignorance of
        /// previous timeouts.
        /// </summary>
        [Fact]
        public void ResetTimeoutIsWorking()
        {
            // Create an evaluation actor that uses a target algorithm with runs for a significant time.
            var runtime = TimeSpan.FromMilliseconds(50);
            var evaluationActorRef = this.CreateEvaluationActorWithLongRunningTargetAlgorithm(
                config: new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetEnableRacing(true)
                    .Build(maximumNumberParallelEvaluations: 1),
                runtime: runtime);

            // Update evaluation instances s. t. many runs have to be executed.
            int numberOfInstances = 20;
            EvaluationActorTest.UpdateNumberInstances(evaluationActorRef, numberOfInstances);

            // Set a timeout, then reset it.
            var timeout = TimeSpan.FromMilliseconds(500);
            evaluationActorRef.Tell(new UpdateTimeout(timeout));
            evaluationActorRef.Tell(new ResetTimeout());

            // Make sure timeout would be detected before last instance.
            Assert.True(runtime.Multiply(numberOfInstances - 1) > timeout);

            // Ask for evaluation.
            evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));

            // Check that all instances get evaluated.
            this.IgnoreMessages<PartialGenomeEvaluationResults<TestResult>>();
            var evaluationFinishedMessage = this.ExpectMsg<GenomeEvaluationFinished>();
            this.CheckForCorrectNumberOfEvaluations(evaluationFinishedMessage, numberOfInstances);
        }

        /// <summary>
        /// Checks that a <see cref="AlgorithmTunerConfiguration.CpuTimeout"/> smaller than the target algorithm's runtime
        /// leads to a result with a <see cref="ResultBase{TResultType}.IsCancelled"/>" flag with a runtime of
        /// <see cref="AlgorithmTunerConfiguration.CpuTimeout"/>.
        /// </summary>
        [Fact]
        public void CpuTimeoutLeadsToCancellations()
        {
            // Create an evaluation actor that uses a target algorithm which runs take a significant time.
            // Also add a CPU timeout for each run.
            var runtime = TimeSpan.FromMilliseconds(60);
            var cpuTimeout = TimeSpan.FromMilliseconds(15);
            var evaluationActorRef = this.CreateEvaluationActorWithLongRunningTargetAlgorithm(
                config: new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetCpuTimeout(cpuTimeout).Build(maximumNumberParallelEvaluations: 1),
                runtime: runtime);

            // Add an evaluation instance.
            EvaluationActorTest.UpdateNumberInstances(evaluationActorRef, 1);

            // Start evaluation.
            evaluationActorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));

            // Makes sure it was cancelled after CPU timeout.
            var results = this.ExpectMsg<PartialGenomeEvaluationResults<TestResult>>();
            this.ExpectMsg<GenomeEvaluationFinished>();
            Assert.True(
                results.RunResults.First().IsCancelled,
                "Should have returned a single cancellation, but didn't.");
            Assert.Equal(
                cpuTimeout,
                results.RunResults.First().Runtime);
        }

        /// <summary>
        /// Checks that evaluations get cancelled when the <see cref="EvaluationActor{A, I, R}"/> is stopped.
        /// </summary>
        [Fact]
        public void EvaluationsAreCancelledOnActorStop()
        {
            // Create an evaluation actor with a long running target algorithm.
            // Remember target algorithm factory to later check the created evaluation runs.
            var targetAlgorithmFactory = new TargetAlgorithmFactory<NoOperation, TestInstance, TestResult>(
                targetAlgorithmCreator: () => new NoOperation(TimeSpan.FromSeconds(6)));

            // Build evaluation actor itself using the created target algorithm factory.
            var actorRef = this.Sys.ActorOf(
                new EvaluationActorPropsBuilder()
                    .SetConfiguration(
                        new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().Build(maximumNumberParallelEvaluations: 1))
                    .SetTargetAlgorithmFactory(targetAlgorithmFactory)
                    .Build(this._resultStorageActorRef));

            // Add an evaluation instance.
            EvaluationActorTest.UpdateNumberInstances(actorRef, 1);

            // Start evaluation.
            actorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));

            // Wait a while to make sure the evaluation really started.
            this.ExpectNoMsg(TimeSpan.FromMilliseconds(1000));

            // Stop the actor.
            this.Sys.Stop(actorRef);

            // Wait a while for PostStop to get called.
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));

            // Check evaluation was really cancelled.
            Assert.True(
                targetAlgorithmFactory.CreatedTargetAlgorithms.Single().IsCancellationRequested,
                "Evaluation should have been cancelled.");
        }

        /// <summary>
        /// Checks that <see cref="EvaluationActor{A, I, R}"/> throws an <see cref="AggregateException"/> if a
        /// certain evaluation results in failures repeatedly.
        /// </summary>
        [Fact]
        public void RepetitiveEvaluationFailureThrowsException()
        {
            // Build evaluation actor that allows 1 failure per evaluation, but uses a risky operation that
            // produces 2 failures in a row.
            var actorRef = this.CreateEvaluationActorWithRiskyOperation(
                config: new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetMaximumNumberConsecutiveFailuresPerEvaluation(1)
                    .Build(maximumNumberParallelEvaluations: 1),
                failuresInARow: 2);

            // Add an evaluation instance.
            EvaluationActorTest.UpdateNumberInstances(actorRef, 1);

            // Expect exception when starting evaluation.
            this.EventFilter.Exception<AggregateException>().ExpectOne(() => { actorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0)); });
        }

        /// <summary>
        /// Checks that <see cref="EvaluationActor{A, I, R}"/> prints information to console if a certain
        /// evaluation results in failures repeatedly.
        /// </summary>
        [Fact]
        public void RepetitiveEvaluationFailureGetsPrintedToConsole()
        {
            // Build evaluation actor that allows 1 failure per evaluation, but uses a risky operation that
            // produces 2 failures in a row.
            var actorRef = this.CreateEvaluationActorWithRiskyOperation(
                config: new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetMaximumNumberConsecutiveFailuresPerEvaluation(1)
                    .Build(maximumNumberParallelEvaluations: 1),
                failuresInARow: 2);

            // Add an evaluation instance.
            EvaluationActorTest.UpdateNumberInstances(actorRef, 1);

            // Then take a look at console output:
            TestUtils.CheckOutput(
                action: () =>
                    {
                        // Start genome evaluation and therefore provoke failing evaluations.
                        actorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));

                        // Wait a while to really let them fail.
                        this.ExpectNoMsg(milliseconds: 500);
                    },
                check: consoleOutput =>
                    {
                        // Check that information about it is written to console.
                        string output = new StringReader(consoleOutput.ToString()).ReadToEnd();
                        Assert.True(
                            output.Contains($"Genome {this._genome} does not work with instance 0"),
                            $"Console output {output} does not contain information about problematic evaluation.");
                    });
        }

        /// <summary>
        /// Checks that <see cref="EvaluationActor{A, I, R}"/> keeps working without throwing an exception if
        /// evaluations only fail from time to time and not several times in a row.
        /// </summary>
        [Fact]
        public void FlakyEvaluationGetsRepeatedWithoutException()
        {
            // Build evaluation actor that allows 1 failure per evaluation and uses risky operations that
            // producs 1 failure in a row.
            var actorRef = this.CreateEvaluationActorWithRiskyOperation(
                config: new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetMaximumNumberConsecutiveFailuresPerEvaluation(1)
                    .Build(maximumNumberParallelEvaluations: 1),
                failuresInARow: 1);

            // Add an evaluation instance.
            EvaluationActorTest.UpdateNumberInstances(actorRef, 1);

            // Start evaluation.
            actorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));

            // Expect successful results.
            this.IgnoreMessages<PartialGenomeEvaluationResults<TestResult>>();
            this.ExpectMsg<GenomeEvaluationFinished>();
        }

        /// <summary>
        /// Checks that <see cref="EvaluationActor{A, I, R}"/> doesn't sum up failures on instances when using
        /// different genomes, i.e. even if evaluations on a certain instance fail multiple times, the overall
        /// genome evaluation is still successful if they only fail once per genome.
        /// </summary>
        [Fact]
        public void InstanceFailuresAreResetForNewGenome()
        {
            // Build evaluation actor that allows 1 failure per evaluation and uses risky operations that
            // produces 1 failure in a row.
            var actorRef = this.CreateEvaluationActorWithRiskyOperation(
                config: new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetMaximumNumberConsecutiveFailuresPerEvaluation(1)
                    .Build(1),
                failuresInARow: 1);

            // Add an evaluation instance.
            EvaluationActorTest.UpdateNumberInstances(actorRef, 1);

            // Start evaluation and expect successful results.
            actorRef.Tell(new GenomeEvaluation(this._genome, evaluationId: 0));
            this.IgnoreMessages<PartialGenomeEvaluationResults<TestResult>>();
            this.ExpectMsg<GenomeEvaluationFinished>();

            // Start a second evaluation on another genome.
            var secondGenomeData = new Genome();
            secondGenomeData.SetGene("value", new Allele<int>(2));
            actorRef.Tell(new GenomeEvaluation(new ImmutableGenome(secondGenomeData), evaluationId: 1));

            // Still expect successful results.
            this.IgnoreMessages<PartialGenomeEvaluationResults<TestResult>>();
            this.ExpectMsg<GenomeEvaluationFinished>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sends a <see cref="ClearInstances"/> and a <see cref="AddInstances{TestInstance}"/> message to the
        /// specified actor such that it receives the specified number of newly created <see cref="TestInstance"/>s.
        /// </summary>
        /// <param name="evaluationActor">The actor to send the message to.</param>
        /// <param name="number">Number of instances to create and send.</param>
        private static void UpdateNumberInstances(IActorRef evaluationActor, int number)
        {
            EvaluationActorTest.UpdateInstances(
                evaluationActor,
                instances: Enumerable.Range(0, number).Select(index => new TestInstance(index.ToString())));
        }

        /// <summary>
        /// Sends a <see cref="ClearInstances"/> and a <see cref="AddInstances{TestInstance}"/> message to the
        /// specified actor such that it receives the specified <see cref="TestInstance"/>s.
        /// </summary>
        /// <param name="evaluationActor">The actor to send the message to.</param>
        /// <param name="instances">The instances to send.</param>
        private static void UpdateInstances(IActorRef evaluationActor, IEnumerable<TestInstance> instances)
        {
            var updateMessages = Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages.UpdateInstances.CreateInstanceUpdateMessages(
                instances.ToImmutableList());
            foreach (var message in updateMessages)
            {
                evaluationActor.Tell(message);
            }
        }

        /// <summary>
        /// Creates an <see cref="EvaluationActor{RiskyOperation, TestInstance, TestResult}"/> that uses a target
        /// algorithm factory which produces <see cref="RiskyOperation"/>s.
        /// </summary>
        /// <param name="config"><see cref="AlgorithmTunerConfiguration"/> to use.</param>
        /// <param name="failuresInARow">The desired number of failures in a row per <see cref="RiskyOperation"/>.
        /// </param>
        /// <returns>An <see cref="IActorRef"/> to the evaluation actor.</returns>
        private IActorRef CreateEvaluationActorWithRiskyOperation(AlgorithmTunerConfiguration config, int failuresInARow)
        {
            var parameterTree = new ParameterTree(new ValueNode<int>("value", new IntegerDomain()));
            return this.Sys.ActorOf(
                Props.Create(
                    () =>
                        new EvaluationActor<RiskyOperation, TestInstance, TestResult>(
                            new TargetAlgorithmFactory<RiskyOperation, TestInstance, TestResult>(
                                () => new RiskyOperation(failuresInARow)),
                            this._resultStorageActorRef,
                            config,
                            parameterTree)));
        }

        /// <summary>
        /// Creates an <see cref="EvaluationActor{NoOperation, TestInstance, TestResult}"/> that uses a target
        /// algorithm factory which always produces target algorithms with the specified runtime.
        /// </summary>
        /// <param name="config"><see cref="AlgorithmTunerConfiguration"/> to use.</param>
        /// <param name="runtime">The target algorithm's desired runtime.</param>
        /// <returns>An <see cref="IActorRef"/> to the evaluation actor.</returns>
        private IActorRef CreateEvaluationActorWithLongRunningTargetAlgorithm(
            AlgorithmTunerConfiguration config,
            TimeSpan runtime)
        {
            // Create target algorithm factory with correct target algorithm runtime.
            var targetAlgorithmFactory = new TargetAlgorithmFactory<NoOperation, TestInstance, TestResult>(
                targetAlgorithmCreator: () => new NoOperation(runtime));
            // Create evaluation actor, using the correct configuration and target algorithm factory.
            return this.Sys.ActorOf(
                new EvaluationActorPropsBuilder()
                    .SetConfiguration(config)
                    .SetTargetAlgorithmFactory(targetAlgorithmFactory)
                    .Build(this._resultStorageActorRef));
        }

        /// <summary>
        /// Checks that the provided <see cref="GenomeEvaluationFinished"/> message specifies the correct number of
        /// target algorithm runs.
        /// </summary>
        /// <param name="finishedMessage">The message to check.</param>
        /// <param name="expectedEvaluations">Number of expected total evaluations.</param>
        private void CheckForCorrectNumberOfEvaluations(
            GenomeEvaluationFinished finishedMessage,
            int expectedEvaluations)
        {
            Assert.Equal(
                expectedEvaluations,
                finishedMessage.ExpectedResultCount);
        }

        #endregion

        /// <summary>
        /// A stub for <see cref="ResultStorageActor{TInstance,TResult}"/> that only reacts to
        /// <see cref="ResultRequest{TInstance}"/>: It sends a <see cref="StorageMiss{TestInstance}"/>
        /// after a certain timeframe.
        /// </summary>
        private class SlowStorageMisser : ReceiveActor
        {
            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="SlowStorageMisser"/> class.
            /// </summary>
            public SlowStorageMisser()
            {
                // If the slow storage misser receives a result request, it will answer with a storage miss after
                // waiting for a while.
                this.Receive<ResultRequest<TestInstance>>(
                    request =>
                        {
                            var sender = this.Sender;
                            Task.Delay(millisecondsDelay: 3000).ContinueWith(
                                task => { sender.Tell(new StorageMiss<TestInstance>(request.Genome, request.Instance)); });
                        });
            }

            #endregion
        }

        /// <summary>
        /// Convenience class for building <see cref="Props"/> for creating an
        /// <see cref="EvaluationActor{TTargetAlgorithm, TInstance, TResult}"/> instance.
        /// Specifies default constructor parameters.
        /// </summary>
        private class EvaluationActorPropsBuilder
        {
            #region Fields

            /// <summary>
            /// The <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/> to provide.
            /// </summary>
            private ITargetAlgorithmFactory<NoOperation, TestInstance, TestResult> _targetAlgorithmFactory
                = new TargetAlgorithmFactory<NoOperation, TestInstance, TestResult>(
                    targetAlgorithmCreator: () => new NoOperation());

            /// <summary>
            /// The <see cref="ParameterTree"/> to provide.
            /// </summary>
            private ParameterTree _parameterTree = new ParameterTree(new ValueNode<int>("value", new IntegerDomain()));

            /// <summary>
            /// The <see cref="AlgorithmTunerConfiguration"/> to provide.
            /// </summary>
            private AlgorithmTunerConfiguration _configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetEnableRacing(true).Build(maximumNumberParallelEvaluations: 1);

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// Builds a <see cref="Props"/> object executing
            /// <see cref="EvaluationActor{TTargetAlgorithm, TInstance, TResult}"/>'s constructor using the
            /// configured arguments.
            /// </summary>
            /// <param name="resultStorageActorRef">Reference to a result storage actor.</param>
            /// <returns>The props.</returns>
            public Props Build(IActorRef resultStorageActorRef)
            {
                return Props.Create(
                    () => new EvaluationActor<NoOperation, TestInstance, TestResult>(
                        this._targetAlgorithmFactory,
                        resultStorageActorRef,
                        this._configuration,
                        this._parameterTree));
            }

            /// <summary>
            /// Sets the <see cref="ITargetAlgorithmFactory{NoOperation, TestInstance, EmptyResult}"/> to provide to
            /// the <see cref="EvaluationActor{NoOperation, TestInstance, EmptyResult}"/> constructor.
            /// Default is a factory which creates the same noop target algorithm <see cref="NoOperation"/>
            /// for all inputs.
            /// </summary>
            /// <param name="targetAlgorithmFactory">The target algorithm factory to provide to the evaluation actor
            /// constructor.</param>
            /// <returns>The <see cref="EvaluationActorPropsBuilder"/> in its new state.</returns>
            public EvaluationActorPropsBuilder SetTargetAlgorithmFactory(
                ITargetAlgorithmFactory<NoOperation, TestInstance, TestResult> targetAlgorithmFactory)
            {
                this._targetAlgorithmFactory = targetAlgorithmFactory;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="ParameterTree"/> to provide to the
            /// <see cref="EvaluationActor{NoOperation, TestInstance, EmptyResult}"/> constructor.
            /// Default is a simple parameter tree representing a single integer value.
            /// </summary>
            /// <param name="parameterTree">The parameter tree to provide to the evaluation actor constructor.</param>
            /// <returns>The <see cref="EvaluationActorPropsBuilder"/> in its new state.</returns>
            public EvaluationActorPropsBuilder SetParameterTree(ParameterTree parameterTree)
            {
                this._parameterTree = parameterTree;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="AlgorithmTunerConfiguration"/> to provide to the
            /// <see cref="EvaluationActor{NoOperation, TestInstance, EmptyResult}"/> constructor.
            /// Default is the default <see cref="AlgorithmTunerConfiguration"/> with 1 core.
            /// </summary>
            /// <param name="configuration">The configuration to provide to the evaluation actor constructor.</param>
            /// <returns>The <see cref="EvaluationActorPropsBuilder"/> in its new state.</returns>
            public EvaluationActorPropsBuilder SetConfiguration(AlgorithmTunerConfiguration configuration)
            {
                this._configuration = configuration;
                return this;
            }

            #endregion
        }
    }
}