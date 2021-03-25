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

namespace Optano.Algorithm.Tuner.Tests.MachineLearning.RandomForest
{
    using System.Linq;
    using System.Threading;

    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;

    using SharpLearning.Containers.Matrices;
    using SharpLearning.Containers.Views;
    using SharpLearning.DecisionTrees.SplitSearchers;
    using SharpLearning.Metrics.Regression;
    using SharpLearning.RandomForest.Learners;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Tests for the GGA++ split criterion.
    /// </summary>
    public class TopPerformerFocusTests
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks if SharpLearning sorts the arrays from its function arguments "in place" via references.
        /// This is a requirement for <see cref="ITopPerformerFocusImpurityCalculator"/> to work correctly.
        /// </summary>
        [Fact]
        public void TargetArrayIsSortedByReference()
        {
            // Build a forest learner with out test substitute classes.
            var forestLearner = new RegressionRandomForestLearner<TestSplitSearcher, TestImpurityCalculator>(runParallel: false);
            // Generate arbitrary training data.
            var matrix = this.GetFeatureMatrix();
            var targets = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();

            // TestSplitSearcher checks if TestImpurityCalculator.CurrentTargets is always "up to date".
            var model = forestLearner.Learn(matrix, targets);

            // just out of curiosity :)
            var predictions = model.Predict(matrix);
            var metric = new MeanSquaredErrorRegressionMetric();
            var error = metric.Error(targets, predictions);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Generates a 10 x 10 matrix filled with the numbers 0 .. 99.
        /// </summary>
        /// <returns>A matrix.</returns>
        private F64Matrix GetFeatureMatrix()
        {
            var flatMatrix = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
            var matrix = new F64Matrix(flatMatrix, 10, 10);
            return matrix;
        }

        #endregion

        /// <summary>
        /// Works like a <see cref="LinearSplitSearcher{TImpurityCalculator}"/>.
        /// Validates that <see cref="TestImpurityCalculator.CurrentTargets"/> always matches the <c>targets</c> parameter from the <see cref="FindBestSplit"/> method.
        /// </summary>
        private class TestSplitSearcher : ISplitSearcher<TestImpurityCalculator>
        {
            #region Fields

            /// <summary>
            /// The linear split searcher.
            /// </summary>
            private readonly LinearSplitSearcher<TestImpurityCalculator> _splitSearcher;

            /// <summary>
            ///  The call count.
            /// </summary>
            private int _callCount = 0;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="TestSplitSearcher"/> class.
            /// </summary>
            /// <param name="minSplitSize">The minimum split size.</param>
            public TestSplitSearcher(int minSplitSize)
            {
                this._splitSearcher = new LinearSplitSearcher<TestImpurityCalculator>(minSplitSize);
            }

            #endregion

            #region Public Methods and Operators

            /// <inheritdoc />
            public SplitResult FindBestSplit(
                TestImpurityCalculator impurityCalculator,
                double[] feature,
                double[] targets,
                Interval1D parentInterval,
                double parentImpurity)
            {
                Interlocked.Increment(ref this._callCount);
                this.CheckIfTargetsDoMatch(impurityCalculator, targets);
                return this._splitSearcher.FindBestSplit(
                    impurityCalculator,
                    feature,
                    targets,
                    parentInterval,
                    parentImpurity);
            }

            #endregion

            #region Methods

            /// <summary>
            /// Checks if <see cref="TestImpurityCalculator.CurrentTargets"/> is equal to <paramref name="targets"/>.
            /// </summary>
            /// <param name="impurityCalculator">The test impurity calculator.</param>
            /// <param name="targets">The targets parameter from <see cref="ISplitSearcher{TImpurityCalculator}.FindBestSplit"/>.</param>
            private void CheckIfTargetsDoMatch(TestImpurityCalculator impurityCalculator, double[] targets)
            {
                if (impurityCalculator.CurrentTargets == null)
                {
                    this._callCount.ShouldBeLessThanOrEqualTo(1);
                    return;
                }

                var arrayComparer = new DoubleArrayEqualityComparer();
                arrayComparer.Equals(impurityCalculator.CurrentTargets, targets).ShouldBe(true);
            }

            #endregion
        }

        /// <summary>
        /// Provides access to private <see cref="TopPerformerFocusImpurityCalculator.Targets"/> array.
        /// </summary>
        private class TestImpurityCalculator : TopPerformerFocusImpurityCalculator
        {
            #region Public properties

            /// <summary>
            /// Gets the current <see cref="TopPerformerFocusImpurityCalculator.Targets"/>.
            /// </summary>
            public double[] CurrentTargets => base.Targets;

            #endregion
        }
    }
}