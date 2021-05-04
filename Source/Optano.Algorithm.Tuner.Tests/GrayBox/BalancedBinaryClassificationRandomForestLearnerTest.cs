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

namespace Optano.Algorithm.Tuner.Tests.GrayBox
{
    using System;

    using Optano.Algorithm.Tuner.GrayBox;

    using SharpLearning.Containers.Matrices;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="BalancedBinaryClassificationRandomForestLearner"/> class.
    /// </summary>
    public class BalancedBinaryClassificationRandomForestLearnerTest : IDisposable
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BalancedBinaryClassificationRandomForestLearnerTest"/> class.
        /// Behaves like the [TestInitialize] of MSTest framework.
        /// </summary>
        public BalancedBinaryClassificationRandomForestLearnerTest()
        {
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <summary>
        /// Checks, that <see cref="BalancedBinaryClassificationRandomForestLearner"/> does not throw an exception, if confronted with supported target arrays.
        /// </summary>
        /// <param name="firstLabel">The first label.</param>
        /// <param name="secondLabel">The second label.</param>
        [Theory]
        [InlineData(0.0, 1.0)]
        [InlineData(0.5, 1.5)]
        public void BalancedBinaryClassificationRandomForestLearnerDoesNotThrowForCorrectLabel(double firstLabel, double secondLabel)
        {
            var trainDataObservations = new F64Matrix(
                new[]
                    {
                        1.0,
                        2.0,
                        3.0,
                        4.0,
                    },
                2,
                2);
            var trainDataLabels = new[] { firstLabel, secondLabel };

            var learner = new BalancedBinaryClassificationRandomForestLearner(numberOfThreads: 1);
            var model = learner.Learn(trainDataObservations, trainDataLabels);
            model.Predict(trainDataObservations);
        }

        /// <summary>
        /// Checks, that <see cref="BalancedBinaryClassificationRandomForestLearner"/> throws a <see cref="NotSupportedException"/>, if confronted with not supported target arrays.
        /// </summary>
        /// <param name="firstLabel">The first label.</param>
        /// <param name="secondLabel">The second label.</param>
        /// <param name="thirdLabel">The third label.</param>
        /// <param name="fourthLabel">The fourth label.</param>
        [Theory]
        [InlineData(0.0, 0.0, 0.0, 0.0)]
        [InlineData(1.0, 2.0, 3.0, 4.0)]
        public void BalancedBinaryClassificationRandomForestLearnerThrowsForWrongLabel(
            double firstLabel,
            double secondLabel,
            double thirdLabel,
            double fourthLabel)
        {
            var trainDataObservations = new F64Matrix(
                new[]
                    {
                        1.0,
                        2.0,
                        3.0,
                        4.0,
                        5.0,
                        6.0,
                        7.0,
                        8.0,
                    },
                4,
                2);
            var trainDataLabels = new[] { firstLabel, secondLabel, thirdLabel, fourthLabel };

            var learner = new BalancedBinaryClassificationRandomForestLearner(numberOfThreads: 1);
            Assert.Throws<NotSupportedException>(() => learner.Learn(trainDataObservations, trainDataLabels));
        }

        /// <summary>
        /// Checks, that <see cref="BalancedBinaryClassificationRandomForestLearner"/> does not throw an exception, if confronted with NaN in observations.
        /// </summary>
        [Fact]
        public void BalancedBinaryClassificationRandomForestLearnerDoesNotThrowAtNanInObservations()
        {
            var trainDataObservations = new F64Matrix(
                new[]
                    {
                        1.0,
                        2.0,
                        double.NaN,
                        double.NaN,
                    },
                2,
                2);
            var trainDataLabels = new[] { 0.0, 1.0 };

            var learner = new BalancedBinaryClassificationRandomForestLearner(numberOfThreads: 1);
            var model = learner.Learn(trainDataObservations, trainDataLabels);
            model.Predict(trainDataObservations);
        }

        /// <summary>
        /// Checks, that <see cref="BalancedBinaryClassificationRandomForestLearner"/> throws an <see cref="ArgumentOutOfRangeException"/>, if confronted with NaN in labels.
        /// </summary>
        [Fact]
        public void BalancedBinaryClassificationRandomForestLearnerThrowsAtNanInLabels()
        {
            var trainDataObservations = new F64Matrix(
                new[]
                    {
                        1.0,
                        2.0,
                        3.0,
                        4.0,
                    },
                2,
                2);
            var trainDataLabels = new[] { 0.0, double.NaN };

            var learner = new BalancedBinaryClassificationRandomForestLearner(numberOfThreads: 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => learner.Learn(trainDataObservations, trainDataLabels));
        }

        #endregion
    }
}