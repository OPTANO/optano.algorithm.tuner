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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.ResultStorage.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="AllResults{TInstance,TResult}"/> class.
    /// </summary>
    public class AllResultsTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Tests that the constructor throws an <see cref="ArgumentNullException"/> if no results are provided.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingResults()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AllResults<TestInstance, TestResult>(runResults: null));
        }

        /// <summary>
        /// Tests that <see cref="AllResults{I,R}.RunResults"/> contains the results provided on intialization.
        /// </summary>
        [Fact]
        public void RunResultsAreSetCorrectly()
        {
            // Create results for two genomes and two test instances:
            var runResults = new Dictionary<ImmutableGenome, IDictionary<TestInstance, TestResult>>();

            // (a) Create genomes and instances.
            var genome1 = new ImmutableGenome(new Genome());
            var genome2 = new ImmutableGenome(new Genome());
            var instance1 = new TestInstance("1");
            var instance2 = new TestInstance("2");

            // (b) Create results for each pair.
            var results1 = new Dictionary<TestInstance, TestResult>();
            var results2 = new Dictionary<TestInstance, TestResult>();
            results1.Add(instance1, new TestResult(1));
            results1.Add(instance2, new TestResult(2));
            results2.Add(instance1, new TestResult(3));
            results2.Add(instance2, new TestResult(4));

            // (c) Add them to dictionary.
            runResults.Add(genome1, results1);
            runResults.Add(genome2, results2);

            // Create all results message.
            var resultMessage = new AllResults<TestInstance, TestResult>(runResults);

            // Check it has copied over the dictionary correctly.
            Assert.Equal(2, resultMessage.RunResults.Count);
            Assert.True(
                TestUtils.SetsAreEquivalent(resultMessage.RunResults[genome1], results1),
                "Results for first genome should have been different.");
            Assert.True(
                TestUtils.SetsAreEquivalent(resultMessage.RunResults[genome2], results2),
                "Results for second genome should have been different.");
        }

        /// <summary>
        /// Tests that the collection returned by <see cref="AllResults{I,R}.RunResults"/> does not reflect any changes
        /// of the dictionaries provided on initialization.
        /// </summary>
        [Fact]
        public void RunResultsAreImmutable()
        {
            // Create run results.
            var runResults = new Dictionary<ImmutableGenome, IDictionary<TestInstance, TestResult>>();
            var genome1 = new ImmutableGenome(new Genome());
            var instance1 = new TestInstance("1");
            var results1 = new Dictionary<TestInstance, TestResult> { { instance1, new TestResult(1) } };
            runResults.Add(genome1, results1);

            // Create message out of them.
            var resultMessage = new AllResults<TestInstance, TestResult>(runResults);
            Assert.True(runResults.Keys.SequenceEqual(resultMessage.RunResults.Keys));
            Assert.True(resultMessage.RunResults[genome1].SequenceEqual(results1));

            // Add more results to original dictionary.
            results1.Add(new TestInstance("2"), new TestResult());

            // Make sure that is not reflected in message.
            Assert.False(resultMessage.RunResults[genome1].SequenceEqual(results1));

            // Remove all genome results from original results.
            runResults.Remove(genome1);

            // Make sure that is not reflected in message.
            Assert.False(runResults.Keys.SequenceEqual(resultMessage.RunResults.Keys));
        }

        #endregion
    }
}