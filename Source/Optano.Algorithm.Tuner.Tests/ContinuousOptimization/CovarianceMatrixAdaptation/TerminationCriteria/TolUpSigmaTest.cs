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

namespace Optano.Algorithm.Tuner.Tests.ContinuousOptimization.CovarianceMatrixAdaptation.TerminationCriteria
{
    using System;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation.TerminationCriteria;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="TolUpSigma"/> class.
    /// </summary>
    public class TolUpSigmaTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="TolUpSigma.IsMet"/> throws a <see cref="ArgumentNullException"/> if called without
        /// a <see cref="CmaEsElements"/> object.
        /// </summary>
        [Fact]
        public void IsMetThrowsForMissingCmaEsData()
        {
            var terminationCriterion = new TolUpSigma();
            Assert.Throws<ArgumentNullException>(() => terminationCriterion.IsMet(data: null));
        }

        /// <summary>
        /// Checks that <see cref="TolUpSigma.IsMet"/> throws a <see cref="ArgumentOutOfRangeException"/> if called
        /// with a <see cref="CmaEsElements"/> object without a covariance matrix.
        /// </summary>
        [Fact]
        public void IsMetThrowsForMissingCovarianceMatrix()
        {
            var terminationCriterion = new TolUpSigma();
            var data = TolUpSigmaTest.CreateCmaEsData(covariances: null, initialStepSize: 0.2, currentStepSize: 0.2);
            Assert.Throws<ArgumentOutOfRangeException>(() => terminationCriterion.IsMet(data));
        }

        /// <summary>
        /// Checks that <see cref="TolUpSigma.IsMet"/> throws a <see cref="ArgumentOutOfRangeException"/> if called
        /// with a <see cref="CmaEsElements"/> object without a <see cref="CmaEsConfiguration"/>.
        /// </summary>
        [Fact]
        public void IsMetThrowsForMissingConfiguration()
        {
            var covariances = Matrix<double>.Build.DenseOfDiagonalArray(
                new[] { 42.3, 0, 0 });
            var data = new CmaEsElements(
                configuration: null,
                generation: 5,
                distributionMean: Vector<double>.Build.Dense(3),
                stepSize: 0.2,
                covariances: covariances,
                covariancesDecomposition: covariances.Evd(),
                evolutionPath: Vector<double>.Build.Dense(3),
                conjugateEvolutionPath: Vector<double>.Build.Dense(3));

            var terminationCriterion = new TolUpSigma();
            Assert.Throws<ArgumentOutOfRangeException>(() => terminationCriterion.IsMet(data));
        }

        /// <summary>
        /// Checks that <see cref="TolUpSigma.IsMet"/> returns true if called with a covariance matrix and step sizes
        /// s.t. the factor between them is greater than <see cref="TolUpSigma.MaxFactor"/> times the square root of
        /// the greatest eigenvalue.
        /// </summary>
        [Fact]
        public void IsMetReturnsTrueForFactorAboveMaxConstant()
        {
            double greatestEigenvalue = 42.3;
            double initialStepSize = 0.2;
            double currentStepSize = (TolUpSigma.MaxFactor * Math.Sqrt(greatestEigenvalue) * initialStepSize) + 0.1;
            var covariances = Matrix<double>.Build.DenseOfDiagonalArray(
                new[] { greatestEigenvalue, 0, 0 });

            var terminationCriterion = new TolUpSigma();
            Assert.True(
                terminationCriterion.IsMet(TolUpSigmaTest.CreateCmaEsData(covariances, initialStepSize, currentStepSize)),
                "Termination criterion should have been met.");
        }

        /// <summary>
        /// Checks that <see cref="TolUpSigma.IsMet"/> returns false if called with a covariance matrix and step sizes
        /// s.t. the factor between them is equal to <see cref="TolUpSigma.MaxFactor"/> times the square root of
        /// the greatest eigenvalue.
        /// </summary>
        [Fact]
        public void IsMetReturnsFalseForFactorEqualToMaxConstant()
        {
            double greatestEigenvalue = 42.3;
            double initialStepSize = 0.2;
            double currentStepSize = TolUpSigma.MaxFactor * Math.Sqrt(greatestEigenvalue) * initialStepSize;
            var covariances = Matrix<double>.Build.DenseOfDiagonalArray(
                new[] { greatestEigenvalue, 0, 0 });

            var terminationCriterion = new TolUpSigma();
            Assert.False(
                terminationCriterion.IsMet(TolUpSigmaTest.CreateCmaEsData(covariances, initialStepSize, currentStepSize)),
                "Termination criterion should not have been met.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a simple <see cref="CmaEsElements"/> object with a certain covariance matrix,
        /// initial step size and step size.
        /// </summary>
        /// <param name="covariances">The covariance matrix to set.</param>
        /// <param name="initialStepSize">The initial step size.</param>
        /// <param name="currentStepSize">The current step size.</param>
        /// <returns>The created <see cref="CmaEsElements"/> object.</returns>
        private static CmaEsElements CreateCmaEsData(
            Matrix<double> covariances,
            double initialStepSize,
            double currentStepSize)
        {
            return new CmaEsElements(
                new CmaEsConfiguration(20, Vector<double>.Build.Dense(3), initialStepSize),
                5,
                Vector<double>.Build.Dense(3),
                currentStepSize,
                covariances,
                covariances?.Evd(),
                Vector<double>.Build.Dense(3),
                Vector<double>.Build.Dense(3));
        }

        #endregion
    }
}