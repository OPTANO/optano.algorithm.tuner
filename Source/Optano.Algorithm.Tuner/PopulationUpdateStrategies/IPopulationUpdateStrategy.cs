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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies
{
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.Tracking;
    using Optano.Algorithm.Tuner.Tuning;

    /// <summary>
    /// A strategy to update <see cref="Population"/> objects.
    /// </summary>
    /// <typeparam name="TInstance">The instance type to use.</typeparam>
    /// <typeparam name="TResult">The result for an individual evaluation.</typeparam>
    public interface IPopulationUpdateStrategy<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Public Methods and Operators

        /// <summary>
        /// Initializes a new phase for the strategy.
        /// </summary>
        /// <param name="basePopulation">Population to start with.</param>
        /// <param name="currentIncumbent">Most recent incumbent genome. Might be <c>null</c>.</param>
        /// <param name="instancesForEvaluation">Instances to use for evaluation.</param>
        void Initialize(
            Population basePopulation,
            IncumbentGenomeWrapper<TResult> currentIncumbent,
            IEnumerable<TInstance> instancesForEvaluation);

        /// <summary>
        /// Performs an iteration of the population update strategy.
        /// </summary>
        /// <param name="generationIndex">The current generation index.</param>
        /// <param name="instancesForEvaluation">Instances to use for evaluation.</param>
        void PerformIteration(
            int generationIndex,
            IEnumerable<TInstance> instancesForEvaluation);

        /// <summary>
        /// Finds an incumbent genome.
        /// </summary>
        /// <returns>A most promising genome.</returns>
        IncumbentGenomeWrapper<TResult> FindIncumbentGenome();

        /// <summary>
        /// Finishes a phase for the strategy.
        /// </summary>
        /// <param name="basePopulation">Population on which this phase was based.</param>
        /// <returns>The <see cref="Population"/> for the next strategy to work with.</returns>
        Population FinishPhase(Population basePopulation);

        /// <summary>
        /// Chooses the next population update strategy after this one finished.
        /// </summary>
        /// <param name="populationUpdateStrategies">Possible strategies.</param>
        /// <returns>Index of the chosen strategy.</returns>
        int NextStrategy(List<IPopulationUpdateStrategy<TInstance, TResult>> populationUpdateStrategies);

        /// <summary>
        /// Returns a value indicating whether the current instantiation of the strategy has terminated.
        /// </summary>
        /// <returns>Whether the current instantiation of the strategy has terminated.</returns>
        bool HasTerminated();

        /// <summary>
        /// Logs information about the current population to console.
        /// </summary>
        void LogPopulationToConsole();

        /// <summary>
        /// Exports the standard deviations of the numerical features of the current population's competitive part via
        /// <see cref="RunStatisticTracker.ComputeAndExportNumericalFeatureCoefficientOfVariation"/>.
        /// </summary>
        void ExportFeatureStandardDeviations();

        /// <summary>
        /// Writes all internal data to file.
        /// <para>Calling <see cref="DumpStatus"/>, terminating the program and then calling
        /// <see cref="UseStatusDump"/> needs to be equivalent to one long run.</para>
        /// </summary>
        void DumpStatus();

        /// <summary>
        /// Reads all internal data from file.
        /// <para>Calling <see cref="DumpStatus"/>, terminating the program and then calling
        /// <see cref="UseStatusDump"/> needs to be equivalent to one long run.</para>
        /// </summary>
        /// <param name="evaluationModel">
        /// Reference to evaluation model handled by
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}"/>.
        /// </param>
        void UseStatusDump(IGeneticEngineering evaluationModel);

        #endregion
    }
}