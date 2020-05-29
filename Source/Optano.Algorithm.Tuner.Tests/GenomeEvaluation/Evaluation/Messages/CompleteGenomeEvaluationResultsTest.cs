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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.Evaluation.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation.Messages;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="CompleteGenomeEvaluationResults"/> class.
    /// </summary>
    public class CompleteGenomeEvaluationResultsTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="CompleteGenomeEvaluationResults.CreateEvaluationResultMessages{TResult}"/>
        /// creates two messages if called with few results: One to send results and one to terminate the message flow.
        /// </summary>
        [Fact]
        public void CreateEvaluationResultMessagesForFewResults()
        {
            var messages = CompleteGenomeEvaluationResults.CreateEvaluationResultMessages(
                    evaluationId: 43,
                    runResults: CompleteGenomeEvaluationResultsTest.CreateResults(number: 2))
                .ToList();

            Assert.True(
                2 == messages.Count,
                "There should be two messages: One to send results and one to terminate the message flow.");
            Assert.True(
                messages[0] is PartialGenomeEvaluationResults<TestResult>,
                "The first message should send results.");
            Assert.True(
                messages[1] is GenomeEvaluationFinished,
                "The second message should terminate the message flow.");

            CompleteGenomeEvaluationResultsTest.CheckAddMessageContainsCorrectResults(
                (PartialGenomeEvaluationResults<TestResult>)messages[0],
                firstResultIdentifier: 0,
                numberResults: 2);
            Assert.Equal(
                43,
                ((PartialGenomeEvaluationResults<TestResult>)messages[0]).EvaluationId);
            Assert.Equal(
                43,
                ((GenomeEvaluationFinished)messages[1]).EvaluationId);
            Assert.Equal(
                2,
                ((GenomeEvaluationFinished)messages[1]).ExpectedResultCount);
        }

        /// <summary>
        /// Checks that <see cref="CompleteGenomeEvaluationResults.CreateEvaluationResultMessages{TResult}"/> creates
        /// several messages if called with many instances: Multiple to send results and a single one to terminate the
        /// message flow.
        /// </summary>
        [Fact]
        public void CreateEvaluationResultMessagesWorksForManyInstances()
        {
            int numberResults = 207;
            var messages = CompleteGenomeEvaluationResults.CreateEvaluationResultMessages(
                evaluationId: 3,
                runResults: CompleteGenomeEvaluationResultsTest.CreateResults(numberResults))
                .ToList();

            Assert.True(
                6 == messages.Count,
                "There should be six messages: One to terminate the message flow and five to send results.");
            Assert.True(messages[5] is GenomeEvaluationFinished, "The last message should terminate the message flow.");
            Assert.Equal(
                3,
                ((GenomeEvaluationFinished)messages[5]).EvaluationId);
            Assert.Equal(
                207,
                ((GenomeEvaluationFinished)messages[5]).ExpectedResultCount);

            for (int i = 0; i <= 3; i++)
            {
                Assert.True(
                    messages[i] is PartialGenomeEvaluationResults<TestResult>,
                    $"The {i + 1}th message should send results.");
                CompleteGenomeEvaluationResultsTest.CheckAddMessageContainsCorrectResults(
                    (PartialGenomeEvaluationResults<TestResult>)messages[i],
                    firstResultIdentifier: i * 50,
                    numberResults: 50);
                Assert.Equal(
                    3,
                    ((PartialGenomeEvaluationResults<TestResult>)messages[i]).EvaluationId);
            }

            Assert.True(
                messages[4] is PartialGenomeEvaluationResults<TestResult>,
                "The fifth message should add the remaining results.");
            CompleteGenomeEvaluationResultsTest.CheckAddMessageContainsCorrectResults(
                (PartialGenomeEvaluationResults<TestResult>)messages[4],
                firstResultIdentifier: 200,
                numberResults: 7);
            Assert.Equal(
                3,
                ((PartialGenomeEvaluationResults<TestResult>)messages[4]).EvaluationId);
        }

        /// <summary>
        /// Checks that <see cref="CompleteGenomeEvaluationResults.CreateEvaluationResultMessages{TResult}"/> creates
        /// several messages if called with results divisible by chunk size: Multiple maximum size ones to send all
        /// results and a single one to terminate the message flow.
        /// </summary>
        [Fact]
        public void CreateInstanceUpdateMessagesWorksForEdgeCases()
        {
            int numberInstances = 100;
            var messages = CompleteGenomeEvaluationResults.CreateEvaluationResultMessages(
                    evaluationId: 230,
                    runResults: CompleteGenomeEvaluationResultsTest.CreateResults(numberInstances))
                .ToList();

            Assert.True(
                3 == messages.Count,
                "There should be three messages: One to clear terminate the message flow and two to send results.");
            Assert.True(
                messages[2] is GenomeEvaluationFinished,
                "The last message should terminate the message flow.");
            Assert.Equal(
                230,
                ((GenomeEvaluationFinished)messages[2]).EvaluationId);
            Assert.Equal(
                100,
                ((GenomeEvaluationFinished)messages[2]).ExpectedResultCount);

            for (int i = 0; i <= 1; i++)
            {
                Assert.True(
                    messages[i] is PartialGenomeEvaluationResults<TestResult>,
                    $"The {i + 1}th message should send results.");
                CompleteGenomeEvaluationResultsTest.CheckAddMessageContainsCorrectResults(
                    (PartialGenomeEvaluationResults<TestResult>)messages[i],
                    firstResultIdentifier: i * 50,
                    numberResults: 50);
                Assert.True(
                    230 == ((PartialGenomeEvaluationResults<TestResult>)messages[i]).EvaluationId,
                    $"Evaluation ID of {i + 1}th partial result message was not as expected.");
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a number of unique <see cref="TestResult"/>s.
        /// </summary>
        /// <param name="number">The number of <see cref="TestResult"/>s to create.</param>
        /// <returns>The created <see cref="TestResult"/>s.</returns>
        private static List<TestResult> CreateResults(int number)
        {
            var results = new List<TestResult>(number);
            for (int i = 0; i < number; i++)
            {
                results.Add(new TestResult(TimeSpan.FromMilliseconds(i)));
            }

            return results;
        }

        /// <summary>
        /// Checks that a <see cref="PartialGenomeEvaluationResults{TResult}"/> message contains the expected
        /// <see cref="TestResult"/>s.
        /// </summary>
        /// <param name="message">The <see cref="PartialGenomeEvaluationResults{TResult}"/> message.</param>
        /// <param name="firstResultIdentifier">
        /// The first expected result identifier. Remaining identifiers are successive integers.
        /// </param>
        /// <param name="numberResults">The expected number of <see cref="TestResult"/>s.</param>
        private static void CheckAddMessageContainsCorrectResults(
            PartialGenomeEvaluationResults<TestResult> message,
            int firstResultIdentifier,
            int numberResults)
        {
            Assert.Equal(
                Enumerable.Range(firstResultIdentifier, numberResults).ToArray(),
                message.RunResults.Select(instance => (int)instance.Runtime.TotalMilliseconds).ToArray());
        }

        #endregion
    }
}
