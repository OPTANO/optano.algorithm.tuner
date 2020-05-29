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
    using SharpLearning.DecisionTrees.ImpurityCalculators;
    using SharpLearning.DecisionTrees.SplitSearchers;

    /// <summary>
    /// An impurity calculator that computes split scores according to https://wiwi.uni-paderborn.de/fileadmin/dep3ls7/Downloads/Publikationen/PDFs/IJCAI-15_1.pdf.
    /// </summary>
    public class TopPerformerFocusImpurityCalculator : ITopPerformerFocusImpurityCalculator
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TopPerformerFocusImpurityCalculator"/> class.
        /// </summary>
        public TopPerformerFocusImpurityCalculator()
            : this(.1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TopPerformerFocusImpurityCalculator"/> class.
        /// </summary>
        /// <param name="topThresholdPercentage">
        /// The percentage of genomes considered to be top performers.
        /// </param>
        public TopPerformerFocusImpurityCalculator(double topThresholdPercentage)
        {
            this.TopThresholdPercentage = topThresholdPercentage;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets a field which is not used for regression impurities.
        /// </summary>
        public double[] TargetNames => new double[0];

        /// <summary>
        /// Gets or sets the weight on of all elements with index &lt;= <see cref="CurrentPosition"/>.
        /// </summary>
        public double WeightedLeft { get; protected set; }

        /// <summary>
        /// Gets or sets the weight on of all elements with index &gt; <see cref="CurrentPosition"/>.
        /// </summary>
        public double WeightedRight { get; protected set; }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the percentage of genomes considered to be top performers.
        /// </summary>
        protected double TopThresholdPercentage { get; }

        /// <summary>
        /// Gets or sets an indicator if the current sample is considered to be a top performer.
        /// <see cref="IsTopPerformer"/>[i] is true, if genome i is considered to be a top performer (within the current interval).
        /// <c>NOTE:</c> <see cref="IsTopPerformer"/>.Length == <see cref="Interval"/>.Length. I.e. make sure to access this array with indices from 0 to Interval.Length - 1.
        /// <c>DO NOT USE</c> "var i = this.Interval.FromInclusive" <c>!!!</c>
        /// Can be used to sepearate the elements in the current node into Top and Under-performers.
        /// Elements are presumed to be ordered by the current feature, so that a split can be defined by a single split index.
        /// </summary>
        protected bool[] IsTopPerformer { get; set; }

        /// <summary>
        /// Gets or sets all target performances &lt;= <see cref="TopThresholdValue"/> are considered to be <c>Top Performance</c>.
        /// </summary>
        protected double TopThresholdValue { get; set; }

        /// <summary>
        /// Gets or sets the split position to compute KPIs for.
        /// </summary>
        protected int CurrentPosition { get; set; }

        /// <summary>
        /// Gets or sets the current interval to work on.
        /// </summary>
        protected Interval1D Interval { get; set; }

        /// <summary>
        /// Gets or sets the alpha value of the total interval.
        /// Updated in <see cref="Init"/>. Stores the impurity of this node, given that <c>no</c> further splits are performed.
        /// Larger values = <c>BETTER</c> score (i.e. <c>lower</c> Impurity!).
        /// </summary>
        protected double AlphaTotalInterval { get; set; }

        /// <summary>
        /// Gets or sets the score for the left side of the current split
        /// Larger values = <c>BETTER</c> score (i.e. <c>lower</c> Impurity!).
        /// </summary>
        protected double AlphaLeft { get; set; }

        /// <summary>
        /// Gets or sets the score for the right side of the current split
        /// Larger values = <c>BETTER</c> score (i.e. <c>lower</c> Impurity!).
        /// </summary>
        protected double AlphaRight { get; set; }

        /// <summary>
        /// Gets or sets the target performance, sorted together with index + feature-values.
        /// </summary>
        protected double[] Targets { get; set; }

        /// <summary>
        /// Gets or sets the weights for each sample.
        /// </summary>
        protected double[] Weights { get; set; }

        /// <summary>
        /// Gets or sets the total number of top performers in the current <see cref="Interval"/>.
        /// </summary>
        protected int TotalTopCount { get; set; }

        /// <summary>
        /// Gets or sets the count of "top performers" in the left side of the current split.
        /// </summary>
        protected int LeftTopCount { get; set; }

        /// <summary>
        /// Gets or sets the count of "top performers" in the right side of the current split.
        /// </summary>
        protected int RightTopCount { get; set; }

        /// <summary>
        /// Gets or sets the total sum of all <see cref="Weights"/> in the current <see cref="Interval"/>.
        /// </summary>
        protected double TotalWeight { get; set; }

        /// <summary>
        /// Gets or sets the weighted sum of target performances in the current <see cref="Interval"/>.
        /// </summary>
        protected double WeightedSumTotalInterval { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Child impurities are not supported.
        /// </summary>
        /// <returns>
        /// The <see cref="ChildImpurities"/>.
        /// </returns>
        public ChildImpurities ChildImpurities()
        {
            return new ChildImpurities(this.AlphaLeft, this.AlphaRight);
        }

        /// <summary>
        /// The actual score improvement.
        /// </summary>
        /// <param name="parentImpurity">
        /// Impurity before computing the current split.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public double ImpurityImprovement(double parentImpurity)
        {
            var improvement = 0d;
            if (this.LeftTopCount > this.RightTopCount)
            {
                improvement = this.AlphaLeft - this.AlphaTotalInterval;
            }
            else if (this.RightTopCount > this.LeftTopCount)
            {
                improvement = this.AlphaRight - this.AlphaTotalInterval;
            }
            else
            {
                improvement = Math.Min(this.AlphaLeft, this.AlphaRight) - this.AlphaTotalInterval;
            }

            return improvement;
        }

        /// <summary>
        /// Refreshes the internal data structures and pre-computed values, according to the given remainder-interval.
        /// </summary>
        /// <param name="uniqueTargets">
        /// Ignored for regression.
        /// </param>
        /// <param name="targets">
        /// Target values, ordered by respective feature-values for the current feature that is evaluated. Ordering needs to be performed in <see cref="ISplitSearcher{TImpurityCalculator}"/> before different splits can be tested.
        /// </param>
        /// <param name="weights">
        /// The weight for each sample.
        /// </param>
        /// <param name="interval">
        /// The interval to work on.
        /// </param>
        public void Init(double[] uniqueTargets, double[] targets, double[] weights, Interval1D interval)
        {
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }

            if (weights.Length == 0)
            {
                weights = Enumerable.Repeat(1d, targets.Length).ToArray();
            }

            if (weights.Length != targets.Length)
            {
                throw new ArgumentException("weights.Length should be 0 or match targets.Length.");
            }

            this.Targets = targets;
            this.Weights = weights;
            this.Interval = interval;

            // compute the current prediction value (i.e. weighted average)
            this.WeightedSumTotalInterval = 0;
            for (var i = this.Interval.FromInclusive; i < this.Interval.ToExclusive; i++)
            {
                var weightedTarget = weights[i] * targets[i];
                this.WeightedSumTotalInterval += weightedTarget;
            }

            this.UpdateIsTopPerformer();

            this.TotalTopCount = this.ComputeTotalTopCount();
            this.AlphaTotalInterval = this.ComputeAlpha(this.Interval.FromInclusive, this.Interval.ToExclusive - 1, this.TotalTopCount);

            this.TotalWeight = this.Weights.Sum();
            this.Reset();
        }

        /// <summary>
        /// Gets the "label" for a leaf. For regression, this is the weighted average of all <see cref="Targets"/> in the node.
        /// Only defined when no no valid split was found so that a node will become a leaf.
        /// </summary>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public double LeafValue()
        {
            return this.WeightedSumTotalInterval / this.TotalWeight;
        }

        /// <summary>
        /// <c>IMPORTANT</c> Instead of computing the <c>Impurity</c>, this <see cref="IImpurityCalculator"/> returns the score according to the implemented top-performance split method.
        /// </summary>
        /// <returns>
        /// The impurity of this node, given that <c>no</c> further splits are performed.
        /// Note that <c>threhold performance</c> only depends on the current elements in <c>this</c> node, and not on that of the parent's threshold!.
        /// </returns>
        public double NodeImpurity()
        {
            return this.AlphaTotalInterval;
        }

        /// <summary>
        /// Resets the internal data fields.
        /// </summary>
        public void Reset()
        {
            this.CurrentPosition = this.Interval.FromInclusive;

            this.WeightedLeft = 0.0;
            this.WeightedRight = this.TotalWeight;

            this.AlphaLeft = 0;
            this.AlphaRight = this.AlphaTotalInterval;

            this.LeftTopCount = 0;
            this.RightTopCount = this.TotalTopCount;
        }

        /// <summary>
        /// Sets the indicator for the T and U sets, and the current top threshold performance.
        /// It is assumed that <paramref name="featureSortedTopPerformers"/> is sorted in the same way as
        /// ITreeBuilder.workFeature.
        /// </summary>
        /// <param name="featureSortedTopPerformers">
        /// The top performer indicator, sorted by current feature.
        /// </param>
        /// <param name="topThresholdValue">
        /// Threshold below which a sample is considered to be a top performer.
        /// </param>
        public void SetTopPerformers(bool[] featureSortedTopPerformers, double topThresholdValue)
        {
            this.IsTopPerformer = featureSortedTopPerformers;
            this.TopThresholdValue = topThresholdValue;
        }

        /// <summary>
        /// Sets the current index to <paramref name="newPosition"/>
        /// <c>AND</c> triggers the computation of KPIs.
        /// </summary>
        /// <param name="newPosition">
        /// The new split position. Needs to be within <see cref="Interval"/>.
        /// </param>
        public void UpdateIndex(int newPosition)
        {
            if (this.CurrentPosition >= newPosition)
            {
                throw new ArgumentException("New position: " + newPosition + " must be larger than current: " + this.CurrentPosition);
            }

            if (this.Interval.FromInclusive > newPosition || this.Interval.ToExclusive <= newPosition)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newPosition),
                    $"Position needs to be within the current Interval. Given value: {newPosition}. Interval.FromInclusive: {this.Interval.FromInclusive} - Interval.ToExclusive: {this.Interval.ToExclusive}.");
            }

            // if new position is a top performer, it will be in the left side of the split
            for (var i = this.CurrentPosition; i < newPosition; i++)
            {
                if (this.IsTopPerformer[i])
                {
                    this.LeftTopCount++;
                    this.RightTopCount--;
                }

                this.WeightedLeft += this.Weights[i];
                this.WeightedRight -= this.Weights[i];
            }

            this.CurrentPosition = newPosition;

            // compute alphas
            // split "on current position": left = everything from start and < current position.
            this.AlphaLeft = this.ComputeAlpha(this.Interval.FromInclusive, this.CurrentPosition - 1, this.LeftTopCount);
            this.AlphaRight = this.ComputeAlpha(this.CurrentPosition, this.Interval.ToExclusive - 1, this.RightTopCount);
        }

        /// <summary>
        /// Sets the new <paramref name="newInterval"/>.
        /// Refreshes the internal data structures, by makig a dummy call to <see cref="Init"/>.
        /// </summary>
        /// <param name="newInterval">
        /// The new interval.
        /// </param>
        public void UpdateInterval(Interval1D newInterval)
        {
            this.Init(new double[0], this.Targets, this.Weights, newInterval);
        }

        /// <inheritdoc />
        public void UpdateIntervalAndTargets(Interval1D newInterval, double[] targets)
        {
            this.Init(new double[0], targets, this.Weights, newInterval);
        }

        /// <summary>
        /// Not defined for regression.
        /// </summary>
        /// <returns>An empty array.</returns>
        public double[] LeafProbabilities()
        {
            return new double[0];
        }

        #endregion

        #region Methods

        /// <summary>
        /// Computes the alpha value for the given subset of elements.
        /// </summary>
        /// <param name="intervalStartInclusive">
        /// First index of the subset interval.
        /// </param>
        /// <param name="intervalEndInclusive">
        /// Last index of the subset interval.
        /// </param>
        /// <param name="topNodesInInterval">
        /// Number of top performing nodes in the interval.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        protected double ComputeAlpha(int intervalStartInclusive, int intervalEndInclusive, int topNodesInInterval)
        {
            var topScore = 0d;
            var underScore = 0d;

            for (var i = intervalStartInclusive; i <= intervalEndInclusive; i++)
            {
                var squaredError = Math.Pow(this.Targets[i] - this.TopThresholdValue, 2);
                if (this.IsTopPerformer[i])
                {
                    // "error" in a good way, i.e. "better than threshold".
                    topScore += squaredError;
                }
                else
                {
                    underScore += squaredError;
                }
            }

            return (topNodesInInterval + topScore) / (1 + underScore);
        }

        /// <summary>
        /// Compute total count of top performers in current <see cref="Interval"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/> count.
        /// </returns>
        private int ComputeTotalTopCount()
        {
            var topCount = 0;
            for (var i = this.Interval.FromInclusive; i < this.Interval.ToExclusive; i++)
            {
                if (this.IsTopPerformer[i])
                {
                    topCount++;
                }
            }

            return topCount;
        }

        /// <summary>
        /// Update is top performer indicator.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="Targets"/> mustn't be null.
        /// </exception>
        private void UpdateIsTopPerformer()
        {
            if (this.Targets == null)
            {
                throw new InvalidOperationException("Method can only be called after this.Targets is set.");
            }

            // TODO MAYBE #31822: Proportion of samples considered to be top performers!
            var topPerformerCount = (int)Math.Ceiling(this.Targets.Length * this.TopThresholdPercentage);

            this.TopThresholdValue = this.Targets.NthSmallestElement(topPerformerCount - 1);
            this.IsTopPerformer = this.Targets.Select(t => t <= this.TopThresholdValue).ToArray();
        }

        #endregion
    }
}
