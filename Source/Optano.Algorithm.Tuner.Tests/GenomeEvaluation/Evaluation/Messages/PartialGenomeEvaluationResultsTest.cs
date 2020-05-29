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
    /// Contains tests for <see cref="PartialGenomeEvaluationResults{TResult}"/>.
    /// </summary>
    public class PartialGenomeEvaluationResultsTest
    {
        #region Fields

        /// <summary>
        /// An evaluation ID used for message construction.
        /// </summary>
        private readonly int _evaluationId = 17;

        /// <summary>
        /// <see cref="TestResult"/>s used for message construction, have to be initialized.
        /// </summary>
        private readonly List<TestResult> _testResults;

        /// <summary>
        /// <see cref="PartialGenomeEvaluationResults{TResult}"/> to use in tests. Needs to be initialized.
        /// </summary>
        private readonly PartialGenomeEvaluationResults<TestResult> _partialGenomeEvaluationResults;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PartialGenomeEvaluationResultsTest"/> class.
        /// </summary>
        public PartialGenomeEvaluationResultsTest()
        {
            this._testResults = this.CreateSingleTestResult();
            this._partialGenomeEvaluationResults = new PartialGenomeEvaluationResults<TestResult>(
                this._evaluationId,
                this._testResults);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="PartialGenomeEvaluationResults{TestResult}"/>'s constructor and setting
        /// the run results to null throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnRunResultsSetToNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new PartialGenomeEvaluationResults<TestResult>(
                    this._evaluationId,
                    runResults: null));
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeEvaluationResults{R}.EvaluationId"/> returns the same ID it was
        /// initialized with.
        /// </summary>
        [Fact]
        public void EvaluationIdIsSetCorrectly()
        {
            Assert.Equal(
                this._evaluationId,
                this._partialGenomeEvaluationResults.EvaluationId);
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeEvaluationResults{R}.RunResults"/> returns the same results it was 
        /// initialized with.
        /// </summary>
        [Fact]
        public void RunResultsAreSetCorrectly()
        {
            Assert.True(
                this._testResults.SequenceEqual(this._partialGenomeEvaluationResults.RunResults),
                "Run results were not set correctly.");
        }

        /// <summary>
        /// Checks that modifying the run results <see cref="PartialGenomeEvaluationResults{R}"/> was initialized 
        /// with does not modify the message.
        /// </summary>
        [Fact]
        public void RunResultsAreImmutable()
        {
            // Precondition check: results are the same.
            Assert.True(this._testResults.SequenceEqual(this._partialGenomeEvaluationResults.RunResults));

            // Modify the external list.
            this._testResults.Add(new TestResult());

            // Check that didn't modify the internal list.
            Assert.False(
                this._testResults.SequenceEqual(this._partialGenomeEvaluationResults.RunResults),
                "Run results have been modified even though they are supposed to be immutable.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a list containing a single <see cref="TestResult"/>.
        /// </summary>
        /// <returns>The list.</returns>
        private List<TestResult> CreateSingleTestResult()
        {
            return new List<TestResult>() { new TestResult() };
        }

        #endregion
    }
}