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

    using SharpLearning.Containers.Matrices;

    /// <summary>
    /// Interface to access a predictor model.
    /// </summary>
    public interface IGenomePredictor
    {
        #region Public Methods and Operators

        /// <summary>
        /// Predicts a single <see cref="Genome"/>'s target performance.
        /// </summary>
        /// <param name="genome">The converted <see cref="T:double[]"/>representation of the <see cref="Genome"/> to predict.</param>
        /// <returns>The predicted performance.</returns>
        double Predict(double[] genome);

        /// <summary>
        /// Predicts all given <see cref="Genome"/>s' target performances.
        /// </summary>
        /// <param name="genomes">The converted <see cref="T:double[]"/>representations of the <see cref="Genome"/> to predict.</param>
        /// <returns>The predicted performance for every <see cref="Genome"/>.</returns>
        double[] Predict(F64Matrix genomes);

        /// <summary>
        /// Predicts the <see cref="Genome"/>s' target performances for the given <see cref="F64Matrix.Rows(int[])"/> that are specifed by <paramref name="indices"/>.
        /// </summary>
        /// <param name="genomes">The converted <see cref="T:double[]"/>representations of the <see cref="Genome"/> to predict.</param>
        /// <param name="indices">The indices to filter <paramref name="genomes"/> by.</param>
        /// <returns>The predicted performance for the <paramref name="indices"/>-filtered <paramref name="genomes"/>.</returns>
        double[] Predict(F64Matrix genomes, int[] indices);

        #endregion
    }
}