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

namespace Optano.Algorithm.Tuner.Tests.ContinuousOptimization.CovarianceMatrixAdaptation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation.TerminationCriteria;
    using Optano.Algorithm.Tuner.Serialization;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="CmaEs{TSearchPoint}"/> class.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class CmaEsTest : IDisposable
    {
        #region Fields

        /// <summary>
        /// Path to write the status file to.
        /// </summary>
        private readonly string _statusFilePath =
            PathUtils.GetAbsolutePathFromCurrentDirectory(CmaEsStatus.FileName);

        /// <summary>
        /// <see cref="ISearchPointSorter{TSearchPoint}"/> used in tests.
        /// </summary>
        private readonly ISearchPointSorter<SearchPoint> _sorter =
            new MinimizeFunction(x => Math.Pow(x[0] - 3, 2) + Math.Pow(x[2] - 1, 4));

        /// <summary>
        /// The <see cref="SearchPoint"/> factory used in tests.
        /// </summary>
        private readonly Func<Vector<double>, SearchPoint> _searchPointFactory = vector => new SearchPoint(vector);

        /// <summary>
        /// <see cref="CmaEs{TSearchPoint}"/> instance used in tests.
        /// </summary>
        private readonly CmaEs<SearchPoint> _runner;

        /// <summary>
        /// <see cref="CmaEsConfiguration"/> used in tests.
        /// </summary>
        private readonly CmaEsConfiguration _configuration;

        /// <summary>
        /// Termination criteria used in tests.
        /// </summary>
        private readonly List<ITerminationCriterion> _terminationCriteria;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CmaEsTest"/> class.
        /// </summary>
        public CmaEsTest()
        {
            this._runner = new CmaEs<SearchPoint>(this._sorter, this._searchPointFactory);
            this._configuration = new CmaEsConfiguration(7, Vector<double>.Build.Dense(3), 0.1);
            this._terminationCriteria = new List<ITerminationCriterion> { new MaxIterations(3) };

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
        /// Checks that calling <see cref="CmaEs{TSearchPoint}"/>'s constructor without a
        /// <see cref="ISearchPointSorter{TSearchPoint}"/> throws a <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingSearchPointSorter()
        {
            Assert.Throws<ArgumentNullException>(
                () => new CmaEs<SearchPoint>(searchPointSorter: null, searchPointFactory: this._searchPointFactory));
        }

        /// <summary>
        /// Checks that calling <see cref="CmaEs{TSearchPoint}"/>'s constructor without a
        /// <see cref="SearchPoint"/> factory throws a <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingSearchPointFactory()
        {
            Assert.Throws<ArgumentNullException>(
                () => new CmaEs<SearchPoint>(this._sorter, searchPointFactory: null));
        }

        /// <summary>
        /// Checks that calling <see cref="CmaEs{TSearchPoint}.Initialize"/> without a
        /// <see cref="CmaEsConfiguration"/> throws a <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void InitializeThrowsForMissingConfiguration()
        {
            Assert.Throws<ArgumentNullException>(
                () => this._runner.Initialize(configuration: null, terminationCriteria: this._terminationCriteria));
        }

        /// <summary>
        /// Checks that calling <see cref="CmaEs{TSearchPoint}.Initialize"/> without a set of
        /// <see cref="ITerminationCriterion"/>s throws a <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void InitializeThrowsForMissingTerminationCriteriaSet()
        {
            Assert.Throws<ArgumentNullException>(
                () => this._runner.Initialize(this._configuration, terminationCriteria: null));
        }

        /// <summary>
        /// Checks that calling <see cref="CmaEs{TSearchPoint}.Initialize"/> without an empty set of
        /// <see cref="ITerminationCriterion"/>s throws a <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void InitializeThrowsForEmptyTerminationCriteriaSet()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this._runner.Initialize(this._configuration, new List<ITerminationCriterion>(0)));
        }

        /// <summary>
        /// Checks that the <see cref="CmaEsConfiguration"/> provided on initialization is used.
        /// </summary>
        [Fact]
        public void RunnerUsesCorrectConfiguration()
        {
            this._runner.Initialize(this._configuration, this._terminationCriteria);
            this._runner.DumpStatus(this._statusFilePath);
            var status = StatusBase.ReadFromFile<CmaEsStatus>(this._statusFilePath);
            Assert.Equal(
                this._configuration.PopulationSize,
                status.Data.Configuration.PopulationSize);
        }

        /// <summary>
        /// Checks that <see cref="CmaEs{TSearchPoint}.NextGeneration"/> throws a
        /// <see cref="InvalidOperationException"/> if called without calling
        /// <see cref="CmaEs{TSearchPoint}.Initialize"/> before.
        /// </summary>
        [Fact]
        public void NextGenerationThrowsIfUninitialized()
        {
            Assert.Throws<InvalidOperationException>(() => this._runner.NextGeneration());
        }

        /// <summary>
        /// Checks that CMA-ES can minimize the function in <see cref="_sorter"/>.
        /// </summary>
        [Fact]
        public void CmaEsCanSolveEasyOptimizationProblem()
        {
            IEnumerable<SearchPoint> population = null;
            this._runner.Initialize(this._configuration, new List<ITerminationCriterion> { new MaxIterations(40) });
            do
            {
                population = this._runner.NextGeneration();
            }
            while (!this._runner.AnyTerminationCriterionMet());

            var bestPoint = population.First();
            TestUtils.Equals(bestPoint.Values[0], 3d, 0.05);
            TestUtils.Equals(bestPoint.Values[2], 1d, 0.05);
        }

        /// <summary>
        /// Checks that <see cref="CmaEs{TSearchPoint}.AnyTerminationCriterionMet"/> throws a
        /// <see cref="InvalidOperationException"/> if called without calling
        /// <see cref="CmaEs{TSearchPoint}.Initialize"/> before.
        /// </summary>
        [Fact]
        public void AnyTerminationCriterionMetThrowsIfUninitialized()
        {
            Assert.Throws<InvalidOperationException>(() => this._runner.AnyTerminationCriterionMet());
        }

        /// <summary>
        /// Checks that <see cref="CmaEs{TSearchPoint}.AnyTerminationCriterionMet"/> returns false if no criterion is met.
        /// </summary>
        [Fact]
        public void AnyTerminationCriterionMetReturnsFalseForNoneMet()
        {
            this._runner.Initialize(
                this._configuration,
                new List<ITerminationCriterion> { new MaxIterations(2), new MaxIterations(5) });
            this._runner.NextGeneration();
            Assert.False(this._runner.AnyTerminationCriterionMet(), "No termination criterion is met.");
        }

        /// <summary>
        /// Checks that <see cref="CmaEs{TSearchPoint}.AnyTerminationCriterionMet"/> returns true once one of them is met.
        /// </summary>
        [Fact]
        public void AnyTerminationCriterionMetReturnsTrueForOneMet()
        {
            this._runner.Initialize(
                this._configuration,
                new List<ITerminationCriterion> { new MaxIterations(2), new MaxIterations(5) });
            this._runner.NextGeneration();
            this._runner.NextGeneration();
            Assert.True(this._runner.AnyTerminationCriterionMet(), "Termination criterion of 2 generations is met.");
        }

        /// <summary>
        /// Checks that <see cref="CmaEs{TSearchPoint}.DumpStatus"/> creates a status file at the correct
        /// place.
        /// </summary>
        [Fact]
        public void DumpStatusCreatesStatusFile()
        {
            this._runner.Initialize(this._configuration, this._terminationCriteria);
            this._runner.DumpStatus(this._statusFilePath);

            Assert.True(File.Exists(this._statusFilePath), $"No file at path {this._statusFilePath}.");
        }

        /// <summary>
        /// Checks that all properties important for the status have been dumped to file.
        /// </summary>
        [Fact]
        public void DumpedStatusHasNoEmptyProperties()
        {
            this._runner.Initialize(this._configuration, this._terminationCriteria);
            this._runner.DumpStatus(this._statusFilePath);

            // Check status dump
            var status = StatusBase.ReadFromFile<CmaEsStatus>(this._statusFilePath);
            Assert.Equal(
                this._terminationCriteria.Count,
                status.TerminationCriteria.Count);
            Assert.Equal(
                this._configuration.PopulationSize,
                status.Data.Configuration.PopulationSize);

            Assert.Equal(0, status.Data.Generation);
            Assert.Equal(
                this._configuration.InitialDistributionMean,
                status.Data.DistributionMean);
            Assert.Equal(this._configuration.InitialStepSize, status.Data.StepSize);

            var covariances = Matrix<double>.Build.DenseIdentity(3);
            var eigendecomposition = covariances.Evd();
            Assert.Equal(
                covariances,
                status.Data.Covariances);
            Assert.Equal(eigendecomposition.D, status.Data.CovariancesDiagonal);
            Assert.Equal(
                eigendecomposition.EigenVectors,
                status.Data.CovariancesEigenVectors);

            var zeroVector = Vector<double>.Build.Dense(3);
            Assert.Equal(
                zeroVector,
                status.Data.EvolutionPath);
            Assert.Equal(
                zeroVector,
                status.Data.ConjugateEvolutionPath);
        }

        /// <summary>
        /// Checks that <see cref="CmaEs{TSearchPoint}.UseStatusDump"/> uses all information from the dump.
        /// </summary>
        [Fact]
        public void UseStatusDumpWorksCorrectly()
        {
            // Create a status dump.
            this._runner.Initialize(this._configuration, this._terminationCriteria);
            this._runner.NextGeneration();
            this._runner.DumpStatus(this._statusFilePath);
            var originalStatusDump = StatusBase.ReadFromFile<CmaEsStatus>(this._statusFilePath);

            // Create a different runner, then use status dump.
            var otherConfiguration = new CmaEsConfiguration(14, Vector<double>.Build.Dense(3), 0.2);
            var otherTerminationCriteria = new List<ITerminationCriterion> { new MaxIterations(89) };
            this._runner.Initialize(otherConfiguration, otherTerminationCriteria);
            this._runner.UseStatusDump(this._statusFilePath);

            // Check newly dumped status vs old one.
            this._runner.DumpStatus(this._statusFilePath);
            var newStatusDump = StatusBase.ReadFromFile<CmaEsStatus>(this._statusFilePath);

            // Compare dumps
            TestUtils.SetsAreEquivalent(
                originalStatusDump.TerminationCriteria.Select(criterion => criterion.GetType()).ToArray(),
                newStatusDump.TerminationCriteria.Select(criterion => criterion.GetType()).ToArray());
            Assert.Equal(
                originalStatusDump.Data.Configuration.CumulationLearningRate,
                newStatusDump.Data.Configuration.CumulationLearningRate);
            Assert.Equal(originalStatusDump.Data.Generation, newStatusDump.Data.Generation);
            Assert.Equal(
                originalStatusDump.Data.DistributionMean,
                newStatusDump.Data.DistributionMean);
            Assert.Equal(originalStatusDump.Data.StepSize, newStatusDump.Data.StepSize);

            Assert.Equal(
                originalStatusDump.Data.Covariances,
                newStatusDump.Data.Covariances);
            Assert.Equal(originalStatusDump.Data.CovariancesDiagonal, newStatusDump.Data.CovariancesDiagonal);
            Assert.Equal(
                originalStatusDump.Data.CovariancesEigenVectors,
                newStatusDump.Data.CovariancesEigenVectors);

            Assert.Equal(
                originalStatusDump.Data.EvolutionPath,
                newStatusDump.Data.EvolutionPath);
            Assert.Equal(
                originalStatusDump.Data.ConjugateEvolutionPath,
                newStatusDump.Data.ConjugateEvolutionPath);
        }

        #endregion
    }
}