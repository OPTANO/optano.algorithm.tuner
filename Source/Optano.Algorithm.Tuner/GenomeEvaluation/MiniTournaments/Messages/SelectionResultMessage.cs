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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Message representing the best genomes that have been found in a number of mini tournaments,
    /// sorted by fitness.
    /// </summary>
    /// <typeparam name="TResult">
    /// Type of target algorithm run result.
    /// </typeparam>
    public class SelectionResultMessage<TResult>
        where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionResultMessage{TResult}" /> class.
        /// </summary>
        /// <param name="orderedGenomes">
        /// The best genomes found, ordered by fitness.
        /// </param>
        /// <param name="genomeToRank">
        /// Genome with a rank for every tournament it participated in.
        /// </param>
        /// <param name="generationBest">
        /// The incumbent genome.
        /// </param>
        /// <param name="generationBestResult">
        /// The result of the incumbent genome.
        /// </param>
        public SelectionResultMessage(
            ImmutableList<ImmutableGenome> orderedGenomes,
            Dictionary<ImmutableGenome, List<GenomeTournamentResult>> genomeToRank,
            ImmutableGenome generationBest,
            IEnumerable<TResult> generationBestResult)
        {
            // Verify parameter.
            if (orderedGenomes == null)
            {
                throw new ArgumentNullException("orderedGenomes");
            }

            if (ReferenceEquals(generationBest, null))
            {
                throw new ArgumentNullException(nameof(generationBest));
            }

            Debug.Assert(
                new ImmutableGenome.GeneValueComparer().Equals(orderedGenomes.FirstOrDefault(), generationBest),
                "Incumbent should be in first position of ordered genome list.");

            this.CompetitiveParents = orderedGenomes.ToImmutableList();
            this.GenomeToRank = genomeToRank.ToImmutableDictionary(new ImmutableGenome.GeneValueComparer());
            this.GenerationBest = generationBest;
            this.GenerationBestResult = generationBestResult.ToImmutableList();
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the best genome in the current generation.
        /// </summary>
        public ImmutableGenome GenerationBest { get; }

        /// <summary>
        /// Gets the actual run result of the generation's best <see cref="Genome"/>.
        /// Used for statistical evaluation.
        /// </summary>
        public ImmutableList<TResult> GenerationBestResult { get; }

        /// <summary>
        /// Gets genomes found in a number of mini tournaments, sorted by fitness.
        /// Only contains the winner(s) of each tournament, i.e. the genomes that are allowed to reproduce.
        /// </summary>
        public ImmutableList<ImmutableGenome> CompetitiveParents { get; }

        /// <summary>
        /// Gets the known ranks for each genome.
        /// If a Genome (or an 'equal copy') competed in several tournaments, we will observe several ranks for it.
        /// This Dictionary uses an <see cref="ImmutableGenome.GeneValueComparer"/> to group those results.
        /// </summary>
        public ImmutableDictionary<ImmutableGenome, List<GenomeTournamentResult>> GenomeToRank { get; }

        #endregion
    }
}