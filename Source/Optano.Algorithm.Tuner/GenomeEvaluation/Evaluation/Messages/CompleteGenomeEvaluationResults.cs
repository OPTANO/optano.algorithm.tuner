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
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Utility methods to send the results of a <see cref="GenomeEvaluation"/>.
    /// </summary>
    public static class CompleteGenomeEvaluationResults
    {
        #region Constants

        /// <summary>
        /// The maximum number of results to wrap into a <see cref="PartialGenomeEvaluationResults{TResult}"/> message.
        /// </summary>
        private const int MaximumResultChunkSize = 50;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates all required messages to send the results of a <see cref="GenomeEvaluation"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="evaluationId">
        /// The evaluation identifier as specified by the <see cref="GenomeEvaluation"/> request.
        /// </param>
        /// <param name="runResults">Results of target algorithm runs.</param>
        /// <returns>
        /// <see cref="PartialGenomeEvaluationResults{TResult}"/> messages partitioning <paramref name="runResults"/>
        /// followed by a <see cref="GenomeEvaluationFinished"/> message.
        /// </returns>
        public static IEnumerable<object> CreateEvaluationResultMessages<TResult>(
            int evaluationId,
            List<TResult> runResults)
            where TResult : ResultBase<TResult>, new()
        {
            foreach (var partialResultMessage in CreatePartialResultMessages(evaluationId, runResults))
            {
                yield return partialResultMessage;
            }

            yield return new GenomeEvaluationFinished(evaluationId, runResults.Count);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates <see cref="PartialGenomeEvaluationResults{TResult}"/>s messages that partition
        /// <paramref name="runResults"/> into chunks of at most <see cref="MaximumResultChunkSize"/> results each.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="evaluationId">
        /// The evaluation identifier as specified by the <see cref="GenomeEvaluation"/> request.
        /// </param>
        /// <param name="runResults">The results to partition.</param>
        /// <returns>
        /// <see cref="PartialGenomeEvaluationResults{TResult}"/> messages partitioning <paramref name="runResults"/>.
        /// </returns>
        private static IEnumerable<object> CreatePartialResultMessages<TResult>(
            int evaluationId,
            List<TResult> runResults)
            where TResult : ResultBase<TResult>, new()
        {
            int nextChunkStartIndex = 0;
            while (nextChunkStartIndex < runResults.Count)
            {
                var remainingResultNumber = runResults.Count - nextChunkStartIndex;
                var chunkSize = Math.Min(MaximumResultChunkSize, remainingResultNumber);

                yield return new PartialGenomeEvaluationResults<TResult>(
                    evaluationId,
                    runResults.GetRange(nextChunkStartIndex, chunkSize));

                nextChunkStartIndex += chunkSize;
            }
        }

        #endregion
    }
}