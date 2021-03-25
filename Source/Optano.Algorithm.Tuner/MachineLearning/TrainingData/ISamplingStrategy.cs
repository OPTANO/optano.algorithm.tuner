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

namespace Optano.Algorithm.Tuner.MachineLearning.TrainingData
{
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;

    /// <summary>
    /// Strategy pattern to filter and aggregate the training data before it is passed to the machine learning model.
    /// </summary>
    public interface ISamplingStrategy
    {
        #region Public Methods and Operators

        /// <summary>
        /// Optional: <see cref="GenomePredictionRandomForest{TSamplingStrategy}"/> passes the <c>params args</c> from its ctor to <see cref="Initialize"/> when the <see cref="ISamplingStrategy"/> is initialized.
        /// </summary>
        /// <param name="args">Arguments to configure the strategy.</param>
        void Initialize(params object[] args);

        /// <summary>
        /// This method should filter the relevant training data and aggregate it so that the <see cref="AggregatedTrainingDataWrapper"/> contains only the relevant genomes and their respective performance.
        /// </summary>
        /// <param name="data">All observed tournament results combined with the generation and tournament.</param>
        /// <returns>All relevant training data.
        /// </returns>
        AggregatedTrainingDataWrapper AggregateTargets(TrainingDataWrapper data);

        #endregion
    }
}