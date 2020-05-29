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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.Sorting
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;

    using Akka.Actor;
    using Akka.Configuration;
    using Akka.TestKit;
    using Akka.TestKit.Xunit2;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Communication;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Actors;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GenomeSorter{TInstance,TResult}"/> class.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class GenomeSorterTest : TestKit
    {
        #region Constants

        /// <summary>
        /// The number of <see cref="MiniTournamentActor{TTargetAlgorithm,TInstance,TResult}"/>s that will be created.
        /// </summary>
        private const int NumberMiniTournamentActors = 2;

        /// <summary>
        /// The maximum number of parallel evaluations.
        /// </summary>
        private const int MaximumNumberParallelEvaluations = 3;

        /// <summary>
        /// The total number of <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>s that will be
        /// created.
        /// </summary>
        private const int TotalNumberEvaluationActors = GenomeSorterTest.NumberMiniTournamentActors * GenomeSorterTest.MaximumNumberParallelEvaluations;

        #endregion

        #region Static Fields

        /// <summary>
        /// HOCON configuration string for the test <see cref="ActorSystem"/>.
        /// </summary>
        private static readonly Config AkkaConfiguration = ConfigurationFactory.ParseString(
            $@"
            akka
            {{
                actor.deployment
                {{
                    /{AkkaNames.TournamentSelector}/{AkkaNames.MiniTournamentWorkers}
                    {{
                        router = round-robin-pool # routing strategy
                        nr-of-instances = {GenomeSorterTest.NumberMiniTournamentActors}
                    }}
                }}
            }}");

        #endregion

        #region Fields

        /// <summary>
        /// A list of <see cref="TestInstance"/>s consisting of a single instance.
        /// </summary>
        private readonly ImmutableList<TestInstance> _singleTestInstance =
            new List<TestInstance> { new TestInstance("0") }.ToImmutableList();

        /// <summary>
        /// <see cref="IRunEvaluator{TResult}"/> used in tests.
        /// </summary>
        private readonly IRunEvaluator<IntegerResult> _runEvaluator =
            new TargetAlgorithm.InterfaceImplementations.ValueConsideration.SortByValue();

        /// <summary>
        /// Reference to the actor which is responsible for storing all evaluation results that have been observed so
        /// far. Has to be initialized.
        /// </summary>
        private readonly IActorRef _resultStorageActor;

        /// <summary>
        /// An actor reference to the <see cref="GenomeSorter{TInstance,TResult}"/> used in tests. Has to be
        /// initialized.
        /// </summary>
        private readonly TestActorRef<GenomeSorter<TestInstance, IntegerResult>> _genomeSorter;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeSorterTest"/> class.
        /// Has to be explicitly defined to add <see cref="AkkaConfiguration"/> to the test <see cref="ActorSystem"/>.
        /// </summary>
        public GenomeSorterTest()
            : base(AkkaConfiguration.WithFallback(ConfigurationFactory.Load()).WithFallback(TestKit.DefaultConfig))
        {
            TestUtils.InitializeLogger();

            this._resultStorageActor = this.Sys.ActorOf(
                props: Props.Create(() => new ResultStorageActor<TestInstance, IntegerResult>()),
                name: AkkaNames.ResultStorageActor);
            this._genomeSorter = this.ActorOfAsTestActorRef<GenomeSorter<TestInstance, IntegerResult>>(
                props: Props.Create(() => new GenomeSorter<TestInstance, IntegerResult>(this._runEvaluator)),
                name: AkkaNames.GenomeSorter);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that <see cref="GenomeSorter{TInstance,TResult}"/>'s constructor throws an exception when called
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
                            Props.Create(() => new GenomeSorter<TestInstance, TestResult>(null)));
                    });
        }

        /// <summary>
        /// Checks the number of <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>s the
        /// <see cref="GenomeSorter{TInstance,TResult}"/> manages equals the number of
        /// <see cref="MiniTournamentActor{TTargetAlgorithm,TInstance,TResult}"/>s times
        /// <see cref="AlgorithmTunerConfiguration.MaximumNumberParallelEvaluations"/>.
        /// </summary>
        [Fact]
        public void GenomeSorterManagesCorrectNumberOfEvaluationActors()
        {
            this.BuildTournamentSelector();

            // Wait a while.
            Thread.Sleep(TimeSpan.FromSeconds(3));

            this.IgnoreMessages<InstancesRequest>();

            // Check genome sorter's routees.
            var routerRequest = this.ActorSelection($"{this._genomeSorter.Path}/{AkkaNames.SortingRouter}").ResolveOne(TimeSpan.FromSeconds(30));
            routerRequest.Wait();
            routerRequest.Result.Tell(new Poll());

            // Check the number of decline messages.
            for (int i = 0; i < GenomeSorterTest.TotalNumberEvaluationActors; i++)
            {
                this.ExpectMsg<Decline>(
                    TimeSpan.FromSeconds(3),
                    $"Found less than {i + 1} routees, expected {GenomeSorterTest.TotalNumberEvaluationActors}.");
            }

            this.ExpectNoMsg(milliseconds: 100);
        }

        /// <summary>
        /// Checks that genome evaluations are done with the instances provided by the
        /// <see cref="SortCommand{TInstance}"/>.
        /// </summary>
        [Fact]
        public void CorrectInstancesAreUsedForSortCommand()
        {
            this.BuildTournamentSelector();

            // Send a simple sort command and wait for completion.
            var originalCommand = new SortCommand<TestInstance>(
                this.CreateGenomesAdheringToParameterTree(GenomeSorterTest.TotalNumberEvaluationActors),
                this._singleTestInstance);
            this._genomeSorter.Tell(originalCommand);
            this.ExpectMsg<SortResult>();

            // Build a new sort command with several new instances.
            var instances = new List<TestInstance>
                                {
                                    new TestInstance("1"),
                                    new TestInstance("2"),
                                };
            var genomes = this.CreateGenomesAdheringToParameterTree(GenomeSorterTest.TotalNumberEvaluationActors);
            var selectCommand = new SortCommand<TestInstance>(genomes, instances.ToImmutableList());

            // Send it & wait for completion.
            this._genomeSorter.Tell(selectCommand);
            this.ExpectMsg<SortResult>();

            // Check that storage actor has knowledge about both new instances for both genomes.
            foreach (var genome in genomes)
            {
                foreach (var instance in instances)
                {
                    this._resultStorageActor.Tell(new ResultRequest<TestInstance>(genome, instance));
                    this.ExpectMsg<ResultMessage<TestInstance, IntegerResult>>();
                }
            }
        }

        /// <summary>
        /// Checks that the <see cref="SortResult"/> message resulting from a <see cref="SortCommand{TInstance}"/>
        /// request stores the items sorted by performance.
        /// </summary>
        [Fact]
        public void SortResultStoresItemsOrderedByPerformance()
        {
            this.BuildTournamentSelector();

            // Create some genomes.
            const int NumberGenomes = 20;
            var genomes = this.CreateGenomesAdheringToParameterTree(NumberGenomes);

            // Send a sort command.
            this._genomeSorter.Tell(new SortCommand<TestInstance>(genomes, this._singleTestInstance));

            // Check if items are sorted by "value" parameter, highest first.
            var expectedOrder = genomes
                .Select(genome => genome.CreateMutableGenome())
                .OrderByDescending(genome => (int)genome.GetGeneValue(ExtractIntegerValue.ParameterName).GetValue())
                .Select(genome => new ImmutableGenome(genome))
                .ToList();
            var actualOrder = this.ExpectMsg<SortResult>().Ranking;
            Assert.True(
                expectedOrder.SequenceEqual(actualOrder, new ImmutableGenome.GeneValueComparer()),
                $"Genomes were not sorted by value: Expected {TestUtils.PrintList(expectedOrder)} but got {TestUtils.PrintList(actualOrder)}.");
        }

        /// <summary>
        /// Checks that <see cref="SortCommand{TInstance}"/>s gets stashed while
        /// <see cref="GenomeSorter{TInstance,TResult}"/> is in waiting for evaluators state.
        /// </summary>
        [Fact]
        public void SortCommandsGetStashedInWaitingForEvaluatorsState()
        {
            // Create some genomes.
            int numberGenomes = 7;
            var genomes = this.CreateGenomesAdheringToParameterTree(numberGenomes);

            // Send some sort commands.
            this._genomeSorter.Tell(new SortCommand<TestInstance>(genomes, this._singleTestInstance));
            this._genomeSorter.Tell(new SortCommand<TestInstance>(genomes, this._singleTestInstance));
            this._genomeSorter.Tell(new SortCommand<TestInstance>(genomes, this._singleTestInstance));

            // Check they are all in stash.
            var messages = this._genomeSorter.UnderlyingActor.Stash.ClearStash().ToList();
            Assert.Equal(3, messages.Count);
            Assert.True(
                messages.All(message => object.Equals(message.Sender, this.TestActor) && message.Message is SortCommand<TestInstance>),
                "Not all stashed messages have been sort commands send by the test actor.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a <see cref="TournamentSelector{TTargetAlgorithm,TInstance,TResult}"/>.
        /// It creates <see cref="MiniTournamentActor{TTargetAlgorithm,TInstance,TResult}"/>s which create the
        /// required <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>s.
        /// The hierarchy must be as in OPTANO Algorithm Tuner to ensure that <see cref="_genomeSorter"/> finds the
        /// actors.
        /// </summary>
        private void BuildTournamentSelector()
        {
            var configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEnableRacing(true)
                .Build(GenomeSorterTest.MaximumNumberParallelEvaluations);
            var parameterTree = new ParameterTree(
                root: new ValueNode<int>(ExtractIntegerValue.ParameterName, new IntegerDomain()));
            var tournamentSelector = this.Sys.ActorOf(
                Props.Create(
                    () => new TournamentSelector<ExtractIntegerValue, TestInstance, IntegerResult>(
                        new ExtractIntegerValueCreator(),
                        this._runEvaluator,
                        configuration,
                        this._resultStorageActor,
                        parameterTree)),
                AkkaNames.TournamentSelector);
        }

        /// <summary>
        /// Create <see cref="ImmutableGenome"/> instances that fit the <see cref="ParameterTree"/> created by
        /// <see cref="BuildTournamentSelector"/>.
        /// All instances will have different gene values.
        /// </summary>
        /// <param name="number">Number of instances to create.</param>
        /// <returns>The created instances.</returns>
        private ImmutableList<ImmutableGenome> CreateGenomesAdheringToParameterTree(int number)
        {
            var genomes = new List<ImmutableGenome>(number);
            for (int i = 0; i < number; i++)
            {
                var genome = new Genome();
                genome.SetGene(ExtractIntegerValue.ParameterName, new Allele<int>(i));
                genomes.Add(new ImmutableGenome(genome));
            }

            return genomes.ToImmutableList();
        }

        #endregion
    }
}
