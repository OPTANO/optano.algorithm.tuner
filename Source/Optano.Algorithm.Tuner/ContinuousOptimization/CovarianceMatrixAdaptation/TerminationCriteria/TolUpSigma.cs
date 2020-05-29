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

namespace Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation.TerminationCriteria
{
    using System;

    using Optano.Algorithm.Tuner.Serialization;

    /// <summary>
    /// Termination criterion for <see cref="CmaEs{TSearchPoint}"/> based on divergence:
    /// Stop if the factor between current and initial step size is greater than a constant times the square root
    /// of the covariance matrix's largest eigenvalue.
    /// <para>
    /// This criterion was defined in Hansen N. Benchmarking a BI-Population CMA-ES on the BBOB-2009 Function
    /// Testbed. In the workshop Proceedings of the Genetic and Evolutionary Computation
    /// Conference, GECCO, pages 2389–2395. ACM, 2009.
    /// </para>
    /// </summary>
    public class TolUpSigma : ITerminationCriterion
    {
        #region Static Fields

        /// <summary>
        /// The maximum factor which does not lead to termination.
        /// </summary>
        public static readonly double MaxFactor = 1E4;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks if the factor between current and initial step size is greater than a <see cref="MaxFactor"/> times
        /// the square root of the covariance matrix's largest eigenvalue.
        /// </summary>
        /// <param name="data">Internal <see cref="CmaEs{TSearchPoint}"/> data.</param>
        /// <returns>Whether the factor is greater.</returns>
        public bool IsMet(CmaEsElements data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (!data.IsCompletelySpecified())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(data),
                    "Data must be completely specified for this termination criterion.");
            }

            var largestEigenvalue = data.CovariancesDiagonal.Diagonal().Maximum();
            return data.StepSize / data.Configuration.InitialStepSize > MaxFactor * Math.Sqrt(largestEigenvalue);
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