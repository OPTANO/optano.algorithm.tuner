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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.GeneticAlgorithm
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.GeneticAlgorithm;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.Tests.Serialization;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="GgaStatus"/>.
    /// </summary>
    public class GgaStatusTest : StatusBaseTest<GgaStatus>
    {
        #region Fields

        /// <summary>
        /// An iteration counter that can be used in tests.
        /// </summary>
        private readonly int _iterationCounter = 34;

        /// <summary>
        /// An incumbent kept counter that can be used in tests.
        /// </summary>
        private readonly int _incumbentKeptCounter = 1;

        /// <summary>
        /// Population that can be used in tests.
        /// </summary>
        private Population _population;

        /// <summary>
        /// Configuration that can be used in tests.
        /// </summary>
        private AlgorithmTunerConfiguration _configuration;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a path to which the status file will get written in tests.
        /// </summary>
        protected override string StatusFilePath =>
            PathUtils.GetAbsolutePathFromExecutableFolderRelative(Path.Combine("status", GgaStatus.FileName));

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentNullException"/> if no ranks are provided.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingRanks()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GgaStatus(
                    this._population,
                    this._iterationCounter,
                    this._incumbentKeptCounter,
                    allKnownRanks: null));
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentOutOfRangeException"/> if a negative number is
        /// provided for the iteration counter.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnNegativeIterationCounter()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GgaStatus(
                    this._population,
                    iterationCounter: -1,
                    incumbentKeptCounter: this._incumbentKeptCounter,
                    allKnownRanks: new Dictionary<Genome, List<GenomeTournamentRank>>()));
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentOutOfRangeException"/> if a negative number is
        /// provided for the incumbent kept counter.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnNegativeIncumbentKeptCounter()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GgaStatus(
                    this._population,
                    iterationCounter: this._iterationCounter,
                    incumbentKeptCounter: -1,
                    allKnownRanks: new Dictionary<Genome, List<GenomeTournamentRank>>()));
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentOutOfRangeException"/> if the incumbent kept
        /// counter is greater than the iteration counter.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnIncumbentKeptCounterGreaterThanIterationCounter()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GgaStatus(
                    this._population,
                    iterationCounter: 4,
                    incumbentKeptCounter: 5,
                    allKnownRanks: new Dictionary<Genome, List<GenomeTournamentRank>>()));
        }

        /// <summary>
        /// Checks that <see cref="GgaStatus.Population"/>
        /// returns the population provided on initialization.
        /// </summary>
        [Fact]
        public void PopulationIsSetCorrectly()
        {
            var status = new GgaStatus(
                this._population,
                this._iterationCounter,
                this._incumbentKeptCounter,
                new Dictionary<Genome, List<GenomeTournamentRank>>());
            Assert.Equal(
                this._population,
                status.Population);
        }

        /// <summary>
        /// Checks that <see cref="GgaStatus.IterationCounter"/>
        /// returns the number provided on initialization.
        /// </summary>
        [Fact]
        public void IterationCounterIsSetCorrectly()
        {
            var status = new GgaStatus(
                this._population,
                13,
                this._incumbentKeptCounter,
                new Dictionary<Genome, List<GenomeTournamentRank>>());
            Assert.Equal(
                13,
                status.IterationCounter);
        }

        /// <summary>
        /// Checks that <see cref="GgaStatus.AllKnownRanks"/>
        /// returns the ranks provided on initialization.
        /// </summary>
        [Fact]
        public void AllKnownRanksAreSetCorrectly()
        {
            var ranks = new Dictionary<Genome, List<GenomeTournamentRank>>();
            var status = new GgaStatus(this._population, this._iterationCounter, this._incumbentKeptCounter, ranks);
            Assert.Equal(
                ranks,
                status.AllKnownRanks);
        }

        /// <summary>
        /// Checks that <see cref="StatusBase.ReadFromFile{Status}"/> correctly deserializes a
        /// status object written to file by <see cref="StatusBase.WriteToFile"/>.
        /// </summary>
        [Fact]
        public override void ReadFromFileDeserializesCorrectly()
        {
            /* Create status. */
            /* (1) population */
            var competitiveGenome = new Genome(2);
            competitiveGenome.SetGene("a", new Allele<int>(6));
            var nonCompetitiveGenome = new Genome(1);
            nonCompetitiveGenome.SetGene("b", new Allele<string>("oh"));
            this._population.AddGenome(competitiveGenome, isCompetitive: true);
            this._population.AddGenome(nonCompetitiveGenome, isCompetitive: false);
            // (2) counters
            int counter = 12;
            int stagnation = 7;
            /* (3) ranks */
            var result = new GenomeTournamentRank
                             {
                                 TournamentRank = 2,
                                 GenerationId = 24,
                                 TournamentId = 11,
                             };
            var results = new List<GenomeTournamentRank> { result };
            var ranks = new Dictionary<Genome, List<GenomeTournamentRank>> { { new Genome(), results } };
            var status = new GgaStatus(this._population, counter, stagnation, ranks);

            /* Write and read it from file. */
            status.WriteToFile(this.StatusFilePath);
            var deserializedStatus = StatusBase.ReadFromFile<GgaStatus>(this.StatusFilePath);

            /* Check it's still the same. */
            /* (a) population */
            Assert.Equal(
                1,
                deserializedStatus.Population.GetCompetitiveIndividuals().Count);
            var deserializedCompetitiveGenome = deserializedStatus.Population.GetCompetitiveIndividuals().First();
            Assert.True(
                Genome.GenomeComparer.Equals(competitiveGenome, deserializedCompetitiveGenome),
                "Expected different competive genome.");
            Assert.Equal(
                competitiveGenome.Age,
                deserializedCompetitiveGenome.Age);
            Assert.Equal(
                1,
                deserializedStatus.Population.GetNonCompetitiveMates().Count);
            var deserializedNonCompetitiveGenome = deserializedStatus.Population.GetNonCompetitiveMates().First();
            Assert.True(
                Genome.GenomeComparer.Equals(nonCompetitiveGenome, deserializedNonCompetitiveGenome),
                "Expected different non-competive genome.");
            Assert.Equal(
                nonCompetitiveGenome.Age,
                deserializedNonCompetitiveGenome.Age);
            // counters
            Assert.Equal(
                counter,
                deserializedStatus.IterationCounter);
            Assert.Equal(
                stagnation,
                deserializedStatus.IncumbentKeptCounter);
            /* (c) ranks */
            var singleResult = deserializedStatus.AllKnownRanks.Single().Value.Single();
            Assert.Equal(result.GenerationId, singleResult.GenerationId);
            Assert.Equal(result.TournamentId, singleResult.TournamentId);
            Assert.Equal(result.TournamentRank, singleResult.TournamentRank);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called before every test case.
        /// </summary>
        protected override void InitializeDefault()
        {
            this._configuration = this.GetDefaultAlgorithmTunerConfiguration();
            this._population = new Population(this._configuration);
        }

        /// <summary>
        /// Creates a status object which can be (de)serialized successfully.
        /// </summary>
        /// <returns>The created object.</returns>
        protected override GgaStatus CreateTestStatus()
        {
            return new GgaStatus(
                this._population,
                this._iterationCounter,
                this._incumbentKeptCounter,
                new Dictionary<Genome, List<GenomeTournamentRank>>());
        }

        #endregion
    }
}