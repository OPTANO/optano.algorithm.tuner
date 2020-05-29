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

namespace Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation
{
    using Optano.Algorithm.Tuner.Serialization;

    /// <summary>
    /// Termination criterion for <see cref="CmaEs{TSearchPoint}"/>.
    /// </summary>
    public interface ITerminationCriterion : IDeserializationRestorer<ITerminationCriterion>
    {
        #region Public Methods and Operators

        /// <summary>
        /// Computes whether the criterion is met and <see cref="CmaEs{TSearchPoint}"/> should be terminated.
        /// </summary>
        /// <param name="data">Internal <see cref="CmaEs{TSearchPoint}"/> data.</param>
        /// <returns>Whether the criterion is met.</returns>
        bool IsMet(CmaEsElements data);

        #endregion
    }
}