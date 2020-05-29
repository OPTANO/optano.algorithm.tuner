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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.InstanceSelection.Messages
{
    using System;

    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="InstanceUpdateFinished"/> class.
    /// </summary>
    public class InstanceUpdateFinishedTest
    {
        #region Fields

        /// <summary>
        /// The number of expected instances.
        /// </summary>
        private readonly int _expectedInstanceCount = 230;

        /// <summary>
        /// <see cref="InstanceUpdateFinished"/> to use in tests. Needs to be initialized.
        /// </summary>
        private readonly InstanceUpdateFinished _finishedMessage;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceUpdateFinishedTest"/> class.
        /// </summary>
        public InstanceUpdateFinishedTest()
        {
            this._finishedMessage = new InstanceUpdateFinished(this._expectedInstanceCount);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="InstanceUpdateFinished"/>'s constructor with a
        /// negative number of expected instances throws a <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnNegativeExpectedInstanceCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new InstanceUpdateFinished(expectedInstanceCount: -1));
        }

        /// <summary>
        /// Checks that <see cref="InstanceUpdateFinished.ExpectedInstanceCount"/> returns the same number
        /// it was initialized with.
        /// </summary>
        [Fact]
        public void ExpectedInstanceCountIsSetCorrectly()
        {
            Assert.Equal(
                this._expectedInstanceCount,
                this._finishedMessage.ExpectedInstanceCount);
        }

        #endregion
    }
}