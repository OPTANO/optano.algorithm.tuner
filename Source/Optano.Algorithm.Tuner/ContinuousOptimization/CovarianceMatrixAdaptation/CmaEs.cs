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
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Factorization;

    using NLog;

    using Optano.Algorithm.Tuner.Serialization;

    /// <summary>
    /// Covariance Matrix Adaptation Evolutionary Strategy as defined in Hansen, N.: The CMA Evolution Strategy: A
    /// Tutorial. In: CoRR abs/1604.00772 (2016) http://arxiv.org/abs/1604.00772.
    /// </summary>
    /// <typeparam name="TSearchPoint">Type of <see cref="SearchPoint"/>s handled by this CMA-ES instance.</typeparam>
    public class CmaEs<TSearchPoint> : IEvolutionBasedContinuousOptimizer<TSearchPoint>
        where TSearchPoint : SearchPoint
    {
        #region Static Fields

        /// <summary>
        /// <see cref="NLog"/> logger object.
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Fields

        /// <summary>
        /// Specifies how to sort the <typeparamref name="TSearchPoint"/>s.
        /// </summary>
        private readonly ISearchPointSorter<TSearchPoint> _searchPointSorter;

        /// <summary>
        /// Specifies how to create a <typeparamref name="TSearchPoint"/> from a <see cref="Vector{T}"/>.
        /// </summary>
        private readonly Func<Vector<double>, TSearchPoint> _searchPointFactory;

        /// <summary>
        /// A set of termination criteria to check in <see cref="AnyTerminationCriterionMet"/>.
        /// </summary>
        private List<ITerminationCriterion> _terminationCriteria = new List<ITerminationCriterion>();

        /// <summary>
        /// Fixed parameters for CMA-ES.
        /// </summary>
        private CmaEsConfiguration _configuration;

        /// <summary>
        /// The current CMA-ES generation, often denoted g in literature.
        /// </summary>
        private int _currentCmaesGeneration;

        /// <summary>
        /// The current distribution mean, often denoted m in literature.
        /// </summary>
        private Vector<double> _distributionMean;

        /// <summary>
        /// The current covariance matrix, often denoted C in literature.
        /// </summary>
        [SuppressMessage(
            "NDepend",
            "ND1905:DontAssignAFieldFromManyMethods",
            Justification = "NDepend warning pops up, because field is assigned in more than 4, namely 5 methods. Violation of limit is not critical and can be ignored.")]
        private Matrix<double> _covariances;

        /// <summary>
        /// The current eigendecomposition of the covariance matrix.
        /// The complete matrix is often denoted C in literature, with decomposition C = B * D^2 * B^T.
        /// </summary>
        private Evd<double> _covariancesDecomposition;

        /// <summary>
        /// The current step size, often denoted sigma in literature.
        /// </summary>
        private double _stepSize;

        /// <summary>
        /// The current evolution path: A sequence of successive steps the strategy took over a number of generations.
        /// Often denoted p_c in literature.
        /// </summary>
        private Vector<double> _evolutionPath;

        /// <summary>
        /// The conjugate evolution path: Similar to <see cref="_evolutionPath"/>, but independent on direction.
        /// Used for step-size control.
        /// Often denoted p_sigma in literature.
        /// </summary>
        private Vector<double> _conjugateEvolutionPath;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CmaEs{TSearchPoint}"/> class.
        /// </summary>
        /// <param name="searchPointSorter">
        /// Object responsible for sorting <typeparamref name="TSearchPoint"/>s.
        /// </param>
        /// <param name="searchPointFactory">
        /// Responsible for creating <typeparamref name="TSearchPoint"/>s from <see cref="Vector{T}"/> objects.
        /// </param>
        public CmaEs(
            ISearchPointSorter<TSearchPoint> searchPointSorter,
            Func<Vector<double>, TSearchPoint> searchPointFactory)
        {
            this._searchPointSorter = searchPointSorter ?? throw new ArgumentNullException(nameof(searchPointSorter));
            this._searchPointFactory =
                searchPointFactory ?? throw new ArgumentNullException(nameof(searchPointFactory));
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Initializes the object to begin a new run.
        /// </summary>
        /// <param name="configuration">The <see cref="CmaEsConfiguration"/> to use.</param>
        /// <param name="terminationCriteria">Criteria when to terminate the run.</param>
        public void Initialize(CmaEsConfiguration configuration, IEnumerable<ITerminationCriterion> terminationCriteria)
        {
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._terminationCriteria = terminationCriteria?.ToList() ??
                                        throw new ArgumentNullException(nameof(terminationCriteria));

            if (!this._terminationCriteria.Any())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(terminationCriteria),
                    "There needs to be at least one termination criterion.");
            }

            this._currentCmaesGeneration = 0;
            this._covariances = Matrix<double>.Build.DenseIdentity(this._configuration.SearchSpaceDimension);
            this._covariancesDecomposition = this._covariances.Evd();
            this._evolutionPath = Vector<double>.Build.Dense(this._configuration.SearchSpaceDimension);
            this._conjugateEvolutionPath = Vector<double>.Build.Dense(this._configuration.SearchSpaceDimension);

            this._distributionMean = this._configuration.InitialDistributionMean;
            this._stepSize = this._configuration.InitialStepSize;
        }

        /// <summary>
        /// Executes a single CMA-ES generation.
        /// </summary>
        /// <remarks><see cref="Initialize"/> needs to be called beforehand.</remarks>
        /// <returns>Current population, best individuals first.</returns>
        public IEnumerable<TSearchPoint> NextGeneration()
        {
            this.CheckIsInitialized();

            this._currentCmaesGeneration++;

            var randomDirections = this.SampleRandomDirections();
            var stepDirections = this.BuildStepDirections(randomDirections).ToList();
            var searchPoints = this.CreatePopulation(stepDirections);

            var searchPointOrder = this._searchPointSorter.Sort(searchPoints);

            var unscaledMeanStep = this.ComputeUnscaledMeanStep(stepDirections, searchPointOrder);
            this._distributionMean += this._stepSize * unscaledMeanStep;
            this.UpdateStepSize(randomDirections, searchPointOrder);
            this.AdaptCovariances(unscaledMeanStep, randomDirections, stepDirections, searchPointOrder);

            // Enforce symmetry.
            this._covariances = this._covariances.UpperTriangle() + this._covariances.StrictlyUpperTriangle().Transpose();
            this._covariancesDecomposition = this._covariances.Evd(Symmetricity.Symmetric);

            return searchPointOrder.Select(idx => searchPoints[idx]);
        }

        /// <summary>
        /// Checks whether any of <see cref="_terminationCriteria"/> is met.
        /// </summary>
        /// <returns>True if and only if at least one of <see cref="_terminationCriteria"/> is met.</returns>
        public bool AnyTerminationCriterionMet()
        {
            this.CheckIsInitialized();

            var decisionParameters = this.WrapData();
            var metCriteria =
                this._terminationCriteria.Where(criterium => criterium.IsMet(decisionParameters)).ToList();
            if (!metCriteria.Any())
            {
                return false;
            }

            Logger.Info("CMA-ES: Termination criterion met.");
            foreach (var criterion in metCriteria)
            {
                Logger.Debug(criterion.GetType().Name);
            }

            return true;
        }

        /// <summary>
        /// Writes all internal data to file.
        /// </summary>
        /// <param name="pathToStatusFile">Path to the file to write.</param>
        public void DumpStatus(string pathToStatusFile)
        {
            var status = new CmaEsStatus(this._terminationCriteria, this.WrapData());
            status.WriteToFile(pathToStatusFile);
        }

        /// <summary>
        /// Reads all internal data from file.
        /// </summary>
        /// <param name="pathToStatusFile">Path to the file to read.</param>
        public void UseStatusDump(string pathToStatusFile)
        {
            var status = StatusBase.ReadFromFile<CmaEsStatus>(pathToStatusFile);

            this._terminationCriteria.Clear();
            foreach (var terminationCriterion in status.TerminationCriteria)
            {
                this._terminationCriteria.Add(terminationCriterion.Restore());
            }

            this._configuration = status.Data.Configuration;
            this._currentCmaesGeneration = status.Data.Generation;
            this._distributionMean = status.Data.DistributionMean;
            this._covariances = status.Data.Covariances;
            this._covariancesDecomposition = this._covariances?.Evd(Symmetricity.Symmetric);
            this._stepSize = status.Data.StepSize;
            this._evolutionPath = status.Data.EvolutionPath;
            this._conjugateEvolutionPath = status.Data.ConjugateEvolutionPath;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Samples <see cref="CmaEsConfiguration.PopulationSize"/> many vectors from N(0, I).
        /// </summary>
        /// <returns>The sampled vectors.</returns>
        private List<Vector<double>> SampleRandomDirections()
        {
            var directions = new List<Vector<double>>(this._configuration.PopulationSize);
            for (int k = 0; k < this._configuration.PopulationSize; k++)
            {
                directions.Add(this.SampleFromStandardNormal());
            }

            return directions;
        }

        /// <summary>
        /// Samples a <see cref="Vector{T}"/> of dimension <see cref="CmaEsConfiguration.SearchSpaceDimension"/>
        /// from N(0, I).
        /// </summary>
        /// <returns>The sampled <see cref="Vector{T}"/>.</returns>
        private Vector<double> SampleFromStandardNormal()
        {
            var z = Vector<double>.Build.Dense(this._configuration.SearchSpaceDimension);
            for (int i = 0; i < z.Count; i++)
            {
                z[i] = Randomizer.Instance.SampleFromNormal(0, 1);
            }

            return z;
        }

        /// <summary>
        /// Builds directions for steps.
        /// </summary>
        /// <param name="randomDirections">The N(0, I)-distributed directions to base the steps on.</param>
        /// <returns>Directions distributed according to <see cref="_covariances"/>.</returns>
        private IEnumerable<Vector<double>> BuildStepDirections(IEnumerable<Vector<double>> randomDirections)
        {
            var d = this._covariancesDecomposition.D.PointwiseSqrt();
            var covariancesShift = this._covariancesDecomposition.EigenVectors * d;

            return randomDirections.Select(direction => covariancesShift * direction);
        }

        /// <summary>
        /// Creates a new population of <typeparamref name="TSearchPoint"/>s.
        /// </summary>
        /// <param name="stepDirections">Step directions distributed according to <see cref="_covariances"/>.</param>
        /// <returns>
        /// A population based on <see cref="_distributionMean"/>, <see cref="_stepSize"/>
        /// and <paramref name="stepDirections"/>.
        /// </returns>
        private List<TSearchPoint> CreatePopulation(IEnumerable<Vector<double>> stepDirections)
        {
            return stepDirections
                .Select(direction => this._searchPointFactory.Invoke(this._distributionMean + (this._stepSize * direction)))
                .ToList();
        }

        /// <summary>
        /// Computes the step of the distribution mean disregarding step-size.
        /// </summary>
        /// <param name="stepDirections">All step directions.</param>
        /// <param name="searchPointOrder">Indices of sorted points based on those directions, best first.</param>
        /// <returns>The computed step.</returns>
        private Vector<double> ComputeUnscaledMeanStep(
            IList<Vector<double>> stepDirections,
            IList<int> searchPointOrder)
        {
            var unscaledMeanStep = Vector<double>.Build.Dense(this._configuration.SearchSpaceDimension);
            for (int i = 0; i < this._configuration.ParentNumber; i++)
            {
                unscaledMeanStep += this._configuration.Weights[i] * stepDirections[searchPointOrder[i]];
            }

            return unscaledMeanStep;
        }

        /// <summary>
        /// Executes step-size control.
        /// </summary>
        /// <param name="randomDirections">The N(0, I)-distributed directions.</param>
        /// <param name="searchPointOrder">Indices of sorted points based on those directions, best first.</param>
        private void UpdateStepSize(IList<Vector<double>> randomDirections, IList<int> searchPointOrder)
        {
            // Compute the mean direction of selected search points.
            var meanDirection = Vector<double>.Build.Dense(this._configuration.SearchSpaceDimension);
            for (int i = 0; i < this._configuration.ParentNumber; i++)
            {
                meanDirection += this._configuration.Weights[i] * randomDirections[searchPointOrder[i]];
            }

            var normalizationConstant = Math.Sqrt(
                this._configuration.StepSizeControlLearningRate *
                (2 - this._configuration.StepSizeControlLearningRate) *
                this._configuration.VarianceEffectiveSelectionMass);
            this._conjugateEvolutionPath =
                ((1 - this._configuration.StepSizeControlLearningRate) * this._conjugateEvolutionPath) +
                (normalizationConstant * this._covariancesDecomposition.EigenVectors * meanDirection);

            var pathLengthRatio = this._conjugateEvolutionPath.L2Norm() / this._configuration.ComputeExpectedConjugateEvolutionPathLength();
            var factor = this._configuration.StepSizeControlLearningRate / this._configuration.StepSizeControlDamping;
            this._stepSize *= Math.Exp(factor * (pathLengthRatio - 1));
        }

        /// <summary>
        /// Adapts the covariance matrix and the evolution path.
        /// </summary>
        /// <param name="unscaledMeanStep">Step of the distribution mean disregarding step-size.</param>
        /// <param name="randomDirections">The N(0, I)-distributed directions.</param>
        /// <param name="stepDirections">All step directions.</param>
        /// <param name="searchPointOrder">Indices of sorted points based on those directions, best first.</param>
        private void AdaptCovariances(
            Vector<double> unscaledMeanStep,
            IList<Vector<double>> randomDirections,
            IList<Vector<double>> stepDirections,
            IList<int> searchPointOrder)
        {
            var normalizationConstant = Math.Sqrt(
                this._configuration.CumulationLearningRate *
                (2 - this._configuration.CumulationLearningRate) *
                this._configuration.VarianceEffectiveSelectionMass);
            var stallingConstant = this.DecideStallingConstant();
            this._evolutionPath = ((1 - this._configuration.CumulationLearningRate) * this._evolutionPath) +
                                  ((stallingConstant * normalizationConstant) * unscaledMeanStep);

            var evolutionPathAsMatrix = this._evolutionPath.ToRowMatrix();
            var rankOneUpdate = this._configuration.RankOneUpdateLearningRate *
                                evolutionPathAsMatrix.TransposeThisAndMultiply(evolutionPathAsMatrix);
            var rankMuUpdate = this.ComputeRankMuUpdate(randomDirections, stepDirections, searchPointOrder);
            var decayFactor = this.ComputeCovarianceDecayFactor(stallingConstant);
            this._covariances = (decayFactor * this._covariances) + rankOneUpdate + rankMuUpdate;
        }

        /// <summary>
        /// Decides whether update of <see cref="_evolutionPath"/> should be stalled by returning a fitting factor.
        /// </summary>
        /// <returns>Either 0 or 1.</returns>
        private int DecideStallingConstant()
        {
            double maximumPathLength =
                Math.Sqrt(1 - Math.Pow(1 - this._configuration.StepSizeControlLearningRate, 2 * (this._currentCmaesGeneration + 1))) *
                (1.4 + (2d / (this._configuration.SearchSpaceDimension + 1))) *
                this._configuration.ComputeExpectedConjugateEvolutionPathLength();
            return this._conjugateEvolutionPath.L2Norm() >= maximumPathLength ? 0 : 1;
        }

        /// <summary>
        /// Computes the matrix that should be added to <see cref="_covariances"/> due to rank-mu-update.
        /// </summary>
        /// <param name="randomDirections">The N(0, I)-distributed directions.</param>
        /// <param name="stepDirections">All step directions.</param>
        /// <param name="searchPointOrder">Indices of sorted points based on those directions, best first.</param>
        /// <returns>The matrix to add.</returns>
        private Matrix<double> ComputeRankMuUpdate(
            IList<Vector<double>> randomDirections,
            IList<Vector<double>> stepDirections,
            IList<int> searchPointOrder)
        {
            var update = Matrix<double>.Build.Dense(
                this._configuration.SearchSpaceDimension,
                this._configuration.SearchSpaceDimension);

            for (int i = 0; i < this._configuration.PopulationSize; i++)
            {
                var weight = this._configuration.Weights[i];
                if (weight < 0)
                {
                    weight *= this._configuration.SearchSpaceDimension /
                              Math.Pow((this._covariancesDecomposition.EigenVectors * randomDirections[searchPointOrder[i]]).L2Norm(), 2);
                }

                var directionAsMatrix = stepDirections[searchPointOrder[i]].ToRowMatrix();
                update += weight * directionAsMatrix.TransposeThisAndMultiply(directionAsMatrix);
            }

            return this._configuration.RankMuUpdateLearningRate * update;
        }

        /// <summary>
        /// Compute decay factor for <see cref="_covariances"/>.
        /// </summary>
        /// <param name="stallingConstant">
        /// Constant that was used when updating <see cref="_evolutionPath"/>, either 0 or 1.
        /// </param>
        /// <returns>The computed factor.</returns>
        private double ComputeCovarianceDecayFactor(int stallingConstant)
        {
            var stallingConstantAdapter =
                (1 - stallingConstant) *
                this._configuration.CumulationLearningRate *
                (2 - this._configuration.CumulationLearningRate);
            var decayFactor =
                1 +
                (this._configuration.RankOneUpdateLearningRate * stallingConstantAdapter) -
                this._configuration.RankOneUpdateLearningRate -
                (this._configuration.RankMuUpdateLearningRate * this._configuration.Weights.Sum());
            return decayFactor;
        }

        /// <summary>
        /// Checks that <see cref="Initialize"/> has been called.
        /// </summary>
        /// <param name="memberName">Name of calling method. Automatically set.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="Initialize"/> has not been called so far.
        /// </exception>
        private void CheckIsInitialized([CallerMemberName] string memberName = "")
        {
            if (this._configuration == null)
            {
                throw new InvalidOperationException(
                    $"Cannot execute {memberName} before calling {nameof(this.Initialize)}.");
            }
        }

        /// <summary>
        /// Wraps internal data into a <see cref="CmaEsElements"/> object.
        /// </summary>
        /// <returns>The created <see cref="CmaEsElements"/> object.</returns>
        private CmaEsElements WrapData()
        {
            return new CmaEsElements(
                this._configuration,
                this._currentCmaesGeneration,
                this._distributionMean,
                this._stepSize,
                this._covariances,
                this._covariancesDecomposition,
                this._evolutionPath,
                this._conjugateEvolutionPath);
        }

        #endregion
    }
}