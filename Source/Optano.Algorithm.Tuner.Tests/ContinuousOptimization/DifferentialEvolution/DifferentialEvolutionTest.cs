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

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="DifferentialEvolution"/> class.
    /// </summary>
    public class DifferentialEvolutionTest : IDisposable
    {
        #region Fields

        /// <summary>
        /// Path to write the status file to.
        /// </summary>
        private readonly string _statusFilePath =
            PathUtils.GetAbsolutePathFromCurrentDirectory(DifferentialEvolutionStatus<SearchPoint>.FileName);

        /// <summary>
        /// <see cref="ISearchPointSorter{TSearchPoint}"/> used in tests.
        /// </summary>
        private readonly ISearchPointSorter<SearchPoint> _sorter =
            new MinimizeFunction(x => Math.Pow(x[0] - 3, 2) + Math.Pow(x[2] - 1, 4));

        /// <summary>
        /// The <see cref="SearchPoint"/> factory used in tests.
        /// </summary>
        private readonly Func<Vector<double>, SearchPoint, SearchPoint> _searchPointFactory =
            (vector, target) => new SearchPoint(vector);

        /// <summary>
        /// Some valid initial positions for <see cref="SearchPoint"/>s.
        /// </summary>
        private readonly List<SearchPoint> _initialPositions =
            Enumerable.Range(0, 5).Select(i => new SearchPoint(Vector<double>.Build.Dense(3))).ToList();

        /// <summary>
        /// <see cref="DifferentialEvolution{TSearchPoint}"/> instance used in tests.
        /// </summary>
        private readonly DifferentialEvolution<SearchPoint> _runner;

        /// <summary>
        /// <see cref="DifferentialEvolutionConfiguration"/> used in tests.
        /// </summary>
        private readonly DifferentialEvolutionConfiguration _configuration;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentialEvolutionTest"/> class.
        /// </summary>
        public DifferentialEvolutionTest()
        {
            this._configuration = new DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder()
                .SetBestPercentage(0.2)
                .SetInitialMeanMutationFactor(0.5)
                .SetInitialMeanCrossoverRate(0.5)
                .SetLearningRate(0.1)
                .BuildWithFallback(null);
            this._runner =
                new DifferentialEvolution<SearchPoint>(this._sorter, this._searchPointFactory, this._configuration);

            Randomizer.Reset();
            Randomizer.Configure(0);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Called after every test.
        /// </summary>
        public void Dispose()
        {
            if (File.Exists(this._statusFilePath))
            {
                File.Delete(this._statusFilePath);
            }

            Randomizer.Reset();
        }

        /// <summary>
        /// Checks that calling <see cref="DifferentialEvolution{TSearchPoint}"/>'s constructor without a
        /// <see cref="ISearchPointSorter{TSearchPoint}"/> throws a <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingSearchPointSorter()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DifferentialEvolution<SearchPoint>(
                    searchPointSorter: null,
                    searchPointFactory: this._searchPointFactory,
                    configuration: this._configuration));
        }

        /// <summary>
        /// Checks that calling <see cref="DifferentialEvolution{TSearchPoint}"/>'s constructor without a
        /// <see cref="SearchPoint"/> factory throws a <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingSearchPointFactory()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DifferentialEvolution<SearchPoint>(
                    this._sorter,
                    searchPointFactory: null,
                    configuration: this._configuration));
        }

        /// <summary>
        /// Checks that calling <see cref="DifferentialEvolution{TSearchPoint}"/>'s constructor without a
        /// <see cref="DifferentialEvolutionConfiguration"/> throws a <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingConfiguration()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DifferentialEvolution<SearchPoint>(
                    this._sorter,
                    this._searchPointFactory,
                    configuration: null));
        }

        /// <summary>
        /// Checks that calling <see cref="DifferentialEvolution{TSearchPoint}.Initialize"/> without a set of
        /// <see cref="SearchPoint"/>s throws a <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void InitializeThrowsForMissingSearchPoints()
        {
            Assert.Throws<ArgumentNullException>(() => this._runner.Initialize(initialPositions: null, maxGenerations: 5));
        }

        /// <summary>
        /// Checks that calling <see cref="DifferentialEvolution{TSearchPoint}.Initialize"/> with an empty set of
        /// <see cref="SearchPoint"/>s throws a <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void InitializeThrowsForEmptyInitialPositions()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._runner.Initialize(initialPositions: new List<SearchPoint>(0), maxGenerations: 5));
        }

        /// <summary>
        /// Checks that calling <see cref="DifferentialEvolution{TSearchPoint}.Initialize"/> with a negative number of maximum
        /// generations throws a <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void InitializeThrowsForNegativeMaxGenerations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._runner.Initialize(this._initialPositions, maxGenerations: -1));
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolution{TSearchPoint}.NextGeneration"/> throws a
        /// <see cref="InvalidOperationException"/> if called without calling
        /// <see cref="DifferentialEvolution{TSearchPoint}.Initialize"/> before.
        /// </summary>
        [Fact]
        public void NextGenerationThrowsIfUninitialized()
        {
            Assert.Throws<InvalidOperationException>(() => this._runner.NextGeneration());
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolution{TSearchPoint}.NextGeneration"/> returns the population in
        /// correct order.
        /// </summary>
        [Fact]
        public void NextGenerationReturnsSortedPopulation()
        {
            var randomPoints = Enumerable.Range(0, 50).Select(i => new SearchPoint(Vector<double>.Build.Random(3)));
            this._runner.Initialize(initialPositions: randomPoints, maxGenerations: 40);
            var population = this._runner.NextGeneration().ToList();
            var ranking = this._sorter.Sort(population);
            Assert.Equal(Enumerable.Range(0, 50).ToArray(), ranking.ToArray());
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolution{TSearchPoint}.NextGeneration"/> does not worsen any point of
        /// the population.
        /// </summary>
        [Fact]
        public void NextGenerationImprovesPopulation()
        {
            var randomPoints = Enumerable.Range(0, 50).Select(i => new SearchPoint(Vector<double>.Build.Random(3)));
            this._runner.Initialize(initialPositions: randomPoints, maxGenerations: 40);
            var firstPopulation = this._runner.NextGeneration().ToList();
            var secondPopulation = this._runner.NextGeneration().ToList();

            for (int i = 0; i < firstPopulation.Count; i++)
            {
                // Check that point is either the same or improved
                var ranking = this._sorter.Sort(new List<SearchPoint> { firstPopulation[i], secondPopulation[i] });
                if (!firstPopulation[i].Values.Equals(secondPopulation[i].Values))
                {
                    Assert.Equal(
                        1,
                        ranking.First());
                }
            }
        }

        /// <summary>
        /// Checks that DE can minimize the function in <see cref="_sorter"/>.
        /// </summary>
        [Fact]
        public void DifferentialEvolutionCanSolveEasyOptimizationProblem()
        {
            var randomPoints = Enumerable.Range(0, 50).Select(i => new SearchPoint(Vector<double>.Build.Random(3)));

            IEnumerable<SearchPoint> population = null;
            this._runner.Initialize(initialPositions: randomPoints, maxGenerations: 40);
            do
            {
                population = this._runner.NextGeneration();
            }
            while (!this._runner.AnyTerminationCriterionMet());

            var bestPoint = population.First();
            Assert.Equal(3d, bestPoint.Values[0], 2);
            Assert.Equal(1d, bestPoint.Values[2], 2);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolution{TSearchPoint}.AnyTerminationCriterionMet"/> throws a
        /// <see cref="InvalidOperationException"/> if called without calling
        /// <see cref="DifferentialEvolution{TSearchPoint}.Initialize"/> before.
        /// </summary>
        [Fact]
        public void AnyTerminationCriterionMetThrowsIfUninitialized()
        {
            Assert.Throws<InvalidOperationException>(() => this._runner.AnyTerminationCriterionMet());
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolution{TSearchPoint}.AnyTerminationCriterionMet"/> returns false if
        /// no criterion is met.
        /// </summary>
        [Fact]
        public void AnyTerminationCriterionMetReturnsFalseForNoneMet()
        {
            var spreadPositions = new List<SearchPoint>
                                      {
                                          new SearchPoint(Vector<double>.Build.DenseOfArray(new[] { 0d, 2d, 2d })),
                                          new SearchPoint(Vector<double>.Build.DenseOfArray(new[] { 0d, 2d, 2d })),
                                          new SearchPoint(Vector<double>.Build.DenseOfArray(new[] { 3d, 2d, 2d })),
                                      };
            this._runner.Initialize(spreadPositions, maxGenerations: 2);
            this._runner.NextGeneration();
            Assert.False(this._runner.AnyTerminationCriterionMet(), "No termination criterion is met.");
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolution{TSearchPoint}.AnyTerminationCriterionMet"/> returns true once
        /// the number of maximum generations is reached.
        /// </summary>
        [Fact]
        public void AnyTerminationCriterionMetReturnsTrueForMaximumGeneration()
        {
            var spreadPositions = new List<SearchPoint>
                                      {
                                          new SearchPoint(Vector<double>.Build.DenseOfArray(new[] { 0d, 2d, 2d })),
                                          new SearchPoint(Vector<double>.Build.DenseOfArray(new[] { 0d, 2d, 2d })),
                                          new SearchPoint(Vector<double>.Build.DenseOfArray(new[] { 3d, 2d, 2d })),
                                      };
            this._runner.Initialize(spreadPositions, maxGenerations: 2);
            this._runner.NextGeneration();
            this._runner.NextGeneration();
            Assert.True(this._runner.AnyTerminationCriterionMet(), "Termination criterion of 2 generations is met.");
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolution{TSearchPoint}.AnyTerminationCriterionMet"/> returns true once
        /// the maximum distance to the best vector is tiny.
        /// </summary>
        [Fact]
        public void AnyTerminationCriterionMetReturnsTrueForMaxDistMet()
        {
            var spreadPositions = new List<SearchPoint>
                                      {
                                          new SearchPoint(Vector<double>.Build.DenseOfArray(new[] { 0d, 1d, 2d })),
                                          new SearchPoint(Vector<double>.Build.DenseOfArray(new[] { 0d, 1.0001d, 2d })),
                                      };
            this._runner.Initialize(spreadPositions, maxGenerations: 2);
            Assert.True(this._runner.AnyTerminationCriterionMet(), "Termination criterion of maximum distance is met.");
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolution{TSearchPoint}.DumpStatus"/> creates a status file at the
        /// correct place.
        /// </summary>
        [Fact]
        public void DumpStatusCreatesStatusFile()
        {
            this._runner.Initialize(this._initialPositions, maxGenerations: 2);
            this._runner.DumpStatus(this._statusFilePath);

            Assert.True(File.Exists(this._statusFilePath), $"No file at path {this._statusFilePath}.");
        }

        /// <summary>
        /// Checks that all properties important for the status have been dumped to file.
        /// </summary>
        [Fact]
        public void DumpedStatusHasNoEmptyProperties()
        {
            int maxGenerations = 12;
            this._runner.Initialize(this._initialPositions, maxGenerations);
            this._runner.DumpStatus(this._statusFilePath);

            // Check status dump
            var status = StatusBase.ReadFromFile<DifferentialEvolutionStatus<SearchPoint>>(this._statusFilePath);
            Assert.Equal(
                this._initialPositions.Select(point => point.Values).OrderBy(x => x.ToString()).ToArray(),
                status.SortedPopulation.Select(point => point.Values).OrderBy(x => x.ToString()).ToArray());
            Assert.Equal(0, status.CurrentGeneration);
            Assert.Equal(
                maxGenerations,
                status.MaxGenerations);
            Assert.Equal(0.5, status.MeanMutationFactor);
            Assert.Equal(0.5, status.MeanCrossoverRate);
        }

        /// <summary>
        /// Checks that <see cref="DifferentialEvolution{TSearchPoint}.UseStatusDump"/> uses all information from the
        /// dump.
        /// </summary>
        [Fact]
        public void UseStatusDumpWorksCorrectly()
        {
            // Create a status dump.
            int maxGenerations = 12;
            this._runner.Initialize(this._initialPositions, maxGenerations);
            this._runner.NextGeneration();
            this._runner.DumpStatus(this._statusFilePath);
            var originalStatusDump =
                StatusBase.ReadFromFile<DifferentialEvolutionStatus<SearchPoint>>(this._statusFilePath);

            // Create a different runner, then use status dump.
            var randomPoints = Enumerable.Range(0, 50).Select(i => new SearchPoint(Vector<double>.Build.Random(3)));
            this._runner.Initialize(randomPoints, maxGenerations * 2);
            this._runner.UseStatusDump(this._statusFilePath);

            // Check newly dumped status vs old one.
            this._runner.DumpStatus(this._statusFilePath);
            var newStatusDump = StatusBase.ReadFromFile<DifferentialEvolutionStatus<SearchPoint>>(this._statusFilePath);

            // Compare dumps
            Assert.Equal(
                originalStatusDump.SortedPopulation.Select(point => point.Values).OrderBy(x => x.ToString()).ToArray(),
                newStatusDump.SortedPopulation.Select(point => point.Values).OrderBy(x => x.ToString()).ToArray());
            Assert.Equal(
                originalStatusDump.CurrentGeneration,
                newStatusDump.CurrentGeneration);
            Assert.Equal(
                originalStatusDump.MaxGenerations,
                newStatusDump.MaxGenerations);
            Assert.Equal(
                originalStatusDump.MeanMutationFactor,
                newStatusDump.MeanMutationFactor);
            Assert.Equal(
                originalStatusDump.MeanCrossoverRate,
                newStatusDump.MeanCrossoverRate);
        }

        #endregion
    }
}