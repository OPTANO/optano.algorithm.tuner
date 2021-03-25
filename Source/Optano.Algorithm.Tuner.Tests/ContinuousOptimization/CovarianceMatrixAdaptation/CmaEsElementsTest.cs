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

    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Factorization;

    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="CmaEsElements"/> class.
    /// </summary>
    public class CmaEsElementsTest
    {
        #region Fields

        /// <summary>
        /// Generation used in tests.
        /// </summary>
        private readonly int _generation = 27;

        /// <summary>
        /// Step size used in tests.
        /// </summary>
        private readonly double _stepSize = 0.3;

        /// <summary>
        /// <see cref="CmaEsConfiguration"/> used in tests.
        /// </summary>
        private readonly CmaEsConfiguration _configuration;

        /// <summary>
        /// Distribution mean used in tests.
        /// </summary>
        private readonly Vector<double> _distributionMean;

        /// <summary>
        /// Covariance matrix used in tests.
        /// </summary>
        private readonly Matrix<double> _covariances;

        /// <summary>
        /// Diagonal in eigenvalue decomposition of <see cref="_covariancesDecomposition"/>:.
        /// </summary>
        private readonly Matrix<double> _diagonal;

        /// <summary>
        /// Eigenvectors in eigenvalue decomposition of <see cref="_covariances"/>:.
        /// </summary>
        private readonly Matrix<double> _eigenvectors;

        /// <summary>
        /// Evolution path used in tests.
        /// </summary>
        private readonly Vector<double> _evolutionPath;

        /// <summary>
        /// Conjugate evolution path used in tests.
        /// </summary>
        private readonly Vector<double> _conjugateEvolutionPath;

        /// <summary>
        /// Eigenvalue decomposition of <see cref="_covariances"/>.
        /// </summary>
        private Evd<double> _covariancesDecomposition;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CmaEsElementsTest"/> class.
        /// </summary>
        public CmaEsElementsTest()
        {
            this._configuration = new CmaEsConfiguration(20, Vector<double>.Build.Random(3), 0.1);
            this._distributionMean = Vector<double>.Build.DenseOfArray(new[] { 1.8, 4.9 });
            this._evolutionPath = Vector<double>.Build.DenseOfArray(new[] { 2.1, 3.2 });
            this._conjugateEvolutionPath = Vector<double>.Build.DenseOfArray(new[] { 1.7, 4.1 });

            this._covariances = Matrix<double>.Build
                .DenseOfArray(
                    new[,]
                        {
                            {
                                3d,
                                1d,
                            },
                            {
                                1d,
                                3d,
                            },
                        });
            this._covariancesDecomposition = this._covariances.Evd(Symmetricity.Symmetric);
            this._diagonal = this._covariancesDecomposition.D;
            this._eigenvectors = this._covariancesDecomposition.EigenVectors;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="CmaEsElements"/>'s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a negative generation number.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForNegativeGeneration()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CmaEsElements(
                    this._configuration,
                    generation: -1,
                    distributionMean: this._distributionMean,
                    stepSize: this._stepSize,
                    covariances: this._covariances,
                    covariancesDecomposition: this._covariancesDecomposition,
                    evolutionPath: this._evolutionPath,
                    conjugateEvolutionPath: this._conjugateEvolutionPath));
        }

        /// <summary>
        /// Checks that <see cref="CmaEsElements"/>'s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a negative step size.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForNegativeStepSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CmaEsElements(
                    this._configuration,
                    this._generation,
                    this._distributionMean,
                    stepSize: -0.1,
                    covariances: this._covariances,
                    covariancesDecomposition: this._covariancesDecomposition,
                    evolutionPath: this._evolutionPath,
                    conjugateEvolutionPath: this._conjugateEvolutionPath));
        }

        /// <summary>
        /// Checks that <see cref="CmaEsElements"/>'s constructor throws a
        /// <see cref="IndexOutOfRangeException"/> if called with an eigenvalue
        /// decomposition of a not diagonalizable matrix.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForNotDiagonalizableMatrix()
        {
            var notDiagonalizable = Matrix<double>.Build
                .DenseOfArray(
                    new[,]
                        {
                            {
                                0d,
                                -1d,
                            },
                            {
                                1d,
                                0d,
                            },
                        });
            this._covariancesDecomposition = notDiagonalizable.Evd();
            Assert.Throws<IndexOutOfRangeException>(
                () => new CmaEsElements(
                    this._configuration,
                    this._generation,
                    this._distributionMean,
                    this._stepSize,
                    covariances: notDiagonalizable,
                    covariancesDecomposition: this._covariancesDecomposition,
                    evolutionPath: this._evolutionPath,
                    conjugateEvolutionPath: this._conjugateEvolutionPath));
        }

        /// <summary>
        /// Checks that <see cref="CmaEsElements.IsCompletelySpecified"/>'s returns <c>false</c> if called with a
        /// <see cref="CmaEsElements"/> object without a <see cref="CmaEsConfiguration"/>.
        /// </summary>
        [Fact]
        public void IsCompletelySpecifiedReturnsFalseForMissingConfiguration()
        {
            var noConfiguration = new CmaEsElements(
                configuration: null,
                generation: this._generation,
                distributionMean: this._distributionMean,
                stepSize: this._stepSize,
                covariances: this._covariances,
                covariancesDecomposition: this._covariancesDecomposition,
                evolutionPath: this._evolutionPath,
                conjugateEvolutionPath: this._conjugateEvolutionPath);
            Assert.False(
                noConfiguration.IsCompletelySpecified(),
                "Data without a configuration is not completely specified.");
        }

        /// <summary>
        /// Checks that <see cref="CmaEsElements.IsCompletelySpecified"/>'s returns <c>false</c> if called with a
        /// <see cref="CmaEsElements"/> object without a distribution mean.
        /// </summary>
        [Fact]
        public void IsCompletelySpecifiedReturnsFalseForMissingDistributionMean()
        {
            var noDistributionMean = new CmaEsElements(
                this._configuration,
                this._generation,
                distributionMean: null,
                stepSize: this._stepSize,
                covariances: this._covariances,
                covariancesDecomposition: this._covariancesDecomposition,
                evolutionPath: this._evolutionPath,
                conjugateEvolutionPath: this._conjugateEvolutionPath);
            Assert.False(
                noDistributionMean.IsCompletelySpecified(),
                "Data without a distribution mean is not completely specified.");
        }

        /// <summary>
        /// Checks that <see cref="CmaEsElements.IsCompletelySpecified"/>'s returns <c>false</c> if called with a
        /// <see cref="CmaEsElements"/> object without covariances.
        /// </summary>
        [Fact]
        public void IsCompletelySpecifiedReturnsFalseForMissingCovariances()
        {
            var noCovariances = new CmaEsElements(
                this._configuration,
                this._generation,
                this._distributionMean,
                this._stepSize,
                covariances: null,
                covariancesDecomposition: this._covariancesDecomposition,
                evolutionPath: this._evolutionPath,
                conjugateEvolutionPath: this._conjugateEvolutionPath);
            Assert.False(
                noCovariances.IsCompletelySpecified(),
                "Data without covariances is not completely specified.");
        }

        /// <summary>
        /// Checks that <see cref="CmaEsElements.IsCompletelySpecified"/>'s returns <c>false</c> if called with a
        /// <see cref="CmaEsElements"/> object without covariances decomposition.
        /// </summary>
        [Fact]
        public void IsCompletelySpecifiedReturnsFalseForMissingCovariancesDecomposition()
        {
            var noCovariancesDecomposition = new CmaEsElements(
                this._configuration,
                this._generation,
                this._distributionMean,
                this._stepSize,
                this._covariances,
                covariancesDecomposition: null,
                evolutionPath: this._evolutionPath,
                conjugateEvolutionPath: this._conjugateEvolutionPath);
            Assert.False(
                noCovariancesDecomposition.IsCompletelySpecified(),
                "Data without a covariances decomposition is not completely specified.");
        }

        /// <summary>
        /// Checks that <see cref="CmaEsElements.IsCompletelySpecified"/>'s returns <c>false</c> if called with a
        /// <see cref="CmaEsElements"/> object without an evolution path.
        /// </summary>
        [Fact]
        public void IsCompletelySpecifiedReturnsFalseForMissingEvolutionPath()
        {
            var noEvolutionPath = new CmaEsElements(
                this._configuration,
                this._generation,
                this._distributionMean,
                this._stepSize,
                this._covariances,
                this._covariancesDecomposition,
                evolutionPath: null,
                conjugateEvolutionPath: this._conjugateEvolutionPath);
            Assert.False(
                noEvolutionPath.IsCompletelySpecified(),
                "Data without evolution path is not completely specified.");
        }

        /// <summary>
        /// Checks that <see cref="CmaEsElements.IsCompletelySpecified"/>'s returns <c>false</c> if called with a
        /// <see cref="CmaEsElements"/> object without a conjugate evolution path.
        /// </summary>
        [Fact]
        public void IsCompletelySpecifiedReturnsFalseForMissingConjugateEvolutionPath()
        {
            var noConjugateEvolutionPath = new CmaEsElements(
                this._configuration,
                this._generation,
                this._distributionMean,
                this._stepSize,
                this._covariances,
                this._covariancesDecomposition,
                this._evolutionPath,
                conjugateEvolutionPath: null);
            Assert.False(
                noConjugateEvolutionPath.IsCompletelySpecified(),
                "Data without a conjugate evolution path is not completely specified.");
        }

        /// <summary>
        /// Checks that <see cref="CmaEsElements.IsCompletelySpecified"/>'s returns <c>true</c> if called with a
        /// <see cref="CmaEsElements"/> where everything is set.
        /// </summary>
        [Fact]
        public void IsCompletelySpecifiedReturnsTrueForEverythingSpecified()
        {
            var data = new CmaEsElements(
                this._configuration,
                this._generation,
                this._distributionMean,
                this._stepSize,
                this._covariances,
                this._covariancesDecomposition,
                this._evolutionPath,
                this._conjugateEvolutionPath);
            Assert.True(
                data.IsCompletelySpecified(),
                "Data is completely specified.");
        }

        /// <summary>
        /// Checks that all getters of <see cref="CmaEsElements"/> return what they were initialized with.
        /// </summary>
        [Fact]
        public void PropertiesAreSetCorrectly()
        {
            var data = new CmaEsElements(
                this._configuration,
                this._generation,
                this._distributionMean,
                this._stepSize,
                this._covariances,
                this._covariancesDecomposition,
                this._evolutionPath,
                this._conjugateEvolutionPath);

            Assert.Equal(this._configuration, data.Configuration);
            Assert.Equal(this._generation, data.Generation);
            Assert.Equal(this._distributionMean, data.DistributionMean);
            Assert.Equal(this._stepSize, data.StepSize);
            Assert.Equal(this._covariances, data.Covariances);
            Assert.Equal(this._diagonal, data.CovariancesDiagonal);
            Assert.Equal(
                this._eigenvectors,
                data.CovariancesEigenVectors);
            Assert.Equal(this._evolutionPath, data.EvolutionPath);
            Assert.Equal(
                this._conjugateEvolutionPath,
                data.ConjugateEvolutionPath);
        }

        /// <summary>
        /// Checks that all getters of <see cref="CmaEsElements"/> return what they were initialized with, even if 
        /// most of that is <c>null</c>.
        /// </summary>
        [Fact]
        public void PropertiesAreSetCorrectlyForMinimumSpecification()
        {
            var data = new CmaEsElements(
                configuration: null,
                generation: this._generation,
                distributionMean: null,
                stepSize: this._stepSize,
                covariances: null,
                covariancesDecomposition: null,
                evolutionPath: null,
                conjugateEvolutionPath: null);

            Assert.Null(data.Configuration);
            Assert.Equal(this._generation, data.Generation);
            Assert.Null(data.DistributionMean);
            Assert.Equal(this._stepSize, data.StepSize);
            Assert.Null(data.Covariances);
            Assert.Null(data.CovariancesDiagonal);
            Assert.Null(data.CovariancesEigenVectors);
            Assert.Null(data.EvolutionPath);
            Assert.Null(data.ConjugateEvolutionPath);
        }

        /// <summary>
        /// Checks that neither changes to data exposed by the <see cref="CmaEsElements"/> object nor to data the
        /// object was initalized with change subsequent return values.
        /// </summary>
        [Fact]
        public void ObjectIsImmutable()
        {
            var data = new CmaEsElements(
                this._configuration,
                this._generation,
                this._distributionMean,
                this._stepSize,
                this._covariances,
                this._covariancesDecomposition,
                this._evolutionPath,
                this._conjugateEvolutionPath);

            // Play around with getters
            var returnedMean = data.DistributionMean;
            returnedMean.Clear();
            Assert.NotEqual(returnedMean, this._distributionMean);
            Assert.Equal(this._distributionMean, data.DistributionMean);

            var returnedCovariances = data.Covariances;
            returnedCovariances.Clear();
            Assert.NotEqual(returnedCovariances, this._covariances);
            Assert.Equal(this._covariances, data.Covariances);

            var returnedDiagonal = data.CovariancesDiagonal;
            returnedDiagonal.Clear();
            Assert.NotEqual(returnedDiagonal, this._diagonal);
            Assert.Equal(this._diagonal, data.CovariancesDiagonal);

            var returnedEigenVectors = data.CovariancesEigenVectors;
            returnedEigenVectors.Clear();
            Assert.NotEqual(returnedEigenVectors, this._eigenvectors);
            Assert.Equal(
                this._eigenvectors,
                data.CovariancesEigenVectors);

            var returnedEvolutionPath = data.EvolutionPath;
            returnedEvolutionPath.Clear();
            Assert.NotEqual(
                returnedEvolutionPath,
                this._evolutionPath);
            Assert.Equal(this._evolutionPath, data.EvolutionPath);

            var returnedConjugateEvolutionPath = data.ConjugateEvolutionPath;
            returnedConjugateEvolutionPath.Clear();
            Assert.NotEqual(
                returnedConjugateEvolutionPath,
                this._conjugateEvolutionPath);
            Assert.Equal(
                this._conjugateEvolutionPath,
                data.ConjugateEvolutionPath);

            // Then change values the object was initialized with.
            this._distributionMean.Clear();
            Assert.NotEqual(
                this._distributionMean,
                data.DistributionMean);

            this._covariances.Clear();
            Assert.NotEqual(this._covariances, data.Covariances);

            this._covariancesDecomposition.D.Clear();
            Assert.NotEqual(
                this._diagonal,
                data.CovariancesDiagonal);
            this._covariancesDecomposition.EigenVectors.Clear();
            Assert.NotEqual(
                this._eigenvectors,
                data.CovariancesEigenVectors);

            this._evolutionPath.Clear();
            Assert.NotEqual(this._evolutionPath, data.EvolutionPath);

            this._conjugateEvolutionPath.Clear();
            Assert.NotEqual(
                this._conjugateEvolutionPath,
                data.ConjugateEvolutionPath);
        }

        #endregion
    }
}