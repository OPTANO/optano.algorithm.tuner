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

    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="ContinuousResult"/> class.
    /// </summary>
    public class ContinuousResultTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="ContinuousResult.Value"/> returns the value provided on construction.
        /// </summary>
        [Fact]
        public void ValueIsSetCorrectly()
        {
            var result = new ContinuousResult(value: 42, runtime: TimeSpan.FromMilliseconds(0));
            Assert.Equal(42, result.Value);
        }

        /// <summary>
        /// Checks that <see cref="ResultBase{TResultType}.Runtime"/> returns the runtime provided on construction.
        /// </summary>
        [Fact]
        public void RuntimeIsSetCorrectly()
        {
            var result = new ContinuousResult(value: 0, runtime: TimeSpan.FromMilliseconds(42));
            Assert.Equal(42, result.Runtime.TotalMilliseconds);
        }

        /// <summary>
        /// Checks that <see cref="ResultBase{TResultType}.IsCancelled"/> returns false by default.
        /// </summary>
        [Fact]
        public void IsCancelledIsFalseByDefault()
        {
            var result = new ContinuousResult(value: 42, runtime: TimeSpan.FromMilliseconds(0));
            Assert.False(result.IsCancelled, "Result is supposedly cancelled directly after construction.");
        }

        /// <summary>
        /// Checks that <see cref="ContinuousResult.ToString"/> returns a representation like
        /// 'Runtime: 42 ms, Value: 2.5' if <see cref="ResultBase{TResultType}.IsCancelled"/> is false.
        /// </summary>
        [Fact]
        public void ToStringIsCorrectForNonCancelledResult()
        {
            var result = new ContinuousResult(value: 2.5, runtime: TimeSpan.FromMilliseconds(42));
            Assert.Equal(
                FormattableString.Invariant($"Runtime: {TimeSpan.FromMilliseconds(42):G}, Value: 2.5"),
                result.ToString());
        }

        /// <summary>
        /// Checks that <see cref="ContinuousResult.ToString"/> returns a representation like
        /// 'Cancelled after 42 ms with value set to 2.5' for a cancelled result.
        /// </summary>
        [Fact]
        public void ToStringIsCorrectForCancelledResult()
        {
            var runtime = TimeSpan.FromMilliseconds(42);
            var result = ContinuousResult.CreateCancelledResult(runtime);
            Assert.Equal(
                FormattableString.Invariant($"Cancelled after {TimeSpan.FromMilliseconds(42):G} with value set to NaN"),
                result.ToString());
        }

        #endregion
    }
}