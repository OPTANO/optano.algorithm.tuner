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

namespace Optano.Algorithm.Tuner.GrayBox
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;

    using SharpLearning.Common.Interfaces;
    using SharpLearning.Containers;
    using SharpLearning.Containers.Matrices;
    using SharpLearning.DecisionTrees.Learners;
    using SharpLearning.DecisionTrees.Models;
    using SharpLearning.RandomForest.Models;

    /// <summary>
    /// An implementation of the balanced random forest for binary classification, based on https://statistics.berkeley.edu/sites/default/files/tech-reports/666.pdf.
    /// </summary>
    public sealed class BalancedBinaryClassificationRandomForestLearner : IIndexedLearner<double>,
                                                                          IIndexedLearner<ProbabilityPrediction>,
                                                                          ILearner<double>,
                                                                          ILearner<ProbabilityPrediction>

    {
        #region Fields

        /// <summary>
        /// The number of trees.
        /// </summary>
        private readonly int _numberOfTrees;

        /// <summary>
        /// The minimum split size.
        /// </summary>
        private readonly int _minimumSplitSize;

        /// <summary>
        /// The minimum information gain.
        /// </summary>
        private readonly double _minimumInformationGain;

        /// <summary>
        /// The sub sample ratio.
        /// </summary>
        private readonly double _subSampleRatio;

        /// <summary>
        /// The maximum tree depth.
        /// </summary>
        private readonly int _maximumTreeDepth;

        /// <summary>
        /// The random seed.
        /// </summary>
        private readonly Random _randomSeed;

        /// <summary>
        /// The number of threads to use in parallel.
        /// </summary>
        private readonly int _numberOfThreads;

        /// <summary>
        /// The features per split.
        /// Can be null.
        /// If null, ceiling(sqrt(#features)) features are used.
        /// </summary>
        private int? _featuresPerSplit;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BalancedBinaryClassificationRandomForestLearner"/> class.
        /// </summary>
        /// <param name="trees">The number of trees to use in the ensemble.</param>
        /// <param name="minimumSplitSize">The minimum size for a node to be split.</param>
        /// <param name="maximumTreeDepth">The maximal tree depth before a leaf is generated.</param>
        /// <param name="featuresPerSplit">The number of features used at each split in each tree. If null, ceiling(sqrt(#features)) features are used.</param>
        /// <param name="minimumInformationGain">The minimum improvement in information gain before a split is made.</param>
        /// <param name="subSampleRatio">The ratio of observations sampled with replacement for each tree: number of sampled observations per tree = 2 x (number of observations in the minority class) x (sub sample ratio).</param>
        /// <param name="seed">The seed for the random number generator.</param>
        /// <param name="numberOfThreads">The number of threads to use in parallel.</param>
        [SuppressMessage(
            "NDepend",
            "ND3101:DontUseSystemRandomForSecurityPurposes",
            Justification = "The generated random numbers are not used in security purposes. Used tool (SharpLearning) requires a random instance.")]
        public BalancedBinaryClassificationRandomForestLearner(
            int trees = 100,
            int minimumSplitSize = 1,
            int maximumTreeDepth = 2000,
            int? featuresPerSplit = null,
            double minimumInformationGain = .000001,
            double subSampleRatio = 1.0,
            int seed = 42,
            int numberOfThreads = 1)
        {
            if (trees < 1)
            {
                throw new ArgumentException("The number of trees must be larger or equal to 1.");
            }

            if (featuresPerSplit != null && featuresPerSplit <= 0)
            {
                throw new ArgumentException("The number of features per split must be larger than 0.");
            }

            if (minimumSplitSize <= 0)
            {
                throw new ArgumentException("The minimum split size must be larger than 0.");
            }

            if (maximumTreeDepth <= 0)
            {
                throw new ArgumentException("The maximum tree depth must be larger than 0.");
            }

            if (minimumInformationGain <= 0)
            {
                throw new ArgumentException("The minimum information gain must be larger than 0.");
            }

            if (subSampleRatio <= 0.0 || subSampleRatio > 1.0)
            {
                throw new ArgumentException("The sub sample ratio must be in (0,1].");
            }

            if (numberOfThreads < 1)
            {
                throw new ArgumentException("The number of threads must be larger or equal to 1.");
            }

            this._numberOfTrees = trees;
            this._minimumSplitSize = minimumSplitSize;
            this._maximumTreeDepth = maximumTreeDepth;
            this._featuresPerSplit = featuresPerSplit;
            this._minimumInformationGain = minimumInformationGain;
            this._subSampleRatio = subSampleRatio;
            this._numberOfThreads = numberOfThreads;
            this._randomSeed = new Random(seed);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Learns a classification random forest.
        /// </summary>
        /// <param name="observations">The observations.</param>
        /// <param name="targets">The targets.</param>
        /// <returns>The classification random forest.</returns>
        public ClassificationForestModel Learn(F64Matrix observations, double[] targets)
        {
            var indices = Enumerable.Range(0, targets.Length).ToArray();
            return this.Learn(observations, targets, indices);
        }

        /// <summary>
        /// Learns a classification random forest.
        /// </summary>
        /// <param name="observations">The observations.</param>
        /// <param name="targets">The targets.</param>
        /// <param name="indices">The indices.</param>
        /// <returns>The classification random forest.</returns>
        [SuppressMessage(
            "NDepend",
            "ND3101:DontUseSystemRandomForSecurityPurposes",
            Justification = "The generated random numbers are not used in security purposes. Used tool (SharpLearning) requires a random instance.")]
        public ClassificationForestModel Learn(F64Matrix observations, double[] targets, int[] indices)
        {
            if (this._featuresPerSplit == null)
            {
                var featuresPerSplit = (int)Math.Ceiling(Math.Sqrt(observations.ColumnCount));
                this._featuresPerSplit = featuresPerSplit <= 0 ? 1 : featuresPerSplit;
            }

            var results = new ConcurrentBag<ClassificationDecisionTreeModel>();

            if (this._numberOfThreads == 1)
            {
                for (var i = 0; i < this._numberOfTrees; i++)
                {
                    results.Add(this.CreateTree(observations, targets, indices, new Random(this._randomSeed.Next())));
                }
            }
            else
            {
                var workItems = Enumerable.Range(0, this._numberOfTrees).ToArray();
                var partitionOptions = new ParallelOptions { MaxDegreeOfParallelism = this._numberOfThreads };
                var rangePartitioner = Partitioner.Create(workItems, true);
                Parallel.ForEach(
                    rangePartitioner,
                    partitionOptions,
                    (work, loopState) => { results.Add(this.CreateTree(observations, targets, indices, new Random(this._randomSeed.Next()))); });
            }

            var models = results.ToArray();
            var rawVariableImportance = this.VariableImportance(models, observations.ColumnCount);

            return new ClassificationForestModel(models, rawVariableImportance);
        }

        /// <summary>
        /// Gets the raw variable importance.
        /// </summary>
        /// <param name="models">The models.</param>
        /// <param name="numberOfFeatures">The number of features.</param>
        /// <returns>The raw variable importance.</returns>
        public double[] VariableImportance(ClassificationDecisionTreeModel[] models, int numberOfFeatures)
        {
            var rawVariableImportance = new double[numberOfFeatures];

            foreach (var model in models)
            {
                var modelVariableImportance = model.GetRawVariableImportance();

                for (var j = 0; j < modelVariableImportance.Length; j++)
                {
                    rawVariableImportance[j] += modelVariableImportance[j];
                }
            }

            return rawVariableImportance;
        }

        #endregion

        #region Explicit Interface Methods

        /// <summary>
        /// Private explicit interface implementation for indexed learning.
        /// </summary>
        /// <param name="observations">The observations.</param>
        /// <param name="targets">The targets.</param>
        /// <param name="indices">The indices.</param>
        /// <returns>The predictor model.</returns>
        IPredictorModel<double> IIndexedLearner<double>.Learn(F64Matrix observations, double[] targets, int[] indices)
        {
            return this.Learn(observations, targets, indices);
        }

        /// <summary>
        /// Private explicit interface implementation for indexed probability learning.
        /// </summary>
        /// <param name="observations">The observations.</param>
        /// <param name="targets">The targets.</param>
        /// <param name="indices">The indices.</param>
        /// <returns>The predictor model.</returns>
        IPredictorModel<ProbabilityPrediction> IIndexedLearner<ProbabilityPrediction>.Learn(F64Matrix observations, double[] targets, int[] indices)
        {
            return this.Learn(observations, targets, indices);
        }

        /// <summary>
        /// Private explicit interface implementation for indexed learning.
        /// </summary>
        /// <param name="observations">The observations.</param>
        /// <param name="targets">The targets.</param>
        /// <returns>The predictor model.</returns>
        IPredictorModel<double> ILearner<double>.Learn(F64Matrix observations, double[] targets)
        {
            return this.Learn(observations, targets);
        }

        /// <summary>
        /// Private explicit interface implementation for indexed probability learning.
        /// </summary>
        /// <param name="observations">The observations.</param>
        /// <param name="targets">The targets.</param>
        /// <returns>The predictor model.</returns>
        IPredictorModel<ProbabilityPrediction> ILearner<ProbabilityPrediction>.Learn(F64Matrix observations, double[] targets)
        {
            return this.Learn(observations, targets);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a tree.
        /// </summary>
        /// <param name="observations">The observations.</param>
        /// <param name="targets">The targets.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="random">The random seed.</param>
        /// <returns>The tree.</returns>
        private ClassificationDecisionTreeModel CreateTree(F64Matrix observations, double[] targets, int[] indices, Random random)
        {
            var distinctClassLabels = targets.Distinct().ToArray();

            if (distinctClassLabels.Length != 2)
            {
                throw new NotSupportedException(
                    $"The BalancedBinaryClassificationRandomForestLearner does only support target arrays, containing exactly two classes, but the following target array was given.{Environment.NewLine}[{string.Join(";", targets.Select(x => $"{x:0.#}"))}]");
            }

            var firstClassIndices = indices.Where(i => targets[i].Equals(distinctClassLabels[0])).ToArray();
            var secondClassIndices = indices.Where(i => targets[i].Equals(distinctClassLabels[1])).ToArray();

            var minorityClassIndices = firstClassIndices.Length <= secondClassIndices.Length ? firstClassIndices : secondClassIndices;
            var majorityClassIndices = firstClassIndices.Length > secondClassIndices.Length ? firstClassIndices : secondClassIndices;

            var numberOfSamplesPerClass = (int)Math.Round(this._subSampleRatio * minorityClassIndices.Length);

            var treeIndices = new int[2 * numberOfSamplesPerClass];
            for (var j = 0; j < numberOfSamplesPerClass; j++)
            {
                treeIndices[j] = minorityClassIndices[random.Next(minorityClassIndices.Length)];
            }

            for (var j = numberOfSamplesPerClass; j < 2 * numberOfSamplesPerClass; j++)
            {
                treeIndices[j] = majorityClassIndices[random.Next(majorityClassIndices.Length)];
            }

            var learner = new ClassificationDecisionTreeLearner(
                this._maximumTreeDepth,
                this._featuresPerSplit ?? throw new ArgumentNullException(nameof(this._featuresPerSplit)),
                this._minimumInformationGain,
                random.Next(),
                this._minimumSplitSize);

            var model = learner.Learn(observations, targets, treeIndices);

            return model;
        }

        #endregion
    }
}