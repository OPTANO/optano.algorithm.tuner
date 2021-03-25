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
    /// Termination criterion for <see cref="CmaEs{TSearchPoint}"/> which checks the number of iterations.
    /// </summary>
    public class MaxIterations : ITerminationCriterion
    {
        #region Fields

        /// <summary>
        /// The maximum number of generations.
        /// </summary>
        private readonly int _maximum;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MaxIterations"/> class.
        /// </summary>
        /// <param name="maximum">The maximum number of generations, at least 1.</param>
        public MaxIterations(int maximum)
        {
            if (maximum < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximum),
                    $"CMA-ES needs at least 1 generation, but was provided with a maximum of {maximum}.");
            }

            this._maximum = maximum;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks whether the current generation should be the last one.
        /// </summary>
        /// <param name="data">Internal <see cref="CmaEs{TSearchPoint}"/> data.</param>
        /// <returns>Whether the generation was the <see cref="_maximum"/>th one.</returns>
        public bool IsMet(CmaEsElements data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return data.Generation >= this._maximum;
        }

        /// <summary>
        /// Restores an object after deserialization by <see cref="StatusBase"/>.
        /// </summary>
        /// <remarks>For example, this method might set equality definitions in dictionaries.</remarks>
        /// <returns>The object in restored state.</returns>
        public ITerminationCriterion Restore()
        {
            // No complex internal state --> nothing to do here.
            return this;
        }

        #endregion
    }
}