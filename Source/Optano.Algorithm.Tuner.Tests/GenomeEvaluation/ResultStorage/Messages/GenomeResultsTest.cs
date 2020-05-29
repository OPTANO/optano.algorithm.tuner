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
namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.ResultStorage.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GenomeResults{TInstance,TResult}"/> class.
    /// </summary>
    public class GenomeResultsTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="GenomeResults{I, R}"/>'s constructor without providing a
        /// results dictionary throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingResultsDictionary()
        {
            Assert.Throws<ArgumentNullException>(() => new GenomeResults<TestInstance, TestResult>(runResults: null));
        }

        /// <summary>
        /// Tests that <see cref="GenomeResults{I,R}.RunResults"/> contains the results provided on initialization.
        /// </summary>
        [Fact]
        public void RunResultsAreSetCorrectly()
        {
            // Create results for two test instances:
            var runResults = new Dictionary<TestInstance, TestResult>();
            var instance1 = new TestInstance("1");
            var instance2 = new TestInstance("2");
            var result1 = new TestResult(1);
            var result2 = new TestResult(2);
            runResults.Add(instance1, result1);
            runResults.Add(instance2, result2);

            // Create results message.
            var resultMessage = new GenomeResults<TestInstance, TestResult>(runResults);

            // Check it has copied over the dictionary correctly.
            Assert.Equal(2, resultMessage.RunResults.Count);
            Assert.Equal(
                result1,
                resultMessage.RunResults[instance1]);
            Assert.Equal(
                result2,
                resultMessage.RunResults[instance2]);
        }

        /// <summary>
        /// Tests that the collection returned by <see cref="GenomeResults{I,R}.RunResults"/> does not reflect any changes
        /// to the dictionary provided on initialization.
        /// </summary>
        [Fact]
        public void RunResultsAreImmutable()
        {
            // Create run results.
            var runResults = new Dictionary<TestInstance, TestResult>();
            var instance = new TestInstance("1");
            var result = new TestResult(1);
            runResults.Add(instance, result);

            // Create message out of them.
            var resultMessage = new GenomeResults<TestInstance, TestResult>(runResults);
            Assert.True(runResults.Keys.SequenceEqual(resultMessage.RunResults.Keys));
            Assert.Equal(result, resultMessage.RunResults[instance]);

            // Remove all results from original results.
            runResults.Remove(instance);

            // Make sure that is not reflected in message.
            Assert.False(runResults.Keys.SequenceEqual(resultMessage.RunResults.Keys));
        }

        #endregion
    }
}
