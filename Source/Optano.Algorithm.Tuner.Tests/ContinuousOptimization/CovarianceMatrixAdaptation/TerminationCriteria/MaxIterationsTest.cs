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

namespace Optano.Algorithm.Tuner.Tests.ContinuousOptimization.CovarianceMatrixAdaptation.TerminationCriteria
{
    using System;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation.TerminationCriteria;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="MaxIterations"/> class.
    /// </summary>
    public class MaxIterationsTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="MaxIterations.IsMet"/> throws a <see cref="ArgumentNullException"/> if called without
        /// a <see cref="CmaEsElements"/> object.
        /// </summary>
        [Fact]
        public void IsMetThrowsForMissingCmaEsData()
        {
            var terminationCriterion = new MaxIterations(maximum: 5);
            Assert.Throws<ArgumentNullException>(() => terminationCriterion.IsMet(data: null));
        }

        /// <summary>
        /// Checks that <see cref="MaxIterations"/> constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a maximum of 0 generations.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForZeroIterations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MaxIterations(maximum: 0));
        }

        /// <summary>
        /// Checks that <see cref="MaxIterations.IsMet"/> returns true if called with the maximum number of
        /// generations.
        /// </summary>
        [Fact]
        public void IsMetReturnsTrueForMaxIterations()
        {
            var terminationCriterion = new MaxIterations(maximum: 5);
            var maxIterations = MaxIterationsTest.CreateCmaEsData(currentGeneration: 5);
            Assert.True(terminationCriterion.IsMet(maxIterations), "Termination criterion should have been met.");
        }

        /// <summary>
        /// Checks that <see cref="MaxIterations.IsMet"/> returns false if called with almost the maximum number
        /// of generations.
        /// </summary>
        [Fact]
        public void IsMetReturnsFalseForAlmostMaxIterations()
        {
            var terminationCriterion = new MaxIterations(maximum: 5);
            var almostMaxIterations = MaxIterationsTest.CreateCmaEsData(currentGeneration: 4);
            Assert.False(terminationCriterion.IsMet(almostMaxIterations), "Termination criterion should not have been met.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a simple <see cref="CmaEsElements"/> object with a certain number of generations.
        /// </summary>
        /// <param name="currentGeneration">The number of generations to set.</param>
        /// <returns>The created <see cref="CmaEsElements"/> object.</returns>
        private static CmaEsElements CreateCmaEsData(int currentGeneration)
        {
            var covariances = Matrix<double>.Build.DenseIdentity(3);
            return new CmaEsElements(
                new CmaEsConfiguration(20, Vector<double>.Build.Dense(3), 0.1),
                currentGeneration,
                Vector<double>.Build.Dense(3),
                0.1,
                covariances,
                covariances.Evd(),
                Vector<double>.Build.Dense(3),
                Vector<double>.Build.Dense(3));
        }

        #endregion
    }
}