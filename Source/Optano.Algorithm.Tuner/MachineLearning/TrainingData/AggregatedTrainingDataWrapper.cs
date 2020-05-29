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
    using SharpLearning.Containers.Matrices;

    /// <summary>
    /// The class wraps the relevant training data that is passed to the <see cref="IGenomeLearner{TGenomePredictor,TSamplingStrategy}"/>.
    /// </summary>
    public class AggregatedTrainingDataWrapper
    {
        #region Public properties

        /// <summary>
        /// Gets or sets the genomes that should be used to train the predictor model.
        /// </summary>
        public F64Matrix RelevantConvertedGenomes { get; set; }

        /// <summary>
        /// Gets or sets genome performance that should be learned by the predictor model.
        /// </summary>
        public double[] RelevantTargets { get; set; }

        #endregion
    }
}