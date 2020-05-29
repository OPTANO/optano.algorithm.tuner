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

namespace Optano.Algorithm.Tuner.Tests.MachineLearning.RandomForest.RandomForestTopPerformerFocus
{
    using System;

    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData.SamplingStrategies;

    using SharpLearning.Containers.Views;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="GenomePredictionRandomForest{TSamplingStrategy}"/>.
    /// </summary>
    public class GenomePredictionRandomForestTests : TestBase
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForest{ReuseOldTreesStrategy}"/>s throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a negative value for features per split.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForNegativeFeaturesPerSplit()
        {
            var forestConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder().SetTreeCount(1)
                .SetRunParallel(false).BuildWithFallback(null);
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenomePredictionRandomForest<ReuseOldTreesStrategy>(forestConfig, -1));
        }

        /// <summary>
        /// Checks that <see cref="GenomePredictionRandomForest{ReuseOldTreesStrategy}"/> throws a
        /// <see cref="NullReferenceException"/> if learning is called without any observations.
        /// </summary>
        [Fact]
        public void TestMatrixShouldNotBeNull()
        {
            var forestConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder().SetTreeCount(1)
                .SetRunParallel(false).BuildWithFallback(null);
            var forest = new GenomePredictionRandomForest<ReuseOldTreesStrategy>(forestConfig, 0);
            Assert.Throws<NullReferenceException>(() => forest.Learn(null, new double[0], new int[0]));
        }

        /// <summary>
        /// Checks that <see cref="TopPerformerFocusImpurityCalculator"/> works correctly.
        /// </summary>
        [Fact]
        public void TestImpurityCalculator()
        {
            var impurityCalculator = new TopPerformerFocusImpurityCalculator(1d / 3);

            var targets = new double[] { 3, 1, 5, 2, 6, 4 };
            var uniques = new double[0];
            var weights = new double[0];
            var interval = new Interval1D(0, targets.Length);

            impurityCalculator.Init(uniques, targets, weights, interval);
            var baseImpurity = impurityCalculator.NodeImpurity();
            // computed 3/31 by hand.
            Assert.True(Math.Abs(baseImpurity - (3d / 31)) < 1e-6, $"Expected: {3d / 31} - Actual: {baseImpurity}");

            // set index to 2, do not skip any
            impurityCalculator.UpdateIndex(1);
            impurityCalculator.UpdateIndex(2);
            var childImps = impurityCalculator.ChildImpurities();
            // expected values computed by hand on a sheet of paper
            Assert.True(Math.Abs(childImps.Left - 1) < 1e-6, $"Expected: {1} - Actual: {childImps.Left}");
            Assert.True(Math.Abs(childImps.Right - (1d / 30)) < 1e-6, $"Expected: {1d / 30} - Actual: {childImps.Right}");

            var improvement = impurityCalculator.ImpurityImprovement(baseImpurity);
            Assert.Equal((1d / 30) - (3d / 31), improvement, 6);
        }

        #endregion
    }
}