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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation.Messages
{
    using System;

    /// <summary>
    /// Message indicating that a <see cref="GenomeEvaluation"/> was handled completely.
    /// All <see cref="PartialGenomeEvaluationResults{TResult}"/> should have been sent beforehand.
    /// </summary>
    public class GenomeEvaluationFinished
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeEvaluationFinished"/> class.
        /// </summary>
        /// <remarks>
        /// Internal because this should be used in combination with
        /// <see cref="PartialGenomeEvaluationResults{TResult}"/> only.
        /// </remarks>
        /// <param name="evaluationId">
        /// The evaluation identifier as specified by the <see cref="GenomeEvaluation"/> request.
        /// </param>
        /// <param name="expectedResultCount">
        /// The number of results sent via <see cref="PartialGenomeEvaluationResults{TResult}"/> messages.
        /// </param>
        internal GenomeEvaluationFinished(int evaluationId, int expectedResultCount)
        {
            if (expectedResultCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedResultCount),
                    $"The expected number of results should not be negative, but was {expectedResultCount}.");
            }

            this.EvaluationId = evaluationId;
            this.ExpectedResultCount = expectedResultCount;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the evaluation identifier as specified by the <see cref="GenomeEvaluation"/> request.
        /// </summary>
        public int EvaluationId { get; }

        /// <summary>
        /// Gets the number of results sent via <see cref="PartialGenomeEvaluationResults{TResult}"/> messages.
        /// </summary>
        public int ExpectedResultCount { get; }

        #endregion
    }
}