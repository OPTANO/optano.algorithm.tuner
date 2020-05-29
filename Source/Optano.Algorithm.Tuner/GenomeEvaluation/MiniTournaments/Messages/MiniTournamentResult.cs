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

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Message containing ranking of a <see cref="MiniTournament"/> and the results the winners produced.
    /// </summary>
    /// <typeparam name="TResult">Type of the produced results.</typeparam>
    public class MiniTournamentResult<TResult>
        where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniTournamentResult{TResult}"/> class.
        /// </summary>
        /// <param name="tournamentId">
        /// The tournament id.
        /// </param>
        /// <param name="allFinishedOrdered">
        /// Total ordering of the participants.
        /// </param>
        /// <param name="winnerResults">
        /// All observed results for each winner.
        /// </param>
        public MiniTournamentResult(
            int tournamentId,
            IEnumerable<ImmutableGenome> allFinishedOrdered,
            Dictionary<ImmutableGenome, ImmutableList<TResult>> winnerResults)
        {
            // Verify parameter.
            if (allFinishedOrdered == null)
            {
                throw new ArgumentNullException(nameof(allFinishedOrdered));
            }

            if (ReferenceEquals(winnerResults, null))
            {
                throw new ArgumentNullException(nameof(winnerResults));
            }

            this.MiniTournamentId = tournamentId;

            // Put genomes into an immutable collections to make the message immutable.
            this.AllFinishedOrdered = allFinishedOrdered.ToImmutableList();
            this.WinnerResults = winnerResults.ToImmutableDictionary();
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the mini tournament id.
        /// </summary>
        public int MiniTournamentId { get; }

        /// <summary>
        /// Gets the number of participants.
        /// </summary>
        public int NumberOfParticipants => this.AllFinishedOrdered.Count;

        /// <summary>
        /// Gets the total ordering of the participants.
        /// </summary>
        public ImmutableList<ImmutableGenome> AllFinishedOrdered { get; }

        /// <summary>
        /// Gets the mini tournament's winners and the results they produced.
        /// </summary>
        public ImmutableDictionary<ImmutableGenome, ImmutableList<TResult>> WinnerResults { get; }

        #endregion
    }
}