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
    /// Contains tests for the <see cref="NoEffectAxis"/> class.
    /// </summary>
    public class NoEffectAxisTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="NoEffectAxis.IsMet"/> throws a <see cref="ArgumentNullException"/> if called without
        /// a <see cref="CmaEsElements"/> object.
        /// </summary>
        [Fact]
        public void IsMetThrowsForMissingCmaEsData()
        {
            var terminationCriterion = new NoEffectAxis();
            Assert.Throws<ArgumentNullException>(() => terminationCriterion.IsMet(data: null));
        }

        /// <summary>
        /// Checks that <see cref="NoEffectAxis.IsMet"/> throws a <see cref="ArgumentOutOfRangeException"/> if called
        /// with a <see cref="CmaEsElements"/> object without a covariance matrix.
        /// </summary>
        [Fact]
        public void IsMetThrowsForMissingCovarianceMatrix()
        {
            var terminationCriterion = new NoEffectAxis();
            var data = NoEffectAxisTest.CreateCmaEsData(covariances: null, currentGeneration: 1, currentStepSize: 0.2);
            Assert.Throws<ArgumentOutOfRangeException>(() => terminationCriterion.IsMet(data));
        }

        /// <summary>
        /// Checks that <see cref="NoEffectAxis.IsMet"/> throws a <see cref="ArgumentOutOfRangeException"/> if called
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

            var terminationCriterion = new NoEffectAxis();
            Assert.Throws<ArgumentOutOfRangeException>(() => terminationCriterion.IsMet(data));
        }

        /// <summary>
        /// Checks that <see cref="NoEffectAxis.IsMet"/> throws a <see cref="ArgumentOutOfRangeException"/> if called
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

            var terminationCriterion = new NoEffectAxis();
            Assert.Throws<ArgumentOutOfRangeException>(() => terminationCriterion.IsMet(data));
        }

        /// <summary>
        /// Checks that <see cref="NoEffectAxis.IsMet"/> returns true if called with a covariance matrix which has 
        /// a diagonal element of <see cref="double.Epsilon"/>.
        /// </summary>
        [Fact]
        public void IsMetReturnsTrueForTinyDiagonalElement()
        {
            var covariances = Matrix<double>.Build.DenseOfDiagonalArray(
                new[] { 1d, 1d, double.Epsilon * double.Epsilon });
            var generation = 6;
            var stepSize = 0.1;

            var terminationCriterion = new NoEffectAxis();
            Assert.True(
                terminationCriterion.IsMet(NoEffectAxisTest.CreateCmaEsData(covariances, generation, stepSize)),
                "Termination criterion should have been met.");
        }

        /// <summary>
        /// Checks that <see cref="NoEffectAxis.IsMet"/> returns true if called with a step size of
        /// <see cref="double.Epsilon"/>.
        /// </summary>
        [Fact]
        public void IsMetReturnsTrueForTinyStepSize()
        {
            var covariances = Matrix<double>.Build.DenseIdentity(3);
            var generation = 6;
            var stepSize = double.Epsilon;

            var terminationCriterion = new NoEffectAxis();
            Assert.True(
                terminationCriterion.IsMet(NoEffectAxisTest.CreateCmaEsData(covariances, generation, stepSize)),
                "Termination criterion should have been met.");
        }

        /// <summary>
        /// Checks that <see cref="NoEffectAxis.IsMet"/> returns false if a significant shift happens.
        /// </summary>
        [Fact]
        public void IsMetReturnsFalseForSignificantShift()
        {
            var covariances = Matrix<double>.Build.DenseIdentity(3);
            var generation = 6;
            var stepSize = double.Epsilon * 10;

            var terminationCriterion = new NoEffectAxis();
            Assert.False(
                terminationCriterion.IsMet(NoEffectAxisTest.CreateCmaEsData(covariances, generation, stepSize)),
                "Termination criterion should not have been met.");
        }

        /// <summary>
        /// Checks that <see cref="NoEffectAxis.IsMet"/> depends on the generation.
        /// </summary>
        [Fact]
        public void IsMetReturnsFalseForDifferentGeneration()
        {
            var covariances = Matrix<double>.Build.DenseOfDiagonalArray(
                new[] { 1d, 1d, double.Epsilon * double.Epsilon });
            var stepSize = 0.1;

            var terminationCriterion = new NoEffectAxis();
            Assert.True(
                terminationCriterion.IsMet(NoEffectAxisTest.CreateCmaEsData(covariances, 6, stepSize)),
                "Termination criterion should have been met.");
            Assert.False(
                terminationCriterion.IsMet(NoEffectAxisTest.CreateCmaEsData(covariances, 7, stepSize)),
                "Termination criterion should not have been met.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a simple <see cref="CmaEsElements"/> object with a certain covariance matrix, current generation
        /// and step size.
        /// </summary>
        /// <param name="covariances">The covariance matrix to set.</param>
        /// <param name="currentGeneration">The generation to set.</param>
        /// <param name="currentStepSize">The current step size.</param>
        /// <returns>The created <see cref="CmaEsElements"/> object.</returns>
        private static CmaEsElements CreateCmaEsData(
            Matrix<double> covariances,
            int currentGeneration,
            double currentStepSize)
        {
            return new CmaEsElements(
                new CmaEsConfiguration(20, Vector<double>.Build.Dense(3), 0.1),
                currentGeneration,
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