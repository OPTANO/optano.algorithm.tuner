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

    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="MiniTournamentResult{TResult}"/> class.
    /// </summary>
    public class MiniTournamentResultTest
    {
        #region Fields

        /// <summary>
        /// <see cref="ImmutableGenome"/>s used as mini tournament winners in test.
        /// </summary>
        private readonly List<ImmutableGenome> _winners;

        /// <summary>
        /// Total ordering of a number of <see cref="ImmutableGenome"/>s which act as tournament participants in tests.
        /// The first ones equal <see cref="_winners"/>.
        /// </summary>
        private readonly List<ImmutableGenome> _allFinishedOrdered;

        /// <summary>
        /// Contains results for all <see cref="ImmutableGenome"/>s stored in <see cref="_winners"/>.
        /// </summary>
        private readonly Dictionary<ImmutableGenome, ImmutableList<TestResult>> _winnerResults;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniTournamentResultTest"/> class.
        /// </summary>
        public MiniTournamentResultTest()
        {
            // Create genome to initialize a number of immutable genomes.
            var genomeWithValue = new Genome();
            genomeWithValue.SetGene("a", new Allele<int>(0));

            this._winners = new List<ImmutableGenome>
                               {
                                   new ImmutableGenome(genomeWithValue),
                                   new ImmutableGenome(genomeWithValue),
                               };

            this._allFinishedOrdered = new List<ImmutableGenome>(this._winners);
            for (int i = 1; i < 7; i++)
            {
                genomeWithValue.SetGene("a", new Allele<int>(i));
                this._allFinishedOrdered.Add(new ImmutableGenome(genomeWithValue));
            }

            this._winnerResults = new Dictionary<ImmutableGenome, ImmutableList<TestResult>>();
            foreach (var genome in this._winners)
            {
                var results = new List<TestResult>();
                for (int i = 0; i < 4; i++)
                {
                    results.Add(new TestResult(i));
                }

                this._winnerResults[genome] = results.ToImmutableList();
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="MiniTournamentResult{TResult}"/>'s constructor without providing 
        /// a total ordering throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingTotalOrdering()
        {
            Assert.Throws<ArgumentNullException>(
                () => new MiniTournamentResult<TestResult>(
                    0,
                    allFinishedOrdered: null,
                    winnerResults: this._winnerResults));
        }

        /// <summary>
        /// Verifies that calling <see cref="MiniTournamentResult{TResult}"/>'s constructor without providing 
        /// results throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingResults()
        {
            Assert.Throws<ArgumentNullException>(
                () => new MiniTournamentResult<TestResult>(0, this._allFinishedOrdered, winnerResults: null));
        }

        /// <summary>
        /// Checks that <see cref="MiniTournamentResult{TResult}.MiniTournamentId"/> returns the ID provided on
        /// initialization.
        /// </summary>
        [Fact]
        public void TournamentIdIsSetCorrectly()
        {
            var result = new MiniTournamentResult<TestResult>(5, this._allFinishedOrdered, this._winnerResults);
            Assert.Equal(5, result.MiniTournamentId);
        }

        /// <summary>
        /// Checks that <see cref="MiniTournamentResult{TResult}.AllFinishedOrdered"/> returns the order provided on
        /// initialization.
        /// </summary>
        [Fact]
        public void AllFinishedOrderedIsSetCorrectly()
        {
            var result = new MiniTournamentResult<TestResult>(5, this._allFinishedOrdered, this._winnerResults);
            Assert.Equal(this._allFinishedOrdered, result.AllFinishedOrdered);
        }

        /// <summary>
        /// Checks that <see cref="MiniTournamentResult{TResult}.WinnerResults"/> returns the results provided on
        /// initialization.
        /// </summary>
        [Fact]
        public void WinnerResultsIsSetCorrectly()
        {
            var result = new MiniTournamentResult<TestResult>(5, this._allFinishedOrdered, this._winnerResults);
            var results = result.WinnerResults;
            Assert.Equal(this._winners.Count, results.Count);
            foreach (var winner in this._winners)
            {
                Console.WriteLine($"Checking results for {winner}...");
                Assert.Equal(this._winnerResults[winner], results[winner]);
            }
        }

        /// <summary>
        /// Checks that <see cref="MiniTournamentResult{TResult}.NumberOfParticipants"/> returns the correct number of
        /// participants.
        /// </summary>
        [Fact]
        public void NumberOfParticipantsIsComputedCorrectly()
        {
            var result = new MiniTournamentResult<TestResult>(5, this._allFinishedOrdered, this._winnerResults);
            Assert.True(
                this._allFinishedOrdered.Count == result.NumberOfParticipants,
                "Number of participants should equal the number of genomes in total ordering.");
        }

        /// <summary>
        /// Checks that changes to the sets provided on initalization do not change
        /// <see cref="MiniTournamentResult{TResult}"/>.
        /// </summary>
        [Fact]
        public void MessageIsImmutable()
        {
            var result = new MiniTournamentResult<TestResult>(5, this._allFinishedOrdered, this._winnerResults);

            // Properties are immutable by type, so just check what happens if mutable parts of input are changed.
            this._allFinishedOrdered.Clear();
            this._winnerResults.Clear();

            // The message should now be different from the input.
            Assert.NotEqual(
                this._allFinishedOrdered.Count,
                result.AllFinishedOrdered.Count);
            Assert.NotEqual(
                this._winnerResults.Count,
                result.WinnerResults.Count);
        }

        #endregion
    }
}