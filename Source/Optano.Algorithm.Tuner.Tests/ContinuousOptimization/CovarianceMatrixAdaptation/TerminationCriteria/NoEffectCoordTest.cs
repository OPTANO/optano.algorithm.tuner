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
    /// Contains tests for the <see cref="NoEffectCoord"/> class.
    /// </summary>
    public class NoEffectCoordTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="NoEffectCoord.IsMet"/> throws a <see cref="ArgumentNullException"/> if called without
        /// a <see cref="CmaEsElements"/> object.
        /// </summary>
        [Fact]
        public void IsMetThrowsForMissingCmaEsData()
        {
            var terminationCriterion = new NoEffectCoord();
            Assert.Throws<ArgumentNullException>(() => terminationCriterion.IsMet(data: null));
        }

        /// <summary>
        /// Checks that <see cref="NoEffectCoord.IsMet"/> throws a <see cref="ArgumentOutOfRangeException"/> if called
        /// with a <see cref="CmaEsElements"/> object without a covariance matrix.
        /// </summary>
        [Fact]
        public void IsMetThrowsForMissingCovarianceMatrix()
        {
            var terminationCriterion = new NoEffectCoord();
            var data = NoEffectCoordTest.CreateCmaEsData(covariances: null, currentStepSize: 0.2);
            Assert.Throws<ArgumentOutOfRangeException>(() => terminationCriterion.IsMet(data));
        }

        /// <summary>
        /// Checks that <see cref="NoEffectCoord.IsMet"/> throws a <see cref="ArgumentOutOfRangeException"/> if called
        /// with a <see cref="CmaEsElements"/> object without a <see cref="CmaEsConfiguration"/>.
        /// </summary>
        [Fact]
        public void IsMetThrowsForMissingConfiguration()
        {
            var covariances = Matrix<double>.Build.DenseOfDiagonalArray(new[] { 1d, 1d, 1d });
            var data = new CmaEsElements(
                configuration: null,
                generation: 5,
                distributionMean: Vector<double>.Build.Dense(3),
                stepSize: 0.2,
                covariances: covariances,
                covariancesDecomposition: covariances.Evd(),
                evolutionPath: Vector<double>.Build.Dense(3),
                conjugateEvolutionPath: Vector<double>.Build.Dense(3));

            var terminationCriterion = new NoEffectCoord();
            Assert.Throws<ArgumentOutOfRangeException>(() => terminationCriterion.IsMet(data));
        }

        /// <summary>
        /// Checks that <see cref="NoEffectCoord.IsMet"/> throws a <see cref="ArgumentOutOfRangeException"/> if called
        /// with a <see cref="CmaEsElements"/> object without a distribution mean.
        /// </summary>
        [Fact]
        public void IsMetThrowsForMissingDistributionMean()
        {
            var covariances = Matrix<double>.Build.DenseOfDiagonalArray(new[] { 1d, 1d, 1d });
            var data = new CmaEsElements(
                configuration: new CmaEsConfiguration(20, Vector<double>.Build.Dense(3), 0.1),
                generation: 5,
                distributionMean: null,
                stepSize: 0.2,
                covariances: covariances,
                covariancesDecomposition: covariances.Evd(),
                evolutionPath: Vector<double>.Build.Dense(3),
                conjugateEvolutionPath: Vector<double>.Build.Dense(3));

            var terminationCriterion = new NoEffectCoord();
            Assert.Throws<ArgumentOutOfRangeException>(() => terminationCriterion.IsMet(data));
        }

        /// <summary>
        /// Checks that <see cref="NoEffectCoord.IsMet"/> returns true if called with a covariance matrix which has 
        /// a diagonal element of <see cref="double.Epsilon"/>.
        /// </summary>
        [Fact]
        public void IsMetReturnsTrueForTinyCovariancesDiagonalElement()
        {
            var covariances = Matrix<double>.Build.DenseOfDiagonalArray(new[] { 1d, 1d, double.Epsilon });
            var stepSize = 0.1;
            var terminationCriterion = new NoEffectCoord();
            Assert.True(
                terminationCriterion.IsMet(NoEffectCoordTest.CreateCmaEsData(covariances, stepSize)),
                "Termination criterion should have been met.");
        }

        /// <summary>
        /// Checks that <see cref="NoEffectCoord.IsMet"/> returns true if called with a step size of
        /// <see cref="double.Epsilon"/>.
        /// </summary>
        [Fact]
        public void IsMetReturnsTrueForTinyStepSize()
        {
            var covariances = Matrix<double>.Build.DenseIdentity(3);
            var stepSize = double.Epsilon;
            var terminationCriterion = new NoEffectCoord();
            Assert.True(
                terminationCriterion.IsMet(NoEffectCoordTest.CreateCmaEsData(covariances, stepSize)),
                "Termination criterion should have been met.");
        }

        /// <summary>
        /// Checks that <see cref="NoEffectCoord.IsMet"/> returns false if a significant shift happens in all coordinates.
        /// </summary>
        [Fact]
        public void IsMetReturnsFalseForSignificantShift()
        {
            var covariances = Matrix<double>.Build.DenseIdentity(3);
            var stepSize = double.Epsilon * 5;
            var terminationCriterion = new NoEffectCoord();
            Assert.False(
                terminationCriterion.IsMet(NoEffectCoordTest.CreateCmaEsData(covariances, stepSize)),
                "Termination criterion should not have been met.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a simple <see cref="CmaEsElements"/> object with a certain covariance matrix and step size.
        /// </summary>
        /// <param name="covariances">The covariance matrix to set.</param>
        /// <param name="currentStepSize">The current step size.</param>
        /// <returns>The created <see cref="CmaEsElements"/> object.</returns>
        private static CmaEsElements CreateCmaEsData(
            Matrix<double> covariances,
            double currentStepSize)
        {
            return new CmaEsElements(
                new CmaEsConfiguration(20, Vector<double>.Build.Dense(3), 0.1),
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