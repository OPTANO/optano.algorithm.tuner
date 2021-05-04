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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.Evaluation
{
    using System;
    using System.IO;
    using System.Linq;

    using Akka.Actor;
    using Akka.Configuration;
    using Akka.TestKit.Xunit2;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Messages;
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
    public class EvaluationActorTest : TestKit
    {
        #region Fields

        /// <summary>
        /// Reference to the generation evaluation actor.
        /// Has to be initialized.
        /// </summary>
        private readonly IActorRef _generationEvaluationActorRef;

        /// <summary>
        /// An actor reference to the <see cref="EvaluationActor{A, I, R}"/> used in tests. Needs to be initialized.
        /// </summary>
        private readonly IActorRef _evaluationActorRef;

        /// <summary>
        /// An <see cref="ImmutableGenome"/> that adheres to the default parameter tree in <see cref="EvaluationActorPropsBuilder"/>.
        /// Needs to be initialized.
        /// </summary>
        private readonly ImmutableGenome _genome;

        /// <summary>
        /// A test instance.
        /// </summary>
        private TestInstance _testInstance = new TestInstance("1");

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
            var targetAlgorithmFactory = new TargetAlgorithmFactory<NoOperation, TestInstance, TestResult>(
                targetAlgorithmCreator: () => new NoOperation(TimeSpan.FromSeconds(1)));

            var resultStorage = this.Sys.ActorOf<ResultStorageActor<TestInstance, TestResult>>();
            this._generationEvaluationActorRef = this.Sys.ActorOf(
                Props.Create(
                    () =>
                        new GenerationEvaluationActor<NoOperation, TestInstance, TestResult>(
                            targetAlgorithmFactory,
                            new KeepSuggestedOrder<TestInstance, TestResult>(),
                            new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().Build(1),
                            resultStorage,
                            new ParameterTree(new ValueNode<int>("value", new IntegerDomain(0, 10, new Allele<int>(0)))),
                            null)));
            this._evaluationActorRef = this.Sys.ActorOf(
                props: new EvaluationActorPropsBuilder().Build(this._generationEvaluationActorRef),
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
                                .Build(this._generationEvaluationActorRef));
                    });
        }

        /// <summary>
        /// Verifies that <see cref="EvaluationActor{A, I, R}"/>'s constructor throws an exception when called without
        /// an <see cref="IActorRef"/> to the generation evaluation actor.
        /// </summary>
        [Fact]
        public void ConstructorThrowsErrorOnMissingGenerationEvaluationActor()
        {
            // Expect an exception on initialization...
            this.EventFilter.Exception<ActorInitializationException>().ExpectOne(
                () =>
                    {
                        // ...when no generation evaluation actor is provided.
                        var evaluationActorRef = this.Sys.ActorOf(
                            new EvaluationActorPropsBuilder().Build(generationEvaluationActorRef: null));
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
                            new EvaluationActorPropsBuilder().SetConfiguration(null).Build(this._generationEvaluationActorRef));
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
                            new EvaluationActorPropsBuilder().SetParameterTree(null).Build(this._generationEvaluationActorRef));
                    });
        }

        /// <summary>
        /// Checks that sending a <see cref="Poll"/> message results in acceptance directly after creating the actor.
        /// </summary>
        [Fact]
        public void PollsAreAcceptedInReadyState()
        {
            ActorRefImplicitSenderExtensions.Tell(this._evaluationActorRef, new Poll());
            this.ExpectMsg<Accept>();
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

            // Start evaluation.
            ((ICanTell)evaluationActorRef).Tell(new Poll(), this._generationEvaluationActorRef);
            evaluationActorRef.Tell(
                new GenomeInstancePairEvaluation<TestInstance>(new GenomeInstancePair<TestInstance>(this._genome, this._testInstance), 0, 0, false));

            // Makes sure it was cancelled after CPU timeout.
            this.IgnoreMessages<Poll>();
            this.IgnoreMessages<GenomeInstancePairEvaluation<TestInstance>>();
            var evaluationResult = this.ExpectMsg<EvaluationResult<TestInstance, TestResult>>();
            Assert.True(
                evaluationResult.RunResult.IsCancelled,
                "Should have returned a single cancellation, but didn't.");
            Assert.Equal(
                cpuTimeout,
                evaluationResult.RunResult.Runtime);
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
                targetAlgorithmCreator: () => new NoOperation(TimeSpan.FromSeconds(30)));

            // Build evaluation actor itself using the created target algorithm factory.
            var actorRef = this.Sys.ActorOf(
                new EvaluationActorPropsBuilder()
                    .SetConfiguration(
                        new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().Build(maximumNumberParallelEvaluations: 1))
                    .SetTargetAlgorithmFactory(targetAlgorithmFactory)
                    .Build(this._generationEvaluationActorRef));

            // Start evaluation.
            ((ICanTell)actorRef).Tell(new Poll(), this._generationEvaluationActorRef);
            ((ICanTell)actorRef).Tell(
                new GenomeInstancePairEvaluation<TestInstance>(new GenomeInstancePair<TestInstance>(this._genome, this._testInstance), 0, 0, false),
                this._generationEvaluationActorRef);

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

            // Expect exception when starting evaluation.

            actorRef.Tell(new Poll());
            actorRef.Tell(
                new GenomeInstancePairEvaluation<TestInstance>(new GenomeInstancePair<TestInstance>(this._genome, this._testInstance), 0, 0, false),
                this.TestActor);

            this.ExpectMsg<Accept>();
            this.ExpectMsg<Status.Failure>();
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

            // Then take a look at console output:
            TestUtils.CheckOutput(
                action: () =>
                    {
                        // Start genome evaluation and therefore provoke failing evaluations.
                        ((ICanTell)actorRef).Tell(new Poll(), this._generationEvaluationActorRef);
                        ((ICanTell)actorRef).Tell(
                            new GenomeInstancePairEvaluation<TestInstance>(
                                new GenomeInstancePair<TestInstance>(this._genome, this._testInstance),
                                0,
                                0,
                                false),
                            this._generationEvaluationActorRef);

                        // Wait a while to really let them fail.
                        this.ExpectNoMsg(milliseconds: 1500);
                    },
                check: consoleOutput =>
                    {
                        // Check that information about it is written to console.
                        string output = new StringReader(consoleOutput.ToString()).ReadToEnd();
                        Assert.True(
                            output.Contains($"Genome {this._genome} does not work with instance 1"),
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
            // produces 1 failure in a row.
            var actorRef = this.CreateEvaluationActorWithRiskyOperation(
                config: new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetMaximumNumberConsecutiveFailuresPerEvaluation(1)
                    .Build(maximumNumberParallelEvaluations: 1),
                failuresInARow: 1);

            // Start evaluation.
            ((ICanTell)actorRef).Tell(new Poll(), this._generationEvaluationActorRef);
            actorRef.Tell(
                new GenomeInstancePairEvaluation<TestInstance>(new GenomeInstancePair<TestInstance>(this._genome, this._testInstance), 0, 0, false));

            // Expect successful results.
            this.ExpectMsg<EvaluationResult<TestInstance, TestResult>>();
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

            // Start evaluation and expect successful results.
            ((ICanTell)actorRef).Tell(new Poll(), this._generationEvaluationActorRef);
            actorRef.Tell(
                new GenomeInstancePairEvaluation<TestInstance>(new GenomeInstancePair<TestInstance>(this._genome, this._testInstance), 0, 0, false));

            this.ExpectMsg<EvaluationResult<TestInstance, TestResult>>(TimeSpan.FromSeconds(120));

            // Start a second evaluation on another genome.
            var secondGenomeData = new Genome();
            secondGenomeData.SetGene("value", new Allele<int>(2));
            ((ICanTell)actorRef).Tell(new Poll(), this._generationEvaluationActorRef);
            actorRef.Tell(
                new GenomeInstancePairEvaluation<TestInstance>(new GenomeInstancePair<TestInstance>(this._genome, this._testInstance), 0, 0, false));

            // Still expect successful results.
            this.ExpectMsg<EvaluationResult<TestInstance, TestResult>>(TimeSpan.FromSeconds(10));
        }

        #endregion

        #region Methods

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
                            config,
                            parameterTree,
                            null,
                            this._generationEvaluationActorRef)));
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
                    .Build(this._generationEvaluationActorRef));
        }

        #endregion

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
            /// <param name="generationEvaluationActorRef">Reference to a generation evaluation actor.</param>
            /// <returns>The props.</returns>
            public Props Build(IActorRef generationEvaluationActorRef)
            {
                return Props.Create(
                    () => new EvaluationActor<NoOperation, TestInstance, TestResult>(
                        this._targetAlgorithmFactory,
                        this._configuration,
                        this._parameterTree,
                        null,
                        generationEvaluationActorRef));
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