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

namespace Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;

    using NLog;

    using Optano.Algorithm.Tuner.Serialization;

    /// <summary>
    /// Differential evolution (DE) variant JADE without archive as defined by J. Zhang and A. C. Sanderson,
    /// "JADE: Adaptive Differential Evolution With Optional External Archive," in IEEE Transactions on Evolutionary
    /// Computation, vol. 13, no. 5, pp. 945-958, Oct. 2009.
    /// </summary>
    /// <typeparam name="TSearchPoint">Type of <see cref="SearchPoint"/>s handled by this JADE instance.</typeparam>
    public class DifferentialEvolution<TSearchPoint> : IEvolutionBasedContinuousOptimizer<TSearchPoint>
        where TSearchPoint : SearchPoint, IDeserializationRestorer<TSearchPoint>
    {
        #region Static Fields

        /// <summary>
        /// <see cref="NLog"/> logger object.
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Fields

        /// <summary>
        /// Specifies how to sort the <typeparamref name="TSearchPoint"/>s.
        /// </summary>
        private readonly ISearchPointSorter<TSearchPoint> _searchPointSorter;

        /// <summary>
        /// Specifies how to create a <typeparamref name="TSearchPoint"/> from a <see cref="Vector{T}"/> and a parent.
        /// </summary>
        private readonly Func<Vector<double>, TSearchPoint, TSearchPoint> _searchPointFactory;

        /// <summary>
        /// Configuration parameter for JADE.
        /// </summary>
        private readonly DifferentialEvolutionConfiguration _configuration;

        /// <summary>
        /// The current population sorted by performance, best <typeparamref name="TSearchPoint"/>s first.
        /// </summary>
        private List<TSearchPoint> _sortedPopulation;

        /// <summary>
        /// The current differential evolution generation, often denoted g in literature.
        /// </summary>
        private int _currentJadeGeneration;

        /// <summary>
        /// The maximum number of differential evolution generations to run.
        /// </summary>
        private int _maxJadeGenerations;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentialEvolution{TSearchPoint}"/> class.
        /// </summary>
        /// <param name="searchPointSorter">
        /// Specifies how to sort the <typeparamref name="TSearchPoint"/>s.
        /// </param>
        /// <param name="searchPointFactory">
        /// Specifies how to create a <typeparamref name="TSearchPoint"/> from a <see cref="Vector{T}"/> and a parent.
        /// </param>
        /// <param name="configuration">The <see cref="DifferentialEvolutionConfiguration"/> to use.</param>
        public DifferentialEvolution(
            ISearchPointSorter<TSearchPoint> searchPointSorter,
            Func<Vector<double>, TSearchPoint, TSearchPoint> searchPointFactory,
            DifferentialEvolutionConfiguration configuration)
        {
            this._searchPointSorter = searchPointSorter ?? throw new ArgumentNullException(nameof(searchPointSorter));
            this._searchPointFactory =
                searchPointFactory ?? throw new ArgumentNullException(nameof(searchPointFactory));

            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.MeanMutationFactor = this._configuration.InitialMeanMutationFactor;
            this.MeanCrossoverRate = this._configuration.InitialMeanCrossoverRate;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the current value of the mean mutation factor, often called mu_F in literature.
        /// </summary>
        public double MeanMutationFactor { get; private set; }

        /// <summary>
        /// Gets the current value of the mean crossover constant, often called mu_{CR} in literature.
        /// </summary>
        public double MeanCrossoverRate { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Initializes the object to begin a new run.
        /// </summary>
        /// <param name="initialPositions">The initial population.</param>
        /// <param name="maxGenerations">The maximum number of generations to run.</param>
        public void Initialize(
            IEnumerable<TSearchPoint> initialPositions,
            int maxGenerations)
        {
            if (maxGenerations < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxGenerations),
                    $"Maximum number of generations must be nonnegative, but was {maxGenerations}.");
            }

            this._currentJadeGeneration = 0;
            this._maxJadeGenerations = maxGenerations;

            if (initialPositions == null)
            {
                throw new ArgumentNullException(nameof(initialPositions));
            }

            var population = initialPositions.ToList();
            if (!population.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(initialPositions), "Population must not be empty.");
            }

            var sortedIndices = this._searchPointSorter.Sort(population);
            this._sortedPopulation = new List<TSearchPoint>(sortedIndices.Select(idx => population[idx]));
        }

        /// <summary>
        /// Executes a single JADE generation.
        /// </summary>
        /// <remarks><see cref="Initialize"/> needs to be called beforehand.</remarks>
        /// <returns>Current population, best individuals first.</returns>
        public IEnumerable<TSearchPoint> NextGeneration()
        {
            this.CheckIsInitialized();

            this._currentJadeGeneration++;

            Logger.Debug($"Mean mutation factor, crossover rate: {this.MeanMutationFactor}; {this.MeanCrossoverRate}");

            // Generate all trial vectors before sorting.
            var mutationFactors = new List<double>(this._sortedPopulation.Count);
            var crossoverRates = new List<double>(this._sortedPopulation.Count);
            var trialVectors = new List<TSearchPoint>(this._sortedPopulation.Count);
            foreach (var target in this._sortedPopulation)
            {
                mutationFactors.Add(this.GenerateMutationFactor());
                crossoverRates.Add(this.GenerateCrossoverRate());
                trialVectors.Add(this.GenerateTrialPoint(target, mutationFactors.Last(), crossoverRates.Last()));
            }

            var ranks = this.DetermineTargetAndTrialRanks(trialVectors);

            // Update population after sorting.
            var successfulMutationFactors = new List<double>();
            var successfulCrossoverRates = new List<double>();
            for (int i = 0; i < this._sortedPopulation.Count; i++)
            {
                // Only replace point if it has a better rank and changed values.
                if (ranks[this._sortedPopulation[i]] > ranks[trialVectors[i]]
                    && !object.Equals(this._sortedPopulation[i].Values, trialVectors[i].Values))
                {
                    this._sortedPopulation[i] = trialVectors[i];
                    successfulMutationFactors.Add(mutationFactors[i]);
                    successfulCrossoverRates.Add(crossoverRates[i]);
                }
            }

            // Parameter adaptation by JADE.
            this.AdaptParameters(successfulMutationFactors, successfulCrossoverRates);

            // Finally, re-sort the population.
            this._sortedPopulation.Sort((point1, point2) => ranks[point1].CompareTo(ranks[point2]));
            return this._sortedPopulation;
        }

        /// <summary>
        /// Checks whether any termination criteria are met.
        /// </summary>
        /// <returns>Whether any termination criteria are met.</returns>
        public bool AnyTerminationCriterionMet()
        {
            this.CheckIsInitialized();

            bool metMaxGenerations = this._currentJadeGeneration >= this._maxJadeGenerations;
            if (metMaxGenerations)
            {
                Logger.Info("JADE: Termination criterion met.");
                Logger.Debug("MaxGenerations");
                return true;
            }

            bool metMaxDist = this.MaxDistCriterionMet();
            if (metMaxDist)
            {
                Logger.Info("JADE: Termination criterion met.");
                Logger.Debug("MaxDist");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Writes all internal data to file.
        /// </summary>
        /// <param name="pathToStatusFile">Path to the file to write.</param>
        public void DumpStatus(string pathToStatusFile)
        {
            var status = new DifferentialEvolutionStatus<TSearchPoint>(
                this._sortedPopulation,
                this._currentJadeGeneration,
                this._maxJadeGenerations,
                this.MeanMutationFactor,
                this.MeanCrossoverRate);
            status.WriteToFile(pathToStatusFile);
        }

        /// <summary>
        /// Reads all internal data from file.
        /// </summary>
        /// <param name="pathToStatusFile">Path to the file to read.</param>
        public void UseStatusDump(string pathToStatusFile)
        {
            var status = StatusBase.ReadFromFile<DifferentialEvolutionStatus<TSearchPoint>>(pathToStatusFile);

            this._sortedPopulation = status.SortedPopulation?.Select(point => point.Restore()).ToList();
            this._currentJadeGeneration = status.CurrentGeneration;
            this._maxJadeGenerations = status.MaxGenerations;
            this.MeanMutationFactor = status.MeanMutationFactor;
            this.MeanCrossoverRate = status.MeanCrossoverRate;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Generates a trial vector for the provided target point using binomial crossover.
        /// </summary>
        /// <param name="target">The <typeparamref name="TSearchPoint"/> to base the trial vector on.</param>
        /// <param name="donor">The <see cref="Vector{T}"/> to use for replacement values.</param>
        /// <param name="crossoverRate">The crossover rate.</param>
        /// <returns>The generated trial vector.</returns>
        private static Vector<double> Crossover(TSearchPoint target, Vector<double> donor, double crossoverRate)
        {
            var fixedReplacementIndex = Randomizer.Instance.Next(0, donor.Count);
            var trial = target.Values.Clone();
            for (int i = 0; i < trial.Count; i++)
            {
                if (i == fixedReplacementIndex || Randomizer.Instance.Decide(probability: crossoverRate))
                {
                    trial[i] = donor[i];
                }
            }

            return trial;
        }

        /// <summary>
        /// Generates a mutation factor in [0, 1].
        /// </summary>
        /// <returns>The generated mutation factor.</returns>
        private double GenerateMutationFactor()
        {
            // Potentially, this is an infinite loop.
            // However, the distribution is symmetric in the non-negative mutation factor,
            // and the probability of failure is < 1/2 for each iteration.
            double unboundedFactor;
            do
            {
                unboundedFactor = Randomizer.Instance.SampleFromCauchyDistribution(
                    location: this.MeanMutationFactor,
                    scale: 0.1);
            }
            while (unboundedFactor <= 0);

            return Math.Min(unboundedFactor, 1);
        }

        /// <summary>
        /// Generates a crossover rate in [0, 1].
        /// </summary>
        /// <returns>The generated crossover rate.</returns>
        private double GenerateCrossoverRate()
        {
            double unboundedRate = Randomizer.Instance.SampleFromNormal(
                mean: this.MeanCrossoverRate,
                standardDeviation: 0.1);
            return Math.Max(0, Math.Min(unboundedRate, 1));
        }

        /// <summary>
        /// Generates a trial point based on <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The <typeparamref name="TSearchPoint"/> to base the trial on.</param>
        /// <param name="mutationFactor">The mutation factor.</param>
        /// <param name="crossoverRate">The crossover rate.</param>
        /// <returns>The generated trial point.</returns>
        private TSearchPoint GenerateTrialPoint(TSearchPoint target, double mutationFactor, double crossoverRate)
        {
            // Resample trial point if it is invalid.
            // This strategy was found to be highly successful in Kreischer, V., Tavares Magalhães, T., Barbosa, H.
            // and Krempser, Eduardo. (2017). Evaluation of Bound Constraints Handling Methods in Differential
            // Evolution using the CEC2017 Benchmark.
            TSearchPoint trialPoint;
            int samples = 0;
            do
            {
                var donor = this.Mutate(target.Values, mutationFactor);
                var trialVector = DifferentialEvolution<TSearchPoint>.Crossover(target, donor, crossoverRate);
                trialPoint = this._searchPointFactory.Invoke(trialVector, target);
                samples++;
            }
            while (!trialPoint.IsValid() && samples < 100);

            if (!trialPoint.IsValid())
            {
                // Did not manage to find a valid trial point --> reuse target.
                trialPoint = this._searchPointFactory.Invoke(target.Values, target);
                Logger.Warn(
                    $"Did not manage to find a valid point based on {target}."
                    + "If this happens often, consider changing your search point type or search point factory.");
            }

            Logger.Debug($"Found valid trial point in {samples} tries.");

            return trialPoint;
        }

        /// <summary>
        /// Generates a donor vector via differential mutation.
        /// </summary>
        /// <param name="target">The target vector to base the donor on.</param>
        /// <param name="mutationFactor">The mutation factor.</param>
        /// <returns>The generated donor vector.</returns>
        private Vector<double> Mutate(Vector<double> target, double mutationFactor)
        {
            var nonTargetVectors = this._sortedPopulation
                .Select(point => point.Values)
                .Where(vector => !object.ReferenceEquals(vector, target));
            var mutationVectors = Randomizer.Instance.ChooseRandomSubset(nonTargetVectors, 2).ToList();
            var goodSearchPoint = this.ChooseRandomGoodSearchPoint();
            return target
                   + (mutationFactor * (goodSearchPoint.Values - target))
                   + (mutationFactor * (mutationVectors[0] - mutationVectors[1]));
        }

        /// <summary>
        /// Randomly chooses a good <typeparamref name="TSearchPoint"/> from <see cref="_sortedPopulation"/>.
        /// </summary>
        /// <returns>The chosen <typeparamref name="TSearchPoint"/>.</returns>
        private TSearchPoint ChooseRandomGoodSearchPoint()
        {
            var numberGoodPoints = (int)Math.Ceiling(this._configuration.BestPercentage * this._sortedPopulation.Count);
            return this._sortedPopulation[Randomizer.Instance.Next(0, numberGoodPoints)];
        }

        /// <summary>
        /// Determines ranks for <see cref="_sortedPopulation"/> and <paramref name="trialPoints"/>.
        /// </summary>
        /// <param name="trialPoints">Additional points to determine ranks for.</param>
        /// <returns>A mapping to relative ranks.</returns>
        private Dictionary<TSearchPoint, int> DetermineTargetAndTrialRanks(List<TSearchPoint> trialPoints)
        {
            var individualsToSort = new List<TSearchPoint>(this._sortedPopulation.Count * 2);
            individualsToSort.AddRange(this._sortedPopulation);
            individualsToSort.AddRange(trialPoints);
            return this._searchPointSorter.DetermineRanks(individualsToSort);
        }

        /// <summary>
        /// Adapts <see cref="MeanMutationFactor"/> and <see cref="MeanCrossoverRate"/>.
        /// </summary>
        /// <param name="successfulMutationFactors">
        /// Mutation factors which led to improved <typeparamref name="TSearchPoint"/>s.
        /// </param>
        /// <param name="successfulCrossoverRates">
        /// Crossover rates which led to improved <typeparamref name="TSearchPoint"/>s.
        /// </param>
        private void AdaptParameters(List<double> successfulMutationFactors, List<double> successfulCrossoverRates)
        {
            if (successfulMutationFactors.Any())
            {
                var successfulMutationFactorLehmerMean =
                    successfulMutationFactors.Sum(factor => Math.Pow(factor, 2)) / successfulMutationFactors.Sum();
                this.MeanMutationFactor = ((1 - this._configuration.LearningRate) * this.MeanMutationFactor)
                                          + (this._configuration.LearningRate * successfulMutationFactorLehmerMean);
            }

            if (successfulCrossoverRates.Any())
            {
                this.MeanCrossoverRate = ((1 - this._configuration.LearningRate) * this.MeanCrossoverRate)
                                         + (this._configuration.LearningRate * successfulCrossoverRates.Average());
            }
        }

        /// <summary>
        /// Checks whether the maximum distance to the best <typeparamref name="TSearchPoint"/> is sufficiently small
        /// to assume convergence.
        /// </summary>
        /// <returns>Whether the maximum distance is tiny.</returns>
        private bool MaxDistCriterionMet()
        {
            var bestVector = this._sortedPopulation.First().Values;
            return this._sortedPopulation.Max(point => Distance.Euclidean(point.Values, bestVector)) < 10E-4;
        }

        /// <summary>
        /// Checks that <see cref="Initialize"/> has been called.
        /// </summary>
        /// <param name="memberName">Name of calling method. Automatically set.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="Initialize"/> has not been called so far.
        /// </exception>
        private void CheckIsInitialized([CallerMemberName] string memberName = "")
        {
            if (this._sortedPopulation == null)
            {
                throw new InvalidOperationException(
                    $"Cannot execute {memberName} before calling {nameof(this.Initialize)}.");
            }
        }

        #endregion
    }
}