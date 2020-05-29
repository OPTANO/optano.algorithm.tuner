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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Akka.Actor;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.InformationFlow;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.SearchPoint;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Updates <see cref="Population"/> objects using <see cref="DifferentialEvolution{TSearchPoint}"/> instances.
    /// </summary>
    /// <typeparam name="TInstance">
    /// The instance type to use.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The result for an individual evaluation.
    /// </typeparam>
    public class DifferentialEvolutionStrategy<TInstance, TResult> : ContinuousOptimizationStrategyBase<GenomeSearchPoint, TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// Detailed options about this strategy.
        /// </summary>
        private readonly DifferentialEvolutionStrategyConfiguration _strategyConfiguration;

        /// <summary>
        /// Defines the information flow between the <see cref="Population"/> used by the main tuning process and the
        /// one managed by <see cref="_differentialEvolutionRunner"/>.
        /// </summary>
        private readonly IInformationFlowStrategy<GenomeSearchPoint> _informationFlow;

        /// <summary>
        /// Specifies how to create a <see cref="GenomeSearchPoint"/> from a <see cref="Vector{T}"/> and a
        /// parent.
        /// </summary>
        private readonly Func<Vector<double>, GenomeSearchPoint, GenomeSearchPoint> _searchPointFactory;

        /// <summary>
        /// The <see cref="DifferentialEvolution{TSearchPoint}"/> instance currently in use.
        /// </summary>
        private DifferentialEvolution<GenomeSearchPoint> _differentialEvolutionRunner;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentialEvolutionStrategy{TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="configuration">Options used for this instance.</param>
        /// <param name="parameterTree">Provides the tunable parameters.</param>
        /// <param name="genomeBuilder">Responsible for creation, modification and crossover of genomes.
        /// Needs to be compatible with the given parameter tree and configuration.</param>
        /// <param name="genomeSorter">
        /// An <see cref="IActorRef" /> to a <see cref="GenomeSorter{TInstance,TResult}" />.
        /// </param>
        /// <param name="targetRunResultStorage">
        /// An <see cref="IActorRef" /> to a <see cref="ResultStorageActor{TInstance,TResult}" />
        /// which knows about all executed target algorithm runs and their results.
        /// </param>
        public DifferentialEvolutionStrategy(
            AlgorithmTunerConfiguration configuration,
            ParameterTree parameterTree,
            GenomeBuilder genomeBuilder,
            IActorRef genomeSorter,
            IActorRef targetRunResultStorage)
            : base(configuration, parameterTree, targetRunResultStorage, new GenomeSearchPointSorter<TInstance>(genomeSorter))
        {
            this._strategyConfiguration =
                this.Configuration.ExtractDetailedConfiguration<DifferentialEvolutionStrategyConfiguration>(
                    DifferentialEvolutionStrategyArgumentParser.Identifier);

            if (this._strategyConfiguration.FocusOnIncumbent)
            {
                this._informationFlow = new LocalDifferentialEvolutionInformationFlow(
                    this._strategyConfiguration,
                    parameterTree,
                    genomeBuilder);
            }
            else
            {
                this._informationFlow = new GlobalDifferentialEvolutionInformationFlow(
                    this._strategyConfiguration,
                    parameterTree,
                    genomeBuilder);
            }

            this._searchPointFactory = (vector, parent) => new GenomeSearchPoint(vector, parent, genomeBuilder);

            this._differentialEvolutionRunner = this.CreateDifferentialEvolutionRunner(
                this._strategyConfiguration.DifferentialEvolutionConfiguration.InitialMeanMutationFactor,
                this._strategyConfiguration.DifferentialEvolutionConfiguration.InitialMeanCrossoverRate);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="IEvolutionBasedContinuousOptimizer{TSearchPoint}"/> instance currently in use.
        /// </summary>
        protected override IEvolutionBasedContinuousOptimizer<GenomeSearchPoint> ContinuousOptimizer
            => this._differentialEvolutionRunner;

        /// <summary>
        /// Gets the status file path to use for the <see cref="DifferentialEvolutionStatus{TSearchPoint}"/>.
        /// </summary>
        protected override string ContinuousOptimizerStatusFilePath =>
            Path.Combine(this.Configuration.StatusFileDirectory, DifferentialEvolutionStatus<GenomeSearchPoint>.FileName);

        /// <summary>
        /// Gets the path to use when working with <see cref="DifferentialEvolutionStrategyStatus{TInstance}"/>.
        /// </summary>
        private string StrategyStatusFilePath => Path.Combine(
            this.Configuration.StatusFileDirectory,
            DifferentialEvolutionStrategyStatus<TInstance>.FileName);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Writes all internal data to file.
        /// <para>Calling <see cref="IPopulationUpdateStrategy{TInstance,TResult}.DumpStatus"/>, terminating the program and then calling
        /// <see cref="IPopulationUpdateStrategy{TInstance,TResult}.UseStatusDump"/> needs to be equivalent to one long run.</para>
        /// </summary>
        public override void DumpStatus()
        {
            var strategyStatus = new DifferentialEvolutionStrategyStatus<TInstance>(
                this.OriginalIncumbent,
                this.CurrentEvaluationInstances,
                this.MostRecentSorting);
            strategyStatus.WriteToFile(this.StrategyStatusFilePath);
            this._differentialEvolutionRunner.DumpStatus(this.ContinuousOptimizerStatusFilePath);
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override void InitializeContinuousOptimizer(
            Population basePopulation,
            IncumbentGenomeWrapper<TResult> currentIncumbent)
        {
            if (basePopulation == null)
            {
                throw new ArgumentNullException(nameof(basePopulation));
            }

            if (!basePopulation.GetCompetitiveIndividuals().Any())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(basePopulation),
                    "Population must have competitive individuals.");
            }

            // Continue the parameter adaptation where we left off before, but re-initialize everything else.
            // If the adaptation was restarted every time, we would need quite long phases to actually see the
            // adaptation happening.
            this._differentialEvolutionRunner = this.CreateDifferentialEvolutionRunner(
                meanMutationFactor: this._differentialEvolutionRunner.MeanMutationFactor,
                meanCrossoverRate: this._differentialEvolutionRunner.MeanCrossoverRate);

            var initialPositions = this._informationFlow
                .DetermineInitialPoints(basePopulation, currentIncumbent?.IncumbentGenome)
                .ToList();
            this._differentialEvolutionRunner.Initialize(
                initialPositions,
                this._strategyConfiguration.MaximumNumberGenerations);

            LoggingHelper.WriteLine(
                VerbosityLevel.Debug,
                $"Tuning {initialPositions.First().Values.Count} continuous parameters.");
        }

        /// <inheritdoc />
        protected override IEnumerable<Genome> DefineCompetitivePopulation(IReadOnlyList<Genome> originalCompetitives)
        {
            return this._informationFlow.DefineCompetitivePopulation(
                originalCompetitives,
                this.OriginalIncumbent,
                this.MostRecentSorting);
        }

        /// <inheritdoc />
        protected override bool HasFixedInstances()
        {
            return this._strategyConfiguration.FixInstances;
        }

        /// <inheritdoc />
        protected override ContinuousOptimizationStrategyStatusBase<GenomeSearchPoint, TInstance> DeserializeStrategyStatusFile()
        {
            return
                StatusBase.ReadFromFile<DifferentialEvolutionStrategyStatus<TInstance>>(this.StrategyStatusFilePath);
        }

        /// <summary>
        /// Creates a new <see cref="DifferentialEvolution{TSearchPoint}"/> instance.
        /// </summary>
        /// <param name="meanMutationFactor">The mean mutation factor to start with.</param>
        /// <param name="meanCrossoverRate">The mean crossover rate to start with.</param>
        /// <returns>The created <see cref="DifferentialEvolution{TSearchPoint}"/> instance.</returns>
        private DifferentialEvolution<GenomeSearchPoint> CreateDifferentialEvolutionRunner(
            double meanMutationFactor,
            double meanCrossoverRate)
        {
            return new DifferentialEvolution<GenomeSearchPoint>(
                this.SearchPointSorter,
                this._searchPointFactory,
                new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder()
                    .SetBestPercentage(this._strategyConfiguration.DifferentialEvolutionConfiguration.BestPercentage)
                    .SetInitialMeanMutationFactor(meanMutationFactor)
                    .SetInitialMeanCrossoverRate(meanCrossoverRate)
                    .SetLearningRate(this._strategyConfiguration.DifferentialEvolutionConfiguration.LearningRate)
                    .BuildWithFallback(null));
        }

        #endregion
    }
}