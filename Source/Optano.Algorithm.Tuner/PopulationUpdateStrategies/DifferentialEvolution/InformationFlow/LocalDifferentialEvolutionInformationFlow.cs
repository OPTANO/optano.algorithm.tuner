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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.InformationFlow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.SearchPoint;

    /// <summary>
    /// Defines the information flow between the overall tuning process and a
    /// <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/> instance which concentrates on improving the incumbent,
    /// i.e. only acts on incumbent-based points.
    /// </summary>
    /// <remarks>
    /// Only acting on points with discrete parameters equal to those of the incumbent means that we try to find good
    /// continuous parameters for a fixed set of discrete ones. It has the advantage that it does not violate any
    /// assumptions of DE: Everything is continuous.
    /// On the negative side, such an information flow strategy means that we only exploit a single information point
    /// from prior optimization.
    /// </remarks>
    /// <seealso cref="GlobalDifferentialEvolutionInformationFlow"/>
    public class LocalDifferentialEvolutionInformationFlow : IInformationFlowStrategy<GenomeSearchPoint>
    {
        #region Fields

        /// <summary>
        /// Detailed options about this strategy.
        /// </summary>
        private readonly DifferentialEvolutionStrategyConfiguration _strategyConfiguration;

        /// <summary>
        /// The structure representing the tunable parameters.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// The <see cref="GenomeBuilder" /> used in tuning.
        /// </summary>
        private readonly GenomeBuilder _genomeBuilder;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDifferentialEvolutionInformationFlow"/> class.
        /// </summary>
        /// <param name="strategyConfiguration">
        /// Options used for the <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/>.
        /// </param>
        /// <param name="parameterTree">Provides the tunable parameters.</param>
        /// <param name="genomeBuilder">Responsible for creation, modification and crossover of genomes.</param>
        public LocalDifferentialEvolutionInformationFlow(
            DifferentialEvolutionStrategyConfiguration strategyConfiguration,
            ParameterTree parameterTree,
            GenomeBuilder genomeBuilder)
        {
            this._strategyConfiguration = strategyConfiguration ?? throw new ArgumentNullException(nameof(strategyConfiguration));
            this._parameterTree = parameterTree ?? throw new ArgumentNullException(nameof(parameterTree));
            this._genomeBuilder = genomeBuilder ?? throw new ArgumentNullException(nameof(genomeBuilder));
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public IEnumerable<GenomeSearchPoint> DetermineInitialPoints(Population basePopulation, Genome currentIncumbent)
        {
            // Use only half of the competitive population size to keep the number of evaluated genomes equal to GGA
            // (as DE evaluates both parents and children in each generation).
            int jadePopulationCount = basePopulation.CompetitiveCount / 2;
            if (jadePopulationCount < 3)
            {
                throw new ArgumentException(
                    $"JADE needs at least 3 individuals to work. To ensure this, the regular competitive population needs at least 6 individuals, but only has {basePopulation.CompetitiveCount}.");
            }

            if (currentIncumbent == null)
            {
                throw new ArgumentNullException(
                    nameof(currentIncumbent),
                    "Cannot use incumbent improving strategy without an incumbent.");
            }

            return this.CreateSearchPointsFromGenome(currentIncumbent, number: jadePopulationCount);
        }

        /// <inheritdoc />
        public IEnumerable<Genome> DefineCompetitivePopulation(
            IReadOnlyList<Genome> originalCompetitives,
            Genome originalIncumbent,
            IList<GenomeSearchPoint> mostRecentSorting)
        {
            if (this._strategyConfiguration.ReplacementRate == 0)
            {
                return this.ReplaceIncumbentWithBestPoint(
                    originalCompetitives,
                    originalIncumbent,
                    mostRecentSorting.First());
            }
            else
            {
                return this.ReplaceSomeGenomesWithBestPoints(
                    originalCompetitives,
                    mostRecentSorting,
                    this._strategyConfiguration.ReplacementRate);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates <see cref="GenomeSearchPoint"/>s with non-continuous parameters based on the values of the provided
        /// genome.
        /// Other parameters are set randomly but for one <see cref="GenomeSearchPoint"/>, which represents the
        /// genome itself.
        /// </summary>
        /// <param name="genome">The genome to base the <see cref="GenomeSearchPoint"/>s on.</param>
        /// <param name="number">The number of <see cref="GenomeSearchPoint"/>s to create.</param>
        /// <returns>The created <see cref="GenomeSearchPoint"/>s.</returns>
        private List<GenomeSearchPoint> CreateSearchPointsFromGenome(Genome genome, int number)
        {
            var initialPositions = new List<GenomeSearchPoint>(number);
            for (int i = 0; i < number - 1; i++)
            {
                initialPositions.Add(this.CreateValidSearchPointFromGenome(genome));
            }

            // Keep provided genome itself in population to ensure an elitist algorithm
            // in the case it is the incumbent.
            var genomePoint = GenomeSearchPoint.CreateFromGenome(
                genome,
                this._parameterTree,
                this._strategyConfiguration.MinimumDomainSize,
                this._genomeBuilder);
            initialPositions.Add(genomePoint);

            return initialPositions;
        }

        /// <summary>
        /// Creates a valid <see cref="GenomeSearchPoint"/> with non-continuous parameters based on the values of the
        /// provided genome.
        /// </summary>
        /// <param name="genome">The genome to base the <see cref="GenomeSearchPoint"/> on.</param>
        /// <returns>
        /// A valid <see cref="GenomeSearchPoint"/>. Usually, its non-continuous parameters mirror that of
        /// <paramref name="genome"/>.
        /// </returns>
        /// <exception cref="TimeoutException"
        /// >Thrown if no valid point could be found in several iterations.
        /// </exception>
        private GenomeSearchPoint CreateValidSearchPointFromGenome(Genome genome)
        {
            GenomeSearchPoint point;

            // First, try to find a valid search point randomly.
            int trials = 0;
            do
            {
                point = GenomeSearchPoint.BaseRandomPointOnGenome(
                    genome,
                    this._parameterTree,
                    this._strategyConfiguration.MinimumDomainSize,
                    this._genomeBuilder);
                trials++;
            }
            while (!point.IsValid() && trials < 50);

            if (point.IsValid())
            {
                return point;
            }

            // If that does not work, try to repair the current point.
            // This might result in changed discrete parameters if the repair modifies them.
            LoggingHelper.WriteLine(
                VerbosityLevel.Warn,
                $"Did not find a valid point after 50 trials, now using a repair operation on {point}. If that changes discrete parameters, JADE performance may suffer.");

            var associatedGenome = point.Genome.CreateMutableGenome();
            this._genomeBuilder.MakeGenomeValid(associatedGenome);
            point = GenomeSearchPoint.CreateFromGenome(
                associatedGenome,
                this._parameterTree,
                this._strategyConfiguration.MinimumDomainSize,
                this._genomeBuilder);

            if (point.IsValid())
            {
                return point;
            }

            // Throw exception if no valid point was found.
            throw new TimeoutException(
                $"Could not find a valid point after 50 trials and a repair operation. Consider modifying your GenomeBuilder implementation.\nCurrent invalid point: {point}.");
        }

        /// <summary>
        /// Creates a new <see cref="IEnumerable{Genome}"/> based on the provided one which replaces
        /// a certain percentage of its members with the best search points returned by differential evolution.
        /// </summary>
        /// <param name="originalGenomes">The <see cref="IEnumerable{Genome}"/> to base the new one on.</param>
        /// <param name="mostRecentSorting">
        /// Most recent sorting found by the <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/> instance.
        /// </param>
        /// <param name="replacementRate">The replacement rate.</param>
        /// <returns>The newly created <see cref="IEnumerable{Genome}"/>.</returns>
        private IEnumerable<Genome> ReplaceSomeGenomesWithBestPoints(
            IReadOnlyList<Genome> originalGenomes,
            IList<GenomeSearchPoint> mostRecentSorting,
            double replacementRate)
        {
            // Round s. t. at least one genome will be replaced by a search point.
            var numberToReplace = (int)Math.Ceiling(replacementRate * originalGenomes.Count);
            var numberToKeep = originalGenomes.Count - numberToReplace;

            // Randomly choose correct number of original genomes.
            var randomizedOriginalGenomes =
                Randomizer.Instance.ChooseRandomSubset(originalGenomes, number: originalGenomes.Count).ToList();
            foreach (var originalGenome in randomizedOriginalGenomes.Take(numberToKeep))
            {
                yield return new Genome(originalGenome);
            }

            // When adding search points, make sure to keep the age structure intact.
            for (int i = 0; i < numberToReplace; i++)
            {
                var genome = new Genome(
                    genome: mostRecentSorting[i].Genome.CreateMutableGenome(),
                    age: randomizedOriginalGenomes[numberToKeep + i].Age);
                yield return genome;
            }
        }

        /// <summary>
        /// Creates a new <see cref="IEnumerable{Genome}"/> based on the provided one which replaces
        /// the original incumbent with the best search point returned by differential evolution.
        /// <p>If there was no original genome, a random genome is replaced.</p>
        /// </summary>
        /// <param name="originalGenomes">The <see cref="IEnumerable{Genome}"/> to base the new one on.</param>
        /// <param name="originalIncumbent">
        /// The original incumbent from the start of the phase. Might be <c>null</c>.
        /// </param>
        /// <param name="bestPoint">
        /// The best point found by the <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/> instance.
        /// </param>
        /// <returns>The newly created <see cref="IEnumerable{Genome}"/>.</returns>
        private IEnumerable<Genome> ReplaceIncumbentWithBestPoint(
            IReadOnlyList<Genome> originalGenomes,
            Genome originalIncumbent,
            GenomeSearchPoint bestPoint)
        {
            if (originalIncumbent == null)
            {
                throw new ArgumentNullException(
                    nameof(originalIncumbent),
                    "Cannot use incumbent improving strategy without an incumbent.");
            }

            // Find a genome looking like the original incumbent.
            var genomeToReplace = originalGenomes.First(
                genome =>
                    genome.IsEngineered.Equals(originalIncumbent.IsEngineered)
                    && genome.Age.Equals(originalIncumbent.Age)
                    && Genome.GenomeComparer.Equals(genome, originalIncumbent));

            // Use all other genomes.
            foreach (var originalGenome in originalGenomes)
            {
                if (object.ReferenceEquals(originalGenome, genomeToReplace))
                {
                    continue;
                }

                yield return new Genome(originalGenome);
            }

            // Add replacement for dropped genome and keep age structure.
            yield return new Genome(bestPoint.Genome.CreateMutableGenome(), genomeToReplace.Age);
        }

        #endregion
    }
}