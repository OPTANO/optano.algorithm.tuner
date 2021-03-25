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

namespace Optano.Algorithm.Tuner.DistributedExecution
{
    using System;

    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestOutOfBox;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData.SamplingStrategies;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// The Master aka cluster seed node. Using default generic parameterization for model-based crossover operator.
    /// </summary>
    /// <typeparam name="TTargetAlgorithm">
    /// The algorithm that should be tuned.
    /// </typeparam>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    public sealed class Master<TTargetAlgorithm, TInstance, TResult> : Master<TTargetAlgorithm, TInstance, TResult,
        StandardRandomForestLearner<ReuseOldTreesStrategy>, GenomePredictionForestModel<GenomePredictionTree>, ReuseOldTreesStrategy>
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult> where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Prevents a default instance of the <see cref="Master{TTargetAlgorithm, TInstance, TResult}"/> class from being created.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// This class should never be initialized. Since there is no static inheritance, a dummy _ctor is required.
        /// </exception>
        private Master()
        {
            throw new InvalidOperationException("This class should never be initialized.");
        }

        #endregion
    }
}