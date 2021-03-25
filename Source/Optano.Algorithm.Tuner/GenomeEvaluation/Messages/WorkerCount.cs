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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Messages
{
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;

    /// <summary>
    /// Message containing the number of <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}" />s that have been available to
    /// <see cref="GenerationEvaluationActor{A, I, R}" /> when the message was created.
    /// </summary>
    public class WorkerCount
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerCount" /> class.
        /// </summary>
        /// <param name="count">
        /// The number of <see cref="EvaluationActor{A, I, R}" />s that are available right
        /// now.
        /// </param>
        public WorkerCount(int count)
        {
            this.Count = count;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the number of <see cref="EvaluationActor{A, I, R}" />s that have been available when the message
        /// was created.
        /// </summary>
        public int Count { get; }

        #endregion
    }
}