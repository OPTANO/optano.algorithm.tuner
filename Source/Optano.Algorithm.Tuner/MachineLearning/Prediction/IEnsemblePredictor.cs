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

namespace Optano.Algorithm.Tuner.MachineLearning.Prediction
{
    using Optano.Algorithm.Tuner.Genomes;

    /// <summary>
    /// Extends the <see cref="IGenomePredictor"/> interface and provides access to the ensemble's internal <typeparamref name="TWeakPredictor"/>s.
    /// </summary>
    /// <typeparam name="TWeakPredictor">
    /// The ensembles internal weak predictor type.
    /// </typeparam>
    public interface IEnsemblePredictor<out TWeakPredictor> : IGenomePredictor
        where TWeakPredictor : IGenomePredictor
    {
        #region Public properties

        /// <summary>
        /// Gets <c>modifiable</c> access to the ensembles internal weak predictors.
        /// Modifiable includes that single internal <typeparamref name="TWeakPredictor"/>s might be <c>replaced</c> with different models.
        /// </summary>
        TWeakPredictor[] InternalModels { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Predicts a single <see cref="Genome"/>'s target performance. Does not compute the average over predictions of all Trees, but instead the individual values.
        /// </summary>
        /// <param name="genome">
        /// The converted <see cref="T:double[]"/>representation of the <see cref="Genome"/> to predict.
        /// </param>
        /// <returns>
        /// Each tree's predicted performance for the <paramref name="genome"/>.
        /// </returns>
        double[] PredictIndividualTreeValues(double[] genome);

        #endregion
    }
}