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

namespace Optano.Algorithm.Tuner.GrayBox.GrayBoxSimulation
{
    using System;

    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// A pair of results, used in the <see cref="GrayBoxSimulation{TTargetAlgorithm, TInstance, TResult}"/>.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class GrayBoxSimulationResultPair<TResult>
        where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GrayBoxSimulationResultPair{TResult}"/> class.
        /// </summary>
        /// <param name="generationID">The generation id.</param>
        /// <param name="blackBoxResult">The black box result.</param>
        /// <param name="grayBoxResult">The gray box result.</param>
        public GrayBoxSimulationResultPair(int generationID, TResult blackBoxResult, TResult grayBoxResult)
        {
            this.GenerationID = generationID;
            this.BlackBoxResult = blackBoxResult;
            this.GrayBoxResult = grayBoxResult;
            this.RuntimeUntilGrayBoxCancellation = grayBoxResult.Runtime;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the generation id.
        /// </summary>
        public int GenerationID { get; }

        /// <summary>
        /// Gets the black box result.
        /// </summary>
        public TResult BlackBoxResult { get; }

        /// <summary>
        /// Gets or sets the gray box result.
        /// </summary>
        public TResult GrayBoxResult { get; set; }

        /// <summary>
        /// Gets or sets the runtime until the gray box cancellation.
        /// </summary>
        public TimeSpan RuntimeUntilGrayBoxCancellation { get; set; }

        #endregion
    }
}