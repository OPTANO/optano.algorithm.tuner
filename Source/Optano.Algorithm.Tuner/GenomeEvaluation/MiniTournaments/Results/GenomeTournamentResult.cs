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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results
{
    /// <summary>
    /// A genome tournament result.
    /// </summary>
    public class GenomeTournamentResult
    {
        #region Public properties

        /// <summary>
        /// Gets or sets the genome's rank within the tournament with <see cref="TournamentId"/>.
        /// </summary>
        public double TournamentRank { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the tournament this result was obtained in.
        /// </summary>
        public int TournamentId { get; set; }

        /// <summary>
        /// Gets or sets the generation in which the <see cref="TournamentId"/> was executed.
        /// </summary>
        public int Generation { get; set; }

        #endregion
    }
}