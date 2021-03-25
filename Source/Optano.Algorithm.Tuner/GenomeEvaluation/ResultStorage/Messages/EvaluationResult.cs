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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages
{
    using System;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Message containing a target algorithm run result.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    public class EvaluationResult<TInstance, TResult>
        where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EvaluationResult{TInstance,TResult}" /> class.
        /// </summary>
        /// <param name="genomeInstancePair">The <see cref="GenomeInstancePair{TInstance}"/>.</param>
        /// <param name="runResult">The run result.</param>
        public EvaluationResult(GenomeInstancePair<TInstance> genomeInstancePair, TResult runResult)
        {
            this.GenomeInstancePair = genomeInstancePair ?? throw new ArgumentNullException(nameof(genomeInstancePair));
            this.RunResult = runResult ?? throw new ArgumentNullException(nameof(runResult));
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the <see cref="GenomeInstancePair{TInstance}"/>.
        /// </summary>
        public GenomeInstancePair<TInstance> GenomeInstancePair { get; }

        /// <summary>
        /// Gets the run result.
        /// </summary>
        public TResult RunResult { get; }

        #endregion
    }
}