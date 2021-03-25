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
    /// Contains tests for the <see cref="RuntimeResult"/> class.
    /// </summary>
    public class RuntimeResultTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="ResultBase{TResultType}.Runtime"/> returns the value provided on construction.
        /// </summary>
        [Fact]
        public void RuntimeIsSetCorrectly()
        {
            var result = new RuntimeResult(TimeSpan.FromMilliseconds(42));
            Assert.Equal(42, result.Runtime.TotalMilliseconds);
        }

        /// <summary>
        /// Checks that <see cref="ResultBase{TResultType}.IsCancelled"/> returns false by default.
        /// </summary>
        [Fact]
        public void IsCancelledIsFalseByDefault()
        {
            var result = new RuntimeResult(TimeSpan.FromMilliseconds(42));
            Assert.False(result.IsCancelled, "Result is supposedly cancelled directly after construction.");
        }

        /// <summary>
        /// Checks that <see cref="ResultBase{TResultType}.ToString"/> returns a representation like '42 ms' if a 
        /// runtime was set.
        /// </summary>
        [Fact]
        public void ToStringIsCorrectForNonCancelledResult()
        {
            var result = new RuntimeResult(TimeSpan.FromMilliseconds(42));
            Assert.Equal(
                FormattableString.Invariant($"{TimeSpan.FromMilliseconds(42):G}"),
                result.ToString());
        }

        /// <summary>
        /// Checks that <see cref="ResultBase{TResultType}.ToString"/> returns a representation like 'Cancelled after 42 ms'
        /// for a cancelled result.
        /// </summary>
        [Fact]
        public void ToStringIsCorrectForCancelledResult()
        {
            var result = RuntimeResult.CreateCancelledResult(TimeSpan.FromMilliseconds(42));
            Assert.Equal(
                FormattableString.Invariant($"Cancelled after {TimeSpan.FromMilliseconds(42):G}"),
                result.ToString());
        }

        #endregion
    }
}