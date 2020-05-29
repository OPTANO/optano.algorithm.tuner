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
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;

    /// <summary>
    /// Message describing one mini tournament that has to be executed.
    /// </summary>
    public class MiniTournament
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniTournament"/> class.
        /// </summary>
        /// <param name="participants">
        /// The tournament's participants.
        /// </param>
        /// <param name="tournamentId">
        /// The tournament id.
        /// </param>
        public MiniTournament(IEnumerable<ImmutableGenome> participants, int tournamentId)
        {
            // Verify parameter.
            if (participants == null)
            {
                throw new ArgumentNullException("participants");
            }

            // Set field.
            this.Participants = participants.ToList();
            this.MiniTournamentId = tournamentId;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the tournament's participants.
        /// </summary>
        public IEnumerable<ImmutableGenome> Participants { get; }

        /// <summary>
        /// Gets the mini tournament id.
        /// </summary>
        public int MiniTournamentId { get; private set; }

        #endregion
    }
}