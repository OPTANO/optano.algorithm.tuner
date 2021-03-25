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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow
{
    using System;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Global;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Local;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Responsible for selecting the correct
    /// <see cref="CovarianceMatrixAdaptationStrategyBase{TSearchPoint, TInstance, TResult}"/> depending on the
    /// <see cref="CovarianceMatrixAdaptationStrategyConfiguration"/>.
    /// </summary>
    public static class CovarianceMatrixAdaptationInformationFlowSwitch
    {
        #region Public Methods and Operators

        /// <summary>
        /// Creates a <see cref="CovarianceMatrixAdaptationStrategyBase{TSearchPoint, TInstance, TResult}"/> depending
        /// on the <see cref="CovarianceMatrixAdaptationStrategyConfiguration"/>.
        /// </summary>
        /// <typeparam name="TInstance">The instance type.</typeparam>
        /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
        /// <param name="configuration">Options to use.</param>
        /// <param name="parameterTree">Provides the tunable parameters.</param>
        /// <param name="genomeBuilder">Responsible for creation, modification and crossover of genomes.
        /// Needs to be compatible with the given parameter tree and configuration.</param>
        /// <param name="generationEvaluationActor">
        /// An <see cref="IActorRef" /> to a <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </param>
        /// <param name="targetRunResultStorage">
        /// An <see cref="IActorRef" /> to a <see cref="ResultStorageActor{TInstance,TResult}" />
        /// which knows about all executed target algorithm runs and their results.
        /// </param>
        /// <returns>
        /// The created <see cref="CovarianceMatrixAdaptationStrategyBase{TSearchPoint, TInstance, TResult}"/> instance.
        /// </returns>
        public static IPopulationUpdateStrategy<TInstance, TResult> CreateCovarianceMatrixAdaptationStrategy<TInstance, TResult>(
            AlgorithmTunerConfiguration configuration,
            ParameterTree parameterTree,
            GenomeBuilder genomeBuilder,
            IActorRef generationEvaluationActor,
            IActorRef targetRunResultStorage)
            where TInstance : InstanceBase
            where TResult : ResultBase<TResult>, new()
        {
            if (StrategyShouldFocusOnIncumbent(configuration))
            {
                return new LocalCovarianceMatrixAdaptationStrategy<TInstance, TResult>(
                    configuration,
                    parameterTree,
                    genomeBuilder,
                    generationEvaluationActor,
                    targetRunResultStorage);
            }
            else
            {
                return new GlobalCovarianceMatrixAdaptationStrategy<TInstance, TResult>(
                    configuration,
                    parameterTree,
                    genomeBuilder,
                    generationEvaluationActor,
                    targetRunResultStorage);
            }
        }

        /// <summary>
        /// Determines the type of the
        /// <see cref="CovarianceMatrixAdaptationStrategyBase{TSearchPoint,TInstance,TResult}"/> to create according to
        /// the provided <see cref="AlgorithmTunerConfiguration"/>.
        /// </summary>
        /// <typeparam name="TInstance">The instance type.</typeparam>
        /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
        /// <param name="configuration">The <see cref="AlgorithmTunerConfiguration"/> to check.</param>
        /// <returns>
        /// The type of the <see cref="CovarianceMatrixAdaptationStrategyBase{TSearchPoint,TInstance,TResult}"/> to
        /// create.
        /// </returns>
        public static Type DetermineCovarianceMatrixAdaptationStrategyType<TInstance, TResult>(
            AlgorithmTunerConfiguration configuration)
            where TInstance : InstanceBase
            where TResult : ResultBase<TResult>, new()
        {
            if (StrategyShouldFocusOnIncumbent(configuration))
            {
                return typeof(LocalCovarianceMatrixAdaptationStrategy<TInstance, TResult>);
            }
            else
            {
                return typeof(GlobalCovarianceMatrixAdaptationStrategy<TInstance, TResult>);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the <see cref="CovarianceMatrixAdaptationStrategyBase{TSearchPoint,TInstance,TResult}"/>
        /// is supposed to focus on the incumbent according to the provided <see cref="AlgorithmTunerConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="AlgorithmTunerConfiguration"/> to check.</param>
        /// <returns>
        /// Whether the <see cref="CovarianceMatrixAdaptationStrategyBase{TSearchPoint,TInstance,TResult}"/>
        /// is supposed to focus on the incumbent.
        /// </returns>
        private static bool StrategyShouldFocusOnIncumbent(AlgorithmTunerConfiguration configuration)
        {
            var strategyConfiguration = configuration
                .ExtractDetailedConfiguration<CovarianceMatrixAdaptationStrategyConfiguration>(
                    CovarianceMatrixAdaptationStrategyArgumentParser.Identifier);
            return strategyConfiguration.FocusOnIncumbent;
        }

        #endregion
    }
}