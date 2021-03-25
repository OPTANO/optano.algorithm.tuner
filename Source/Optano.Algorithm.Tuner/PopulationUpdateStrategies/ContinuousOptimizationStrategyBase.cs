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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.GeneticAlgorithm;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.Tracking;

    /// <summary>
    /// Parts of a <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/> implementation useful for strategies
    /// employing a <see cref="IEvolutionBasedContinuousOptimizer{TSearchPoint}"/>.
    /// </summary>
    /// <typeparam name="TSearchPoint">
    /// The type of <see cref="SearchPoint"/>s handled by this strategy instance.
    /// </typeparam>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    public abstract class ContinuousOptimizationStrategyBase<TSearchPoint, TInstance, TResult> : IPopulationUpdateStrategy<TInstance, TResult>
        where TSearchPoint : SearchPoint, IGenomeRepresentation, IDeserializationRestorer<TSearchPoint>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// The current generation index.
        /// </summary>
        private int _currentGeneration;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuousOptimizationStrategyBase{TSearchPoint, TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="configuration">Options used for this instance.</param>
        /// <param name="parameterTree">Provides the tunable parameters.</param>
        /// <param name="targetRunResultStorage">
        /// An <see cref="IActorRef" /> to a <see cref="ResultStorageActor{TInstance,TResult}" />
        /// which knows about all executed target algorithm runs and their results.
        /// </param>
        /// <param name="searchPointSorter">
        /// A <see cref="ISearchPointSorter{TSearchPoint}"/> which evaluates
        /// <typeparamref name="TSearchPoint"/>s via a <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// configuration
        /// or
        /// parameterTree
        /// or
        /// targetRunResultStorage
        /// or
        /// searchPointSorter.
        /// </exception>
        protected ContinuousOptimizationStrategyBase(
            AlgorithmTunerConfiguration configuration,
            ParameterTree parameterTree,
            IActorRef targetRunResultStorage,
            GenomeAssistedSorterBase<TSearchPoint, TInstance, TResult> searchPointSorter)
        {
            this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.ParameterTree = parameterTree ?? throw new ArgumentNullException(nameof(parameterTree));
            this.TargetRunResultStorage =
                targetRunResultStorage ?? throw new ArgumentNullException(nameof(targetRunResultStorage));
            this.SearchPointSorter = searchPointSorter ?? throw new ArgumentNullException(nameof(searchPointSorter));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a number of options used for this instance.
        /// </summary>
        protected AlgorithmTunerConfiguration Configuration { get; }

        /// <summary>
        /// Gets the structure representing the tunable parameters.
        /// </summary>
        protected ParameterTree ParameterTree { get; }

        /// <summary>
        /// Gets the <see cref="ISearchPointSorter{TSearchPoint}"/> which evaluates
        /// <typeparamref name="TSearchPoint"/>s via a <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </summary>
        protected GenomeAssistedSorterBase<TSearchPoint, TInstance, TResult> SearchPointSorter { get; }

        /// <summary>
        /// Gets an <see cref="IActorRef" /> to a <see cref="ResultStorageActor{TInstance,TResult}" />
        /// which knows about all executed target algorithm runs and their results.
        /// </summary>
        protected IActorRef TargetRunResultStorage { get; }

        /// <summary>
        /// Gets the <see cref="IEvolutionBasedContinuousOptimizer{TSearchPoint}"/> instance currently in use.
        /// </summary>
        protected abstract IEvolutionBasedContinuousOptimizer<TSearchPoint> ContinuousOptimizer { get; }

        /// <summary>
        /// Gets the status file path to use for the <see cref="IEvolutionBasedContinuousOptimizer{TSearchPoint}"/>.
        /// </summary>
        protected abstract string ContinuousOptimizerStatusFilePath { get; }

        /// <summary>
        /// Gets the incumbent from before the start of the continuous optimization phase.
        /// </summary>
        protected Genome OriginalIncumbent { get; private set; }

        /// <summary>
        /// Gets the most recent sorting returned by <see cref="ContinuousOptimizer"/>.
        /// </summary>
        protected List<TSearchPoint> MostRecentSorting { get; private set; }

        /// <summary>
        /// Gets the set of <typeparamref name="TInstance"/>s used for evaluation in this generation.
        /// </summary>
        protected List<TInstance> CurrentEvaluationInstances { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public virtual void Initialize(
            Population basePopulation,
            IncumbentGenomeWrapper<TResult> currentIncumbent,
            IEnumerable<TInstance> instancesForEvaluation)
        {
            // If the instance set is fixed throughout the phase, this is the only place it is updated.
            // Even if it is not fixed, some methods sort provided points in their Initialize methods and
            // therefore need the update.
            this.CurrentEvaluationInstances = instancesForEvaluation?.ToList() ??
                                              throw new ArgumentNullException(nameof(instancesForEvaluation));
            this.SearchPointSorter.UpdateInstances(this.CurrentEvaluationInstances);

            this.InitializeContinuousOptimizer(basePopulation, currentIncumbent);
            this.OriginalIncumbent = currentIncumbent?.IncumbentGenome;
        }

        /// <summary>
        /// Updates the current population.
        /// </summary>
        /// <param name="currentGeneration">The current generation.</param>
        /// <param name="instancesForEvaluation">Instances to use for evaluation.</param>
        public void PerformIteration(int currentGeneration, IEnumerable<TInstance> instancesForEvaluation)
        {
            if (currentGeneration < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentGeneration),
                    $"Generation index may not be negative, but was {currentGeneration}.");
            }

            this._currentGeneration = currentGeneration;
            if (!this.HasFixedInstances())
            {
                this.CurrentEvaluationInstances = instancesForEvaluation.ToList();
                this.SearchPointSorter.UpdateInstances(this.CurrentEvaluationInstances);
            }

            this.MostRecentSorting = this.ContinuousOptimizer.NextGeneration().ToList();
        }

        /// <summary>
        /// Finds an incumbent genome.
        /// </summary>
        /// <returns>A most promising genome.</returns>
        [SuppressMessage(
            "NDepend",
            "ND2302:CautionWithListContains",
            Justification = "Numer of Instances for evaluation will always be \"small\". Overhead for HashSet is not worth it.")]
        public IncumbentGenomeWrapper<TResult> FindIncumbentGenome()
        {
            // Use the best genome found by the continuous optimization method.
            var incumbentGenome = this.MostRecentSorting.First().Genome;

            // All the generation's evaluations are stored in run result storage. Find them for the incumbent.
            var resultRequest = this.TargetRunResultStorage
                .Ask<GenomeResults<TInstance, TResult>>(new GenomeResultsRequest(incumbentGenome));
            resultRequest.Wait();

            var currentGenerationResults = resultRequest.Result.RunResults
                .Where(result => this.CurrentEvaluationInstances.Contains(result.Key))
                .Select(result => result.Value);

            // Finally return all information. The incumbent's generation is always the current one, as the algorithm
            // tuner will not make use of it if the genome has not changed.
            return new IncumbentGenomeWrapper<TResult>
                       {
                           IncumbentGeneration = this._currentGeneration,
                           IncumbentGenome = incumbentGenome.CreateMutableGenome(),
                           IncumbentInstanceResults = currentGenerationResults.ToImmutableList(),
                       };
        }

        /// <inheritdoc />
        public virtual Population FinishPhase(Population basePopulation)
        {
            if (basePopulation == null)
            {
                throw new ArgumentNullException(nameof(basePopulation));
            }

            // We may not have a recent sorting in the case that the termination criterion is met directly
            // (e.g. when genomes are too similar).
            if (this.MostRecentSorting == null)
            {
                return basePopulation;
            }

            // Merge old population with internal state.
            var updatedPopulation = new Population(this.Configuration);
            foreach (var nonCompetitive in basePopulation.GetNonCompetitiveMates())
            {
                updatedPopulation.AddGenome(new Genome(nonCompetitive), isCompetitive: false);
            }

            foreach (var competitive in this.DefineCompetitivePopulation(basePopulation.GetCompetitiveIndividuals()))
            {
                updatedPopulation.AddGenome(competitive, isCompetitive: true);
            }

            // Reset most recent sorting as we do not have it for the next phase.
            this.MostRecentSorting = null;

            return updatedPopulation;
        }

        /// <summary>
        /// Chooses the next population update strategy after this one finished.
        /// </summary>
        /// <param name="populationUpdateStrategies">Possible strategies.</param>
        /// <returns>Index of the chosen strategy.</returns>
        public int NextStrategy(List<IPopulationUpdateStrategy<TInstance, TResult>> populationUpdateStrategies)
        {
            if (populationUpdateStrategies == null)
            {
                throw new ArgumentNullException(nameof(populationUpdateStrategies));
            }

            return populationUpdateStrategies.FindIndex(strategy => strategy is GgaStrategy<TInstance, TResult>);
        }

        /// <summary>
        /// Returns a value indicating whether the current instantiation of the strategy has terminated.
        /// </summary>
        /// <returns>Whether the current instantiation of the strategy has terminated.</returns>
        public bool HasTerminated()
        {
            return this.ContinuousOptimizer.AnyTerminationCriterionMet();
        }

        /// <summary>
        /// Logs information about the current population to console.
        /// </summary>
        public void LogPopulationToConsole()
        {
            LoggingHelper.WriteLine(VerbosityLevel.Debug, "Current population:");
            LoggingHelper.WriteLine(
                VerbosityLevel.Debug,
                $"Competitive genomes:\n {string.Join("\n ", this.MostRecentSorting.Select(point => point.Genome.ToFilteredGeneString(this.ParameterTree)))}");
        }

        /// <summary>
        /// Exports the standard deviations of the numerical features of the current population's competitive part via
        /// <see cref="RunStatisticTracker.ComputeAndExportNumericalFeatureCoefficientOfVariation"/>.
        /// </summary>
        public void ExportFeatureStandardDeviations()
        {
            RunStatisticTracker.ComputeAndExportNumericalFeatureCoefficientOfVariation(
                this.ParameterTree,
                this.MostRecentSorting.Select(point => point.Genome.CreateMutableGenome()),
                this._currentGeneration);
        }

        /// <inheritdoc />
        public abstract void DumpStatus();

        /// <inheritdoc />
        public virtual void UseStatusDump(IGeneticEngineering evaluationModel)
        {
            var strategyStatus = this.DeserializeStrategyStatusFile();
            this.OriginalIncumbent = strategyStatus.OriginalIncumbent;
            this.MostRecentSorting = strategyStatus.MostRecentSorting?.Select(point => point.Restore()).ToList();
            this.CurrentEvaluationInstances = strategyStatus.CurrentEvaluationInstances;

            // Instances must be updated in case they are fixed, i.e. only updated at initialization.
            if (this.CurrentEvaluationInstances != null)
            {
                this.SearchPointSorter.UpdateInstances(this.CurrentEvaluationInstances);
            }

            this.ContinuousOptimizer.UseStatusDump(this.ContinuousOptimizerStatusFilePath);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the <see cref="ContinuousOptimizer"/> for a new strategy phase.
        /// </summary>
        /// <param name="basePopulation">Population to start with.</param>
        /// <param name="currentIncumbent">Most recent incumbent genome. Might be <c>null</c>.</param>
        protected abstract void InitializeContinuousOptimizer(
            Population basePopulation,
            IncumbentGenomeWrapper<TResult> currentIncumbent);

        /// <summary>
        /// Determines whether the continuous optimization method should use a fixed set of evaluation instances for
        /// the complete phase.
        /// </summary>
        /// <returns>
        /// Whether the continuous optimization method should use a fixed set of evaluation instances for
        /// the complete phase.
        /// </returns>
        protected abstract bool HasFixedInstances();

        /// <summary>
        /// Defines the competitive genomes to use when defining a new population at the end of a phase.
        /// </summary>
        /// <param name="originalCompetitives">
        /// The competitive genomes from the population the new one is based on.
        /// </param>
        /// <returns>The competitive genomes to add to the new population.</returns>
        protected abstract IEnumerable<Genome> DefineCompetitivePopulation(IReadOnlyList<Genome> originalCompetitives);

        /// <summary>
        /// Deserializes the current strategy status file.
        /// </summary>
        /// <returns>The deserialized strategy status.</returns>
        protected abstract ContinuousOptimizationStrategyStatusBase<TSearchPoint, TInstance> DeserializeStrategyStatusFile();

        #endregion
    }
}