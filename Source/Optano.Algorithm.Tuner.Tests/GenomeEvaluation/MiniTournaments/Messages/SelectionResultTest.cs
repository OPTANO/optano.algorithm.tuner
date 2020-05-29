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
    using System.Collections.Immutable;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="SelectionResultMessage{TResult}"/> class.
    /// </summary>
    public class SelectionResultTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="SelectionResultMessage{TResult}"/>'s constructor without providing 
        /// orderedGenomes throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingGenomes()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SelectionResultMessage<TestResult>(
                    orderedGenomes: null,
                    genomeToRank: new Dictionary<ImmutableGenome, List<GenomeTournamentResult>>(),
                    generationBest: null,
                    generationBestResult: null));
        }

        /// <summary>
        /// Checks that <see cref="SelectionResultMessage{TResult}.CompetitiveParents"/> returns the genomes provided on initialization.
        /// </summary>
        [Fact]
        public void GenomesAreSetCorreclty()
        {
            // Set genomes in constructor.
            var genomes = new List<ImmutableGenome>() { new ImmutableGenome(new Genome()) }.ToImmutableList();
            var message = new SelectionResultMessage<TestResult>(
                genomes,
                new Dictionary<ImmutableGenome, List<GenomeTournamentResult>>(),
                genomes[0],
                new[] { new TestResult(TimeSpan.FromMilliseconds(1)) });

            // Check message's genomes are equal to the original ones.
            Assert.True(
                genomes.SequenceEqual(message.CompetitiveParents),
                "Some genome has different gene values than the ones that have been provided in the constructor.");
        }

        /// <summary>
        /// Checks that modifying the genome list <see cref="SelectionResultMessage{TResult}"/> was initialized with does not 
        /// modify the message.
        /// </summary>
        [Fact]
        public void GenomeCollectionIsImmutable()
        {
            // Create message.
            var originalGenomes = new List<ImmutableGenome>() { new ImmutableGenome(new Genome()) };
            var message = new SelectionResultMessage<TestResult>(
                originalGenomes.ToImmutableList(),
                new Dictionary<ImmutableGenome, List<GenomeTournamentResult>>(),
                originalGenomes[0],
                new[] { new TestResult(TimeSpan.FromMilliseconds(1)) });

            // Precondition: Same genes.
            Assert.True(originalGenomes.SequenceEqual(message.CompetitiveParents));

            // Change original genomes.
            originalGenomes.RemoveAt(0);

            // Check genes are now different.
            Assert.False(
                originalGenomes.SequenceEqual(message.CompetitiveParents),
                "List of orderedGenomes was changed even if message is supposed to be immutable.");
        }

        #endregion
    }
}