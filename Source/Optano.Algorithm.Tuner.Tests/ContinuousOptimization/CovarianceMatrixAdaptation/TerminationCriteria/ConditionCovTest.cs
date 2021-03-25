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
    /// Contains tests for the <see cref="ConditionCov"/> class.
    /// </summary>
    public class ConditionCovTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="ConditionCov.IsMet"/> throws a <see cref="ArgumentNullException"/> if called without
        /// a <see cref="CmaEsElements"/> object.
        /// </summary>
        [Fact]
        public void IsMetThrowsForMissingCmaEsData()
        {
            var terminationCriterion = new ConditionCov();
            Assert.Throws<ArgumentNullException>(() => terminationCriterion.IsMet(data: null));
        }

        /// <summary>
        /// Checks that <see cref="ConditionCov.IsMet"/> throws a <see cref="ArgumentOutOfRangeException"/> if called
        /// with a <see cref="CmaEsElements"/> object without a covariance matrix.
        /// </summary>
        [Fact]
        public void IsMetThrowsForMissingCovarianceMatrix()
        {
            var terminationCriterion = new ConditionCov();
            var data = ConditionCovTest.CreateCmaEsData(covariances: null);
            Assert.Throws<ArgumentOutOfRangeException>(() => terminationCriterion.IsMet(data));
        }

        /// <summary>
        /// Checks that <see cref="ConditionCov.IsMet"/> returns true if called with a covariance matrix with condition
        /// number above <see cref="ConditionCov.MaxCondition"/>.
        /// </summary>
        [Fact]
        public void IsMetReturnsTrueForMatrixAboveMaxCondition()
        {
            var terminationCriterion = new ConditionCov();
            var matrixAboveMaxCondition = Matrix<double>.Build.DenseOfDiagonalArray(
                new[] { ConditionCov.MaxCondition * 10, 10 - 0.1 });
            Assert.True(
                terminationCriterion.IsMet(ConditionCovTest.CreateCmaEsData(matrixAboveMaxCondition)),
                "Termination criterion should have been met.");
        }

        /// <summary>
        /// Checks that <see cref="ConditionCov.IsMet"/> returns false if called with a covariance matrix with condition
        /// number equal to <see cref="ConditionCov.MaxCondition"/>.
        /// </summary>
        [Fact]
        public void IsMetReturnsFalseForMatrixWithMaxCondition()
        {
            var terminationCriterion = new ConditionCov();
            var matrixWithMaxCondition = Matrix<double>.Build.DenseOfDiagonalArray(
                new[] { ConditionCov.MaxCondition * 10, 10 });
            Assert.False(
                terminationCriterion.IsMet(ConditionCovTest.CreateCmaEsData(matrixWithMaxCondition)),
                "Termination criterion should not have been met.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a simple <see cref="CmaEsElements"/> object with a certain covariance matrix.
        /// </summary>
        /// <param name="covariances">The covariance matrix to set.</param>
        /// <returns>The created <see cref="CmaEsElements"/> object.</returns>
        private static CmaEsElements CreateCmaEsData(Matrix<double> covariances)
        {
            return new CmaEsElements(
                new CmaEsConfiguration(20, Vector<double>.Build.Dense(3), 0.1),
                5,
                Vector<double>.Build.Dense(3),
                0.1,
                covariances,
                covariances?.Evd(),
                Vector<double>.Build.Dense(3),
                Vector<double>.Build.Dense(3));
        }

        #endregion
    }
}