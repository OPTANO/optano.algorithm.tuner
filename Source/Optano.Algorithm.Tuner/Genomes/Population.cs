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

namespace Optano.Algorithm.Tuner.Genomes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration;

    /// <summary>
    /// Population used in the Genetic Algorithm.
    /// </summary>
    public class Population
    {
        #region Fields

        /// <summary>
        /// Competitive part of the population.
        /// </summary>
        private readonly List<Genome> _competitive;

        /// <summary>
        /// Non-competitive part of the population.
        /// </summary>
        private readonly List<Genome> _nonCompetitive;

        /// <summary>
        /// The configuration.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _configuration;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Population" /> class.
        /// </summary>
        /// <param name="configuration">Configuration parameters.</param>
        public Population(AlgorithmTunerConfiguration configuration)
        {
            this._competitive = new List<Genome>(configuration.PopulationSize / 2);
            this._nonCompetitive = new List<Genome>(configuration.PopulationSize / 2);
            this._configuration = configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Population" /> class.
        /// Copy constructor for <see cref="Population"/> class.
        /// </summary>
        /// <param name="original">The <see cref="Population"/> object to copy.</param>
        public Population(Population original)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            this._competitive = original._competitive.Select(genome => new Genome(genome)).ToList();
            this._nonCompetitive = original._nonCompetitive.Select(genome => new Genome(genome)).ToList();
            this._configuration = original._configuration;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the total number of genomes in this <see cref="Population"/>.
        /// </summary>
        public int Count => this.CompetitiveCount + this.NonCompetitiveCount;

        /// <summary>
        /// Gets the number of competitive genomes.
        /// </summary>
        public int CompetitiveCount => this._competitive.Count;

        /// <summary>
        /// Gets the number of non-competitive genomes.
        /// </summary>
        public int NonCompetitiveCount => this._nonCompetitive.Count;

        /// <summary>
        /// Gets all <see cref="Genome"/>s of this <see cref="Population"/>, regardless of gender.
        /// </summary>
        public IEnumerable<Genome> AllGenomes => this._competitive.Union(this._nonCompetitive);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns the competitive part of the population.
        /// </summary>
        /// <returns>The competitive part of the population.</returns>
        public IReadOnlyList<Genome> GetCompetitiveIndividuals()
        {
            return this._competitive.AsReadOnly();
        }

        /// <summary>
        /// Returns the non competitive part of the population.
        /// </summary>
        /// <returns>The non competitive part of the population.</returns>
        public IReadOnlyList<Genome> GetNonCompetitiveMates()
        {
            return this._nonCompetitive.AsReadOnly();
        }

        /// <summary>
        /// Ages each individual and removes the ones that exceed <see cref="AlgorithmTunerConfiguration.MaxGenomeAge"/>.
        /// </summary>
        public void Age()
        {
            // Go through competitive population.
            // Traverse in reverse to make removals possible.
            for (var i = this._competitive.Count - 1; i >= 0; i--)
            {
                this._competitive[i].AgeOnce();
                if (this._competitive[i].Age > this._configuration.MaxGenomeAge)
                {
                    this._competitive.RemoveAt(i);
                }
            }

            // Do the same for non competitive part of the population.
            for (var i = this._nonCompetitive.Count - 1; i >= 0; i--)
            {
                this._nonCompetitive[i].AgeOnce();
                if (this._nonCompetitive[i].Age > this._configuration.MaxGenomeAge)
                {
                    this._nonCompetitive.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Replaces <see cref="AlgorithmTunerConfiguration.PopulationMutantRatio"/> percent of the non competitive population with new, random individuals.
        /// Replacement is happening randomly.
        /// The new individuals have the same age distribution as the replaced ones.
        /// </summary>
        /// <param name="genomeBuilder"><see cref="GenomeBuilder"/> to build the random individuals.</param>
        public void ReplaceIndividualsWithMutants(GenomeBuilder genomeBuilder)
        {
            var numToReplace = (int)Math.Ceiling(this._configuration.PopulationMutantRatio * this._nonCompetitive.Count);
            var replacementIndices = Randomizer.Instance.ChooseRandomSubset(Enumerable.Range(0, this._nonCompetitive.Count), numToReplace);

            // since we only replace in non-competitives, the population best cannot be replaced by accident.
            foreach (var index in replacementIndices)
            {
                var oldIndividual = this._nonCompetitive[index];
                this._nonCompetitive[index] = genomeBuilder.CreateRandomGenome(oldIndividual.Age);
            }
        }

        /// <summary>
        /// Add genome to population.
        /// </summary>
        /// <param name="genome">The genome.</param>
        /// <param name="isCompetitive">Whether it should be added to the competive population part or not.</param>
        public void AddGenome(Genome genome, bool isCompetitive)
        {
            if (isCompetitive)
            {
                this._competitive.Add(genome);
            }
            else
            {
                this._nonCompetitive.Add(genome);
            }
        }

        /// <summary>
        /// Returns whether the population is empty.
        /// </summary>
        /// <returns>True if the population is empty.</returns>
        public bool IsEmpty()
        {
            return !this._competitive.Any() && !this._nonCompetitive.Any();
        }

        #endregion
    }
}