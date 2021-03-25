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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.ResultStorage.Messages
{
    using System;

    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="EvaluationStatistic"/> class.
    /// </summary>
    public class EvaluationStatisticTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="EvaluationStatistic"/>'s constructor with a negative configuration count
        /// throws a <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnNegativeConfigurationCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EvaluationStatistic(configurationCount: -1, totalEvaluationCount: 3));
        }

        /// <summary>
        /// Verifies that calling <see cref="EvaluationStatistic"/>'s constructor with a total evaluation count smaller
        /// than the configuration count throws a <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnEvaluationCountSmallerThanConfigurationCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EvaluationStatistic(configurationCount: 3, totalEvaluationCount: 2));
        }

        /// <summary>
        /// Checks that <see cref="EvaluationStatistic.TotalEvaluationCount"/> returns the number that was provided on
        /// initialization.
        /// </summary>
        [Fact]
        public void TotalEvaluationCountIsSetCorrectly()
        {
            var message = new EvaluationStatistic(configurationCount: 3, totalEvaluationCount: 45);
            Assert.Equal(45, message.TotalEvaluationCount);
        }

        /// <summary>
        /// Checks that <see cref="EvaluationStatistic.ConfigurationCount"/> returns the number that was provided on
        /// initialization.
        /// </summary>
        [Fact]
        public void ConfigurationCountIsSetCorrectly()
        {
            var message = new EvaluationStatistic(configurationCount: 3, totalEvaluationCount: 45);
            Assert.Equal(3, message.ConfigurationCount);
        }

        #endregion
    }
}