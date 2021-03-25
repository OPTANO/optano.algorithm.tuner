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

namespace Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus
{
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.MachineLearning.Prediction;

    using SharpLearning.Common.Interfaces;
    using SharpLearning.Containers.Matrices;

    /// <summary>
    /// The genome prediction forest model.
    /// </summary>
    /// <typeparam name="TWeakPredictor">
    /// Type of the weak predictor used by this ensemble.
    /// </typeparam>
    public class GenomePredictionForestModel<TWeakPredictor> : IEnsemblePredictor<TWeakPredictor>, IPredictorModel<double>
        where TWeakPredictor : IWeakPredictor
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomePredictionForestModel{TWeakPredictor}"/> class.
        /// </summary>
        /// <param name="models">
        /// The weak predictor models.
        /// </param>
        /// <param name="rawVariableImportance">
        /// The raw variable importance.
        /// Not used for this model.
        /// </param>
        public GenomePredictionForestModel(IEnumerable<TWeakPredictor> models, double[] rawVariableImportance)
        {
            this.InternalModels = models.ToArray();
            if (rawVariableImportance == null)
            {
                rawVariableImportance = new double[0];
            }

            this.RawVariableImportance = rawVariableImportance;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets the internal models.
        /// </summary>
        public TWeakPredictor[] InternalModels { get; protected set; }

        #endregion

        #region Explicit Interface properties

        /// <summary>
        /// Gets the internal models.
        /// </summary>
        TWeakPredictor[] IEnsemblePredictor<TWeakPredictor>.InternalModels
        {
            get
            {
                return this.InternalModels;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the raw variable importance.
        /// </summary>
        protected double[] RawVariableImportance { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Predict the score for a single genome.
        /// </summary>
        /// <param name="genome">
        /// The genome.
        /// </param>
        /// <returns>
        /// The <see cref="double"/> score.
        /// </returns>
        public double Predict(double[] genome)
        {
            return this.InternalModels.Average(m => m.Predict(genome));
        }

        /// <summary>
        /// Predict score with individual trees.
        /// </summary>
        /// <param name="genome">
        /// The genome.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/> with the score that was predicted by each individual <typeparamref name="TWeakPredictor"/>.
        /// </returns>
        public double[] PredictIndividualTreeValues(double[] genome)
        {
            return this.InternalModels.Select(m => m.Predict(genome)).ToArray();
        }

        /// <summary>
        /// Predict a batch of genomes.
        /// </summary>
        /// <param name="genomes">
        /// The genomes.
        /// Row-wise.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/> score for each genome.
        /// </returns>
        public double[] Predict(F64Matrix genomes)
        {
            var indices = Enumerable.Range(0, genomes.RowCount).ToArray();
            return this.Predict(genomes, indices);
        }

        /// <summary>
        /// Batch-prediction for subset of the given genomes.
        /// </summary>
        /// <param name="genomes">
        /// The genomes.
        /// </param>
        /// <param name="indices">
        /// The row indices of genomes to predict for.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/> prediction for each given index.
        /// </returns>
        public double[] Predict(F64Matrix genomes, int[] indices)
        {
            var predictions = new double[indices.Length];

            for (var i = 0; i < indices.Length; i++)
            {
                var currentRow = indices[i];
                predictions[i] = this.Predict(genomes.Row(currentRow));
            }

            return predictions;
        }

        /// <summary>
        /// Get raw variable importance.
        /// </summary>
        /// <returns>
        /// The <see cref="T:double[]"/>.
        /// </returns>
        public double[] GetRawVariableImportance()
        {
            return this.RawVariableImportance;
        }

        /// <summary>
        /// Get variable importance.
        /// </summary>
        /// <param name="featureNameToIndex">
        /// The feature name to index.
        /// </param>
        /// <returns>
        /// The <see cref="Dictionary{String, Double}"/> that contains the importance for each given feature name.
        /// </returns>
        public Dictionary<string, double> GetVariableImportance(Dictionary<string, int> featureNameToIndex)
        {
            var max = this.RawVariableImportance.Max();

            var scaledVariableImportance = this.RawVariableImportance.Select(v => (v / max) * 100.0).ToArray();

            return featureNameToIndex.ToDictionary(kvp => kvp.Key, kvp => scaledVariableImportance[kvp.Value]).OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        #endregion
    }
}