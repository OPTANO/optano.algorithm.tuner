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
    using System;
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// The mini tournament result.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    public class MiniTournamentResult<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniTournamentResult{TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="miniTournamentId">
        /// The mini tournament id.
        /// </param>
        /// <param name="winnerStats">
        /// The <see cref="ImmutableGenomeStats{TInstance,TResult}"/> of the mini tournament's winners.
        /// </param>
        /// <param name="genomeToTournamentRank">
        /// The genome to tournament rank dictionary.
        /// </param>
        public MiniTournamentResult(
            int miniTournamentId,
            IReadOnlyList<ImmutableGenomeStats<TInstance, TResult>> winnerStats,
            IReadOnlyDictionary<ImmutableGenome, IReadOnlyList<GenomeTournamentRank>> genomeToTournamentRank)
        {
            this.MiniTournamentId = miniTournamentId;
            this.WinnerStats = winnerStats ?? throw new ArgumentNullException(nameof(winnerStats));
            this.GenomeToTournamentRank = genomeToTournamentRank ?? throw new ArgumentNullException(nameof(genomeToTournamentRank));
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the mini tournament id.
        /// </summary>
        public int MiniTournamentId { get; }

        /// <summary>
        /// Gets the <see cref="ImmutableGenomeStats{TInstance,TResult}"/> of the mini tournament's winners.
        /// </summary>
        public IReadOnlyList<ImmutableGenomeStats<TInstance, TResult>> WinnerStats { get; }

        /// <summary>
        /// Gets the genome to tournament rank dictionary.
        /// </summary>
        public IReadOnlyDictionary<ImmutableGenome, IReadOnlyList<GenomeTournamentRank>> GenomeToTournamentRank { get; }

        #endregion
    }
}