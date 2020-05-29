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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.MiniTournaments.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;

    using Akka.Actor;
    using Akka.Configuration;
    using Akka.TestKit.TestActors;
    using Akka.TestKit.Xunit2;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Communication;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Actors;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    using Xunit;

    using SortByValue = Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration.SortByValue;

    /// <summary>
    /// Contains tests for the <see cref="MiniTournamentActor{TTargetAlgorithm,TInstance,TResult}"/> class.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupTwoName)]
    public class MiniTournamentActorTest : TestKit
    {
        #region Fields

        /// <summary>
        /// Reference to the actor which is responsible for storing all evaluation results that have been observed so
        /// far. Has to be initialized.
        /// </summary>
        private readonly IActorRef _resultStorageActorRef;

        /// <summary>
        /// An actor reference to the <see cref="MiniTournamentActor{A, I, R}"/> used in tests. Has to be
        /// initialized.
        /// </summary>
        private readonly IActorRef _miniTournamentActorRef;

        /// <summary>
        /// Actor reference to an actor not reacting to any messages.
        /// </summary>
        private readonly IActorRef _blackHoleActorRef;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniTournamentActorTest"/> class.
        /// </summary>
        public MiniTournamentActorTest()
            : base(ConfigurationFactory.Load().WithFallback(TestKit.DefaultConfig))
        {
            TestUtils.InitializeLogger();
            // Initialize blackhole actor.
            this._blackHoleActorRef = this.Sys.ActorOf(BlackHoleActor.Props);

            // Initialize other actors.
            this._resultStorageActorRef = this.Sys.ActorOf(
                props: Props.Create(() => new ResultStorageActor<TestInstance, TestResult>()),
                name: AkkaNames.ResultStorageActor);
            this._miniTournamentActorRef = this.Sys.ActorOf(
                props: new MiniTournamentActorPropsBuilder().Build(this._resultStorageActorRef, this._blackHoleActorRef),
                name: "MiniTournamentActor");
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that <see cref="MiniTournamentActor{A, I, R}"/>'s constructor throws an exception when called
        /// without a <see cref="TargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingTargetAlgorithmFactory()
        {
            // Expect an exception on initialization...
            this.EventFilter.Exception<ActorInitializationException>().ExpectOne(
                () =>
                    {
                        // ...when no target algorithm factory is provided.
                        var failingActorRef = this.Sys.ActorOf(
                            new MiniTournamentActorPropsBuilder()
                                .SetTargetAlgorithmFactory(null)
                                .Build(this._resultStorageActorRef, this._blackHoleActorRef));
                    });
        }

        /// <summary>
        /// Verifies that <see cref="MiniTournamentActor{A, I, R}"/>'s constructor throws an exception when called
        /// without an <see cref="IRunEvaluator{TResult}"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingRunEvaluator()
        {
            // Expect an exception on initialization...
            this.EventFilter.Exception<ActorInitializationException>().ExpectOne(
                () =>
                    {
                        // ...when no run evaluator is provided.
                        var failingActorRef = this.Sys.ActorOf(
                            new MiniTournamentActorPropsBuilder()
                                .SetRunEvaluator(null)
                                .Build(this._resultStorageActorRef, this._blackHoleActorRef));
                    });
        }

        /// <summary>
        /// Verifies that <see cref="MiniTournamentActor{A, I, R}"/>'s constructor throws an exception when called
        /// without a <see cref="AlgorithmTunerConfiguration"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingAlgorithmTunerConfiguration()
        {
            // Expect an exception on initialization...
            this.EventFilter.Exception<ActorInitializationException>().ExpectOne(
                () =>
                    {
                        // ...when no configuration is provided.
                        var failingActorRef = this.Sys.ActorOf(
                            new MiniTournamentActorPropsBuilder()
                                .SetConfiguration(null)
                                .Build(this._resultStorageActorRef, this._blackHoleActorRef));
                    });
        }

        /// <summary>
        /// Verifies that <see cref="MiniTournamentActor{A, I, R}"/>'s constructor throws an exception when called
        /// without an <see cref="IActorRef"/> to a result storage actor.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingResultStorageActorRef()
        {
            // Expect an exception on initialization...
            this.EventFilter.Exception<ActorInitializationException>().ExpectOne(
                () =>
                    {
                        // ...when no result storage actor ref is provided.
                        var failingActorRef = this.Sys.ActorOf(
                            new MiniTournamentActorPropsBuilder()
                                .Build(resultStorageActorRef: null, taskProvidingActorRef: this._blackHoleActorRef));
                    });
        }

        /// <summary>
        /// Verifies that <see cref="MiniTournamentActor{A, I, R}"/>'s constructor throws an exception when called
        /// without a <see cref="ParameterTree"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingParameterTree()
        {
            // Expect an exception on initialization...
            this.EventFilter.Exception<ActorInitializationException>().ExpectOne(
                () =>
                    {
                        // ...when no parameter tree is provided.
                        var failingActorRef = this.Sys.ActorOf(
                            new MiniTournamentActorPropsBuilder()
                                .SetParameterTree(null)
                                .Build(this._resultStorageActorRef, this._blackHoleActorRef));
                    });
        }

        /// <summary>
        /// Verifies that <see cref="MiniTournamentActor{A, I, R}"/>'s constructor throws an exception when called
        /// without an <see cref="IActorRef"/> to a <see cref="TournamentSelector{A, I, R}"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingTournamentSelector()
        {
            // Expect an exception on initialization...
            this.EventFilter.Exception<ActorInitializationException>().ExpectOne(
                () =>
                    {
                        // ...when no tournament selector is provided.
                        var failingActorRef = this.Sys.ActorOf(
                            new MiniTournamentActorPropsBuilder()
                                .Build(this._resultStorageActorRef, taskProvidingActorRef: null));
                    });
        }

        /// <summary>
        /// Verifies that starting a <see cref="MiniTournamentActor{A, I, R}"/> with
        /// <see cref="AlgorithmTunerConfiguration.MaximumNumberParallelEvaluations"/> higher than the number of available
        /// processors on the system writes a warning to the console.
        /// </summary>
        [Fact]
        public void NumberOfProcessorsLessThanNumberParallelEvaluationsWritesWarning()
        {
            TestUtils.CheckOutput(
                action: () =>
                    {
                        // Build mini tournament actor requesting too many parallel evaluations.
                        var manyCoresConfiguration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                            .Build(maximumNumberParallelEvaluations: Environment.ProcessorCount + 1);
                        var miniTournamentActorRef = this.Sys.ActorOf(
                            new MiniTournamentActorPropsBuilder()
                                .SetConfiguration(manyCoresConfiguration)
                                .Build(this._resultStorageActorRef, this._blackHoleActorRef));

                        // Make sure PreStart has been called on the actor by interacting with it.
                        miniTournamentActorRef.Tell(new Poll());
                        this.ExpectMsg<Decline>();
                    },
                check: consoleOutput =>
                    {
                        // Check that a warning is written to console.
                        Assert.True(consoleOutput.ToString().Contains("Warning"), $"No warning was written to console.");
                    });
        }

        /// <summary>
        /// Checks that starting a <see cref="MiniTournamentActor{A, I, R}"/> with
        /// <see cref="AlgorithmTunerConfiguration.MaximumNumberParallelEvaluations"/> equal to the number of available
        /// processors on the system does not write a warning to the console.
        /// </summary>
        [Fact]
        public void NumberProcessorsMatchingNumberParallelEvaluationsDoesNotWriteWarning()
        {
            TestUtils.CheckOutput(
                action: () =>
                    {
                        // Build mini tournament actor requesting parallel evaluations equals to available processors.
                        var manyCoresConfiguration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                            .Build(maximumNumberParallelEvaluations: Environment.ProcessorCount);
                        var miniTournamentActorRef = this.Sys.ActorOf(
                            new MiniTournamentActorPropsBuilder()
                                .SetConfiguration(manyCoresConfiguration)
                                .Build(this._resultStorageActorRef, this._blackHoleActorRef));

                        // Make sure PreStart has been called on the actor by interacting with it.
                        miniTournamentActorRef.Tell(new Poll());
                        this.ExpectMsg<Decline>();
                    },
                check: consoleOutput =>
                    {
                        // Check that no warning is written to console.
                        Assert.False(consoleOutput.ToString().Contains("Warning"), "Warning was written to console.");
                    });
        }

        /// <summary>
        /// Checks the number of <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>s the
        /// <see cref="MiniTournamentActor{A, I, R}"/> creates equals
        /// <see cref="AlgorithmTunerConfiguration.MaximumNumberParallelEvaluations"/>.
        /// </summary>
        [Fact]
        public void MiniTournamentActorCreatesCorrectNumberOfEvaluationActors()
        {
            // Create config with a number of parallel runs.
            int parallelRuns = 5;
            var parallelRunConfig = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMaximumMiniTournamentSize(10)
                .Build(maximumNumberParallelEvaluations: parallelRuns);

            // Create fitting actor.
            var miniTournamentActorProps = new MiniTournamentActorPropsBuilder()
                .SetConfiguration(parallelRunConfig)
                .Build(this._resultStorageActorRef, this._blackHoleActorRef);
            var miniTournamentActorRef = this.Sys.ActorOf(miniTournamentActorProps, "parallelMiniTournamentActor");

            // Interact with it to make sure it finished creating its children.
            miniTournamentActorRef.Tell(new Poll());
            this.ExpectMsg<Decline>();
            this.ExpectMsg<InstancesRequest>();

            // Poll all of its evaluation actors.
            this.ActorSelection($"{miniTournamentActorRef.Path}/EvaluationActors/*").Tell(new Poll());

            // Make sure the number of decline messages matches the number of parallel runs specified in the configuration.
            this.IgnoreMessages<InstancesRequest>(instanceRequest => instanceRequest != null);
            for (int i = 0; i < parallelRuns; i++)
            {
                this.FishForMessage<Decline>(isMessage: decline => true);
            }
            this.IgnoreNoMessages();
            this.ExpectNoMsg(milliseconds: 100);
        }

        /// <summary>
        /// Checks that a newly created <see cref="MiniTournamentActor{A, I, R}"/> sends an
        /// <see cref="InstancesRequest"/> to the provided actor reference for its
        /// <see cref="TournamentSelector{A, I, R}"/>.
        /// </summary>
        [Fact]
        public void NewlyCreatedMiniTournamentActorAsksForConfiguration()
        {
            // Build mini tournament actor with the test actor as task provider.
            var miniTournamentActorRef = this.Sys.ActorOf(
                new MiniTournamentActorPropsBuilder()
                    .Build(this._resultStorageActorRef, this.TestActor));
            // Check that it directly asks for configuration.
            this.ExpectMsg<InstancesRequest>();
        }

        /// <summary>
        /// Checks that sending a <see cref="Poll"/> message results in declination if the actor does not know about
        /// any instances yet.
        /// </summary>
        [Fact]
        public void PollsAreDeclinedInWaitForInstancesState()
        {
            this._miniTournamentActorRef.Tell(new Poll());
            this.ExpectMsg<Decline>();
        }

        /// <summary>
        /// Checks that sending a <see cref="Poll"/> message provokes an <see cref="InstancesRequest"/> if the actor
        /// does not know about any instances yet.
        /// </summary>
        [Fact]
        public void PollsProvokeInstancesRequestInWaitForInstancesState()
        {
            this._miniTournamentActorRef.Tell(new Poll());
            this.ExpectMsg<Decline>();
            this.ExpectMsg<InstancesRequest>();
        }

        /// <summary>
        /// Checks that sending a <see cref="Poll"/> message results in acceptance as soon as the actor knows about the
        /// instances to evaluate the genomes on.
        /// </summary>
        [Fact]
        public void PollsAreAcceptedInReadyState()
        {
            MiniTournamentActorTest.UpdateNumberInstances(this._miniTournamentActorRef, number: 1);
            this._miniTournamentActorRef.Tell(new Poll());
            this.ExpectMsg<Accept>();
        }

        /// <summary>
        /// Checks that sending <see cref="ClearInstances"/>, <see cref="AddInstances{TInstance}"/> and
        /// <see cref="InstanceUpdateFinished"/> messages results in an evaluation strategy using the recently
        /// provided instances.
        /// </summary>
        [Fact]
        public void InstanceUpdatesWork()
        {
            // Send two consecutive instance updates.
            var oldInstance = new TestInstance("old");
            var newInstance = new TestInstance("updated");
            MiniTournamentActorTest.UpdateInstances(this._miniTournamentActorRef, new List<TestInstance> { oldInstance });
            MiniTournamentActorTest.UpdateInstances(this._miniTournamentActorRef, new List<TestInstance> { newInstance });

            // Send two evaluations to be conducted by two different evaluation actors.
            var genomes = MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(2);
            this._miniTournamentActorRef.Tell(new MiniTournament(genomes, tournamentId: 0));
            // Wait for completion.
            this.ExpectMsg<MiniTournamentResult<TestResult>>();

            // Check that storage actor has knowledge about the new instance, but not about the old one.
            foreach (var genome in genomes)
            {
                this._resultStorageActorRef.Tell(new ResultRequest<TestInstance>(genome, oldInstance));
                this.ExpectMsg<StorageMiss<TestInstance>>();
                this._resultStorageActorRef.Tell(new ResultRequest<TestInstance>(genome, newInstance));
                this.ExpectMsg<ResultMessage<TestInstance, TestResult>>();
            }
        }

        /// <summary>
        /// Check that sending <see cref="ClearInstances"/> and <see cref="AddInstances{TInstance}"/> messages without
        /// any <see cref="InstanceUpdateFinished"/> message results in the actor waiting for the rest of the
        /// instances.
        /// </summary>
        [Fact]
        public void InstanceUpdateWithoutFinishedMessageResultsInWaitForInstancesState()
        {
            // Make sure we are in ready state.
            MiniTournamentActorTest.UpdateNumberInstances(this._miniTournamentActorRef, number: 1);
            this._miniTournamentActorRef.Tell(new Poll());
            this.ExpectMsg<Accept>();

            // Then send an incomplete instance update.
            var instances = new List<TestInstance> { new TestInstance("test") };
            var updateMessages = Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages.UpdateInstances.CreateInstanceUpdateMessages(
                instances.ToImmutableList());
            foreach (var message in updateMessages.Where(message => !(message is InstanceUpdateFinished)))
            {
                this._miniTournamentActorRef.Tell(message);
            }

            // Actor should now request instances on polls.
            this._miniTournamentActorRef.Tell(new Poll());
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
            MiniTournamentActorTest.UpdateNumberInstances(this._miniTournamentActorRef, number: 1);
            this._miniTournamentActorRef.Tell(new Poll());
            this.ExpectMsg<Accept>();

            // Then send an incomplete instance update.
            this._miniTournamentActorRef.Tell(new InstanceUpdateFinished(0));
            this.ExpectMsg<InstancesRequest>();

            // Actor should now request instances on polls.
            this._miniTournamentActorRef.Tell(new Poll());
            this.ExpectMsg<Decline>();
            this.ExpectMsg<InstancesRequest>();
        }

        /// <summary>
        /// Check that sending an <see cref="AddInstances{TInstance}"/> message without any <see cref="ClearInstances"/>
        /// message results in the actor requesting a new update and transferring to the wait for instances state.
        /// </summary>
        [Fact]
        public void AddInstanceMessageWithoutClearMessageResultsInWaitFoInstancesState()
        {
            // Make sure we are in ready state.
            MiniTournamentActorTest.UpdateNumberInstances(this._miniTournamentActorRef, number: 1);
            this._miniTournamentActorRef.Tell(new Poll());
            this.ExpectMsg<Accept>();

            // Then send an incomplete instance update.
            var instances = new List<TestInstance> { new TestInstance("test") };
            this._miniTournamentActorRef.Tell(new AddInstances<TestInstance>(instances));
            this.ExpectMsg<InstancesRequest>();

            // Actor should now request instances on polls.
            this._miniTournamentActorRef.Tell(new Poll());
            this.ExpectMsg<Decline>();
            this.ExpectMsg<InstancesRequest>();
        }

        /// <summary>
        /// Check that sending <see cref="ClearInstances"/> and <see cref="InstanceUpdateFinished"/> in combination
        /// with a wrong number of <see cref="AddInstances{TInstance}"/> messages results in the actor requesting a
        /// resending of that information.
        /// </summary>
        [Fact]
        public void InconsistentInstanceUpdateProvokesNewInstancesRequest()
        {
            this._miniTournamentActorRef.Tell(new ClearInstances());
            this._miniTournamentActorRef.Tell(new InstanceUpdateFinished(expectedInstanceCount: 2));
            this.ExpectMsg<InstancesRequest>();
        }

        /// <summary>
        /// Checks that sending a <see cref="Poll"/> message does not result in an <see cref="Accept"/> message if the
        /// actor is busy with a <see cref="MiniTournament"/>.
        /// </summary>
        [Fact]
        public void PollsAreNotAcceptedInWorkingState()
        {
            // Send instances to provoke ready state.
            MiniTournamentActorTest.UpdateNumberInstances(this._miniTournamentActorRef, number: 1);

            // Send mini tournament to provoke working state.
            this._miniTournamentActorRef.Tell(new MiniTournament(MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(1), tournamentId: 0));
            this.IgnoreMessages<MiniTournamentResult<TestResult>>();

            // Send poll and verify that it is not accepted.
            this._miniTournamentActorRef.Tell(new Poll());
            this.ExpectNoMsg();
        }

        /// <summary>
        /// Checks that the actor correctly answers <see cref="InstancesRequest"/>s when in ready state.
        /// </summary>
        [Fact]
        public void InstancesRequestAreAnsweredInReadyState()
        {
            // Send instances to provoke ready state.
            var instance = new TestInstance("test");
            MiniTournamentActorTest.UpdateInstances(this._miniTournamentActorRef, new List<TestInstance> { instance });

            this._miniTournamentActorRef.Tell(new InstancesRequest());
            this.ExpectMsg<ClearInstances>();
            var answer = this.ExpectMsg<AddInstances<TestInstance>>();
            this.ExpectMsg<InstanceUpdateFinished>();

            Assert.Equal(instance, answer.Instances.Single());
        }

        /// <summary>
        /// Checks that the actor correctly answers <see cref="InstancesRequest"/>s when in working state.
        /// </summary>
        [Fact]
        public void InstancesRequestAreAnsweredInWorkingState()
        {
            // Send instances to provoke ready state.
            var instance = new TestInstance("test");
            MiniTournamentActorTest.UpdateInstances(this._miniTournamentActorRef, new List<TestInstance> { instance });

            // Send mini tournament to provoke working state.
            this._miniTournamentActorRef.Tell(new MiniTournament(MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(1), tournamentId: 0));
            this.IgnoreMessages<MiniTournamentResult<TestResult>>();

            this._miniTournamentActorRef.Tell(new InstancesRequest());
            this.ExpectMsg<ClearInstances>();
            var answer = this.ExpectMsg<AddInstances<TestInstance>>();
            this.ExpectMsg<InstanceUpdateFinished>();

            Assert.Equal(instance, answer.Instances.Single());
        }

        /// <summary>
        /// Checks that the <see cref="MiniTournamentResult{TResult}"/> message resulting from a <see cref="MiniTournament"/>
        /// request contains a number of genomes corresponding to
        /// <see cref="AlgorithmTunerConfiguration.TournamentWinnerPercentage"/>.
        /// </summary>
        [Fact]
        public void CorrectNumberOfWinnersIsReturned()
        {
            // Build mini tournament actor with a certain winner percentage.
            const double WinnerPercentage = 0.1;
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetTournamentWinnerPercentage(WinnerPercentage).Build(maximumNumberParallelEvaluations: 1);
            var miniTournamentActorRef = this.Sys.ActorOf(
                new MiniTournamentActorPropsBuilder()
                    .SetConfiguration(configuration)
                    .Build(this._resultStorageActorRef, this._blackHoleActorRef));

            // Set an instance to evaluate on.
            MiniTournamentActorTest.UpdateNumberInstances(miniTournamentActorRef, number: 1);

            // Send a mini tournament.
            const int NumberGenomes = 20;
            miniTournamentActorRef.Tell(new MiniTournament(MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(NumberGenomes), tournamentId: 0));

            // Expect correct number winners.
            int expectedNumberWinners = (int)Math.Ceiling(WinnerPercentage * NumberGenomes);
            var winners = this.ExpectMsg<MiniTournamentResult<TestResult>>().WinnerResults.Keys.ToList();
            Assert.Equal(
                expectedNumberWinners,
                winners.Count);
        }

        /// <summary>
        /// Checks that the <see cref="MiniTournamentResult{TResult}"/> message resulting from a <see cref="MiniTournament"/>
        /// request stores a mini tournament ID equal to the request's one.
        /// </summary>
        [Fact]
        public void MiniTournamentResultStoresCorrectMiniTournamentId()
        {
            // Provoke a mini tournament result with a certain ID.
            MiniTournamentActorTest.UpdateNumberInstances(this._miniTournamentActorRef, number: 1);
            var genomes = MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(1);
            this._miniTournamentActorRef.Tell(new MiniTournament(genomes, tournamentId: 7));

            var result = this.ExpectMsg<MiniTournamentResult<TestResult>>();
            Assert.Equal(7, result.MiniTournamentId);
        }

        /// <summary>
        /// Checks that the <see cref="MiniTournamentResult{TResult}"/> message resulting from a <see cref="MiniTournament"/>
        /// request stores the participants sorted by performance.
        /// </summary>
        [Fact]
        public void MiniTournamentResultStoresParticipantsOrderedByPerformance()
        {
            // Create some genomes.
            const int NumberGenomes = 20;
            var genomes = MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(NumberGenomes);

            // Create mini tournament actor using an evaluator which sorts by a certain parameter, highest first.
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().Build(maximumNumberParallelEvaluations: 1);
            var miniTournamentActorProps = Props.Create(
                () =>
                    new MiniTournamentActor<ExtractIntegerValue, TestInstance, IntegerResult>(
                        new ExtractIntegerValueCreator(),
                        new SortByValue(),
                        configuration,
                        this._resultStorageActorRef,
                        MiniTournamentActorTest.CreateParameterTree(),
                        this._blackHoleActorRef));
            var miniTournamentActorRef = this.Sys.ActorOf(miniTournamentActorProps);

            // Set an instance to evaluate on.
            MiniTournamentActorTest.UpdateNumberInstances(miniTournamentActorRef, number: 1);

            // Send a mini tournament.
            miniTournamentActorRef.Tell(new MiniTournament(genomes, tournamentId: 0));

            // Check if participants are sorted by "value" parameter, highest first.
            var expectedOrder = genomes
                .Select(genome => genome.CreateMutableGenome())
                .OrderByDescending(genome => (int)genome.GetGeneValue(ExtractIntegerValue.ParameterName).GetValue())
                .Select(genome => new ImmutableGenome(genome))
                .ToList();
            var actualOrder = this.ExpectMsg<MiniTournamentResult<IntegerResult>>().AllFinishedOrdered;
            Assert.True(
                expectedOrder.SequenceEqual(actualOrder, new ImmutableGenome.GeneValueComparer()),
                $"Genomes were not sorted by value: Expected {TestUtils.PrintList(expectedOrder)} but got {TestUtils.PrintList(actualOrder)}.");
        }

        /// <summary>
        /// Checks that the <see cref="MiniTournamentResult{TResult}"/> message resulting from a <see cref="MiniTournament"/>
        /// request stores all of the winners' results.
        /// </summary>
        [Fact]
        public void MiniTournamentResultContainsWinnerResults()
        {
            // Create a configuration using a winner percentage of almost 1.
            const double WinnerPercentage = 0.9;
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetTournamentWinnerPercentage(WinnerPercentage).Build(maximumNumberParallelEvaluations: 1);

            // Create mini tournament actor using a runner which extracts a certain parameter.
            var miniTournamentActorProps = Props.Create(
                () =>
                    new MiniTournamentActor<ExtractIntegerValue, TestInstance, IntegerResult>(
                        new ExtractIntegerValueCreator(),
                        new SortByValue(),
                        configuration,
                        this._resultStorageActorRef,
                        MiniTournamentActorTest.CreateParameterTree(),
                        this._blackHoleActorRef));
            var miniTournamentActorRef = this.Sys.ActorOf(miniTournamentActorProps);

            // Set a lot of instances to evaluate on.
            MiniTournamentActorTest.UpdateNumberInstances(miniTournamentActorRef, number: 102);

            // Send a mini tournament.
            var genomes = MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(2);
            miniTournamentActorRef.Tell(new MiniTournament(genomes, tournamentId: 1));

            // Check each winner comes with 102 results storing the correct parameter value.
            var winners = this.ExpectMsg<MiniTournamentResult<IntegerResult>>().WinnerResults.WithComparers(new ImmutableGenome.GeneValueComparer());
            foreach (var genome in genomes)
            {
                Assert.Contains(genome, winners.Keys, new ImmutableGenome.GeneValueComparer());
                Assert.Equal(
                    102,
                    winners[genome].Count);
                Assert.True(
                    winners[genome].All(
                        result => result.Value.Equals(genome.CreateMutableGenome().GetGeneValue(ExtractIntegerValue.ParameterName).GetValue())),
                    "Some result was not as expected.");
            }
        }

        /// <summary>
        /// Checks that a timeout gets activated if a sufficient number of genomes was evaluated successfully to ensure
        /// the desired number of winner candidates.
        /// </summary>
        [Fact]
        public void TimeoutGetsUpdatedAfterSufficientNumberOfSuccessfulGenomes()
        {
            // Create a mini tournament actor using a target algorithm that needs some time.
            var runtime = TimeSpan.FromMilliseconds(30);
            var configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetEnableRacing(true).Build(maximumNumberParallelEvaluations: 1);
            var miniTournamentActorRef = this.CreateEvaluationActorWithLongRunningTargetAlgorithm(
                configuration,
                runtime);

            // Update evaluation instances s. t. many runs have to be executed.
            const int NumberOfInstances = 6;
            var instances = Enumerable.Range(0, NumberOfInstances)
                .Select(index => new TestInstance(index.ToString())).ToList();
            MiniTournamentActorTest.UpdateInstances(miniTournamentActorRef, instances);

            // Start tournament.
            const int NumberOfGenomes = 1;
            var genomes = MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(NumberOfGenomes);
            miniTournamentActorRef.Tell(new MiniTournament(genomes, tournamentId: 0));

            // Fake some genomes successfully completing.
            var genomeTimeout = TimeSpan.FromMilliseconds(60);
            MiniTournamentActorTest.FakeSuccessfullyCompletingEvaluations(miniTournamentActorRef, NumberOfGenomes, timeNeeded: genomeTimeout);

            // Finish tournament.
            this.ExpectMsg<MiniTournamentResult<TestResult>>();

            // Make sure timeout would be detected before last instance.
            int expectedInstancesPerGenome = 1 + (int)Math.Floor(genomeTimeout.TotalMilliseconds / runtime.TotalMilliseconds);
            Assert.True(expectedInstancesPerGenome < NumberOfInstances);

            // Check that instances have only been evaluated until timeout.
            this.CheckCorrectNumberOfEvaluations(genomes, instances, expectedInstancesPerGenome);
        }

        /// <summary>
        /// Checks that timeout gets reset after one mini tournament was finished s. t. a newly started mini
        /// tournament won't be hindered by it.
        /// </summary>
        [Fact]
        public void TimeoutGetsResetAfterTournamentCompletion()
        {
            // Create a mini tournament actor using a target algorithm that needs some time.
            var runtime = TimeSpan.FromMilliseconds(30);
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetEnableRacing(true)
                .Build(maximumNumberParallelEvaluations: 1);
            var miniTournamentActorRef = this.CreateEvaluationActorWithLongRunningTargetAlgorithm(
                configuration,
                runtime);

            // Update evaluation instances s. t. many runs have to be executed.
            const int NumberOfInstances = 6;
            var instances = Enumerable.Range(0, NumberOfInstances)
                .Select(index => new TestInstance(index.ToString())).ToList();
            MiniTournamentActorTest.UpdateInstances(miniTournamentActorRef, instances);

            // Start tournament.
            const int NumberOfGenomes = 1;
            var genomes = MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(NumberOfGenomes);
            miniTournamentActorRef.Tell(new MiniTournament(genomes, tournamentId: 0));

            // Fake some genomes successfully completing.
            var genomeTimeout = TimeSpan.FromMilliseconds(60);
            MiniTournamentActorTest.FakeSuccessfullyCompletingEvaluations(miniTournamentActorRef, NumberOfGenomes, timeNeeded: genomeTimeout);

            // Finish tournament.
            this.ExpectMsg<MiniTournamentResult<TestResult>>();

            // Start and finish a new tournament.
            miniTournamentActorRef.Tell(new MiniTournament(genomes, tournamentId: 1));
            this.ExpectMsg<MiniTournamentResult<TestResult>>();

            // Make sure no timeout was used.
            this.CheckCorrectNumberOfEvaluations(genomes, instances, NumberOfInstances);
        }

        /// <summary>
        /// Checks that no timeout gets activated if the number of successfully evaluated genomes does not cover the
        /// desired number of winner candidates.
        /// </summary>
        [Fact]
        public void TimeoutDoesNotGetUpdatedAfterInsufficientNumberOfSuccesfulGenomes()
        {
            // Use a certain percentage of winners.
            const double WinnerPercentage = 0.5;
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEnableRacing(true)
                .SetTournamentWinnerPercentage(WinnerPercentage)
                .Build(maximumNumberParallelEvaluations: 1);

            // Determine the desired number of winners.
            const int NumberOfGenomes = 4;
            int winners = (int)Math.Ceiling(WinnerPercentage * NumberOfGenomes);

            // Create a mini tournament actor using a target algorithm that needs some time.
            var runtime = TimeSpan.FromMilliseconds(30);
            var miniTournamentActorRef =
                this.CreateEvaluationActorWithLongRunningTargetAlgorithm(configuration, runtime);

            // Update evaluation instances s. t. many runs have to be executed.
            const int NumberOfInstances = 4;
            List<TestInstance> instances = Enumerable.Range(0, NumberOfInstances)
                .Select(index => new TestInstance(index.ToString())).ToList();
            MiniTournamentActorTest.UpdateInstances(miniTournamentActorRef, instances);

            // Start tournament.
            var genomes = MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(NumberOfGenomes);
            miniTournamentActorRef.Tell(new MiniTournament(genomes, tournamentId: 0));

            // Fake some genome evaluations:
            // Fake genomes successfully completing (one less than needed for timeout activation)
            var genomeTimeout = TimeSpan.FromMilliseconds(60);
            MiniTournamentActorTest.FakeSuccessfullyCompletingEvaluations(
                miniTournamentActorRef,
                number: winners - 1,
                timeNeeded: genomeTimeout);
            // Also fake a genome that was cancelled once.
            var evaluationResultMessages = CompleteGenomeEvaluationResults.CreateEvaluationResultMessages(
                evaluationId: winners - 1,
                runResults: new List<TestResult>() { TestResult.CreateCancelledResult(TimeSpan.Zero) });
            foreach (var message in evaluationResultMessages)
            {
                miniTournamentActorRef.Tell(message);
            }

            // Finish tournament.
            this.ExpectMsg<MiniTournamentResult<TestResult>>();

            // Make sure timeout would be detected before last instance.
            Assert.True(runtime.Multiply(NumberOfInstances - 1) > genomeTimeout);

            // Check that instances have been evaluated even after timeout.
            this.CheckCorrectNumberOfEvaluations(genomes, instances, NumberOfInstances);
        }

        /// <summary>
        /// Checks that no timeout gets activated if runtime tuning is turned off.
        /// </summary>
        [Fact]
        public void TimeoutDoesNotGetUpdatedWithoutRuntimeTuning()
        {
            // Create a mini tournament actor without runtime tuning but using a target algorithm that needs some time.
            var runtime = TimeSpan.FromMilliseconds(30);
            var configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                    .SetEnableRacing(false)
                    .Build(maximumNumberParallelEvaluations: 1);
            var miniTournamentActorRef = this.CreateEvaluationActorWithLongRunningTargetAlgorithm(
                configuration,
                runtime);

            // Update evaluation instances s. t. many runs have to be executed.
            int numberOfInstances = 6;
            var instances = Enumerable.Range(0, numberOfInstances)
                .Select(index => new TestInstance(index.ToString())).ToList();
            MiniTournamentActorTest.UpdateInstances(miniTournamentActorRef, instances);

            // Start tournament.
            const int NumberOfGenomes = 1;
            var genomes = MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(NumberOfGenomes);
            miniTournamentActorRef.Tell(new MiniTournament(genomes, tournamentId: 0));

            // Fake some genomes successfully completing.
            var genomeTimeout = TimeSpan.FromMilliseconds(60);
            MiniTournamentActorTest.FakeSuccessfullyCompletingEvaluations(miniTournamentActorRef, NumberOfGenomes, timeNeeded: genomeTimeout);

            // Finish tournament.
            this.ExpectMsg<MiniTournamentResult<TestResult>>();

            // Make sure timeout would be detected before last instance.
            int expectedInstancesPerGenome = 1 + (int)Math.Floor(genomeTimeout.TotalMilliseconds / runtime.TotalMilliseconds);
            Assert.True(expectedInstancesPerGenome < numberOfInstances);

            // Check that instances have continued to be evaluated after timeout.
            this.CheckCorrectNumberOfEvaluations(genomes, instances, numberOfInstances);
        }

        /// <summary>
        /// Checks that timeout gets updated if fitter genomes are found.
        /// </summary>
        [Fact]
        public void TimeoutGetsUpdatedOnNewFitGenome()
        {
            // Create a mini tournament actor using a target algorithm that needs some time.
            var runtime = TimeSpan.FromMilliseconds(30);
            var configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetEnableRacing(true).Build(maximumNumberParallelEvaluations: 1);
            var miniTournamentActorRef = this.CreateEvaluationActorWithLongRunningTargetAlgorithm(
                configuration,
                runtime);

            // Update evaluation instances s. t. many runs have to be executed.
            int numberOfInstances = 6;
            var instances = Enumerable.Range(0, numberOfInstances)
                .Select(index => new TestInstance(index.ToString())).ToList();
            MiniTournamentActorTest.UpdateInstances(miniTournamentActorRef, instances);

            // Start tournament.
            int numberOfGenomes = 2;
            var genomes = MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(numberOfGenomes);
            miniTournamentActorRef.Tell(new MiniTournament(genomes, tournamentId: 0));

            // Fake some other genomes successfully completing.
            var genomeTimeout = TimeSpan.FromMilliseconds(60);
            var runResults = new List<TestResult> { new TestResult(genomeTimeout) };
            foreach (var message in CompleteGenomeEvaluationResults.CreateEvaluationResultMessages(2, runResults))
            {
                miniTournamentActorRef.Tell(message);
            }

            // Fake a genome with even better completion time.
            var betterTimeout = TimeSpan.FromMilliseconds(30);
            runResults = new List<TestResult> { new TestResult(betterTimeout) };
            foreach (var message in CompleteGenomeEvaluationResults.CreateEvaluationResultMessages(3, runResults))
            {
                miniTournamentActorRef.Tell(message);
            }

            // Finish tournament.
            this.ExpectMsg<MiniTournamentResult<TestResult>>();

            // Check that instances have only been evaluated only until the second timeout.
            // Make sure second timeout would be detected before last instance.
            int expectedInstancesPerGenome = 1 + (int)Math.Floor(betterTimeout.TotalMilliseconds / runtime.TotalMilliseconds);
            Assert.True(expectedInstancesPerGenome < numberOfInstances);
            this.CheckCorrectNumberOfEvaluations(genomes, instances, expectedInstancesPerGenome);
        }

        /// <summary>
        /// Checks that timeout computation only considers finished evaluations.
        /// </summary>
        [Fact]
        public void TimeoutOnlyConsideredFinishedGenomes()
        {
            // Create a mini tournament actor using a target algorithm that needs some time.
            var runtime = TimeSpan.FromMilliseconds(30);
            var configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetEnableRacing(true).Build(maximumNumberParallelEvaluations: 1);
            var miniTournamentActorRef = this.CreateEvaluationActorWithLongRunningTargetAlgorithm(
                configuration,
                runtime);

            // Update evaluation instances s. t. many runs have to be executed.
            int numberOfInstances = 6;
            var instances = Enumerable.Range(0, numberOfInstances)
                .Select(index => new TestInstance(index.ToString())).ToList();
            MiniTournamentActorTest.UpdateInstances(miniTournamentActorRef, instances);

            // Start tournament.
            int numberOfGenomes = 2;
            var genomes = MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(numberOfGenomes);
            miniTournamentActorRef.Tell(new MiniTournament(genomes, tournamentId: 0));

            // Fake some other genomes successfully completing.
            var genomeTimeout = TimeSpan.FromMilliseconds(60);
            var runResults = new List<TestResult> { new TestResult(genomeTimeout) };
            foreach (var message in CompleteGenomeEvaluationResults.CreateEvaluationResultMessages(2, runResults))
            {
                miniTournamentActorRef.Tell(message);
            }

            // Fake a genome with even better completion time, but which does not send a GenomeEvaluationFinished message.
            var betterTimeout = TimeSpan.FromMilliseconds(30);
            runResults = new List<TestResult> { new TestResult(betterTimeout) };
            foreach (var message in CompleteGenomeEvaluationResults.CreateEvaluationResultMessages(3, runResults))
            {
                if (message is GenomeEvaluationFinished)
                {
                    continue;
                }

                miniTournamentActorRef.Tell(message);
            }

            // Finish tournament.
            this.ExpectMsg<MiniTournamentResult<TestResult>>();

            // Check that instances have only been evaluated only until the first timeout.
            // Make sure first timeout would be detected before last instance.
            int expectedInstancesPerGenome = 1 + (int)Math.Floor(genomeTimeout.TotalMilliseconds / runtime.TotalMilliseconds);
            Assert.True(expectedInstancesPerGenome < numberOfInstances);
            this.CheckCorrectNumberOfEvaluations(genomes, instances, expectedInstancesPerGenome);
        }

        /// <summary>
        /// Checks that sending a <see cref="Poll"/> message results in acceptance directly after a
        /// <see cref="MiniTournament"/> was completed.
        /// </summary>
        [Fact]
        public void ActorAcceptsPollsAfterTournamentCompletion()
        {
            // Send instances to provoke ready state.
            MiniTournamentActorTest.UpdateNumberInstances(this._miniTournamentActorRef, number: 1);
            // Send mini tournament to provoke working state.
            this._miniTournamentActorRef.Tell(new MiniTournament(MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(1), tournamentId: 0));
            // Wait for completion.
            this.ExpectMsg<MiniTournamentResult<TestResult>>();
            // Send poll.
            this._miniTournamentActorRef.Tell(new Poll());
            // Check it is accepted.
            this.ExpectMsg<Accept>();
        }

        /// <summary>
        /// Checks that <see cref="MiniTournamentActor{A,I,R}"/> terminates the complete actor system if one of its
        /// <see cref="EvaluationActor{A,I,R}"/>s throws an exception.
        /// </summary>
        [Fact]
        public void ActorSystemIsTerminatedOnEvaluationError()
        {
            // Create mini tournament actor with two evaluation actors.
            // We use a risky operation here that will fail.
            var config = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMaximumNumberConsecutiveFailuresPerEvaluation(0)
                .Build(maximumNumberParallelEvaluations: 2);
            var failingMiniTournamentActor = this.Sys.ActorOf(
                Props.Create(
                    () => new MiniTournamentActor<RiskyOperation, TestInstance, TestResult>(
                        new TargetAlgorithmFactory<RiskyOperation, TestInstance, TestResult>(() => new RiskyOperation(10)),
                        new KeepSuggestedOrder<TestResult>(),
                        config,
                        this._resultStorageActorRef,
                        MiniTournamentActorTest.CreateParameterTree(),
                        this._blackHoleActorRef)),
                "failingMiniTournamentActor");

            // Listen to system termination.
            bool systemIsTerminated = false;
            this.Sys.RegisterOnTermination(() => systemIsTerminated = true);

            // Start a mini tournament to provoke failure.
            MiniTournamentActorTest.UpdateNumberInstances(failingMiniTournamentActor, number: 1);
            failingMiniTournamentActor.Tell(new MiniTournament(MiniTournamentActorTest.CreateGenomesAdheringToParameterTree(1), 1));

            // Wait a while to give the program a chance to terminate the actor system.
            Thread.Sleep(1000);

            // Make sure the actor system is terminated after a while.
            Assert.True(systemIsTerminated, "System should have been terminated.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a <see cref="ParameterTree"/> that consists only of a root node: An integer with key
        /// <see cref="ExtractIntegerValue.ParameterName"/>.
        /// </summary>
        /// <returns>The parameter tree.</returns>
        private static ParameterTree CreateParameterTree()
        {
            return new ParameterTree(
                root: new ValueNode<int>(ExtractIntegerValue.ParameterName, new IntegerDomain()));
        }

        /// <summary>
        /// Create <see cref="ImmutableGenome"/> instances that fit the <see cref="ParameterTree"/> constructed in
        /// <see cref="MiniTournamentActorTest.CreateParameterTree"/>.
        /// All instances will have different gene values.
        /// </summary>
        /// <param name="number">Number of instances to create.</param>
        /// <returns>The created instances.</returns>
        private static List<ImmutableGenome> CreateGenomesAdheringToParameterTree(int number)
        {
            var genomes = new List<ImmutableGenome>(number);
            for (int i = 0; i < number; i++)
            {
                var genome = new Genome();
                genome.SetGene(ExtractIntegerValue.ParameterName, new Allele<int>(i));
                genomes.Add(new ImmutableGenome(genome));
            }

            return genomes;
        }

        /// <summary>
        /// Sends a <see cref="ClearInstances"/> and a <see cref="AddInstances{TestInstance}"/> message to the
        /// specified actor such that it receives the specified number of newly created <see cref="TestInstance"/>s.
        /// </summary>
        /// <param name="miniTournamentActor">The actor to send the message to.</param>
        /// <param name="number">Number of instances to create and send.</param>
        private static void UpdateNumberInstances(IActorRef miniTournamentActor, int number)
        {
            MiniTournamentActorTest.UpdateInstances(
                miniTournamentActor,
                instances: Enumerable.Range(0, number).Select(index => new TestInstance(index.ToString())));
        }

        /// <summary>
        /// Sends a <see cref="ClearInstances"/> and a <see cref="AddInstances{TestInstance}"/> message to the
        /// specified actor such that it receives the specified <see cref="TestInstance"/>s.
        /// </summary>
        /// <param name="miniTournamentActor">The actor to send the message to.</param>
        /// <param name="instances">The instances to send.</param>
        private static void UpdateInstances(IActorRef miniTournamentActor, IEnumerable<TestInstance> instances)
        {
            var updateMessages = Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages.UpdateInstances.CreateInstanceUpdateMessages(
                instances.ToImmutableList());
            foreach (var message in updateMessages)
            {
                miniTournamentActor.Tell(message);
            }
        }

        /// <summary>
        /// Sends <see cref="PartialGenomeEvaluationResults{TResult}"/> and <see cref="GenomeEvaluationFinished"/>
        /// messages to the provided <see cref="IActorRef"/>.
        /// </summary>
        /// <param name="miniTournamentActor">The actor to send the messages to.</param>
        /// <param name="number">The number of messages to send.</param>
        /// <param name="timeNeeded">The time to set for the <see cref="TestResult"/>s.</param>
        private static void FakeSuccessfullyCompletingEvaluations(IActorRef miniTournamentActor, int number, TimeSpan timeNeeded)
        {
            // Set results.
            var runResults = new List<TestResult> { new TestResult(timeNeeded) };

            // Send them for a number of evaluations.
            for (int id = 0; id < number; id++)
            {
                foreach (var message in CompleteGenomeEvaluationResults.CreateEvaluationResultMessages(id, runResults))
                {
                    miniTournamentActor.Tell(message);
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="MiniTournamentActor{NoOperation, TestInstance, TestResult}"/> that uses a target
        /// algorithm factory which always produces target algorithms with the specified runtime.
        /// </summary>
        /// <param name="config"><see cref="AlgorithmTunerConfiguration"/> to use.</param>
        /// <param name="runtime">The target algorithm's desired runtime.</param>
        /// <returns>An <see cref="IActorRef"/> to the mini tournament actor.</returns>
        private IActorRef CreateEvaluationActorWithLongRunningTargetAlgorithm(
            AlgorithmTunerConfiguration config,
            TimeSpan runtime)
        {
            // Create target algorithm factory with correct target algorithm runtime.
            var targetAlgorithmFactory = new TargetAlgorithmFactory<NoOperation, TestInstance, TestResult>(
                targetAlgorithmCreator: () => new NoOperation(runtime));
            // Create mini tournament actor, using the correct configuration and target algorithm factory.
            return this.Sys.ActorOf(
                new MiniTournamentActorPropsBuilder()
                    .SetConfiguration(config)
                    .SetTargetAlgorithmFactory(targetAlgorithmFactory)
                    .Build(this._resultStorageActorRef, this._blackHoleActorRef));
        }

        /// <summary>
        /// Checks whether the expected number of instances evaluations was performed for each of the provided genomes.
        /// </summary>
        /// <param name="genomes">Genomes to check.</param>
        /// <param name="instances">All possible instances they could have been evaluated on.</param>
        /// <param name="expectedInstancesPerGenome">The expected number of instance evaluations per genome.</param>
        private void CheckCorrectNumberOfEvaluations(
            List<ImmutableGenome> genomes,
            List<TestInstance> instances,
            int expectedInstancesPerGenome)
        {
            // For each genome:
            foreach (var genome in genomes)
            {
                // Count the number of instance evaluation...
                int genomeEvaluations = 0;
                foreach (var instance in instances)
                {
                    // ...by asking the storage actor for results for every possible instance.
                    this._resultStorageActorRef.Tell(new ResultRequest<TestInstance>(genome, instance));
                    var result = this.ExpectMsg<object>();
                    if (result is ResultMessage<TestInstance, TestResult>)
                    {
                        genomeEvaluations++;
                    }
                }

                // Then check if the number is as expected.
                Assert.Equal(
                    expectedInstancesPerGenome,
                    genomeEvaluations);
            }
        }

        #endregion

        /// <summary>
        /// Convenience class for building <see cref="Props"/> for creating a
        /// <see cref="MiniTournamentActor{TTargetAlgorithm, TInstance, TResult}"/> instance.
        /// Specifies default constructor parameters.
        /// </summary>
        private class MiniTournamentActorPropsBuilder
        {
            #region Fields

            /// <summary>
            /// The <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/> to provide.
            /// </summary>
            private ITargetAlgorithmFactory<NoOperation, TestInstance, TestResult> _targetAlgorithmFactory
                = new TargetAlgorithmFactory<NoOperation, TestInstance, TestResult>(
                    targetAlgorithmCreator: () => new NoOperation());

            /// <summary>
            /// The <see cref="IRunEvaluator{TestResult}"/> to provide.
            /// </summary>
            private IRunEvaluator<TestResult> _runEvaluator = new KeepSuggestedOrder<TestResult>();

            /// <summary>
            /// The <see cref="ParameterTree"/> to provide.
            /// </summary>
            private ParameterTree _parameterTree = MiniTournamentActorTest.CreateParameterTree();

            /// <summary>
            /// The <see cref="AlgorithmTunerConfiguration"/> to provide.
            /// </summary>
            private AlgorithmTunerConfiguration _configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetEnableRacing(true).Build(maximumNumberParallelEvaluations: 2);

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// Builds a <see cref="Props"/> object executing
            /// <see cref="MiniTournamentActor{TTargetAlgorithm, TInstance, TResult}"/>'s constructor using the
            /// configured arguments.
            /// </summary>
            /// <param name="resultStorageActorRef">Reference to a result storage actor.</param>
            /// <param name="taskProvidingActorRef">Reference to an actor to ask for evaluation configuration.</param>
            /// <returns>The props.</returns>
            public Props Build(IActorRef resultStorageActorRef, IActorRef taskProvidingActorRef)
            {
                return Props.Create(
                    () => new MiniTournamentActor<NoOperation, TestInstance, TestResult>(
                        this._targetAlgorithmFactory,
                        this._runEvaluator,
                        this._configuration,
                        resultStorageActorRef,
                        this._parameterTree,
                        taskProvidingActorRef));
            }

            /// <summary>
            /// Sets the <see cref="ITargetAlgorithmFactory{NoOperation, TestInstance, EmptyResult}"/> to provide to
            /// the <see cref="MiniTournamentActor{NoOperation, TestInstance, EmptyResult}"/> constructor.
            /// Default is a factory which creates the same noop target algorithm <see cref="NoOperation"/>
            /// for all inputs.
            /// </summary>
            /// <param name="targetAlgorithmFactory">The target algorithm factory to provide to the mini tournament
            /// actor constructor.</param>
            /// <returns>The <see cref="MiniTournamentActorPropsBuilder"/> in its new state.</returns>
            public MiniTournamentActorPropsBuilder SetTargetAlgorithmFactory(
                ITargetAlgorithmFactory<NoOperation, TestInstance, TestResult> targetAlgorithmFactory)
            {
                this._targetAlgorithmFactory = targetAlgorithmFactory;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="IRunEvaluator{TestResult}"/> to provide to the
            /// <see cref="MiniTournamentActor{NoOperation, TestInstance, EmptyResult}"/> constructor. Default is an
            /// evaluator that doesn't reorder the genomes at all.
            /// </summary>
            /// <param name="runEvaluator">The run evaluator to provide to the min tournament actor constructor.
            /// </param>
            /// <returns>The <see cref="MiniTournamentActorPropsBuilder"/> in its new state.</returns>
            public MiniTournamentActorPropsBuilder SetRunEvaluator(IRunEvaluator<TestResult> runEvaluator)
            {
                this._runEvaluator = runEvaluator;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="ParameterTree"/> to provide to the
            /// <see cref="MiniTournamentActor{NoOperation, TestInstance, EmptyResult}"/> constructor.
            /// Default is a simple parameter tree representing a single integer value.
            /// </summary>
            /// <param name="parameterTree">The parameter tree to provide to the mini tournament actor constructor.</param>
            /// <returns>The <see cref="MiniTournamentActorPropsBuilder"/> in its new state.</returns>
            public MiniTournamentActorPropsBuilder SetParameterTree(ParameterTree parameterTree)
            {
                this._parameterTree = parameterTree;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="AlgorithmTunerConfiguration"/> to provide to the
            /// <see cref="MiniTournamentActor{NoOperation, TestInstance, EmptyResult}"/> constructor.
            /// Default is the default <see cref="AlgorithmTunerConfiguration"/> with 1 core.
            /// </summary>
            /// <param name="configuration">The configuration to provide to the mini tournament actor constructor.
            /// </param>
            /// <returns>The <see cref="MiniTournamentActorPropsBuilder"/> in its new state.</returns>
            public MiniTournamentActorPropsBuilder SetConfiguration(AlgorithmTunerConfiguration configuration)
            {
                this._configuration = configuration;
                return this;
            }

            #endregion
        }
    }
}
