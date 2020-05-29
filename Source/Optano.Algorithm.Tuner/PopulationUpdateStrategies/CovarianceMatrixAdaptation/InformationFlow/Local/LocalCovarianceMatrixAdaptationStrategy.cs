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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Local
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Akka.Actor;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Uses <see cref="CmaEs{TSearchPoint}"/> instances to update <see cref="Population"/> objects: In each phase,
    /// optimizes the continuous parameter set to the discrete parameter set of the current incumbent.
    /// </summary>
    /// <typeparam name="TInstance">
    /// The instance type to use.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The result for an individual evaluation.
    /// </typeparam>
    public class LocalCovarianceMatrixAdaptationStrategy<TInstance, TResult>
        : CovarianceMatrixAdaptationStrategyBase<PartialGenomeSearchPoint, TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// The <see cref="GenomeBuilder" /> used in tuning.
        /// </summary>
        private readonly GenomeBuilder _genomeBuilder;

        /// <summary>
        /// Specifies how to create a <see cref="PartialGenomeSearchPoint"/> from a <see cref="Vector{T}"/>.
        /// </summary>
        private Func<Vector<double>, PartialGenomeSearchPoint> _searchPointFactory;

        /// <summary>
        /// The <see cref="CmaEs{TSearchPoint}"/> instance currently in use.
        /// </summary>
        private CmaEs<PartialGenomeSearchPoint> _cmaEsRunner;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalCovarianceMatrixAdaptationStrategy{TInstance, TResult}"/>
        /// class.
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
        public LocalCovarianceMatrixAdaptationStrategy(
            AlgorithmTunerConfiguration configuration,
            ParameterTree parameterTree,
            GenomeBuilder genomeBuilder,
            IActorRef genomeSorter,
            IActorRef targetRunResultStorage)
            : base(configuration, parameterTree, genomeSorter, targetRunResultStorage)
        {
            this._genomeBuilder = genomeBuilder ?? throw new ArgumentNullException(nameof(genomeBuilder));

            // Create a dummy search point factory to enable CMA-ES dumps.
            this._searchPointFactory = vector => throw new InvalidOperationException("Called search point factory without initialization!");
            this._cmaEsRunner = new CmaEs<PartialGenomeSearchPoint>(this.SearchPointSorter, this._searchPointFactory);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="IEvolutionBasedContinuousOptimizer{TSearchPoint}"/> instance currently in use.
        /// </summary>
        protected override IEvolutionBasedContinuousOptimizer<PartialGenomeSearchPoint> ContinuousOptimizer
            => this._cmaEsRunner;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Writes all internal data to file.
        /// <para>Calling <see cref="IPopulationUpdateStrategy{TInstance,TResult}.DumpStatus"/>, terminating the program and then calling
        /// <see cref="IPopulationUpdateStrategy{TInstance,TResult}.UseStatusDump"/> needs to be equivalent to one long run.</para>
        /// </summary>
        public override void DumpStatus()
        {
            var strategyStatus = new CovarianceMatrixAdaptationStrategyStatus<PartialGenomeSearchPoint, TInstance>(
                this.OriginalIncumbent,
                this.CurrentEvaluationInstances,
                this.MostRecentSorting);
            strategyStatus.WriteToFile(this.StrategyStatusFilePath);
            this._cmaEsRunner.DumpStatus(this.ContinuousOptimizerStatusFilePath);
        }

        /// <inheritdoc />
        public override void UseStatusDump(IGeneticEngineering evaluationModel)
        {
            // The CMA-ES runner must be reinitialized for every phase in order to use the correct genome as base.
            // Recreate the latest runner.
            var strategyStatus = this.DeserializeStrategyStatusFile();
            var originalIncumbent = strategyStatus.OriginalIncumbent;
            if (originalIncumbent != null)
            {
                this._cmaEsRunner = this.CreateCmaEsRunner(evaluationBase: originalIncumbent);
            }

            base.UseStatusDump(evaluationModel);
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

            if (currentIncumbent == null)
            {
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    "CMA-ES with focus on incumbent can only be executed if an incumbent exists, i.e. it is not possible to run it on its own.");
                throw new ArgumentNullException(nameof(currentIncumbent));
            }

            // We do not reuse anything from potential old configurations, because old information may be
            // outdated at the point a new phase is started.
            var initialMean = PartialGenomeSearchPoint.CreateFromGenome(
                currentIncumbent.IncumbentGenome,
                this.ParameterTree,
                this.StrategyConfiguration.MinimumDomainSize).Values;
            var cmaEsConfiguration = new CmaEsConfiguration(
                populationSize: basePopulation.CompetitiveCount,
                initialDistributionMean: initialMean,
                initialStepSize: this.StrategyConfiguration.InitialStepSize);

            this._cmaEsRunner = this.CreateCmaEsRunner(evaluationBase: currentIncumbent.IncumbentGenome);
            this._cmaEsRunner.Initialize(cmaEsConfiguration, this.CreateTerminationCriteria());
        }

        /// <inheritdoc />
        protected override IEnumerable<Genome> DefineCompetitivePopulation(IReadOnlyList<Genome> originalCompetitives)
        {
            var updatedCompetitives = new List<Genome>();

            // Round s. t. at least one genome will be replaced by a search point.
            // Also keep at least one genome to be able to keep the incumbent.
            var numberToReplace = Math.Min(
                (int)Math.Ceiling(this.StrategyConfiguration.ReplacementRate * originalCompetitives.Count),
                originalCompetitives.Count - 1);

            // We will add the incumbent later, so subtract one.
            var numberToKeep = originalCompetitives.Count - numberToReplace - 1;

            var randomizedCompetitiveBasePopulation = Randomizer.Instance.ChooseRandomSubset(
                originalCompetitives,
                number: originalCompetitives.Count).ToList();
            foreach (var competitive in randomizedCompetitiveBasePopulation.Take(numberToKeep))
            {
                updatedCompetitives.Add(new Genome(competitive));
            }

            // Use incumbent from before phase to stay elitist overall.
            // If it is already contained, use another genome.
            updatedCompetitives.Add(
                !updatedCompetitives.Contains(this.OriginalIncumbent, new Genome.GeneValueComparator())
                    ? this.OriginalIncumbent
                    : new Genome(randomizedCompetitiveBasePopulation[numberToKeep]));

            // Make sure the age balance stays correct for the rest of the population.
            // Shuffle ages to assign them randomly.
            var ageDistribution = Randomizer.Instance
                .ChooseRandomSubset(
                    originalCompetitives.Select(individual => individual.Age),
                    originalCompetitives.Count)
                .ToList();
            foreach (var alreadyIncluded in updatedCompetitives)
            {
                ageDistribution.Remove(alreadyIncluded.Age);
            }

            // Fill up competitive genomes with best CMA-ES output.
            for (int i = 0; i < numberToReplace; i++)
            {
                var genome = new Genome(this.MostRecentSorting[i].Genome.CreateMutableGenome(), age: ageDistribution[i]);
                updatedCompetitives.Add(genome);
            }

            return updatedCompetitives;
        }

        /// <summary>
        /// Creates a new <see cref="CmaEs{TSearchPoint}"/> which evaluates points based on the provided
        /// <see cref="Genome"/>.
        /// </summary>
        /// <param name="evaluationBase">The <see cref="Genome"/> determining discrete parameters.</param>
        /// <returns>The newly created <see cref="CmaEs{TSearchPoint}"/>.</returns>
        private CmaEs<PartialGenomeSearchPoint> CreateCmaEsRunner(Genome evaluationBase)
        {
            PartialGenomeSearchPoint.ObtainParameterBounds(
                this.ParameterTree,
                this.StrategyConfiguration.MinimumDomainSize,
                out var lowerBounds,
                out var upperBounds);
            var converter = new GenomeSearchPointConverter(this.ParameterTree, this.StrategyConfiguration.MinimumDomainSize);
            this._searchPointFactory = vector =>
                new PartialGenomeSearchPoint(new ImmutableGenome(evaluationBase), vector, converter, this._genomeBuilder, lowerBounds, upperBounds);
            return new CmaEs<PartialGenomeSearchPoint>(this.SearchPointSorter, this._searchPointFactory);
        }

        #endregion
    }
}