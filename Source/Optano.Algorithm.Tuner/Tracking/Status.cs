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

namespace Optano.Algorithm.Tuner.Tracking
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.MachineLearning.Prediction;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.Tuning;

    /// <summary>
    /// An object wrapping the current status of tuning a target algorithm.
    /// Can be serialized to a file and deserialized from one.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    /// <typeparam name="TLearnerModel">
    /// ML model that trains a <typeparamref name="TPredictorModel"/>.
    /// </typeparam>
    /// <typeparam name="TPredictorModel">
    /// A trained ML model that predicts the performance for a given <see cref="Genome"/>.
    /// </typeparam>
    /// <typeparam name="TSamplingStrategy">
    /// A strategy used for aggregating observed training data.
    /// </typeparam>
    public class Status<TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy> : StatusBase
        where TResult : ResultBase<TResult>, new()
        where TInstance : InstanceBase
        where TLearnerModel : IGenomeLearner<TPredictorModel, TSamplingStrategy>
        where TPredictorModel : IEnsemblePredictor<GenomePredictionTree>
        where TSamplingStrategy : IEnsembleSamplingStrategy<IGenomePredictor>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="Status{TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}"/> class.
        /// </summary>
        /// <param name="generation">
        /// The current generation.
        /// </param>
        /// <param name="population">
        /// The current population stored by the <see cref="PopulationUpdateStrategyManager{TInstance,TResult}"/>.
        /// </param>
        /// <param name="configuration">
        /// The algorithm tuner configuration parameters.
        /// </param>
        /// <param name="geneticEngineering">
        /// The complete <see cref="GeneticEngineering{TLearnerModel, TPredictorModel, TSamplingStrategy}"/> object 
        /// contained in <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}"/>.
        /// </param>
        /// <param name="currentUpdateStrategyIndex">
        /// Index of the current <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/> used by
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}"/>.
        /// </param>
        /// <param name="incumbentQuality">
        /// The average incumbent compare value scores.
        /// </param>
        /// <param name="incumbentGenomeWrapper">
        /// The incumbent genome wrapper.
        /// </param>
        /// <param name="informationHistory">
        /// <see cref="GenerationInformation"/> for all previous generations.
        /// </param>
        /// <param name="elapsedTime">
        /// The elapsed time of the tuning.
        /// </param>
        /// <param name="defaultGenome">The default genome, if any. Can be null.</param>
        [SuppressMessage(
            "NDepend",
            "ND1002:MethodsWithTooManyParametersCritical",
            Justification = "The constructor needs to get all of these parameters.")]
        public Status(
            int generation,
            Population population,
            AlgorithmTunerConfiguration configuration,
            GeneticEngineering<TLearnerModel, TPredictorModel, TSamplingStrategy> geneticEngineering,
            int currentUpdateStrategyIndex,
            List<double> incumbentQuality,
            IncumbentGenomeWrapper<TResult> incumbentGenomeWrapper,
            List<GenerationInformation> informationHistory,
            TimeSpan elapsedTime,
            ImmutableGenome defaultGenome = null)
        {
            this.Generation = generation;
            this.Population = population ??
                              throw new ArgumentNullException(nameof(population));
            this.Configuration = configuration ??
                                 throw new ArgumentNullException(nameof(configuration));
            this.GeneticEngineering = geneticEngineering ??
                                      throw new ArgumentNullException(nameof(geneticEngineering));
            this.CurrentUpdateStrategyIndex = currentUpdateStrategyIndex;
            this.IncumbentQuality = incumbentQuality ??
                                    throw new ArgumentNullException(nameof(incumbentQuality));
            this.IncumbentGenomeWrapper = incumbentGenomeWrapper ??
                                          throw new ArgumentNullException(nameof(incumbentGenomeWrapper));
            this.InformationHistory = informationHistory ?? throw new ArgumentNullException(nameof(informationHistory));
            this.ElapsedTime = elapsedTime;
            this.DefaultGenome = defaultGenome;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the default genome, if any. Can be null.
        /// </summary>
        public ImmutableGenome DefaultGenome { get; }

        /// <summary>
        /// Gets the incumbent genome wrapper.
        /// </summary>
        public IncumbentGenomeWrapper<TResult> IncumbentGenomeWrapper { get; }

        /// <summary>
        /// Gets the incumbent quality.
        /// </summary>
        public List<double> IncumbentQuality { get; }

        /// <summary>
        /// Gets the stored generation.
        /// </summary>
        public int Generation { get; }

        /// <summary>
        /// Gets the population as stored by
        /// <see cref="PopulationUpdateStrategyManager{TInstance,TResult}"/>.
        /// </summary>
        public Population Population { get; }

        /// <summary>
        /// Gets the configuration used for the run so far.
        /// </summary>
        public AlgorithmTunerConfiguration Configuration { get; }

        /// <summary>
        /// Gets the genetic engineering in its current status.
        /// </summary>
        public GeneticEngineering<TLearnerModel, TPredictorModel, TSamplingStrategy> GeneticEngineering { get; }

        /// <summary>
        /// Gets a number of run results.
        /// </summary>
        public ImmutableDictionary<ImmutableGenome, ImmutableDictionary<TInstance, TResult>> RunResults { get; private set; }

        /// <summary>
        /// Gets the index of the current <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/> used by
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}"/>.
        /// </summary>
        public int CurrentUpdateStrategyIndex { get; }

        /// <summary>
        /// Gets the elapsed time of the tuning.
        /// </summary>
        public TimeSpan ElapsedTime { get; }

        /// <summary>
        /// Gets <see cref="GenerationInformation"/>s for all previous generations.
        /// </summary>
        public List<GenerationInformation> InformationHistory { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Sets run results for the <see cref="Status{TInstance, TResult, TLearnerModel, TPredictorModel, TSamplingStrategy}"/> object.
        /// </summary>
        /// <param name="runResults">
        /// The run results to store.
        /// </param>
        public void SetRunResults(ImmutableDictionary<ImmutableGenome, ImmutableDictionary<TInstance, TResult>> runResults)
        {
            this.RunResults = runResults;
        }

        /// <summary>
        /// Serializes the <see cref="Status{TInstance,TResult,TLearnerModel,TPredictorModel,TSamplingStrategy}"/> object and writes it to a file.
        /// </summary>
        /// <param name="path">
        /// File to write the object to.
        /// </param>
        public override void WriteToFile(string path)
        {
            // Check object is complete
            if (this.RunResults == null)
            {
                throw new InvalidOperationException("Cannot write incomplete status to file. Please call SetRunResults.");
            }

            base.WriteToFile(path);
        }

        #endregion
    }
}