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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.Results
{
    using System;

    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="ResultBase{TResultType}"/> class.
    /// </summary>
    public class ResultBaseTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that the result yielded by <see cref="ResultBase{TResultType}.CreateCancelledResult(TimeSpan, TargetAlgorithmStatus)"/>
        /// returns true for <see cref="ResultBase{TResultType}.IsCancelled"/>.
        /// </summary>
        [Fact]
        public void CancelledResultIsMarkedAsCancelled()
        {
            var result = ResultBase<TestResult>.CreateCancelledResult(TimeSpan.Zero);
            Assert.True(result.IsCancelled, "Cancelled result not marked as cancelled.");
        }

        /// <summary>
        /// Checks that the result yielded by <see cref="ResultBase{TResultType}.CreateCancelledResult(TimeSpan, TargetAlgorithmStatus)"/>
        /// returns the runtime provided on construction when <see cref="ResultBase{TResultType}.Runtime"/> is
        /// requested.
        /// </summary>
        [Fact]
        public void CancelledResultReturnsCorrectRuntime()
        {
            var runtime = TimeSpan.FromMilliseconds(42);
            var result = ResultBase<TestResult>.CreateCancelledResult(runtime);
            Assert.Equal(runtime, result.Runtime);
        }

        /// <summary>
        /// Checks that <see cref="ResultBase{TResultType}.CreateCancelledResult(TimeSpan, TargetAlgorithmStatus)"/> throws an <see cref="ArgumentOutOfRangeException"/>, if the given target algorithm status is running or finished.
        /// </summary>
        /// <param name="targetAlgorithmStatus">The target algorithm status.</param>
        [Theory]
        [InlineData(TargetAlgorithmStatus.Running)]
        [InlineData(TargetAlgorithmStatus.Finished)]
        public void CreateCancelledResultThrowsIfTargetAlgorithmStatusIsRunningOrFinished(TargetAlgorithmStatus targetAlgorithmStatus)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => { ResultBase<TestResult>.CreateCancelledResult(TimeSpan.FromSeconds(30), targetAlgorithmStatus); });
        }

        #endregion
    }
}