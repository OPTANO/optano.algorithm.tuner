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

namespace Optano.Algorithm.Tuner.Tests.ContinuousOptimization.CovarianceMatrixAdaptation
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation.TerminationCriteria;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.Tests.Serialization;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="CmaEsStatus"/> class.
    /// </summary>
    public class CmaEsStatusTest : StatusBaseTest<CmaEsStatus>
    {
        #region Fields

        /// <summary>
        /// <see cref="ITerminationCriterion"/>s that can be used in tests.
        /// </summary>
        private List<ITerminationCriterion> _terminationCriteria;

        /// <summary>
        /// <see cref="CmaEsElements"/> that can be used in tests.
        /// </summary>
        private CmaEsElements _data;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a path to which the status file will get written in tests.
        /// </summary>
        protected override string StatusFilePath =>
            PathUtils.GetAbsolutePathFromExecutableFolderRelative(
                Path.Combine("status", CmaEsStatus.FileName));

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentNullException"/> if no termination criteria are
        /// provided.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingTerminationCriteria()
        {
            Assert.Throws<ArgumentNullException>(() => new CmaEsStatus(terminationCriteria: null, data: this._data));
        }

        /// <summary>
        /// Checks that the constructor throws an <see cref="ArgumentNullException"/> if no
        /// <see cref="CmaEsElements"/> are provided.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingData()
        {
            Assert.Throws<ArgumentNullException>(() => new CmaEsStatus(this._terminationCriteria, data: null));
        }

        /// <summary>
        /// Checks that <see cref="CmaEsStatus.TerminationCriteria"/>
        /// returns the <see cref="ITerminationCriterion"/>s provided on initialization.
        /// </summary>
        [Fact]
        public void TerminationCriteriaAreSetCorrectly()
        {
            var cmaEsStatus =
                new CmaEsStatus(this._terminationCriteria, this._data);
            Assert.Equal(
                this._terminationCriteria,
                cmaEsStatus.TerminationCriteria);
        }

        /// <summary>
        /// Checks that <see cref="CmaEsStatus.Data"/>
        /// returns the <see cref="CmaEsElements"/>s provided on initialization.
        /// </summary>
        [Fact]
        public void DataIsSetCorrectly()
        {
            var cmaEsStatus = new CmaEsStatus(this._terminationCriteria, this._data);
            Assert.Equal(
                this._data,
                cmaEsStatus.Data);
        }

        /// <summary>
        /// Checks that <see cref="StatusBase.ReadFromFile{Status}"/> correctly deserializes a
        /// status object written to file by <see cref="StatusBase.WriteToFile"/>.
        /// </summary>
        [Fact]
        public override void ReadFromFileDeserializesCorrectly()
        {
            /* Create status. */
            this._terminationCriteria = new List<ITerminationCriterion> { new TolUpSigma() };
            var configuration = new CmaEsConfiguration(
                populationSize: 20,
                initialDistributionMean: Vector<double>.Build.Random(3),
                initialStepSize: 0.1);
            var covariances = 17 * Matrix<double>.Build.DenseIdentity(2);
            var covariancesDecomposition = covariances.Evd(Symmetricity.Symmetric);
            this._data = new CmaEsElements(
                configuration,
                generation: 24,
                stepSize: 0.14,
                distributionMean: Vector<double>.Build.Random(2),
                covariances: covariances,
                covariancesDecomposition: covariancesDecomposition,
                evolutionPath: Vector<double>.Build.Random(2),
                conjugateEvolutionPath: Vector<double>.Build.Random(2));
            var cmaEsStatus = new CmaEsStatus(this._terminationCriteria, this._data);

            /* Write and read it from file. */
            cmaEsStatus.WriteToFile(this.StatusFilePath);
            var deserializedStatus = StatusBase.ReadFromFile<CmaEsStatus>(this.StatusFilePath);

            /* Check it's still the same. */
            Assert.Equal(
                this._terminationCriteria.Count,
                deserializedStatus.TerminationCriteria.Count);
            Assert.Equal(
                this._terminationCriteria[0].GetType(),
                deserializedStatus.TerminationCriteria[0].GetType());
            Assert.Equal(
                configuration.PopulationSize,
                deserializedStatus.Data.Configuration.PopulationSize);
            Assert.Equal(
                configuration.InitialDistributionMean,
                deserializedStatus.Data.Configuration.InitialDistributionMean);
            Assert.Equal(
                configuration.InitialStepSize,
                deserializedStatus.Data.Configuration.InitialStepSize);
            Assert.Equal(
                this._data.Generation,
                deserializedStatus.Data.Generation);
            Assert.Equal(
                this._data.StepSize,
                deserializedStatus.Data.StepSize);
            Assert.Equal(
                this._data.DistributionMean,
                deserializedStatus.Data.DistributionMean);
            Assert.Equal(
                this._data.Covariances,
                deserializedStatus.Data.Covariances);
            Assert.Equal(
                this._data.CovariancesDiagonal,
                deserializedStatus.Data.CovariancesDiagonal);
            Assert.Equal(
                this._data.CovariancesEigenVectors,
                deserializedStatus.Data.CovariancesEigenVectors);
            Assert.Equal(
                this._data.EvolutionPath,
                deserializedStatus.Data.EvolutionPath);
            Assert.Equal(
                this._data.ConjugateEvolutionPath,
                deserializedStatus.Data.ConjugateEvolutionPath);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called before every test case.
        /// </summary>
        protected override void InitializeDefault()
        {
            this._data = new CmaEsElements(
                configuration: new CmaEsConfiguration(20, Vector<double>.Build.Random(3), 0.1),
                generation: 1,
                stepSize: 0.1,
                distributionMean: Vector<double>.Build.Random(2),
                covariances: Matrix<double>.Build.DenseIdentity(2),
                covariancesDecomposition: Matrix<double>.Build.DenseIdentity(2).Evd(),
                evolutionPath: Vector<double>.Build.Random(2),
                conjugateEvolutionPath: Vector<double>.Build.Random(2));
            this._terminationCriteria = new List<ITerminationCriterion> { new MaxIterations(57) };
        }

        /// <summary>
        /// Creates a status object which can be (de)serialized successfully.
        /// </summary>
        /// <returns>The created object.</returns>
        protected override CmaEsStatus CreateTestStatus()
        {
            return new CmaEsStatus(this._terminationCriteria, this._data);
        }

        #endregion
    }
}