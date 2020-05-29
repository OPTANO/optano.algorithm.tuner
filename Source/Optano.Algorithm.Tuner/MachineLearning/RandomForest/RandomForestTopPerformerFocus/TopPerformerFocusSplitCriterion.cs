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

namespace Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus
{
    using System;
    using System.Linq;

    using SharpLearning.Containers.Extensions;
    using SharpLearning.Containers.Views;
    using SharpLearning.DecisionTrees.SplitSearchers;

    /// <summary>
    /// The top performer focus split criterion.
    /// </summary>
    public class TopPerformerFocusSplitCriterion : ISplitSearcher<ITopPerformerFocusImpurityCalculator>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TopPerformerFocusSplitCriterion"/> class.
        /// </summary>
        /// <param name="minimumSplitSize">
        /// The minimum split size.
        /// </param>
        public TopPerformerFocusSplitCriterion(int minimumSplitSize)
            : this(minimumSplitSize, 0d)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TopPerformerFocusSplitCriterion"/> class.
        /// </summary>
        /// <param name="minimumSplitSize">
        /// The minimum split size.
        /// </param>
        /// <param name="minimumLeafWeight">
        /// The minimum leaf weight.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="minimumSplitSize"/> must be greater than 0.
        /// </exception>
        public TopPerformerFocusSplitCriterion(int minimumSplitSize, double minimumLeafWeight)
        {
            if (minimumSplitSize <= 0)
            {
                throw new ArgumentException("minimum split size must be larger than 0", nameof(minimumSplitSize));
            }

            this.MinimumSplitSize = minimumSplitSize;
            this.MinimumLeafWeight = minimumLeafWeight;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the minimum split size.
        /// </summary>
        private int MinimumSplitSize { get; set; }

        /// <summary>
        /// Gets or sets the minimum leaf weight.
        /// </summary>
        private double MinimumLeafWeight { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Find the best split.
        /// </summary>
        /// <param name="impurityCalculator">
        /// The impurity calculator.
        /// </param>
        /// <param name="feature">
        /// The feature.
        /// </param>
        /// <param name="targets">
        /// The targets.
        /// </param>
        /// <param name="parentInterval">
        /// The parent interval.
        /// </param>
        /// <param name="parentImpurity">
        /// The parent impurity.
        /// </param>
        /// <returns>
        /// The <see cref="SplitResult"/>.
        /// </returns>
        public SplitResult FindBestSplit(
            ITopPerformerFocusImpurityCalculator impurityCalculator,
            double[] feature,
            double[] targets,
            Interval1D parentInterval,
            double parentImpurity)
        {
            var bestKnownSplit = new SplitResult(-1, 0, 0, 0, 0);

            var prevSplitStartIndex = parentInterval.FromInclusive;
            var previousFeatureValue = feature[prevSplitStartIndex];

            impurityCalculator.UpdateIntervalAndTargets(parentInterval, targets);

            for (var currentSplitIndex = prevSplitStartIndex + this.MinimumSplitSize;
                 currentSplitIndex <= parentInterval.ToExclusive - this.MinimumSplitSize;
                 currentSplitIndex++)
            {
                var currentFeatureValue = feature[currentSplitIndex];
                if (Math.Abs(previousFeatureValue - currentFeatureValue) > 1e-10)
                {
                    impurityCalculator.UpdateIndex(currentSplitIndex);

                    if (impurityCalculator.WeightedLeft < this.MinimumLeafWeight
                        || impurityCalculator.WeightedRight < this.MinimumLeafWeight)
                    {
                        continue;
                    }

                    var currentImprovement = impurityCalculator.ImpurityImprovement(parentImpurity);

                    // check if the split is better than current best
                    if (currentImprovement > bestKnownSplit.ImpurityImprovement)
                    {
                        var childImpurities = impurityCalculator.ChildImpurities();
                        var bestThreshold = (currentFeatureValue + previousFeatureValue) * 0.5;

                        bestKnownSplit = new SplitResult(
                            currentSplitIndex,
                            bestThreshold,
                            currentImprovement,
                            childImpurities.Left,
                            childImpurities.Right);
                    }
                }

                previousFeatureValue = currentFeatureValue;
            }

            return bestKnownSplit;
        }

        #endregion
    }
}