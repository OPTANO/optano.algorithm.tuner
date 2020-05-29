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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages
{
    using System;

    /// <summary>
    /// Message indicating that an instance update was sent completely.
    /// All <see cref="AddInstances{TInstance}"/> messages should have been sent beforehand.
    /// </summary>
    public class InstanceUpdateFinished
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceUpdateFinished"/> class.
        /// </summary>
        /// <remarks>
        /// Internal because this should be used in combination with
        /// and <see cref="ClearInstances"/> and <see cref="AddInstances{TInstance}"/> only.
        /// </remarks>
        /// <param name="expectedInstanceCount">
        /// The number of instances sent via <see cref="AddInstances{TInstance}"/> messages.
        /// </param>
        internal InstanceUpdateFinished(int expectedInstanceCount)
        {
            if (expectedInstanceCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedInstanceCount),
                    $"The expected number of instances should not be negative, but was {expectedInstanceCount}.");
            }

            this.ExpectedInstanceCount = expectedInstanceCount;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the number of instances sent via <see cref="AddInstances{TInstance}"/> messages.
        /// </summary>
        public int ExpectedInstanceCount { get; }

        #endregion
    }
}