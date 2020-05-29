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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.MiniTournaments.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Messages;
    using Optano.Algorithm.Tuner.Genomes;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="MiniTournament"/> class.
    /// </summary>
    public class MiniTournamentTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="MiniTournament"/>'s constructor without providing 
        /// participants throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingParticipants()
        {
            Assert.Throws<ArgumentNullException>(() => new MiniTournament(participants: null, tournamentId: 0));
        }

        /// <summary>
        /// Checks that <see cref="MiniTournament.Participants"/> returns the genomes provided on initialization.
        /// </summary>
        [Fact]
        public void ParticipantsAreSetCorrectly()
        {
            // Set participants in constructor.
            var participants = new List<ImmutableGenome>() { new ImmutableGenome(new Genome()) };
            var message = new MiniTournament(participants, tournamentId: 0);

            // Check gene values of messages' participants are equal to the original ones.
            Assert.True(
                participants.SequenceEqual(message.Participants),
                "Some participant has different gene values than the ones that have been provided in the constructor.");
        }

        /// <summary>
        /// Checks that modifying the participant list <see cref="MiniTournament"/> was initialized with does not 
        /// modify the message.
        /// </summary>
        [Fact]
        public void ParticipantCollectionIsImmutable()
        {
            // Create message.
            var originalParticipants = new List<ImmutableGenome>() { new ImmutableGenome(new Genome()) };
            var message = new MiniTournament(originalParticipants, tournamentId: 0);

            // Precondition: Same genes.
            Assert.True(originalParticipants.SequenceEqual(message.Participants));

            // Change original participants.
            originalParticipants.RemoveAt(0);

            // Check genes are now different.
            Assert.False(
                originalParticipants.SequenceEqual(message.Participants),
                "Participants have been changed even if message is supposed to be immutable.");
        }

        #endregion
    }
}