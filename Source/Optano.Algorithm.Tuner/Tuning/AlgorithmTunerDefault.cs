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

namespace Optano.Algorithm.Tuner.Tuning
{
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestOutOfBox;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData.SamplingStrategies;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// Responsible for managing the overall tuning process of OPTANO Algorithm Tuner.
    /// </summary>
    /// <typeparam name="TTargetAlgorithm">
    /// Type of the target algorithm.
    /// Must implement <see cref="ITargetAlgorithm{TInstance,TResult}"/>.
    /// </typeparam>
    /// <typeparam name="TInstance">
    /// Type of instance the target algorithm is able to process.
    /// Must be a subtype of <see cref="InstanceBase"/>.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// Type of result of a target algorithm run.
    /// Must be a subtype of <see cref="ResultBase{TResultType}"/>.
    /// </typeparam>
    public class AlgorithmTuner<TTargetAlgorithm, TInstance, TResult> : AlgorithmTuner<TTargetAlgorithm, TInstance, TResult,
        StandardRandomForestLearner<ReuseOldTreesStrategy>, GenomePredictionForestModel<GenomePredictionTree>, ReuseOldTreesStrategy>
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult> where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> class.
        /// </summary>
        /// <param name="targetAlgorithmFactory">
        /// The target algorithm factory.
        /// </param>
        /// <param name="runEvaluator">
        /// The run evaluator.
        /// </param>
        /// <param name="trainingInstances">
        /// The training instances.
        /// </param>
        /// <param name="parameterTree">
        /// The parameter tree.
        /// </param>
        /// <param name="configuration">
        /// The configuration.
        /// </param>
        public AlgorithmTuner(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            IRunEvaluator<TResult> runEvaluator,
            IEnumerable<TInstance> trainingInstances,
            ParameterTree parameterTree,
            AlgorithmTunerConfiguration configuration)
            : this(
                targetAlgorithmFactory,
                runEvaluator,
                trainingInstances,
                parameterTree,
                configuration,
                new GenomeBuilder(parameterTree, configuration))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> class.
        /// </summary>
        /// <param name="targetAlgorithmFactory">
        /// The target algorithm factory.
        /// </param>
        /// <param name="runEvaluator">
        /// The run evaluator.
        /// </param>
        /// <param name="trainingInstances">
        /// The training instances.
        /// </param>
        /// <param name="parameterTree">
        /// The parameter tree.
        /// </param>
        /// <param name="configuration">
        /// The configuration.
        /// </param>
        /// <param name="genomeBuilder">
        /// The genome builder.
        /// </param>
        public AlgorithmTuner(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            IRunEvaluator<TResult> runEvaluator,
            IEnumerable<TInstance> trainingInstances,
            ParameterTree parameterTree,
            AlgorithmTunerConfiguration configuration,
            GenomeBuilder genomeBuilder)
            : base(targetAlgorithmFactory, runEvaluator, trainingInstances, parameterTree, configuration, genomeBuilder)
        {
        }

        #endregion
    }
}