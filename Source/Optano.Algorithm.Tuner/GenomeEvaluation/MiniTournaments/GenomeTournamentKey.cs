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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments
{
    using System;

    using Optano.Algorithm.Tuner.Genomes;

    /// <summary>
    /// A unique key, containing a genome and a tournament id.
    /// </summary>
    public class GenomeTournamentKey
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeTournamentKey"/> class.
        /// </summary>
        /// <param name="genome">The genome.</param>
        /// <param name="tournamentId">The tournament id.</param>
        public GenomeTournamentKey(ImmutableGenome genome, int tournamentId)
        {
            this.Genome = genome;
            this.TournamentId = tournamentId;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome.
        /// </summary>
        public ImmutableGenome Genome { get; }

        /// <summary>
        /// Gets the tournament id.
        /// </summary>
        public int TournamentId { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks if equal.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>True, if equal.</returns>
        public static bool operator ==(GenomeTournamentKey left, GenomeTournamentKey right)
        {
            return object.Equals(left, right);
        }

        /// <summary>
        /// Checks if unequal.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>True, if unequal.</returns>
        public static bool operator !=(GenomeTournamentKey left, GenomeTournamentKey right)
        {
            return !object.Equals(left, right);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(null, obj))
            {
                return false;
            }

            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((GenomeTournamentKey)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.TournamentId, ImmutableGenome.GenomeComparer.GetHashCode(this.Genome));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks if equal.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>True, if equal.</returns>
        protected bool Equals(GenomeTournamentKey other)
        {
            return this.TournamentId == other.TournamentId
                   && ImmutableGenome.GenomeComparer.Equals(this.Genome, other.Genome);
        }

        #endregion
    }
}