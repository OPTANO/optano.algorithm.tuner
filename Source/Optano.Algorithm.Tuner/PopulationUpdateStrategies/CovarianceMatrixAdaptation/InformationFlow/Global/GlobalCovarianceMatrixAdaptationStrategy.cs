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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Global
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
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Uses <see cref="CmaEs{TSearchPoint}"/> instances to update <see cref="Population"/> objects:
    /// Continuizes all parameters and works on the complete competitive population.
    /// </summary>
    /// <typeparam name="TInstance">
    /// The instance type to use.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The result for an individual evaluation.
    /// </typeparam>
    public class GlobalCovarianceMatrixAdaptationStrategy<TInstance, TResult>
        : CovarianceMatrixAdaptationStrategyBase<ContinuizedGenomeSearchPoint, TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// Specifies how to create a <see cref="ContinuizedGenomeSearchPoint"/> from a <see cref="Vector{T}"/>.
        /// </summary>
        private readonly Func<Vector<double>, ContinuizedGenomeSearchPoint> _searchPointFactory;

        /// <summary>
        /// The <see cref="CmaEs{TSearchPoint}"/> instance currently in use.
        /// </summary>
        private CmaEs<ContinuizedGenomeSearchPoint> _cmaEsRunner;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="GlobalCovarianceMatrixAdaptationStrategy{TInstance, TResult}"/> class.
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
        public GlobalCovarianceMatrixAdaptationStrategy(
            AlgorithmTunerConfiguration configuration,
            ParameterTree parameterTree,
            GenomeBuilder genomeBuilder,
            IActorRef genomeSorter,
            IActorRef targetRunResultStorage)
            : base(configuration, parameterTree, genomeSorter, targetRunResultStorage)
        {
            if (genomeBuilder == null)
            {
                throw new ArgumentNullException(nameof(genomeBuilder));
            }

            ContinuizedGenomeSearchPoint.ObtainParameterBounds(parameterTree, out var lowerBounds, out var upperBounds);
            this._searchPointFactory = vector =>
                new ContinuizedGenomeSearchPoint(vector, parameterTree, genomeBuilder, lowerBounds, upperBounds);
            this._cmaEsRunner = new CmaEs<ContinuizedGenomeSearchPoint>(this.SearchPointSorter, this._searchPointFactory);
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        protected override IEvolutionBasedContinuousOptimizer<ContinuizedGenomeSearchPoint> ContinuousOptimizer => this._cmaEsRunner;

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public override void DumpStatus()
        {
            var strategyStatus = new CovarianceMatrixAdaptationStrategyStatus<ContinuizedGenomeSearchPoint, TInstance>(
                this.OriginalIncumbent,
                this.CurrentEvaluationInstances,
                this.MostRecentSorting);
            strategyStatus.WriteToFile(this.StrategyStatusFilePath);
            this._cmaEsRunner.DumpStatus(this.ContinuousOptimizerStatusFilePath);
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

            // We do not reuse anything from potential old configurations, because old information may be
            // outdated at the point a new phase is started.
            var initialMean = currentIncumbent != null
                                  ? ContinuizedGenomeSearchPoint.CreateFromGenome(currentIncumbent.IncumbentGenome, this.ParameterTree).Values
                                  : this.ComputeMeanOfCompetitivePopulationPart(basePopulation);
            var cmaEsConfiguration = new CmaEsConfiguration(
                populationSize: basePopulation.CompetitiveCount,
                initialDistributionMean: initialMean,
                initialStepSize: this.StrategyConfiguration.InitialStepSize);
            this._cmaEsRunner = new CmaEs<ContinuizedGenomeSearchPoint>(this.SearchPointSorter, this._searchPointFactory);
            this._cmaEsRunner.Initialize(cmaEsConfiguration, this.CreateTerminationCriteria());
        }

        /// <inheritdoc />
        protected override IEnumerable<Genome> DefineCompetitivePopulation(IReadOnlyList<Genome> originalCompetitives)
        {
            var competitivePopulation = new List<Genome>();

            // Consider ages to ensure the age balance stays correct.
            var ageDistribution = originalCompetitives.Select(individual => individual.Age).ToList();
            var nextAgeIndex = 0;

            // If exists: Use incumbent from before phase to stay elitist overall.
            if (this.OriginalIncumbent != null)
            {
                competitivePopulation.Add(this.OriginalIncumbent);
                ageDistribution.Remove(this.OriginalIncumbent.Age);
            }

            // Fill up competitive population with CMA-ES output.
            // Shuffle points to assign ages randomly.
            var missingPoints = this.MostRecentSorting
                .Take(this.MostRecentSorting.Count - competitivePopulation.Count)
                .ToList();
            foreach (var point in Randomizer.Instance.ChooseRandomSubset(missingPoints, missingPoints.Count))
            {
                competitivePopulation.Add(
                    new Genome(point.Genome.CreateMutableGenome(), age: ageDistribution[nextAgeIndex]));

                nextAgeIndex++;
            }

            return competitivePopulation;
        }

        /// <summary>
        /// Computes the mean of the competitive population part.
        /// </summary>
        /// <param name="population">The <see cref="Population"/>.</param>
        /// <returns>The mean of the competitive population part.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if population does not contain competitive individuals.
        /// </exception>
        private Vector<double> ComputeMeanOfCompetitivePopulationPart(Population population)
        {
            var competitivePopulationPart = population.GetCompetitiveIndividuals();
            if (!competitivePopulationPart.Any())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(population),
                    "Population must contain competitive individuals.");
            }

            var searchPointValueSum = Vector<double>.Build.Dense(this.ParameterTree.GetParameters().Count(), value: 0.0);
            foreach (var genome in competitivePopulationPart)
            {
                searchPointValueSum += ContinuizedGenomeSearchPoint.CreateFromGenome(genome, this.ParameterTree).Values;
            }

            return searchPointValueSum / competitivePopulationPart.Count;
        }

        #endregion
    }
}