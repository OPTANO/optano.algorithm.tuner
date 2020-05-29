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
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="SelectCommand{TInstance}"/> class.
    /// </summary>
    public class SelectCommandTest
    {
        #region Static Fields

        /// <summary>
        /// Empty list of <see cref="TestInstance"/>s.
        /// </summary>
        private static readonly List<TestInstance> emptyInstancesList = new List<TestInstance>();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="SelectCommand{I}"/>'s constructor without providing 
        /// participants throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingParticipants()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SelectCommand<TestInstance>(
                participants: null,
                instances: SelectCommandTest.emptyInstancesList,
                currentGeneration: 0));
        }

        /// <summary>
        /// Verifies that calling <see cref="SelectCommand{I}"/>'s constructor without providing 
        /// instances throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingInstances()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SelectCommand<TestInstance>(
                    participants: new List<ImmutableGenome>(),
                    instances: null,
                    currentGeneration: 0));
        }

        /// <summary>
        /// Checks that <see cref="SelectCommand{I}.Participants"/> returns the genomes provided on initialization.
        /// </summary>
        [Fact]
        public void ParticipantsAreSetCorrectly()
        {
            // Set participants in constructor.
            var participants = new List<ImmutableGenome>() { new ImmutableGenome(new Genome()) };
            var message = new SelectCommand<TestInstance>(participants, SelectCommandTest.emptyInstancesList, currentGeneration: 0);

            // Check message's participants are equal to the original ones.
            Assert.True(
                participants.SequenceEqual(message.Participants),
                "Some participant has different gene values than the ones that have been provided in the constructor.");
        }

        /// <summary>
        /// Checks that <see cref="SelectCommand{I}.Instances"/> returns the instances that were provided on
        /// initialization. 
        /// </summary>
        [Fact]
        public void InstancesAreSetCorrectly()
        {
            // Set instances in constructor.
            var instances = new List<TestInstance>() { new TestInstance("test") };
            var message = new SelectCommand<TestInstance>(
                participants: new List<ImmutableGenome>(),
                instances: instances,
                currentGeneration: 0);

            // Check message's instances are the original ones.
            Assert.True(instances.SequenceEqual(message.Instances), "Instances were not set correctly.");
        }

        /// <summary>
        /// Checks that modifying the participant list <see cref="SelectCommand{I}"/> was initialized with does not 
        /// modify the message.
        /// </summary>
        [Fact]
        public void ParticipantCollectionIsImmutable()
        {
            // Create message.
            var originalParticipants = new List<ImmutableGenome>() { new ImmutableGenome(new Genome()) };
            var message = new SelectCommand<TestInstance>(originalParticipants, SelectCommandTest.emptyInstancesList, currentGeneration: 0);

            // Precondition: Same genes.
            Assert.True(originalParticipants.SequenceEqual(message.Participants));

            // Change original participants.
            originalParticipants.RemoveAt(0);

            // Check genes are now different.
            Assert.False(
                originalParticipants.SequenceEqual(message.Participants),
                "List of participants was changed even if message is supposed to be immutable.");
        }

        /// <summary>
        /// Checks that modifying the instance list <see cref="SelectCommand{I}"/> was initialized with does not 
        /// modify the message.
        /// </summary>
        [Fact]
        public void InstanceCollectionIsImmutable()
        {
            // Create message.
            var originalInstances = new List<TestInstance>() { new TestInstance("test") };
            var message = new SelectCommand<TestInstance>(
                participants: new List<ImmutableGenome>(),
                instances: originalInstances,
                currentGeneration: 0);

            // Precondition: Same instances.
            Assert.True(originalInstances.SequenceEqual(message.Instances));

            // Change original instances.
            originalInstances.RemoveAt(0);

            // Check instances are now different.
            Assert.False(
                originalInstances.SequenceEqual(message.Instances),
                "List of instances was changed even if message is supposed to be immutable.");
        }

        #endregion
    }
}
