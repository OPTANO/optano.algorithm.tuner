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

namespace Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation
{
    using System;

    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Double;
    using MathNet.Numerics.LinearAlgebra.Factorization;

    /// <summary>
    /// A wrapper exposing immutable versions of internal <see cref="CmaEs{TSearchPoint}"/> elements to
    /// <see cref="ITerminationCriterion"/> objects.
    /// </summary>
    public class CmaEsElements
    {
        #region Fields

        /// <summary>
        /// Backing field for <see cref="DistributionMean"/>.
        /// </summary>
        private readonly Vector<double> _distributionMean;

        /// <summary>
        /// Backing field for <see cref="Covariances"/>.
        /// </summary>
        private readonly Matrix<double> _covariances;

        /// <summary>
        /// Backing field for <see cref="CovariancesDiagonal"/>.
        /// </summary>
        private readonly DiagonalMatrix _covariancesDiagonal;

        /// <summary>
        /// Backing field for <see cref="CovariancesEigenVectors"/>.
        /// </summary>
        private readonly Matrix<double> _covariancesEigenVectors;

        /// <summary>
        /// Backing field for <see cref="EvolutionPath"/>.
        /// </summary>
        private readonly Vector<double> _evolutionPath;

        /// <summary>
        /// Backing field for <see cref="ConjugateEvolutionPath"/>.
        /// </summary>
        private readonly Vector<double> _conjugateEvolutionPath;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CmaEsElements"/> class.
        /// </summary>
        /// <param name="configuration">Fixed parameters for CMA-ES.</param>
        /// <param name="generation">The current generation.</param>
        /// <param name="distributionMean">The current distribution mean.</param>
        /// <param name="stepSize">The current step size.</param>
        /// <param name="covariances">The current covariance matrix.</param>
        /// <param name="covariancesDecomposition">The current eigendecomposition of the covariance matrix.</param>
        /// <param name="evolutionPath">The current evolution path.</param>
        /// <param name="conjugateEvolutionPath">The conjugate evolution path.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="generation"/> or <paramref name="stepSize"/> are negative.
        /// </exception>
        public CmaEsElements(
            CmaEsConfiguration configuration,
            int generation,
            Vector<double> distributionMean,
            double stepSize,
            Matrix<double> covariances,
            Evd<double> covariancesDecomposition,
            Vector<double> evolutionPath,
            Vector<double> conjugateEvolutionPath)
        {
            if (generation < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(generation),
                    $"Generation must be nonnegative, but was {generation}.");
            }

            if (stepSize < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stepSize),
                    $"Step size must be nonnegatives, but was {stepSize}.");
            }

            this.Configuration = configuration;
            this.Generation = generation;
            this._distributionMean = distributionMean?.Clone();
            this.StepSize = stepSize;
            this._covariances = covariances?.Clone();
            this._covariancesDiagonal = covariancesDecomposition == null ? null : DiagonalMatrix.OfMatrix(covariancesDecomposition.D);
            this._covariancesEigenVectors = covariancesDecomposition?.EigenVectors.Clone();
            this._evolutionPath = evolutionPath?.Clone();
            this._conjugateEvolutionPath = conjugateEvolutionPath?.Clone();
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets fixed parameters for CMA-ES.
        /// </summary>
        public CmaEsConfiguration Configuration { get; }

        /// <summary>
        /// Gets the current generation, often denoted g in literature.
        /// </summary>
        public int Generation { get; }

        /// <summary>
        /// Gets the current distribution mean, often denoted m in literature.
        /// </summary>
        public Vector<double> DistributionMean => this._distributionMean?.Clone();

        /// <summary>
        /// Gets the current step size, often denoted sigma in literature.
        /// </summary>
        public double StepSize { get; }

        /// <summary>
        /// Gets the current covariance matrix, often denoted C in literature.
        /// </summary>
        public Matrix<double> Covariances => this._covariances?.Clone();

        /// <summary>
        /// Gets the diagonal matrix in the current eigendecomposition of the covariance matrix.
        /// The complete matrix is often denoted C in literature, with decomposition
        /// C = B * D^2 * B^T, D^2 being the returned matrix.
        /// </summary>
        public DiagonalMatrix CovariancesDiagonal =>
            this._covariancesDiagonal == null ? null : DiagonalMatrix.OfMatrix(this._covariancesDiagonal);

        /// <summary>
        /// Gets the orthogonal matrix in the current eigendecomposition of the covariance matrix.
        /// The complete matrix is often denoted C in literature, with decomposition
        /// C = B * D^2 * B^T, B being the returned matrix.
        /// </summary>
        public Matrix<double> CovariancesEigenVectors => this._covariancesEigenVectors?.Clone();

        /// <summary>
        /// Gets the current evolution path: A sequence of successive steps the strategy took over a number of
        /// generations.
        /// Often denoted p_c in literature.
        /// </summary>
        public Vector<double> EvolutionPath => this._evolutionPath?.Clone();

        /// <summary>
        /// Gets the conjugate evolution path: Similar to <see cref="EvolutionPath"/>, but independent on direction.
        /// Used for step-size control.
        /// Often denoted p_sigma in literature.
        /// </summary>
        public Vector<double> ConjugateEvolutionPath => this._conjugateEvolutionPath?.Clone();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Determines whether this object is completely specified, i.e. no fields or properties are <c>null</c>.
        /// </summary>
        /// <remarks>Should be <c>true</c> once <see cref="CmaEs{TSearchPoint}.Initialize"/> has been run.</remarks>
        /// <returns>Whether this object is completely specified.</returns>
        public bool IsCompletelySpecified()
        {
            return this.Configuration != null
                   && this._distributionMean != null
                   && this._covariances != null
                   && this._covariancesDiagonal != null
                   && this._covariancesEigenVectors != null
                   && this._evolutionPath != null
                   && this._conjugateEvolutionPath != null;
        }

        #endregion
    }
}