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

namespace Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation
{
    using System;
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Serialization;

    /// <summary>
    /// An object wrapping the current status of <see cref="CmaEs{TSearchPoint}"/>.
    /// Can be serialized to a file and deserialized from one.
    /// </summary>
    public class CmaEsStatus : StatusBase
    {
        #region Constants

        /// <summary>
        /// File name to use for serialized data.
        /// </summary>
        public const string FileName = "status.cmaes";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CmaEsStatus"/> class.
        /// </summary>
        /// <param name="terminationCriteria">Criteria for termination.</param>
        /// <param name="data">Internal <see cref="CmaEs{TSearchPoint}"/> data.</param>
        public CmaEsStatus(
            List<ITerminationCriterion> terminationCriteria,
            CmaEsElements data)
        {
            if (terminationCriteria == null)
            {
                throw new ArgumentNullException(nameof(terminationCriteria));
            }

            this.TerminationCriteria = terminationCriteria;
            this.Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the criteria for termination.
        /// </summary>
        public List<ITerminationCriterion> TerminationCriteria { get; }

        /// <summary>
        /// Gets internal <see cref="CmaEs{TSearchPoint}"/> data.
        /// </summary>
        public CmaEsElements Data { get; }

        #endregion
    }
}