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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation
{
    using System.Collections.Generic;
    using System.IO;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation.TerminationCriteria;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// A base class for strategies updating the <see cref="Population"/> objects using
    /// <see cref="CmaEs{TSearchPoint}"/> instances.
    /// </summary>
    /// <typeparam name="TSearchPoint">
    /// The type of <see cref="SearchPoint"/>s handled by this strategy instance.
    /// </typeparam>
    /// <typeparam name="TInstance">
    /// The instance type to use.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The result for an individual evaluation.
    /// </typeparam>
    public abstract class CovarianceMatrixAdaptationStrategyBase<TSearchPoint, TInstance, TResult>
        : ContinuousOptimizationStrategyBase<TSearchPoint, TInstance, TResult>
        where TSearchPoint : SearchPoint, IRepairedGenomeRepresentation, IDeserializationRestorer<TSearchPoint>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// A set of termination criteria based on numerical stability.
        /// </summary>
        private readonly IReadOnlyList<ITerminationCriterion> _numericalStabilityTerminationCriteria =
            new List<ITerminationCriterion> { new ConditionCov(), new NoEffectAxis(), new NoEffectCoord() };

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="CovarianceMatrixAdaptationStrategyBase{TSearchPoint, TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="configuration">Options used for this instance.</param>
        /// <param name="parameterTree">Provides the tunable parameters.</param>
        /// <param name="genomeSorter">
        /// An <see cref="IActorRef" /> to a <see cref="GenomeSorter{TInstance,TResult}" />.
        /// </param>
        /// <param name="targetRunResultStorage">
        /// An <see cref="IActorRef" /> to a <see cref="ResultStorageActor{TInstance,TResult}" />
        /// which knows about all executed target algorithm runs and their results.
        /// </param>
        protected CovarianceMatrixAdaptationStrategyBase(
            AlgorithmTunerConfiguration configuration,
            ParameterTree parameterTree,
            IActorRef genomeSorter,
            IActorRef targetRunResultStorage)
            : base(configuration, parameterTree, targetRunResultStorage, new RepairedGenomeSearchPointSorter<TSearchPoint, TInstance>(genomeSorter))
        {
            this.StrategyConfiguration =
                this.Configuration.ExtractDetailedConfiguration<CovarianceMatrixAdaptationStrategyConfiguration>(
                    CovarianceMatrixAdaptationStrategyArgumentParser.Identifier);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets detailed options about this strategy.
        /// </summary>
        protected CovarianceMatrixAdaptationStrategyConfiguration StrategyConfiguration { get; }

        /// <summary>
        /// Gets the path to use when working with
        /// <see cref="CovarianceMatrixAdaptationStrategyStatus{PartialGenomeSearchPoint, TInstance}"/>.
        /// </summary>
        protected string StrategyStatusFilePath => Path.Combine(
            this.Configuration.StatusFileDirectory,
            CovarianceMatrixAdaptationStrategyStatus<TSearchPoint, TInstance>.FileName);

        /// <summary>
        /// Gets the path to use when working with <see cref="CmaEsStatus"/>.
        /// </summary>
        protected override string ContinuousOptimizerStatusFilePath =>
            Path.Combine(this.Configuration.StatusFileDirectory, CmaEsStatus.FileName);

        #endregion

        #region Methods

        /// <summary>
        /// Creates the termination criteria for the upcoming CMA-ES phase.
        /// </summary>
        /// <returns>The created set of termination criteria.</returns>
        protected IEnumerable<ITerminationCriterion> CreateTerminationCriteria()
        {
            var terminationCriteria = new List<ITerminationCriterion>(this._numericalStabilityTerminationCriteria);
            terminationCriteria.Add(new TolUpSigma());
            terminationCriteria.Add(new MaxIterations(this.StrategyConfiguration.MaximumNumberGenerations));

            return terminationCriteria;
        }

        /// <inheritdoc />
        protected override bool HasFixedInstances()
        {
            return this.StrategyConfiguration.FixInstances;
        }

        /// <inheritdoc />
        protected override ContinuousOptimizationStrategyStatusBase<TSearchPoint, TInstance> DeserializeStrategyStatusFile()
        {
            return StatusBase.ReadFromFile<CovarianceMatrixAdaptationStrategyStatus<TSearchPoint, TInstance>>(
                this.StrategyStatusFilePath);
        }

        #endregion
    }
}