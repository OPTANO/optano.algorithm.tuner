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

namespace Optano.Algorithm.Tuner.Tests.ContinuousOptimization.DifferentialEvolution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.Tests.Serialization;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="DifferentialEvolutionStatus{TSearchPoint}"/> class.
    /// </summary>
    public class DifferentialEvolutionStatusTest : StatusBaseTest<DifferentialEvolutionStatus<SearchPoint>>
    {
        #region Fields

        /// <summary>
        /// Population used in tests.
        /// </summary>
        private readonly List<SearchPoint> _sortedPopulation = new List<SearchPoint>();

        /// <summary>
        /// The generation used in tests.
        /// </summary>
        private readonly int _generation = 12;

        /// <summary>
        /// Maximum generation number used in tests.
        /// </summary>
        private readonly int _maxGenerations = 23;

        /// <summary>
        /// The mean mutation factor used in tests.
        /// </summary>
        private readonly double _meanMutationFactor = 0.9;

        /// <summary>
        /// The mean crossover rate used in tests.
        /// </summary>
        private readonly double _meanCrossoverRate = 0.5;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a path to which the status file will get written in tests.
        /// </summary>
        protected override string StatusFilePath =>
            PathUtils.GetAbsolutePathFromExecutableFolderRelative(
                Path.Combine("status", DifferentialEvolutionStatus<SearchPoint>.FileName));

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentOutOfRangeException"/> if generation number is
        /// negative.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnNegativeGeneration()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DifferentialEvolutionStatus<SearchPoint>(
                    this._sortedPopulation,
                    currentGeneration: -1,
                    maxGenerations: this._maxGenerations,
                    meanMutationFactor: this._meanMutationFactor,
                    meanCrossoverRate: this._meanCrossoverRate));
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentOutOfRangeException"/> if maximum generation
        /// number is smaller than the current generation.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnGenerationMaximumSmallerCurrentGeneration()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DifferentialEvolutionStatus<SearchPoint>(
                    this._sortedPopulation,
                    currentGeneration: 4,
                    maxGenerations: 3,
                    meanMutationFactor: this._meanMutationFactor,
                    meanCrossoverRate: this._meanCrossoverRate));
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentOutOfRangeException"/> if mean mutation factor is
        /// negative.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnNegativeMeanMutationFactor()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DifferentialEvolutionStatus<SearchPoint>(
                    this._sortedPopulation,
                    this._generation,
                    this._maxGenerations,
                    meanMutationFactor: -0.1,
                    meanCrossoverRate: this._meanCrossoverRate));
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentOutOfRangeException"/> if mean mutation factor is
        /// greater than 1.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMeanMutationFactorGreaterThan1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DifferentialEvolutionStatus<SearchPoint>(
                    this._sortedPopulation,
                    this._generation,
                    this._maxGenerations,
                    meanMutationFactor: 1.1,
                    meanCrossoverRate: this._meanCrossoverRate));
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentOutOfRangeException"/> if mean crossover rate is
        /// negative.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnNegativeMeanCrossoverRate()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DifferentialEvolutionStatus<SearchPoint>(
                    this._sortedPopulation,
                    this._generation,
                    this._maxGenerations,
                    this._meanMutationFactor,
                    meanCrossoverRate: -0.1));
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentOutOfRangeException"/> if mean crossover rate is
        /// greater than 1..
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMeanCrossoverRateGreaterThan1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DifferentialEvolutionStatus<SearchPoint>(
                    this._sortedPopulation,
                    this._generation,
                    this._maxGenerations,
                    this._meanMutationFactor,
                    meanCrossoverRate: 1.1));
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStatus{TSearchPoint}"/>'s constructor does not throw for a
        /// single population member, a generation of 0, a maximum generation number of 0, and mutation factors as
        /// well as crossover rates of 0 and 1.
        /// </summary>
        [Fact]
        public void ConstructorCanHandleEdgeValues()
        {
            var status = new DifferentialEvolutionStatus<SearchPoint>(
                sortedPopulation: new List<SearchPoint> { new SearchPoint(Vector<double>.Build.Random(3)) },
                currentGeneration: 0,
                maxGenerations: 0,
                meanMutationFactor: 0,
                meanCrossoverRate: 1);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStatus{TSearchPoint}.SortedPopulation"/>
        /// returns the population provided on initialization.
        /// </summary>
        [Fact]
        public void SortedPopulationIsSetCorrectly()
        {
            var status = this.CreateTestStatus();
            Assert.Equal(
                this._sortedPopulation,
                status.SortedPopulation);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStatus{TSearchPoint}.CurrentGeneration"/>
        /// returns the generation provided on initialization.
        /// </summary>
        [Fact]
        public void CurrentGenerationIsSetCorrectly()
        {
            var status = this.CreateTestStatus();
            Assert.Equal(
                this._generation,
                status.CurrentGeneration);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStatus{TSearchPoint}.MaxGenerations"/>
        /// returns the maximum generation number provided on initialization.
        /// </summary>
        [Fact]
        public void MaxGenerationsIsSetCorrectly()
        {
            var status = this.CreateTestStatus();
            Assert.Equal(
                this._maxGenerations,
                status.MaxGenerations);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStatus{TSearchPoint}.MeanCrossoverRate"/>
        /// returns the rate provided on initialization.
        /// </summary>
        [Fact]
        public void MeanCrossoverRateIsSetCorrectly()
        {
            var status = this.CreateTestStatus();
            Assert.Equal(
                this._meanCrossoverRate,
                status.MeanCrossoverRate);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolutionStatus{TSearchPoint}.MeanMutationFactor"/>
        /// returns the factor provided on initialization.
        /// </summary>
        [Fact]
        public void MeanMutationFactorIsSetCorrectly()
        {
            var status = this.CreateTestStatus();
            Assert.Equal(
                this._meanMutationFactor,
                status.MeanMutationFactor);
        }

        /// <summary>
        /// Checks that <see cref="StatusBase.ReadFromFile{Status}"/> correctly deserializes a
        /// status object written to file by <see cref="StatusBase.WriteToFile"/>.
        /// </summary>
        [Fact]
        public override void ReadFromFileDeserializesCorrectly()
        {
            var status = this.CreateTestStatus();
            status.WriteToFile(this.StatusFilePath);
            var deserializedStatus =
                StatusBase.ReadFromFile<DifferentialEvolutionStatus<SearchPoint>>(this.StatusFilePath);

            /* Check it's still the same. */
            Assert.Equal(
                this._sortedPopulation.Select(point => point.Values).ToArray(),
                deserializedStatus.SortedPopulation.Select(point => point.Values).ToArray());
            Assert.Equal(
                this._generation,
                deserializedStatus.CurrentGeneration);
            Assert.Equal(
                this._maxGenerations,
                status.MaxGenerations);
            Assert.Equal(
                this._meanCrossoverRate,
                deserializedStatus.MeanCrossoverRate);
            Assert.Equal(
                this._meanMutationFactor,
                deserializedStatus.MeanMutationFactor);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called before every test case.
        /// </summary>
        protected override void InitializeDefault()
        {
            for (int i = 0; i < 5; i++)
            {
                this._sortedPopulation.Add(new SearchPoint(Vector<double>.Build.Random(3)));
            }
        }

        /// <summary>
        /// Creates a status object which can be (de)serialized successfully.
        /// </summary>
        /// <returns>The created object.</returns>
        protected override DifferentialEvolutionStatus<SearchPoint> CreateTestStatus()
        {
            return new DifferentialEvolutionStatus<SearchPoint>(
                this._sortedPopulation,
                this._generation,
                this._maxGenerations,
                this._meanMutationFactor,
                this._meanCrossoverRate);
        }

        #endregion
    }
}