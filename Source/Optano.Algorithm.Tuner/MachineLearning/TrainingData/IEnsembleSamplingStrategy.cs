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

namespace Optano.Algorithm.Tuner.MachineLearning.TrainingData
{
    using Optano.Algorithm.Tuner.MachineLearning.Prediction;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData.SamplingStrategies;

    /// <summary>
    /// Extension of the <see cref="ISamplingStrategy"/> that enables postprocessing of newly trained ensemble models.
    /// E.g. some of the trees in a random forest can be modified/swapped out after the forest is trained.
    /// </summary>
    /// <typeparam name="TGenomePredictor">Type of the weak predictor to use within the ensemble.</typeparam>
    public interface IEnsembleSamplingStrategy<in TGenomePredictor> : ISamplingStrategy
        where TGenomePredictor : IGenomePredictor
    {
        #region Public Methods and Operators

        /// <summary>
        /// This method can be used to alter some of the properties of the <paramref name="newModel"/>.
        /// For example, the <see cref="ReuseOldTreesStrategy"/> replaces some of the <paramref name="newModel"/>'s internal weak predictors with weak predictors from previous generations in order to preserve some of the data history.
        /// </summary>
        /// <param name="newModel">A freshly trained <see cref="IEnsemblePredictor{TWeakPredictor}"/>.</param>
        void PostProcessModel(IEnsemblePredictor<TGenomePredictor> newModel);

        #endregion
    }
}