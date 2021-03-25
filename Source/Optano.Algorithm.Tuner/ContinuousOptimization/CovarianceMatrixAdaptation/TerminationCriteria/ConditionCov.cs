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

namespace Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation.TerminationCriteria
{
    using System;

    using Optano.Algorithm.Tuner.Serialization;

    /// <summary>
    /// Termination criterion for <see cref="CmaEs{TSearchPoint}"/> based on numerical stability:
    /// Stop if the condition number of the covariance matrix exceeds a certain number.
    /// <para>
    /// This criterion was defined in Auger A, Hansen N: A restart CMA evolution strategy with increasing population size.
    /// In Proceedings of the IEEE Congress on Evolutionary Computation, 2005.
    /// </para>
    /// </summary>
    public class ConditionCov : ITerminationCriterion
    {
        #region Static Fields

        /// <summary>
        /// The maximum condition number which does not lead to termination.
        /// </summary>
        public static readonly double MaxCondition = 1E14;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks whether the condition number of the covariance matrix exceeds <see cref="MaxCondition"/>.
        /// </summary>
        /// <param name="data">Internal <see cref="CmaEs{TSearchPoint}"/> data.</param>
        /// <returns>Whether the condition number exceeds <see cref="MaxCondition"/>.</returns>
        public bool IsMet(CmaEsElements data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Covariances == null)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(data),
                    "Data must have the covariance matrix set for this termination criterion.");
            }

            return data.Covariances.ConditionNumber() > MaxCondition;
        }

        /// <summary>
        /// Restores an object after deserialization by <see cref="StatusBase"/>.
        /// </summary>
        /// <remarks>For example, this method might set equality definitions in dictionaries.</remarks>
        /// <returns>The object in restored state.</returns>
        public ITerminationCriterion Restore()
        {
            // No internal state --> nothing to do here.
            return this;
        }

        #endregion
    }
}