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

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation.Messages;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GenomeEvaluationFinished"/> class.
    /// </summary>
    public class GenomeEvaluationFinishedTest
    {
        #region Fields

        /// <summary>
        /// A number which can be used as the evaluation identifier.
        /// </summary>
        private readonly int _evaluationId = 16;

        /// <summary>
        /// The number of expected evaluation results.
        /// </summary>
        private readonly int _expectedResultCount = 230;

        /// <summary>
        /// <see cref="GenomeEvaluationFinished"/> to use in tests. Needs to be initialized.
        /// </summary>
        private readonly GenomeEvaluationFinished _finishedMessage;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeEvaluationFinishedTest"/> class.
        /// </summary>
        public GenomeEvaluationFinishedTest()
        {
            this._finishedMessage = new GenomeEvaluationFinished(this._evaluationId, this._expectedResultCount);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="GenomeEvaluationFinished"/>'s constructor with a
        /// negative number of expected evaluation results throws a <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnNegativeExpectedResultCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenomeEvaluationFinished(this._evaluationId, expectedResultCount: -1));
        }

        /// <summary>
        /// Checks that <see cref="GenomeEvaluationFinished.ExpectedResultCount"/> returns the same number
        /// it was initialized with.
        /// </summary>
        [Fact]
        public void ExpectedResultCountIsSetCorrectly()
        {
            Assert.Equal(
                this._expectedResultCount,
                this._finishedMessage.ExpectedResultCount);
        }

        /// <summary>
        /// Checks that <see cref="GenomeEvaluationFinished.EvaluationId"/> returns the same ID it was initialized
        /// with.
        /// </summary>
        [Fact]
        public void EvaluationIdIsSetCorrectly()
        {
            Assert.Equal(
                this._evaluationId,
                this._finishedMessage.EvaluationId);
        }

        #endregion
    }
}