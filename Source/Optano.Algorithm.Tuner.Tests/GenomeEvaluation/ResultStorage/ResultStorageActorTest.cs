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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.ResultStorage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Akka.Actor;
    using Akka.Configuration;
    using Akka.TestKit.Xunit2;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="ResultStorageActor{TInstance,TResult}"/>.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class ResultStorageActorTest : TestKit
    {
        #region Static Fields

        /// <summary>
        /// An empty <see cref="ImmutableGenome"/>.
        /// </summary>
        private static readonly ImmutableGenome Genome = new ImmutableGenome(new Genome());

        /// <summary>
        /// A simple <see cref="TargetAlgorithm.InterfaceImplementations.TestInstance"/>.
        /// </summary>
        private static readonly TestInstance TestInstance = new TestInstance("1");

        /// <summary>
        /// Runtime of <see cref="TestResult"/>.
        /// </summary>
        private static readonly TimeSpan TestResultRuntime = TimeSpan.FromMilliseconds(42);

        /// <summary>
        /// A <see cref="TargetAlgorithm.InterfaceImplementations.TestResult"/> with a runtime of <see cref="TestResultRuntime"/>.
        /// </summary>
        private static readonly TestResult TestResult = new TestResult(ResultStorageActorTest.TestResultRuntime);

        #endregion

        #region Fields

        /// <summary>
        /// An actor reference to the <see cref="ResultStorageActor{TInstance, TResult}"/> used in tests. Needs to be initialized.
        /// </summary>
        private readonly IActorRef _resultStorageActorRef;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultStorageActorTest"/> class.
        /// </summary>
        public ResultStorageActorTest()
            : base(ConfigurationFactory.Load().WithFallback(TestKit.DefaultConfig))
        {
            this._resultStorageActorRef = this.Sys.ActorOf(
                Props.Create(() => new ResultStorageActor<TestInstance, TestResult>()),
                AkkaNames.ResultStorageActor);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that sending a <see cref="ResultMessage{TInstance,TResult}"/> message containing a run result leads to that result
        /// being stored in the <see cref="ResultStorageActor{TInstance, TResult}"/> s.t. subsequent <see cref="ResultRequest{TInstance}"/>s
        /// can find it.
        /// </summary>
        [Fact]
        public void RunResultGetsStored()
        {
            // Send result message containing a target algorithm run result.
            this._resultStorageActorRef.Tell(new ResultMessage<TestInstance, TestResult>(ResultStorageActorTest.Genome, ResultStorageActorTest.TestInstance, ResultStorageActorTest.TestResult));
            
            // Check if that one is returned if the genome - instance combination gets requested.
            this._resultStorageActorRef.Tell(new ResultRequest<TestInstance>(ResultStorageActorTest.Genome, ResultStorageActorTest.TestInstance));
            var storedRuntime = this.ExpectMsg<ResultMessage<TestInstance, TestResult>>().RunResult.Runtime;
            Assert.Equal(
                ResultStorageActorTest.TestResultRuntime,
                storedRuntime);
        }

        /// <summary>
        /// Checks that sending a <see cref="ResultMessage{TInstance, TResult}"/> message containing a run result leads to that result
        /// being stored in the <see cref="ResultStorageActor{TInstance, TResult}"/> even if a result for the same genome, but different
        /// instance is already stored.
        /// </summary>
        [Fact]
        public void RunResultGetsStoredEvenIfResultForOtherInstanceAlreadyExists()
        {
            // Prepare second instance and result.
            TestInstance secondInstance = new TestInstance("2");
            var secondResultRuntime = ResultStorageActorTest.TestResultRuntime + TimeSpan.FromMilliseconds(1);
            var secondResult = new TestResult(secondResultRuntime);

            // Send two result messages containing run results.
            this._resultStorageActorRef.Tell(new ResultMessage<TestInstance, TestResult>(ResultStorageActorTest.Genome, ResultStorageActorTest.TestInstance, ResultStorageActorTest.TestResult));
            this._resultStorageActorRef.Tell(
                new ResultMessage<TestInstance, TestResult>(ResultStorageActorTest.Genome, secondInstance, secondResult));

            // Check if the runtime of the second result is returned if the genome - instance combination gets
            // requested.
            this._resultStorageActorRef.Tell(new ResultRequest<TestInstance>(ResultStorageActorTest.Genome, secondInstance));
            var result = this.ExpectMsg<ResultMessage<TestInstance, TestResult>>().RunResult;
            Assert.Equal(
                secondResultRuntime,
                result.Runtime);
        }

        /// <summary>
        /// Checks that when sending two different <see cref="ResultMessage{TInstance, TResult}"/>s containing the same genome - instance
        /// combination, but different run results, the second result is not stored.
        /// </summary>
        [Fact]
        public void NewResultIsNotStoredIfOneAlreadyExists()
        {
            // Prepare second result.
            var secondRuntime = ResultStorageActorTest.TestResultRuntime + TimeSpan.FromMilliseconds(1);
            var secondResult = new TestResult(secondRuntime);

            // Send two result messages containing two different results.
            this._resultStorageActorRef.Tell(new ResultMessage<TestInstance, TestResult>(ResultStorageActorTest.Genome, ResultStorageActorTest.TestInstance, ResultStorageActorTest.TestResult));
            this._resultStorageActorRef.Tell(new ResultMessage<TestInstance, TestResult>(ResultStorageActorTest.Genome, ResultStorageActorTest.TestInstance, secondResult));

            // Check that the first result's runtime is returned if the genome - instance combination gets requested.
            this._resultStorageActorRef.Tell(new ResultRequest<TestInstance>(ResultStorageActorTest.Genome, ResultStorageActorTest.TestInstance));
            var result = this.ExpectMsg<ResultMessage<TestInstance, TestResult>>().RunResult;
            Assert.Equal(
                ResultStorageActorTest.TestResultRuntime,
                result.Runtime);
        }

        /// <summary>
        /// Checks that a <see cref="ResultRequest{TInstance}"/> on empty storage results in an answering
        /// <see cref="StorageMiss{TInstance}"/>.
        /// </summary>
        [Fact]
        public void ResultRequestOnEmptyStorageProducesStorageMiss()
        {
            this._resultStorageActorRef.Tell(new ResultRequest<TestInstance>(ResultStorageActorTest.Genome, ResultStorageActorTest.TestInstance));
            this.ExpectMsg<StorageMiss<TestInstance>>();
        }

        /// <summary>
        /// Checks that a <see cref="ResultRequest{TInstance}"/> results in an answering
        /// <see cref="StorageMiss{TInstance}"/> if there
        /// is a result stored for the genome, but it was done on another instance.
        /// </summary>
        [Fact]
        public void ResultRequestProducesStorageMissEvenIfGenomeIsKnown()
        {
            // Prepare another instance.
            TestInstance secondInstance = new TestInstance("2");

            // Add result for it.
            this._resultStorageActorRef.Tell(new ResultMessage<TestInstance, TestResult>(ResultStorageActorTest.Genome, secondInstance, ResultStorageActorTest.TestResult));

            // Message result request and check that a storage miss is returned.
            this._resultStorageActorRef.Tell(new ResultRequest<TestInstance>(ResultStorageActorTest.Genome, ResultStorageActorTest.TestInstance));
            this.ExpectMsg<StorageMiss<TestInstance>>();
        }

        /// <summary>
        /// Checks that a <see cref="ResultRequest{TInstance}"/> on a known genome - instance combination with a run
        /// result yields that run result as a <see cref="ResultMessage{TInstance, TResult}"/> message.
        /// </summary>
        [Fact]
        public void ResultRequestReturnsCorrectResultForStoredRun()
        {
            // Store the result.
            this._resultStorageActorRef.Tell(new ResultMessage<TestInstance, TestResult>(ResultStorageActorTest.Genome, ResultStorageActorTest.TestInstance, ResultStorageActorTest.TestResult));

            // Request it again and compare the runtime to the stored one.
            this._resultStorageActorRef.Tell(new ResultRequest<TestInstance>(ResultStorageActorTest.Genome, ResultStorageActorTest.TestInstance));
            var result = this.ExpectMsg<ResultMessage<TestInstance, TestResult>>().RunResult;
            Assert.Equal(
                ResultStorageActorTest.TestResultRuntime,
                result.Runtime);
        }

        /// <summary>
        /// Checks that a <see cref="ResultRequest{TInstance}"/> is not comparing a genome's age or the genes' order,
        /// but only the gene values, i.e. it will not return a <see cref="StorageMiss{TInstance}"/> if there is a run result
        /// for a genome - instance combination with a younger genome that has a different gene order, but overall
        /// contains the same genes.
        /// </summary>
        [Fact]
        public void ResultRequestReturnsCorrectResultForReorderedAgedGenome()
        {
            // Create first genome.
            var genome1 = new Genome(age: 1);
            genome1.SetGene("a", new Allele<int>(1));
            genome1.SetGene("b", new Allele<int>(2));

            // Then create a second one with same gene values, but different age and order.
            var genome2 = new Genome(age: 2);
            genome2.SetGene("b", new Allele<int>(2));
            genome2.SetGene("a", new Allele<int>(1));

            // Store result for first one.
            this._resultStorageActorRef.Tell(
                new ResultMessage<TestInstance, TestResult>(new ImmutableGenome(genome1), ResultStorageActorTest.TestInstance, ResultStorageActorTest.TestResult));

            // But request it for the second one.
            this._resultStorageActorRef.Tell(
                new ResultRequest<TestInstance>(new ImmutableGenome(genome2), ResultStorageActorTest.TestInstance));
            this.ExpectMsg<ResultMessage<TestInstance, TestResult>>();
        }

        /// <summary>
        /// Checks that a <see cref="AllResultsRequest"/> on a <see cref="ResultStorageActor{TInstance, TResult}"/> returns all stored
        /// results.
        /// </summary>
        [Fact]
        public void AllResultsRequestReturnsAllStoredResults()
        {
            // Create genomes.
            var genome1 = new Genome(age: 1);
            genome1.SetGene("a", new Allele<int>(1));
            var genome2 = new Genome(age: 2);
            genome2.SetGene("a", new Allele<int>(2));

            // Create instances.
            var instance1 = new TestInstance("1");
            var instance2 = new TestInstance("2");

            // Create results.
            var result1 = new TestResult(TimeSpan.FromMilliseconds(1));
            var result2 = new TestResult(TimeSpan.FromMilliseconds(2));
            var result3 = new TestResult(TimeSpan.FromMilliseconds(3));

            // Store some results.
            this._resultStorageActorRef.Tell(
                new ResultMessage<TestInstance, TestResult>(new ImmutableGenome(genome1), instance1, result1));
            this._resultStorageActorRef.Tell(
                new ResultMessage<TestInstance, TestResult>(new ImmutableGenome(genome1), instance2, result2));
            this._resultStorageActorRef.Tell(
                new ResultMessage<TestInstance, TestResult>(new ImmutableGenome(genome2), instance1, result3));

            // Ask for all results.
            this._resultStorageActorRef.Tell(new AllResultsRequest());
            var results = this.ExpectMsg<AllResults<TestInstance, TestResult>>();

            // Check returned results are associated with correct genomes.
            Assert.Equal(2, results.RunResults.Count);
            Assert.True(
                results.RunResults.Keys.Select(g => g.CreateMutableGenome()).Any(g => new Genome.GeneValueComparator().Equals(g, genome1)),
                "Expected different genome.");
            Assert.True(
                results.RunResults.Keys.Select(g => g.CreateMutableGenome()).Any(g => new Genome.GeneValueComparator().Equals(g, genome2)),
                "Expected different genome.");

            // Check all results have been returned.
            var resultsFirstGenome = new Dictionary<TestInstance, TestResult>(
                results.RunResults.Single(
                    kvp => new Genome.GeneValueComparator().Equals(kvp.Key.CreateMutableGenome(), genome1)).Value);
            Assert.Equal(2, resultsFirstGenome.Count);
            Assert.Equal(result1.Runtime, resultsFirstGenome[instance1].Runtime);
            Assert.Equal(result2.Runtime, resultsFirstGenome[instance2].Runtime);
            var resultsSecondGenome = new Dictionary<TestInstance, TestResult>(
                results.RunResults.Single(
                    kvp => new Genome.GeneValueComparator().Equals(kvp.Key.CreateMutableGenome(), genome2)).Value);
            Assert.Single(resultsSecondGenome);
            Assert.Equal(result3.Runtime, resultsSecondGenome[instance1].Runtime);
        }

        /// <summary>
        /// Checks that a <see cref="EvaluationStatisticRequest"/> on a
        /// <see cref="ResultStorageActor{TInstance, TResult}"/> returns the correct numbers.
        /// </summary>
        [Fact]
        public void EvaluationStatisticRequestReturnsCorrectNumbers()
        {
            // Create 3 genomes, two with the same values.
            var genome1 = new Genome(age: 1);
            genome1.SetGene("a", new Allele<int>(1));
            var genome2 = new Genome(age: 2);
            genome2.SetGene("a", new Allele<int>(2));
            var genome3 = new Genome(age: 3);
            genome3.SetGene("a", new Allele<int>(2));

            // Create instances.
            var instance1 = new TestInstance("1");
            var instance2 = new TestInstance("2");

            // Store some results.
            var result = new TestResult(TimeSpan.FromMilliseconds(1));
            this._resultStorageActorRef.Tell(
                new ResultMessage<TestInstance, TestResult>(new ImmutableGenome(genome1), instance1, result));
            this._resultStorageActorRef.Tell(
                new ResultMessage<TestInstance, TestResult>(new ImmutableGenome(genome2), instance1, result));
            this._resultStorageActorRef.Tell(
                new ResultMessage<TestInstance, TestResult>(new ImmutableGenome(genome3), instance2, result));

            // Ask for statistic.
            this._resultStorageActorRef.Tell(new EvaluationStatisticRequest());
            var results = this.ExpectMsg<EvaluationStatistic>();

            // Check the two identical genome values have been recognized.
            Assert.Equal(
                2,
                results.ConfigurationCount);
            Assert.Equal(3, results.TotalEvaluationCount);
        }

        /// <summary>
        /// Checks that a <see cref="GenomeResultsRequest"/> on a <see cref="ResultStorageActor{I,R}"/> returns all 
        /// stored results for the provided genome.
        /// </summary>
        [Fact]
        public void GenomeResultsRequestReturnsAllStoredResultsForGenome()
        {
            // Create genomes.
            var genome1 = new Genome(age: 1);
            genome1.SetGene("a", new Allele<int>(1));
            var genome2 = new Genome(age: 2);
            genome2.SetGene("a", new Allele<int>(2));

            // Create instances.
            var instance1 = new TestInstance("1");
            var instance2 = new TestInstance("2");

            // Create results.
            var result1 = new TestResult(1);
            var result2 = new TestResult(2);
            var result3 = new TestResult(3);

            // Store some results.
            this._resultStorageActorRef.Tell(
                new ResultMessage<TestInstance, TestResult>(new ImmutableGenome(genome1), instance1, result1));
            this._resultStorageActorRef.Tell(
                new ResultMessage<TestInstance, TestResult>(new ImmutableGenome(genome1), instance2, result2));
            this._resultStorageActorRef.Tell(
                new ResultMessage<TestInstance, TestResult>(new ImmutableGenome(genome2), instance1, result3));

            // Ask for all results of one of the genomes.
            this._resultStorageActorRef.Tell(new GenomeResultsRequest(new ImmutableGenome(genome1)));
            var results = this.ExpectMsg<GenomeResults<TestInstance, TestResult>>();

            // Check all results for that genome have been returned.
            var instanceResults = new Dictionary<TestInstance, TestResult>(results.RunResults);
            Assert.Equal(2, instanceResults.Count);
            Assert.Equal(result1.Runtime, instanceResults[instance1].Runtime);
            Assert.Equal(result2.Runtime, instanceResults[instance2].Runtime);
        }

        /// <summary>
        /// Checks that a <see cref="GenomeResultsRequest"/> on a <see cref="ResultStorageActor{I,R}"/> may return an 
        /// empty collections if no results exist for the provided genome.
        /// </summary>
        [Fact]
        public void GenomeResultsRequestMayReturnEmptyCollection()
        {
            // Create genome.
            var genome = new ImmutableGenome(new Genome(age: 1));

            // Ask for all results for that genome.
            this._resultStorageActorRef.Tell(new GenomeResultsRequest(genome));
            var results = this.ExpectMsg<GenomeResults<TestInstance, TestResult>>();

            // Check no results for that genome have been returned.
            Assert.True(results.RunResults.IsEmpty, "Did not expect any results for the genome.");
        }

        #endregion
    }
}
