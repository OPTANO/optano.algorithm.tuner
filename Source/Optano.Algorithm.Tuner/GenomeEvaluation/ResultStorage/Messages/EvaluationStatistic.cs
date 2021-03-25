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

    /// <summary>
    /// A message containing statistical information about all results a
    /// <see cref="ResultStorageActor{TInstance, TResult}"/> has collected up to the message send point.
    /// </summary>
    public class EvaluationStatistic
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EvaluationStatistic"/> class.
        /// </summary>
        /// <param name="configurationCount">
        /// The number of unique parameter configurations that have been evaluated.
        /// </param>
        /// <param name="totalEvaluationCount">
        /// The total sum of evaluations, i. e. configuration - instance runs, that have been executed.
        /// </param>
        public EvaluationStatistic(int configurationCount, int totalEvaluationCount)
        {
            if (configurationCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configurationCount),
                    configurationCount,
                    "Configuration count should never be negative.");
            }

            if (totalEvaluationCount < configurationCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalEvaluationCount),
                    $"Each configuration has at least one evaluation, but a total evaluation count of {totalEvaluationCount} was provided for {configurationCount} configurations.");
            }

            this.ConfigurationCount = configurationCount;
            this.TotalEvaluationCount = totalEvaluationCount;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the number of unique parameter configurations that have been evaluated.
        /// </summary>
        public int ConfigurationCount { get; }

        /// <summary>
        /// Gets the total sum of evaluations, i. e. configuration - instance runs, that have been executed.
        /// </summary>
        public int TotalEvaluationCount { get; }

        #endregion
    }
}