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

namespace Optano.Algorithm.Tuner.MachineLearning
{
    using Optano.Algorithm.Tuner.MachineLearning.Prediction;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData;

    /// <summary>
    /// Interface to start the training of an <see cref="IGenomeLearner{TGenomePredictor, TSamplingStrategy}"/>.
    /// </summary>
    /// <typeparam name="TGenomePredictor">
    /// Type of the trained genome predictor.
    /// </typeparam>
    /// <typeparam name="TSamplingStrategy">
    /// Type of the sampling strategy used for aggregating training data.
    /// </typeparam>
    public interface IGenomeLearner<out TGenomePredictor, in TSamplingStrategy>
        where TGenomePredictor : IGenomePredictor where TSamplingStrategy : ISamplingStrategy, new()
    {
        #region Public Methods and Operators

        /// <summary>
        /// Trains the <see cref="IGenomeLearner{TGenomePredictor, TSamplingStrategy}"/>, using the given <paramref name="data"/>.
        /// </summary>
        /// <param name="data">
        /// The training data.
        /// </param>
        /// <returns>
        /// A trained model of type <typeparamref name="TGenomePredictor"/>.
        /// </returns>
        TGenomePredictor Learn(TrainingDataWrapper data);

        #endregion
    }
}