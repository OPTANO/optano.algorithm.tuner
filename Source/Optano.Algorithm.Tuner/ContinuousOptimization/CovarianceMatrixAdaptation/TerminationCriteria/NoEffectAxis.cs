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
    /// Termination criterion for <see cref="CmaEs{TSearchPoint}"/> based on numerical stability:
    /// Stop if adding a 0.1-standard deviation vector in any principal axis direction of the covariance matrix does
    /// not change the distribution mean.
    /// <para>
    /// This criterion was defined in Auger A, Hansen N: A restart CMA evolution strategy with increasing population size.
    /// In Proceedings of the IEEE Congress on Evolutionary Computation, 2005.
    /// </para>
    /// </summary>
    public class NoEffectAxis : ITerminationCriterion
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks adding a 0.1-standard deviation vector in a principal axis direction of the covariance matrix does
        /// not change the distribution mean.
        /// <para>Axis is chosen dependent on generation.</para>
        /// </summary>
        /// <param name="data">Internal <see cref="CmaEs{TSearchPoint}"/> data.</param>
        /// <returns>Whether no change occurred in distribution mean.</returns>
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

            int index = data.Generation % data.Configuration.SearchSpaceDimension;
            var principalAxisDirection =
                Math.Sqrt(data.CovariancesDiagonal[index, index]) * data.CovariancesEigenVectors.Column(index);
            var shiftedMean = data.DistributionMean + ((0.1 * data.StepSize) * principalAxisDirection);

            // Might be true due to numerical issues.
            return data.DistributionMean.Equals(shiftedMean);
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