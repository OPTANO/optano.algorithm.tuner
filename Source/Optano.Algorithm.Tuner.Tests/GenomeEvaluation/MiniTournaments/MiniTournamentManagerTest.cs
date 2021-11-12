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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.MiniTournaments
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Moq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Priority_Queue;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Tests for <see cref="MiniTournamentManager{TInstance,TResult}"/>.
    /// </summary>
    public class MiniTournamentManagerTest
    {
        #region Static Fields

        /// <summary>
        /// The name for the test allele.
        /// </summary>
        private static readonly string TestAlleleName = "test";

        #endregion

        #region Fields

        /// <summary>
        /// A set of test instances.
        /// </summary>
        private IReadOnlyList<TestInstance> _instances;

        /// <summary>
        /// A set of tournament participants.
        /// </summary>
        private IReadOnlyList<ImmutableGenome> _participants;

        /// <summary>
        /// The global priority queue.
        /// </summary>
        private IPriorityQueue<GenomeTournamentKey, double> _globalQueue;

        /// <summary>
        /// The run evaluator.
        /// </summary>
        private IRunEvaluator<TestInstance, ContinuousResult> _runEvaluator;

        /// <summary>
        /// The test mini tournament manager.
        /// </summary>
        private MiniTournamentManager<TestInstance, ContinuousResult> _defaultManager;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniTournamentManagerTest"/> class.
        /// </summary>
        public MiniTournamentManagerTest()
        {
            this._instances = Enumerable.Range(0, 5).Select(i => new TestInstance($"{i}")).ToList();
            this._participants = this.CreateGenomesDescendingByAge(8, 0).ToList();
            this._globalQueue = new SimplePriorityQueue<GenomeTournamentKey, double>();
            this._runEvaluator = new KeepSuggestedOrder<TestInstance, ContinuousResult>();

            this._defaultManager = new MiniTournamentManager<TestInstance, ContinuousResult>(
                this._participants,
                this._instances,
                42,
                0,
                this._runEvaluator,
                false,
                1);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Tests if the constructor sets all observable properties as expected.
        /// </summary>
        [Fact]
        public void MiniTournamentManagerIsInitializedProperly()
        {
            this._defaultManager.Participants.ShouldBe(this._participants);
            this._defaultManager.MiniTournamentId.ShouldBe(42);
            this._defaultManager.IsTournamentFinished.ShouldBeFalse();
        }

        /// <summary>
        /// Tests that the dequeue returns elements in the expected order.
        /// </summary>
        [Fact]
        public void QueueReturnsGenomesInCorrectOrder()
        {
            this.RequeueAllEvaluations(this._defaultManager);
            this._defaultManager.StartSynchronizingQueue(this._globalQueue);
            this._globalQueue.Count.ShouldBe(this._participants.Count);

            // queue is not sorted. only the head contains the element with highest priority (=smallest age).
            var i = 0;
            var orderedParticipants = this._participants.OrderBy(p => p.Age).ToList();

            while (this._globalQueue.Count > 0)
            {
                var genomeTournamentKey = this._globalQueue.Dequeue();
                genomeTournamentKey.ShouldNotBeNull();
                genomeTournamentKey.Genome.ShouldBe(orderedParticipants[i++]);
            }
        }

        /// <summary>
        /// Tests that priorities in queue are updated according to run evaluator's new priority.
        /// </summary>
        [Fact]
        public void PriorityInQueueIsUpdatedWhenInstancesAreStarted()
        {
            var orderedParticipants = this._participants.OrderBy(p => p.Age).ToList();

            this.RequeueAllEvaluations(this._defaultManager);
            this._defaultManager.StartSynchronizingQueue(this._globalQueue);

            this._globalQueue.First.ShouldNotBeNull();
            this._globalQueue.First.Genome.ShouldBe(orderedParticipants[0]);

            var genomeTournamentKey = new GenomeTournamentKey(orderedParticipants[0], this._defaultManager.MiniTournamentId);
            this._defaultManager.TryGetNextInstanceAndUpdateGenomePriority(genomeTournamentKey, out var instance).ShouldBeTrue();
            instance.ShouldNotBeNull();
            this._defaultManager.TryGetNextInstanceAndUpdateGenomePriority(genomeTournamentKey, out instance).ShouldBeTrue();
            instance.ShouldNotBeNull();

            // after 2 instances have been drawn, age - open instances of 2nd genome have higher priority.
            // (age: 0 - open: 3) > (age: 1 - open: 5) | Priority: firstGenome: -3 > secondGenome: -4
            this._globalQueue.First.ShouldNotBeNull();
            this._globalQueue.First.Genome.ShouldBe(orderedParticipants[1]);
        }

        /// <summary>
        /// Tests that genomes without open instances are removed from the global queue.
        /// </summary>
        [Fact]
        public void GenomeWithoutOpenInstancesIsRemovedFromQueue()
        {
            this.RequeueAllEvaluations(this._defaultManager);
            this._defaultManager.StartSynchronizingQueue(this._globalQueue);
            var genomeTournamentKey = new GenomeTournamentKey(this._participants.First(), this._defaultManager.MiniTournamentId);

            for (var i = 0; i < this._instances.Count; i++)
            {
                this._defaultManager.TryGetNextInstanceAndUpdateGenomePriority(genomeTournamentKey, out var _).ShouldBeTrue();
            }

            this._defaultManager.TryGetNextInstanceAndUpdateGenomePriority(genomeTournamentKey, out var _).ShouldBeFalse();
            this._globalQueue.Count.ShouldBe(this._participants.Count - 1);
            this._globalQueue.ShouldNotContain(genomeTournamentKey);
        }

        /// <summary>
        /// Tests that genomes which get killed by racing are removed from the global queue.
        /// </summary>
        [Fact]
        public void RacingRemovesKilledGenomesFromQueue()
        {
            var participants = this.CreateGenomesDescendingByAge(2, 0).ToList();
            var instances = this._instances.Take(2).ToList();
            var queue = new SimplePriorityQueue<GenomeTournamentKey, double>();

            // always return second genome as target to kill by racing.
            var evaluatorMock = new Mock<IRunEvaluator<TestInstance, ContinuousResult>>();
            evaluatorMock.Setup(
                    e => e.GetGenomesThatCanBeCancelledByRacing(
                        It.IsAny<IReadOnlyList<ImmutableGenomeStats<TestInstance, ContinuousResult>>>(),
                        It.IsAny<int>()))
                .Returns(new[] { participants[1] });

            var manager = new MiniTournamentManager<TestInstance, ContinuousResult>(
                participants,
                instances,
                0,
                0,
                evaluatorMock.Object,
                true,
                1);

            this.RequeueAllEvaluations(manager);
            manager.StartSynchronizingQueue(queue);

            queue.Count.ShouldBe(2);

            // start an instance
            var winnerGenome = new GenomeTournamentKey(participants[0], 0);
            manager.TryGetNextInstanceAndUpdateGenomePriority(winnerGenome, out var instance).ShouldBeTrue();

            // send a result
            var result = new ContinuousResult(42, TimeSpan.Zero);
            var genomeInstancePair = new GenomeInstancePair<TestInstance>(winnerGenome.Genome, instance);
            manager.UpdateResult(genomeInstancePair, result);

            // second genome should have been killed by racing + removed from queue
            queue.Count.ShouldBe(1);
            queue.First.ShouldBe(winnerGenome);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initially, all genome instance evaluations are considered "running", while results are fetched from <see cref="ResultStorageActor{TInstance,TResult}"/>.
        /// For testing, this step is skipped an all evaluations need to be requeued.
        /// </summary>
        /// <param name="manager">The manager to update.</param>
        private void RequeueAllEvaluations(MiniTournamentManager<TestInstance, ContinuousResult> manager)
        {
            foreach (var genomeInstancePair in this._instances.SelectMany(
                i => manager.Participants.Select(p => new GenomeInstancePair<TestInstance>(p, i))))
            {
                manager.RequeueEvaluationIfRelevant(genomeInstancePair);
            }
        }

        /// <summary>
        /// Creates an ordered enumerable of genomes, sorted descending by age.
        /// </summary>
        /// <param name="count">The number of genomes to create.</param>
        /// <param name="startAge">The age of the youngest genome.</param>
        /// <returns>The genomes.</returns>
        private IEnumerable<ImmutableGenome> CreateGenomesDescendingByAge(int count, int startAge)
        {
            var genomes = Enumerable.Range(startAge, count).OrderByDescending(i => i).Select(g => new Genome(g));
            foreach (var genome in genomes)
            {
                genome.SetGene(MiniTournamentManagerTest.TestAlleleName, new Allele<int>(genome.Age));
                yield return new ImmutableGenome(genome);
            }
        }

        #endregion
    }
}