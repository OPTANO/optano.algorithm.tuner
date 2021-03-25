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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// The tournament winners with rank.
    /// </summary>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    public class TournamentWinnersWithRank<TResult>
        where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TournamentWinnersWithRank{TResult}"/> class.
        /// </summary>
        /// <param name="competitiveParents">
        /// The competitive parents ordered by fitness.
        /// </param>
        /// <param name="generationBest">
        /// The incumbent genome.
        /// </param>
        /// <param name="generationBestResult">
        /// The result of the incumbent genome.
        /// </param>
        /// <param name="genomeToTournamentRank">
        /// Genome with a rank for every tournament it participated in.
        /// </param>
        public TournamentWinnersWithRank(
            IEnumerable<ImmutableGenome> competitiveParents,
            ImmutableGenome generationBest,
            ImmutableList<TResult> generationBestResult,
            ImmutableDictionary<ImmutableGenome, List<GenomeTournamentRank>> genomeToTournamentRank)
        {
            this.CompetitiveParents = competitiveParents.Select(g => g.CreateMutableGenome()).ToImmutableList();
            this.GenerationBest = generationBest.CreateMutableGenome();
            this.GenerationBestResult = generationBestResult;

            // Since genomeToTournamentRank also used an allele-based equality comparer, the keys should be unique!
            this.GenomeToTournamentRank = genomeToTournamentRank.ToDictionary(
                g => g.Key.CreateMutableGenome(),
                g => g.Value,
                Genome.GenomeComparer);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the competitive parents.
        /// </summary>
        public ImmutableList<Genome> CompetitiveParents { get; }

        /// <summary>
        /// Gets the generation best.
        /// </summary>
        public Genome GenerationBest { get; }

        /// <summary>
        /// Gets the generation best result.
        /// </summary>
        public ImmutableList<TResult> GenerationBestResult { get; }

        /// <summary>
        /// Gets a dictionary containing the observed tournament results, grouped by <see cref="Genome"/>.
        /// </summary>
        public Dictionary<Genome, List<GenomeTournamentRank>> GenomeToTournamentRank { get; }

        #endregion
    }
}