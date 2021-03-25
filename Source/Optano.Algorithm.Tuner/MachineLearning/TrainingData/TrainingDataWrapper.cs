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

namespace Optano.Algorithm.Tuner.MachineLearning.TrainingData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;

    using SharpLearning.Containers.Matrices;

    /// <summary>
    /// Wrapper class that holds all observed training data and related auxillary information.
    /// </summary>
    public class TrainingDataWrapper
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TrainingDataWrapper"/> class.
        /// </summary>
        /// <param name="tournamentResults">
        /// The tournament results.
        /// </param>
        /// <param name="currentGeneration">
        /// The current generation.
        /// </param>
        public TrainingDataWrapper(Dictionary<Genome, List<GenomeTournamentRank>> tournamentResults, int currentGeneration)
        {
            if (currentGeneration < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentGeneration));
            }

            this.TournamentResults = tournamentResults ?? throw new ArgumentNullException(nameof(tournamentResults));
            this.CurrentGeneration = currentGeneration;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets the <see cref="T:double[]"/>-representation of <see cref="Genomes"/>. 
        /// Will be computed by <see cref="GenomeTransformation{TCategoricalEncoding}"/>, called by <see cref="GeneticEngineering{TLearnerModel, TPredictorModel, TSamplingStrategy}"/>.
        /// </summary>
        public F64Matrix ConvertedGenomes { get; set; }

        /// <summary>
        /// Gets a dictionary that holds all observed <see cref="GenomeTournamentRank"/>s for each <see cref="Genome"/>.
        /// </summary>
        public Dictionary<Genome, List<GenomeTournamentRank>> TournamentResults { get; }

        /// <summary>
        /// Gets all known genomes.
        /// </summary>
        public IEnumerable<Genome> Genomes => this.TournamentResults.Keys;

        /// <summary>
        /// Gets the generation during which the data snapshot was taken.
        /// </summary>
        public int CurrentGeneration { get; }

        /// <summary>
        /// Gets the number of <see cref="Genome"/>s.
        /// <c>NOT</c> the <see cref="TotalObservationCount"/>.
        /// </summary>
        public int Count => this.TournamentResults.Count;

        /// <summary>
        /// Gets the total number of observations stored in <see cref="TrainingDataWrapper"/>.
        /// </summary>
        public int TotalObservationCount
        {
            get
            {
                return (int)this.TournamentResults.Sum(t => t.Value.Count);
            }
        }

        #endregion
    }
}