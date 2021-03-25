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

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Tests for <see cref="GenomeStats{TInstance,TResult}"/>.
    /// </summary>
    public class GenomeStatsTest
    {
        #region Static Fields

        /// <summary>
        /// A list of 10 test instances.
        /// </summary>
        private static readonly IReadOnlyList<TestInstance> _instances = Enumerable.Range(0, 10).Select(i => new TestInstance($"{i}")).ToList();

        /// <summary>
        /// A test genome.
        /// </summary>
        private static readonly ImmutableGenome _genome = new ImmutableGenome(new Genome(1));

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeStatsTest"/> class.
        /// </summary>
        public GenomeStatsTest()
        {
            this.AllOpenStats = new GenomeStats<TestInstance, TestResult>(
                GenomeStatsTest._genome,
                GenomeStatsTest._instances,
                Enumerable.Empty<TestInstance>());
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets an instance of stats where all <see cref="_instances"/> are initially <see cref="GenomeStats{TInstance,TResult}.OpenInstances"/>.
        /// </summary>
        private GenomeStats<TestInstance, TestResult> AllOpenStats { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Tests if the open and running instances are moved to the correct collection in <see cref="GenomeStats{TInstance,TResult}"/>' constructor.
        /// </summary>
        /// <param name="numOpen">The number of open instances to add.</param>
        /// <param name="numRunning">The number of running instances to add.</param>
        [Theory]
        [InlineData(10, 0)]
        [InlineData(0, 10)]
        [InlineData(5, 5)]
        public void Test_Initialization(int numOpen, int numRunning)
        {
            var openInstances = Enumerable.Range(0, numOpen).Select(i => new TestInstance($"{i}")).ToList();
            var runningInstances = Enumerable.Range(numOpen, numRunning).Select(i => new TestInstance($"{i}")).ToList();

            var stats = new GenomeStats<TestInstance, TestResult>(GenomeStatsTest._genome, openInstances, runningInstances);
            stats.OpenInstances.ShouldBe(openInstances);
            stats.RunningInstances.ShouldBe(runningInstances);
        }

        /// <summary>
        /// Checks if boolean properties and counts of <see cref="GenomeStats{TInstance,TResult}"/> are correct.
        /// </summary>
        /// <param name="numOpen">The number of open instances to add.</param>
        /// <param name="numRunning">The number of running instances to add.</param>
        [Theory]
        [InlineData(10, 0)]
        [InlineData(0, 10)]
        [InlineData(10, 10)]
        public void PropertiesAndCollectionsAreInitializesProperly(int numOpen, int numRunning)
        {
            var stats = this.CreateStats(numOpen, numRunning);
            stats.HasOpenInstances.ShouldBe(numOpen > 0);
            stats.HasOpenOrRunningInstances.ShouldBe((numOpen + numRunning) > 0);

            stats.TotalInstanceCount.ShouldBe(numOpen + numRunning);

            stats.CancelledByRacingInstances.ShouldBeEmpty();
            stats.FinishedInstances.ShouldBeEmpty();

            stats.IsCancelledByRacing.ShouldBeFalse();
            stats.RuntimeOfFinishedInstances.ShouldBe(TimeSpan.Zero);

            stats.AllInstancesFinishedWithoutCancelledResult.ShouldBeFalse();
        }

        /// <summary>
        /// Tests that requeue of open instances is not possible.
        /// </summary>
        [Fact]
        public void RequeueOpenInstanceReturnsFalseAndDoesNotAddDuplicateToOpenInstances()
        {
            this.AllOpenStats.RequeueInstance(GenomeStatsTest._instances[0]).ShouldBeFalse();
            this.AllOpenStats.OpenInstances.ShouldBe(GenomeStatsTest._instances);
        }

        /// <summary>
        /// Tests that open instances are started properly.
        /// </summary>
        [Fact]
        public void TryStartInstanceMovesInstanceFromOpenToRunning()
        {
            var stats = this.CreateStats(1, 0);

            stats.TryStartInstance(out var instance).ShouldBeTrue();
            instance.ShouldNotBeNull();
            stats.OpenInstances.ShouldNotContain(instance);
            stats.RunningInstances.ShouldContain(instance);

            stats.HasOpenInstances.ShouldBeFalse();
            stats.HasOpenOrRunningInstances.ShouldBeTrue();
        }

        /// <summary>
        /// Tests that TryStartInstance does not change the state when no open instance exists.
        /// </summary>
        [Fact]
        public void TryStartInstanceWithNoOpenInstanceDoesNothing()
        {
            var stats = this.CreateStats(0, 1);

            stats.TryStartInstance(out var instance).ShouldBeFalse();
            stats.HasOpenOrRunningInstances.ShouldBeTrue();
            stats.HasOpenInstances.ShouldBeFalse();

            stats.RunningInstances.ShouldContain(new TestInstance($"0"));
        }

        /// <summary>
        /// Tests that open instance cannot be finished without starting it first.
        /// </summary>
        [Fact]
        public void FinishOpenInstanceReturnsFalseAndDoesNothing()
        {
            this.AllOpenStats.FinishInstance(GenomeStatsTest._instances[0], new TestResult(TimeSpan.FromSeconds(42))).ShouldBeFalse();
            this.AllOpenStats.OpenInstances.ShouldBe(GenomeStatsTest._instances);
            this.AllOpenStats.RunningInstances.ShouldBeEmpty();
            this.AllOpenStats.FinishedInstances.ShouldBeEmpty();
        }

        /// <summary>
        /// Tests if running instance is finished properly.
        /// </summary>
        [Fact]
        public void FinishInstanceMovesInstanceFromRunningToFinished()
        {
            var stats = this.CreateStats(0, 1);
            var instance = new TestInstance($"0");
            var result = new TestResult(TimeSpan.FromSeconds(42));

            stats.FinishInstance(instance, result).ShouldBeTrue();
            stats.OpenInstances.ShouldBeEmpty();
            stats.FinishedInstances.ContainsKey(instance).ShouldBeTrue();
            stats.FinishedInstances[instance].ShouldBe(result);

            stats.RuntimeOfFinishedInstances.ShouldBe(result.Runtime);
        }

        /// <summary>
        ///  Tests that the runtime of all finished instance is measured correctly.
        /// </summary>
        [Fact]
        public void TotalRuntimeOfFinishedInstancesIsCorrect()
        {
            var totalRuntime = TimeSpan.Zero;
            var instances = Enumerable.Range(0, 4).Select(i => new TestInstance($"{i}")).ToList();
            var results = Enumerable.Range(1, instances.Count).Select(i => new TestResult(TimeSpan.FromSeconds(i * 10))).ToList();

            var stats = new GenomeStats<TestInstance, TestResult>(GenomeStatsTest._genome, Enumerable.Empty<TestInstance>(), instances);

            for (var i = 0; i < instances.Count; i++)
            {
                totalRuntime += results[i].Runtime;
                stats.FinishInstance(instances[i], results[i]).ShouldBeTrue();
            }

            stats.HasOpenInstances.ShouldBeFalse();
            stats.HasOpenOrRunningInstances.ShouldBeFalse();

            stats.FinishedInstances.Count.ShouldBe(instances.Count);
            stats.RuntimeOfFinishedInstances.ShouldBe(totalRuntime);
        }

        /// <summary>
        /// Tests that open + running instances are moved to the <see cref="GenomeStats{TInstance,TResult}.CancelledByRacingInstances"/> when killing the genome.
        /// </summary>
        [Fact]
        public void UpdateCancelledByRacingMovesAllUnfinishedInstancesToCancelledByRacing()
        {
            var stats = this.CreateStats(5, 7);
            stats.UpdateCancelledByRacing().ShouldBeTrue();
            stats.CancelledByRacingInstances.Count().ShouldBe(12);
            stats.OpenInstances.ShouldBeEmpty();
            stats.RunningInstances.ShouldBeEmpty();
            stats.FinishedInstances.ShouldBeEmpty();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes GenomeStats.
        /// </summary>
        /// <param name="numOpen">The number of initially open instances.</param>
        /// <param name="numRunning">The number of initially running instances.</param>
        /// <returns>The new genome stats.</returns>
        private GenomeStats<TestInstance, TestResult> CreateStats(int numOpen, int numRunning)
        {
            var openInstances = Enumerable.Range(0, numOpen).Select(i => new TestInstance($"{i}")).ToList();
            var runningInstances = Enumerable.Range(numOpen, numRunning).Select(i => new TestInstance($"{i}")).ToList();

            var stats = new GenomeStats<TestInstance, TestResult>(GenomeStatsTest._genome, openInstances, runningInstances);
            return stats;
        }

        #endregion
    }
}