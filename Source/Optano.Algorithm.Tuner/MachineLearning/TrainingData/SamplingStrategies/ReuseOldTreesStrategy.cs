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

namespace Optano.Algorithm.Tuner.MachineLearning.TrainingData.SamplingStrategies
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results;
    using Optano.Algorithm.Tuner.MachineLearning.Prediction;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;

    /// <summary>
    /// The <see cref="AggregateTargets"/> method only selects the newest tournament results as training data.
    /// In order to also make use of historical data, <see cref="ReuseOldTreesStrategy"/> selects <see cref="KeepOldTreeRatio"/> * nTrees many <see cref="IEnsemblePredictor{TWeakPredictor}.InternalModels"/> (at random) and replaces them with (randomly chosen) models from the previous generation's <see cref="IEnsemblePredictor{TWeakPredictor}"/>.
    /// </summary>
    public class ReuseOldTreesStrategy : IEnsembleSamplingStrategy<IGenomePredictor>
    {
        #region Public properties

        /// <summary>
        /// Gets the proportion of trees to replace with old trees.
        /// </summary>
        public double KeepOldTreeRatio { get; private set; }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="IEnsemblePredictor{TWeakPredictor}"/> from the previous iteration.
        /// </summary>
        protected IEnsemblePredictor<IGenomePredictor> OldEnsemble { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Optional: <see cref="GenomePredictionRandomForest{TSamplingStrategy}"/> passes the <c>params args</c> from its ctor to <see cref="Initialize"/> when the <see cref="ISamplingStrategy"/> is initialized.
        /// </summary>
        /// <param name="args">args[0]: Expected to contain the <see cref="KeepOldTreeRatio"/>.</param>
        public void Initialize(params object[] args)
        {
            if (args == null || args.Length < 1)
            {
                this.KeepOldTreeRatio = .3;
                return;
            }

            if (args[0] is double)
            {
                this.KeepOldTreeRatio = (double)args[0];
            }

            // TODO MAYBE #31822: Find a nice way to pass and handle these parameters!
        }

        /// <summary>
        /// This method only selects the newest tournament results as training data.
        /// </summary>
        /// <param name="data">All historical training data.</param>
        /// <returns>Only the training data where <see cref="TrainingDataWrapper.CurrentGeneration"/> == <see cref="GenomeTournamentRank.GenerationId"/>.</returns>
        public AggregatedTrainingDataWrapper AggregateTargets(TrainingDataWrapper data)
        {
            // we just want to train on the newest results
            // assumption: list of known results for genome g is ordered
            // we need to be able to handle duplicates, since genomes are not neccessarily unique
            var filteredObservations = data.Genomes
                .Select(g => data.TournamentResults[g].Where(t => t.GenerationId == data.CurrentGeneration).ToList())
                .ToArray();

            var relevantCount = filteredObservations.Sum(r => r.Count);
            var relevantIndices = new int[relevantCount];
            var relevantTargets = new double[relevantCount];

            var currentRelevantIndex = 0;
            for (var i = 0; i < filteredObservations.Length; i++)
            {
                // filteredObservations[i].Count can be 0.
                for (var repeat = 0; repeat < filteredObservations[i].Count; repeat++)
                {
                    relevantIndices[currentRelevantIndex] = i;
                    relevantTargets[currentRelevantIndex] = filteredObservations[i][repeat].TournamentRank;
                    currentRelevantIndex++;
                }
            }

            // this should match
            Debug.Assert(currentRelevantIndex == relevantCount, "Each feature column should have been handled.");
            var relevantConvertedGenomes = data.ConvertedGenomes.Rows(relevantIndices);

            var result = new AggregatedTrainingDataWrapper()
                             {
                                 RelevantConvertedGenomes = relevantConvertedGenomes,
                                 RelevantTargets = relevantTargets,
                             };
            return result;
        }

        /// <summary>
        /// In order to also make use of historical data, <see cref="ReuseOldTreesStrategy"/> selects <see cref="KeepOldTreeRatio"/> * nTrees many <see cref="IEnsemblePredictor{TWeakPredictor}.InternalModels"/> (at random) and replaces them with (randomly chosen) models from the previous generation's <see cref="IEnsemblePredictor{TWeakPredictor}"/>.
        /// In the first iteration, this method only sets <see cref="OldEnsemble"/> to <paramref name="newModel"/>.
        /// </summary>
        /// <param name="newModel">The freshly trained predictor model, using data from <see cref="AggregateTargets"/>.</param>
        public void PostProcessModel(IEnsemblePredictor<IGenomePredictor> newModel)
        {
            // no old weak learners to reuse in 1st iteration
            if (this.OldEnsemble == null)
            {
                this.OldEnsemble = newModel;
                return;
            }

            // our RF stores the internal models as list. c# will not treat this as enumerable, but internally cast instead
            var numberToReuse = (int)Math.Ceiling(this.OldEnsemble.InternalModels.Length * this.KeepOldTreeRatio);
            var reusedTrees = Randomizer.Instance.ChooseRandomSubset(this.OldEnsemble.InternalModels, numberToReuse).ToArray();
            var treeIndexToReplace = Randomizer.Instance.ChooseRandomSubset(Enumerable.Range(0, newModel.InternalModels.Length), numberToReuse)
                .ToArray();

            // replace trees
            for (var replaceIndex = 0; replaceIndex < numberToReuse; replaceIndex++)
            {
                newModel.InternalModels[treeIndexToReplace[replaceIndex]] = reusedTrees[replaceIndex];
            }

            // update previous ensemble
            this.OldEnsemble = newModel;
        }

        #endregion
    }
}