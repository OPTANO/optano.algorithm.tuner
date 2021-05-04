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

namespace Optano.Algorithm.Tuner.Tuning
{
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Switches between different <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/> objects.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    internal class PopulationUpdateStrategyManager<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// All possible <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/>s.
        /// </summary>
        private readonly List<IPopulationUpdateStrategy<TInstance, TResult>> _populationUpdateStrategies;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PopulationUpdateStrategyManager{TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="populationUpdateStrategies">All possible <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/>s.</param>
        /// <param name="configuration">Algorithm tuner configuration parameters.</param>
        public PopulationUpdateStrategyManager(
            List<IPopulationUpdateStrategy<TInstance, TResult>> populationUpdateStrategies,
            AlgorithmTunerConfiguration configuration)
        {
            this._populationUpdateStrategies = populationUpdateStrategies;
            this.CurrentUpdateStrategyIndex = 0;
            this.BasePopulation = new Population(configuration);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the index of the current <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/> in
        /// <see cref="_populationUpdateStrategies"/>.
        /// </summary>
        public int CurrentUpdateStrategyIndex { get; private set; }

        /// <summary>
        /// Gets the genome population the current <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/> gets based on.
        /// Is updated each time the strategy is changed.
        /// </summary>
        public Population BasePopulation { get; private set; }

        /// <summary>
        /// Gets the current strategy.
        /// </summary>
        public IPopulationUpdateStrategy<TInstance, TResult> CurrentStrategy => this._populationUpdateStrategies[this.CurrentUpdateStrategyIndex];

        /// <summary>
        /// Gets a value indicating whether <see cref="BasePopulation"/> was initialized.
        /// </summary>
        public bool HasPopulation => !this.BasePopulation.IsEmpty();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Calls <see cref="IPopulationUpdateStrategy{TInstance,TResult}.DumpStatus"/> for all possible strategies.
        /// </summary>
        public void DumpStatus()
        {
            foreach (var strategy in this._populationUpdateStrategies)
            {
                strategy.DumpStatus();
            }
        }

        /// <summary>
        /// Uses a status dump.
        /// </summary>
        /// <param name="currentStrategyIndex">
        /// Index of the current <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/> in
        /// <see cref="_populationUpdateStrategies"/>.
        /// </param>
        /// <param name="population">The population as stored in <see cref="BasePopulation"/>.</param>
        /// <param name="geneticEngineering">
        /// Reference to genetic engineering used by
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}"/>.
        /// </param>
        public void UseStatusDump(int currentStrategyIndex, Population population, IGeneticEngineering geneticEngineering)
        {
            this.CurrentUpdateStrategyIndex = currentStrategyIndex;
            this.BasePopulation = population;

            // Restore status of all population update strategies.
            foreach (var strategy in this._populationUpdateStrategies)
            {
                strategy.UseStatusDump(geneticEngineering);
            }
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="population">The population to start with.</param>
        public void Initialize(Population population)
        {
            this.BasePopulation = population;
            this.CurrentStrategy.Initialize(new Population(this.BasePopulation), null, null, 0, false);
        }

        /// <summary>
        /// Changes to the correct <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/> based on the current
        /// strategy.
        /// </summary>
        /// <param name="initializationInstances">
        /// Instances to use for potential evaluations on strategy initialization.
        /// </param>
        /// <param name="currentIncumbent">Most recent incumbent genome. Might be <c>null</c>.</param>
        /// <param name="currentGeneration">The current generation.</param>
        /// <param name="useGrayBoxInGeneration">Boolean indicating whether to use gray box tuning in current generation.</param>
        /// <returns>The chosen <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/>.</returns>
        public IPopulationUpdateStrategy<TInstance, TResult> ChangePopulationUpdateStrategy(
            ICollection<TInstance> initializationInstances,
            IncumbentGenomeWrapper<TResult> currentIncumbent,
            int currentGeneration,
            bool useGrayBoxInGeneration)
        {
            var currentStrategy = this.CurrentStrategy;
            while (currentStrategy.HasTerminated())
            {
                this.FinishPhase();

                this.CurrentUpdateStrategyIndex = currentStrategy.NextStrategy(this._populationUpdateStrategies);

                var newStrategy = this.CurrentStrategy;
                newStrategy.Initialize(this.BasePopulation, currentIncumbent, initializationInstances, currentGeneration, useGrayBoxInGeneration);

                LoggingHelper.WriteLine(
                    VerbosityLevel.Info,
                    $"Changing strategy from {currentStrategy.GetType().Name} to {newStrategy.GetType().Name}.");

                currentStrategy = newStrategy;
            }

            return currentStrategy;
        }

        /// <summary>
        /// Finishes a strategy phase.
        /// </summary>
        public void FinishPhase()
        {
            this.BasePopulation = this.CurrentStrategy.FinishPhase(this.BasePopulation);
        }

        #endregion
    }
}