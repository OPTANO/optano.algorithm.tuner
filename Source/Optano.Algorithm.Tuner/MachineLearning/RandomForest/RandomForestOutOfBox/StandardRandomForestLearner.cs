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

namespace Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestOutOfBox
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using Optano.Algorithm.Tuner.MachineLearning.Prediction;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData;

    using SharpLearning.Containers.Matrices;
    using SharpLearning.DecisionTrees.ImpurityCalculators;
    using SharpLearning.DecisionTrees.Learners;
    using SharpLearning.DecisionTrees.SplitSearchers;
    using SharpLearning.DecisionTrees.TreeBuilders;
    using SharpLearning.RandomForest.Learners;

    /// <summary>
    /// The standard random forest learner.
    /// </summary>
    /// <typeparam name="TSamplingStrategy">
    /// The strategy used for aggregating training data.
    /// </typeparam>
    public class StandardRandomForestLearner<TSamplingStrategy> :
        GenericRandomizedForestBase<GenomePredictionForestModel<GenomePredictionTree>, GenomePredictionTree>,
        IGenomeLearner<GenomePredictionForestModel<GenomePredictionTree>, TSamplingStrategy>
        where TSamplingStrategy : IEnsembleSamplingStrategy<IGenomePredictor>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardRandomForestLearner{TSamplingStrategy}"/> class.
        /// </summary>
        /// <param name="config">
        /// The parsed <see cref="GenomePredictionRandomForestConfig"/>.
        /// </param>
        /// <param name="featuresPerSplit">
        /// The number of features that are used for the training of a tree.
        /// </param>
        public StandardRandomForestLearner(GenomePredictionRandomForestConfig config, int featuresPerSplit)
        {
            if (featuresPerSplit < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(featuresPerSplit),
                    $"Features per split needs to be non-negative, but was {featuresPerSplit}.");
            }

            this.InitializeGenericRandomForestBase(
                config.TreeCount,
                config.MinimumSplitSize,
                config.MaximumTreeDepth,
                featuresPerSplit,
                config.MinimumInformationGain,
                config.SubSampleRatio,
                // use non-user specified seed.
                Randomizer.Instance.Next(),
                config.RunParallel);

            // TODO MAYBE #31822: Find a way to get sampling strategy args!
            this.SamplingStrategy = new TSamplingStrategy();
            this.SamplingStrategy.Initialize(null);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the strategy that is used to aggregate and filter the <see cref="TrainingDataWrapper"/>, and to <see cref="IEnsembleSamplingStrategy{TGenomePredictor}.PostProcessModel"/> the predictor model.
        /// </summary>
        protected TSamplingStrategy SamplingStrategy { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// OPTANO Algorithm Tuner specific method to start the training of the <see cref="StandardRandomForestLearner{TSamplingStrategy}"/>.
        /// </summary>
        /// <param name="data">
        /// All historical tournament data.
        /// </param>
        /// <returns>
        /// A trained and post-processed <see cref="GenomePredictionForestModel{TWeakPredictor}"/>.
        /// </returns>
        public GenomePredictionForestModel<GenomePredictionTree> Learn(TrainingDataWrapper data)
        {
            var aggregatedData = this.SamplingStrategy.AggregateTargets(data);
            var forestModel = this.Learn(aggregatedData.RelevantConvertedGenomes, aggregatedData.RelevantTargets);
            this.SamplingStrategy.PostProcessModel(forestModel);

            return forestModel;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Method that makes the generically typed call to <see cref="GenericRandomizedForestBase{TForestModel,TTreeTypeModel}"/>, using the <see cref="LinearSplitSearcher{TImpurityCalculator}"/> and <see cref="TopPerformerFocusImpurityCalculator"/>.
        /// </summary>
        /// <param name="observations">
        /// The training data.
        /// </param>
        /// <param name="targets">
        /// The algorithm performance measures.
        /// </param>
        /// <param name="indices">
        /// The subset of <paramref name="observations"/> to use.
        /// </param>
        /// <returns>
        /// The <see cref="GenomePredictionTree"/>.
        /// </returns>
        [SuppressMessage(
            "StyleCop.CSharp.SpacingRules",
            "SA1014:OpeningGenericBracketsMustBeSpacedCorrectly",
            Justification = "Rule makes generic type arguments unreadable.")]
        [SuppressMessage(
            "StyleCop.CSharp.ReadabilityRules",
            "SA1110:OpeningParenthesisMustBeOnDeclarationLine",
            Justification = "10+ generic arguments for function... You really do not want them on a single line.")]
        [SuppressMessage(
            "NDepend",
            "ND3101:DontUseSystemRandomForSecurityPurposes",
            Justification = "The generated random numbers are not used in security purposes. Used tool (SharpLearning) requires a Random instance.")]
        protected override GenomePredictionTree CallCreateTree(F64Matrix observations, double[] targets, int[] indices)
        {
            var singleTree = this.CreateTree<
                GenericRegressionDecisionTreeLearner<
                    DepthFirstTreeBuilder<
                        GenomePredictionTree,
                        LinearSplitSearcher<RegressionImpurityCalculator>,
                        RegressionImpurityCalculator>,
                    GenomePredictionTree,
                    LinearSplitSearcher<RegressionImpurityCalculator>,
                    RegressionImpurityCalculator>,
                DepthFirstTreeBuilder<
                    GenomePredictionTree,
                    LinearSplitSearcher<RegressionImpurityCalculator>,
                    RegressionImpurityCalculator>,
                LinearSplitSearcher<RegressionImpurityCalculator>,
                RegressionImpurityCalculator>(
                observations,
                targets,
                indices,
                new Random(this.m_random.Next()),
                // passed as arguments to the <see cref="GenericRegressionDecisionTreeLearner"/> ctor
                this.m_maximumTreeDepth,
                this.m_featuresPrSplit,
                this.m_minimumInformationGain,
                this.m_random.Next(),
                this.m_minimumSplitSize);

            return singleTree;
        }

        #endregion
    }
}