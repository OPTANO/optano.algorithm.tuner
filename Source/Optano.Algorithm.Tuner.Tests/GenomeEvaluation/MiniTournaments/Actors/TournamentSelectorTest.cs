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
    using System.Linq;

    using Akka.Actor;
    using Akka.Configuration;
    using Akka.TestKit.Xunit2;

    using Optano.Algorithm.Tuner;
    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Communication;
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
    /// Contains tests for the <see cref="TournamentSelector{TTargetAlgorithm,TInstance,TResult}"/> class.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupTwoName)]
    public class TournamentSelectorTest : TestKit, IDisposable
    {
        #region Constants

        /// <summary>
        /// Name to use for see <see cref="IActorRef"/> <see cref="_tournamentSelectorRef"/>.
        /// </summary>
        private const string DefaultTournamentSelectorName = "DefaultTournamentSelector";

        /// <summary>
        /// Name to use for an <see cref="IActorRef"/> for any <see cref="TournamentSelector{A, I, R}"/> that does not
        /// equal <see cref="_tournamentSelectorRef"/>.
        /// </summary>
        private const string SpecialTournamentSelectorName = "SpecialTournamentSelector";

        /// <summary>
        /// Name to use for an <see cref="IActorRef"/> to a <see cref="TournamentSelector{A, I, R}"/> that is not
        /// allowed to deploy <see cref="MiniTournamentActor{A, I, R}"/>s on its own node.
        /// </summary>
        private const string WaitingTournamentSelectorName = "WaitingTournamentSelector";

        #endregion

        #region Static Fields

        /// <summary>
        /// HOCON configuration string for the test <see cref="ActorSystem"/>. Makes sure
        /// <see cref="TournamentSelector{A, I, R}"/> is not waiting for remote workers to join the cluster.
        /// </summary>
        private static readonly Config akkaConfiguration = ConfigurationFactory.ParseString(
            $@"
            akka
            {{
                actor.deployment
                {{
                    /{TournamentSelectorTest.SpecialTournamentSelectorName}/{AkkaNames.MiniTournamentWorkers}
                    {{
                        router = round-robin-pool # routing strategy
                        nr-of-instances = 2
                    }}
                    /{TournamentSelectorTest.DefaultTournamentSelectorName}/{AkkaNames.MiniTournamentWorkers}
                    {{
                        router = round-robin-pool # routing strategy
                        nr-of-instances = 2
                    }}
                    /{TournamentSelectorTest.WaitingTournamentSelectorName}/{AkkaNames.MiniTournamentWorkers}
                    {{
                        router = round-robin-pool # routing strategy
                        max-nr-of-instances-per-node = 2
                        cluster
                        {{
                            enabled = on
                            allow-local-routees = off
                        }}
                    }}
                }}
            }}");

        #endregion

        #region Fields

        /// <summary>
        /// A list of <see cref="TestInstance"/>s consisting of a single instance.
        /// </summary>
        private readonly List<TestInstance> _singleTestInstance = new List<TestInstance> { new TestInstance("test") };

        /// <summary>
        /// Reference to the actor which is responsible for storing all evaluation results that have been observed so
        /// far. Has to be initialized.
        /// </summary>
        private readonly IActorRef _resultStorageActorRef;

        /// <summary>
        /// An actor reference to the <see cref="TournamentSelector{A, I, R}"/> used in tests. Has to be
        /// initialized.
        /// </summary>
        private readonly IActorRef _tournamentSelectorRef;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TournamentSelectorTest"/> class.
        /// Has to be explicitely defined to add <see cref="akkaConfiguration"/> to the test <see cref="ActorSystem"/>.
        /// </summary>
        public TournamentSelectorTest()
            : base(akkaConfiguration.WithFallback(ConfigurationFactory.Load()).WithFallback(TestKit.DefaultConfig))
        {
            this._resultStorageActorRef = this.Sys.ActorOf(
                Props.Create(
                    () =>
                        new ResultStorageActor<TestInstance, TestResult>()),
                AkkaNames.ResultStorageActor);
            this._tournamentSelectorRef = this.Sys.ActorOf(
                new TournamentSelectorPropsBuilder().Build(this._resultStorageActorRef),
                name: TournamentSelectorTest.DefaultTournamentSelectorName);

            Randomizer.Configure();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that <see cref="TournamentSelector{A, I, R}"/>'s constructor throws an exception when called
        /// without a <see cref="TargetAlgorithmFactory{A, I, R}"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingTargetAlgorithmFactory()
        {
            // Expect an exception on initialization...
            this.EventFilter.Exception<ActorInitializationException>().ExpectOne(
                () =>
                    {
                        // ...when no target algorithm factory is provided.
                        var tournamentSelectorRef = this.Sys.ActorOf(
                            new TournamentSelectorPropsBuilder()
                                .SetTargetAlgorithmFactory(null)
                                .Build(this._resultStorageActorRef),
                            name: TournamentSelectorTest.SpecialTournamentSelectorName);
                    });
        }

        /// <summary>
        /// Verifies that <see cref="TournamentSelector{A, I, R}"/>'s constructor throws an exception when called
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
                        var tournamentSelectorRef = this.Sys.ActorOf(
                            new TournamentSelectorPropsBuilder().SetRunEvaluator(null).Build(this._resultStorageActorRef),
                            name: TournamentSelectorTest.SpecialTournamentSelectorName);
                    });
        }

        /// <summary>
        /// Verifies that <see cref="TournamentSelector{A, I, R}"/>'s constructor throws an exception when called
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
                        var tournamentSelectorRef = this.Sys.ActorOf(
                            new TournamentSelectorPropsBuilder().SetConfiguration(null).Build(this._resultStorageActorRef),
                            name: TournamentSelectorTest.SpecialTournamentSelectorName);
                    });
        }

        /// <summary>
        /// Verifies that <see cref="TournamentSelector{A, I, R}"/>'s constructor throws an exception when called
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
                        var tournamentSelectorRef = this.Sys.ActorOf(
                            new TournamentSelectorPropsBuilder().Build(resultStorageActorRef: null),
                            name: TournamentSelectorTest.SpecialTournamentSelectorName);
                    });
        }

        /// <summary>
        /// Verifies that <see cref="TournamentSelector{A, I, R}"/>'s constructor throws an exception when called
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
                        var miniTournamentActorRef = this.Sys.ActorOf(
                            new TournamentSelectorPropsBuilder().SetParameterTree(null).Build(this._resultStorageActorRef),
                            name: TournamentSelectorTest.SpecialTournamentSelectorName);
                    });
        }

        /// <summary>
        /// Checks that genome evaluations are done with the instances provided by the <see cref="SelectCommand{TInstance}"/>.
        /// </summary>
        [Fact]
        public void CorrectInstancesAreUsedForSelectCommand()
        {
            // Build tournament selector with a small tournament size to test using several workers
            var config = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetMaximumMiniTournamentSize(1)
                .Build(maximumNumberParallelEvaluations: 1);
            var tournamentSelector = this.Sys.ActorOf(
                props: new TournamentSelectorPropsBuilder().SetConfiguration(config).Build(this._resultStorageActorRef),
                name: TournamentSelectorTest.SpecialTournamentSelectorName);

            // Send a simple select command and wait for completion.
            var originalCommand = new SelectCommand<TestInstance>(
                this.CreateGenomesAdheringToParameterTree(2),
                new List<TestInstance> { new TestInstance("0") },
                0);
            tournamentSelector.Tell(originalCommand);
            this.ExpectMsg<SelectionResultMessage<TestResult>>();

            // Build a new select command with several new instances.
            var instances = new List<TestInstance>()
                                {
                                    new TestInstance("1"),
                                    new TestInstance("2"),
                                };
            var genomes = this.CreateGenomesAdheringToParameterTree(2);
            var selectCommand = new SelectCommand<TestInstance>(genomes, instances, 1);

            // Send it & wait for completion.
            tournamentSelector.Tell(selectCommand);
            this.ExpectMsg<SelectionResultMessage<TestResult>>();

            // Check that storage actor has knowledge about both new instances for both genomes.
            foreach (var genome in genomes)
            {
                foreach (var instance in instances)
                {
                    this._resultStorageActorRef.Tell(new ResultRequest<TestInstance>(genome, instance));
                    this.ExpectMsg<ResultMessage<TestInstance, TestResult>>();
                }
            }
        }

        /// <summary>
        /// Checks that the number of winners returned with <see cref="SelectionResultMessage{TResult}"/> is as expected.
        /// </summary>
        [Fact]
        public void CorrectNumberOfWinnersIsReturned()
        {
            // Build tournament selector with a specific winner percentage and tournament size.
            double winnerPercentage = 0.5;
            int miniTournamentSize = 2;
            IActorRef tournamentSelector = this.CreateTournamentSelector(winnerPercentage, miniTournamentSize);

            // Send selection command.
            int numberGenomes = 7;
            var genomes = this.CreateGenomesAdheringToParameterTree(numberGenomes);
            tournamentSelector.Tell(new SelectCommand<TestInstance>(genomes, this._singleTestInstance, 0));

            // Wait for results.
            var results = this.ExpectMsg<SelectionResultMessage<TestResult>>();

            // Make sure the correct number of genomes are returned.
            int numberTournaments = (int)Math.Ceiling((double)numberGenomes / miniTournamentSize);
            int balancedTournamentSize = numberGenomes / numberTournaments;
            int numberEnlargedTournaments = numberGenomes % numberTournaments;
            int winnersInEnlargedTournaments = (int)Math.Ceiling((balancedTournamentSize + 1) * winnerPercentage);
            int winnersInUsualTournaments = (int)Math.Ceiling(balancedTournamentSize * winnerPercentage);
            int expectedNumberWinners = (numberEnlargedTournaments * winnersInEnlargedTournaments)
                                        + ((numberTournaments - numberEnlargedTournaments) * winnersInUsualTournaments);

            Assert.Equal(
                expected: expectedNumberWinners,
                actual: results.CompetitiveParents.Count);
        }

        /// <summary>
        /// Checks that the winners returned with <see cref="SelectionResultMessage{TResult}"/> is sorted correctly.
        /// </summary>
        [Fact]
        public void WinnersAreSortedCorrectly()
        {
            // Build configuration with a specific winner percentage and tournament size.
            AlgorithmTunerConfiguration config = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetTournamentWinnerPercentage(0.5)
                .SetMaximumMiniTournamentSize(2)
                .Build(maximumNumberParallelEvaluations: 1);

            // Build tournament selector selecting winners by a specific parameter.
            var tournamentSelector = this.Sys.ActorOf(
                props: Props.Create(
                    () => new TournamentSelector<ExtractIntegerValue, TestInstance, IntegerResult>(
                        new ExtractIntegerValueCreator(),
                        new SortByValue(),
                        config,
                        this._resultStorageActorRef,
                        TournamentSelectorTest.CreateParameterTree())),
                name: TournamentSelectorTest.SpecialTournamentSelectorName);

            // Send selection command.
            int numberGenomes = 4;
            var genomes = this.CreateGenomesAdheringToParameterTree(numberGenomes);
            tournamentSelector.Tell(new SelectCommand<TestInstance>(genomes, this._singleTestInstance, 0));

            // Wait for results.
            var results = this.ExpectMsg<SelectionResultMessage<IntegerResult>>();

            // Check that they are sorted correctly.
            var sortedResults = results.CompetitiveParents
                .OrderByDescending(
                    genome =>
                        (int)genome.CreateMutableGenome().GetGeneValue(ExtractIntegerValue.ParameterName).GetValue());
            Assert.True(
                sortedResults.SequenceEqual(results.CompetitiveParents, new ImmutableGenome.GeneValueComparer()),
                $"Expected winners sorted like {TestUtils.PrintList(sortedResults)}, but were {TestUtils.PrintList(results.CompetitiveParents)}.");
        }

        /// <summary>
        /// Checks that <see cref="SelectCommand{I}"/>s gets stashed while <see cref="TournamentSelector{A, I, R}"/> is
        /// in waiting for workers state.
        /// </summary>
        [Fact]
        public void SelectCommandsGetStashedInWaitingForWorkersState()
        {
            // Create some genomes.
            int numberGenomes = 7;
            var genomes = this.CreateGenomesAdheringToParameterTree(numberGenomes);

            // Create tournament selector not able to deploy own mini tournament workers.
            var waitingTournamentSelectorRef = this.ActorOfAsTestActorRef<TournamentSelector<NoOperation, TestInstance, TestResult>>(
                    new TournamentSelectorPropsBuilder().Build(this._resultStorageActorRef),
                    name: TournamentSelectorTest.WaitingTournamentSelectorName);

            // Send some select commands.
            waitingTournamentSelectorRef.Tell(new SelectCommand<TestInstance>(genomes, this._singleTestInstance, 0));
            waitingTournamentSelectorRef.Tell(new SelectCommand<TestInstance>(genomes, this._singleTestInstance, 0));
            waitingTournamentSelectorRef.Tell(new SelectCommand<TestInstance>(genomes, this._singleTestInstance, 0));

            // Check they are all in stash.
            var messages = waitingTournamentSelectorRef.UnderlyingActor.Stash.ClearStash();
            Assert.Equal(3, messages.Count());
            Assert.True(
                messages.All(message => message.Sender == this.TestActor && message.Message is SelectCommand<TestInstance>),
                "Not all stashed messages have been select commands send by the test actor.");
        }

        /// <summary>
        /// Checks that the genomes returned by <see cref="SelectionResultMessage{TResult}"/> have the potential to be the fittest genomes
        /// in their respective mini tournaments, i.e. they do not include the m-1 worst genomes if m is the size of a
        /// mini tournament that has a single winner.
        /// </summary>
        [Fact]
        public void WorstGenomesAreNeverWinners()
        {
            // Create some genomes.
            int numberGenomes = 20;
            var genomes = this.CreateGenomesAdheringToParameterTree(numberGenomes);

            // Create a configuration using a certain winner percentage and mini tournament size.
            double winnerPercentage = 0.1;
            int miniTournamentSize = 10;
            AlgorithmTunerConfiguration configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetTournamentWinnerPercentage(winnerPercentage)
                .SetMaximumMiniTournamentSize(miniTournamentSize)
                .Build(maximumNumberParallelEvaluations: 1);

            // Create tournament selector using an evaluator which sorts by a certain parameter, highest first.
            var tournamentSelectorProps = Props.Create(
                () =>
                    new TournamentSelector<ExtractIntegerValue, TestInstance, IntegerResult>(
                        new ExtractIntegerValueCreator(),
                        new SortByValue(),
                        configuration,
                        this._resultStorageActorRef,
                        TournamentSelectorTest.CreateParameterTree()));
            var tournamentSelector = this.Sys.ActorOf(tournamentSelectorProps, name: TournamentSelectorTest.SpecialTournamentSelectorName);

            // Try selecting the best genomes via mini tournaments multiple times and make sure the worst genomes never
            // get chosen:
            int worstPossibleWinnerValue = miniTournamentSize - (int)(winnerPercentage * miniTournamentSize);
            for (int i = 0; i < 15; i++)
            {
                // Send a select command.
                tournamentSelector.Tell(new SelectCommand<TestInstance>(genomes, this._singleTestInstance, 0));

                // Wait for results.
                var results = this.ExpectMsg<SelectionResultMessage<IntegerResult>>();

                // Make sure none belongs to the worst genomes.
                foreach (var genome in results.CompetitiveParents)
                {
                    int geneValue =
                        (int)genome.CreateMutableGenome().GetGeneValue(ExtractIntegerValue.ParameterName).GetValue();
                    Assert.True(
                        geneValue >= worstPossibleWinnerValue,
                        $"Found a winner with value {geneValue}, but expected at least {worstPossibleWinnerValue}.");
                }
            }
        }

        /// <summary>
        /// Checks that <see cref="InstancesRequest"/>s are answered correctly when the
        /// <see cref="TournamentSelector{A, I, R}"/> is in working state.
        /// </summary>
        [Fact]
        public void InstanceRequestsAreAnsweredInWorkingState()
        {
            // Create tournament selector with a target algorithm that needs a while.
            var slowTournamentSelector = this.Sys.ActorOf(
                new TournamentSelectorPropsBuilder()
                    .SetTargetAlgorithmFactory(
                        new TargetAlgorithmFactory<NoOperation, TestInstance, TestResult>(
                            () => new NoOperation(TimeSpan.FromMilliseconds(1000))))
                    .Build(this._resultStorageActorRef),
                name: TournamentSelectorTest.SpecialTournamentSelectorName);

            // Provoke working state by sending a select command.
            slowTournamentSelector.Tell(
                new SelectCommand<TestInstance>(
                    this.CreateGenomesAdheringToParameterTree(2),
                    this._singleTestInstance,
                    0));

            // Wait a while to make sure working state was reached.
            this.ExpectNoMsg(milliseconds: 500);

            // Send instances request.
            slowTournamentSelector.Tell(new InstancesRequest());

            // Check it is answered with the instance that was used in the select command.
            this.ExpectMsg<ClearInstances>();
            var answer = this.ExpectMsg<AddInstances<TestInstance>>();
            Assert.True(
                object.Equals(this._singleTestInstance.Single(), answer.Instances.Single()),
                "Instances request was not answered by single instance given in select command.");
        }

        /// <summary>
        /// Checks that <see cref="InstancesRequest"/>s provoke a poll to the one sending it when the
        /// <see cref="TournamentSelector{A, I, R}"/> is in working state.
        /// </summary>
        [Fact]
        public void InstanceRequestsProvokePollsInWorkingState()
        {
            // Create tournament selector with a target algorithm that needs a while.
            var slowTournamentSelector = this.Sys.ActorOf(
                new TournamentSelectorPropsBuilder()
                    .SetTargetAlgorithmFactory(
                        new TargetAlgorithmFactory<NoOperation, TestInstance, TestResult>(
                            () => new NoOperation(TimeSpan.FromMilliseconds(1000))))
                    .Build(this._resultStorageActorRef),
                name: TournamentSelectorTest.SpecialTournamentSelectorName);

            // Provoke working state by sending a select command.
            slowTournamentSelector.Tell(
                new SelectCommand<TestInstance>(
                    this.CreateGenomesAdheringToParameterTree(2),
                    this._singleTestInstance,
                    0));

            // Wait a while to make sure working state was reached.
            this.ExpectNoMsg(milliseconds: 500);

            // Send instances request and expect both the instances and a poll as answer.
            slowTournamentSelector.Tell(new InstancesRequest());
            this.ExpectMsg<ClearInstances>();
            this.ExpectMsg<AddInstances<TestInstance>>();
            this.ExpectMsg<InstanceUpdateFinished>();
            this.ExpectMsg<Poll>();
        }

        /// <summary>
        /// Checks that a new <see cref="SelectCommand{I}"/> can be handled after a first one finishes.
        /// </summary>
        [Fact]
        public void MultipleSelectionsCanBeDoneInSuccession()
        {
            for (int i = 0; i < 2; i++)
            {
                this._tournamentSelectorRef.Tell(
                    new SelectCommand<TestInstance>(this.CreateGenomesAdheringToParameterTree(2), this._singleTestInstance, 0));
                this.ExpectMsg<SelectionResultMessage<TestResult>>();
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override void AfterAll()
        {
            // Trace.WriteLine($"###AFTER_ALL###\r\n{Environment.StackTrace}\r\n###EOM###");
            // Console.WriteLine($"###AFTER_ALL###\r\n{Environment.StackTrace}\r\n###EOM###");
            // LoggingHelper.WriteLine(VerbosityLevel.Trace, $"###AFTER_ALL###\r\n{Environment.StackTrace}\r\n###EOM###");

            Randomizer.Reset();
            base.AfterAll();
            this.Shutdown();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            this.AfterAll();
            if (disposing)
            {
                this.Shutdown();
            }

            base.Dispose(disposing);
        }

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
        /// Create <see cref="ImmutableGenome"/> instances that fit the <see cref="ParameterTree"/> created by
        /// <see cref="CreateParameterTree"/>.
        /// All instances will have different gene values.
        /// </summary>
        /// <param name="number">Number of instances to create.</param>
        /// <returns>The created instances.</returns>
        private List<ImmutableGenome> CreateGenomesAdheringToParameterTree(int number)
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
        /// Creates a new <see cref="TournamentSelector{NoOperation, TestInstance, TestResult}"/> in the
        /// actor system.
        /// </summary>
        /// <param name="winnerPercentage">The percentage of winners in a mini tournament.</param>
        /// <param name="miniTournamentSize">The size of a mini tournament.</param>
        /// <returns>An <see cref="IActorRef"/> to the created tournament selector.</returns>
        private IActorRef CreateTournamentSelector(double winnerPercentage, int miniTournamentSize)
        {
            // Build configuration.
            AlgorithmTunerConfiguration config = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetTournamentWinnerPercentage(winnerPercentage)
                .SetMaximumMiniTournamentSize(miniTournamentSize)
                .Build(maximumNumberParallelEvaluations: 1);

            // Build tournament selector itself.
            return this.Sys.ActorOf(
                props: new TournamentSelectorPropsBuilder().SetConfiguration(config).Build(this._resultStorageActorRef),
                name: TournamentSelectorTest.SpecialTournamentSelectorName);
        }

        #endregion

        /// <summary>
        /// Convenience class for building the <see cref="Props"/> for a
        /// <see cref="TournamentSelector{NoOperation, TestInstance, EmptyResult}"/> instance.
        /// Specifies default constructor parameters.
        /// </summary>
        private class TournamentSelectorPropsBuilder
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
            private ParameterTree _parameterTree = TournamentSelectorTest.CreateParameterTree();

            /// <summary>
            /// The <see cref="AlgorithmTunerConfiguration"/> to provide.
            /// </summary>
            private AlgorithmTunerConfiguration _configuration =
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetEnableRacing(true).Build(maximumNumberParallelEvaluations: 1);

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// Builds a <see cref="Props"/> object executing
            /// <see cref="TournamentSelector{NoOperation, TestInstance, EmptyResult}"/>'s constructor using the
            /// configured arguments.
            /// </summary>
            /// <param name="resultStorageActorRef">Reference to a result storage actor.</param>
            /// <returns>The props.</returns>
            public Props Build(IActorRef resultStorageActorRef)
            {
                return Props.Create(
                    () => new TournamentSelector<NoOperation, TestInstance, TestResult>(
                        this._targetAlgorithmFactory,
                        this._runEvaluator,
                        this._configuration,
                        resultStorageActorRef,
                        this._parameterTree));
            }

            /// <summary>
            /// Sets the <see cref="ITargetAlgorithmFactory{NoOperation, TestInstance, EmptyResult}"/> to provide to
            /// the <see cref="TournamentSelector{NoOperation, TestInstance, EmptyResult}"/> constructor.
            /// Default is a factory which creates the same noop target algorithm <see cref="NoOperation"/>
            /// for all inputs.
            /// </summary>
            /// <param name="targetAlgorithmFactory">The target algorithm factory to provide to the tournament selector
            /// constructor.</param>
            /// <returns>The <see cref="TournamentSelectorPropsBuilder"/> in its new state.</returns>
            public TournamentSelectorPropsBuilder SetTargetAlgorithmFactory(
                ITargetAlgorithmFactory<NoOperation, TestInstance, TestResult> targetAlgorithmFactory)
            {
                this._targetAlgorithmFactory = targetAlgorithmFactory;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="IRunEvaluator{TestResult}"/> to provide to the
            /// <see cref="TournamentSelector{NoOperation, TestInstance, EmptyResult}"/> constructor. Default is an
            /// evaluator that doesn't reorder the genomes at all.
            /// </summary>
            /// <param name="runEvaluator">The run evaluator to provide to the min tournament actor constructor.
            /// </param>
            /// <returns>The <see cref="TournamentSelectorPropsBuilder"/> in its new state.</returns>
            public TournamentSelectorPropsBuilder SetRunEvaluator(IRunEvaluator<TestResult> runEvaluator)
            {
                this._runEvaluator = runEvaluator;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="ParameterTree"/> to provide to the
            /// <see cref="TournamentSelector{NoOperation, TestInstance, EmptyResult}"/> constructor.
            /// Default is a simple parameter tree representing a single integer value.
            /// </summary>
            /// <param name="parameterTree">The parameter tree to provide to the tournament selector constructor.</param>
            /// <returns>The <see cref="TournamentSelectorPropsBuilder"/> in its new state.</returns>
            public TournamentSelectorPropsBuilder SetParameterTree(ParameterTree parameterTree)
            {
                this._parameterTree = parameterTree;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="AlgorithmTunerConfiguration"/> to provide to the
            /// <see cref="TournamentSelector{NoOperation, TestInstance, EmptyResult}"/> constructor.
            /// Default is the default <see cref="AlgorithmTunerConfiguration"/> with 1 core.
            /// </summary>
            /// <param name="configuration">The configuration to provide to the tournament selector constructor.
            /// </param>
            /// <returns>The <see cref="TournamentSelectorPropsBuilder"/> in its new state.</returns>
            public TournamentSelectorPropsBuilder SetConfiguration(AlgorithmTunerConfiguration configuration)
            {
                this._configuration = configuration;
                return this;
            }

            #endregion
        }
    }
}
