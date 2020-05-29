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
    using System.Collections.Immutable;

    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Message containing target algorithm run results for a specific genome.
    /// </summary>
    /// <typeparam name="TResult">Type of the result of a target algorithm run.</typeparam>
    public class PartialGenomeEvaluationResults<TResult>
        where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PartialGenomeEvaluationResults{TResult}"/> class.
        /// </summary>
        /// <remarks>
        /// Internal because this should be used in combination with <see cref="GenomeEvaluationFinished"/> only.
        /// </remarks>
        /// <param name="evaluationId">
        /// The evaluation identifier as specified by the <see cref="GenomeEvaluation"/> request.
        /// </param>
        /// <param name="runResults">
        /// Results of target algorithm runs.
        /// </param>
        internal PartialGenomeEvaluationResults(int evaluationId, IEnumerable<TResult> runResults)
        {
            this.EvaluationId = evaluationId;

            // Copy the fields over s.t. the message is immutable.
            this.RunResults = runResults?.ToImmutableList() ?? throw new ArgumentNullException(nameof(runResults));
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the evaluation identifier as specified by the <see cref="GenomeEvaluation"/> request.
        /// </summary>
        public int EvaluationId { get; }

        /// <summary>
        /// Gets the results of target algorithm runs.
        /// </summary>
        public ImmutableList<TResult> RunResults { get; }

        #endregion
    }
}