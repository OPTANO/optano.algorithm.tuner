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

namespace Optano.Algorithm.Tuner.MachineLearning
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning.GenomeRepresentation;
    using Optano.Algorithm.Tuner.MachineLearning.Prediction;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestTopPerformerFocus;
    using Optano.Algorithm.Tuner.MachineLearning.TargetLeafSampling;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;

    using SharpLearning.Containers.Matrices;
    using SharpLearning.DecisionTrees.Models;
    using SharpLearning.DecisionTrees.SplitSearchers;

    /// <summary>
    /// This class preforms the training of the random forest predicting parameter combination quality, and performs
    /// the 'genetic engineering' as described in
    /// https://wiwi.uni-paderborn.de/fileadmin/dep3ls7/Downloads/Publikationen/PDFs/IJCAI-15_1.pdf.
    /// </summary>
    /// <typeparam name="TLearnerModel">
    /// ML algorithm that can train an <see cref="IEnsemblePredictor{TWeakPredictor}"/>.
    /// </typeparam>
    /// <typeparam name="TPredictorModel">
    /// Ensemble predictor that works on <see cref="GenomePredictionTree"/>s 
    /// for predicting performance of potential offspring genomes.
    /// </typeparam>
    /// <typeparam name="TSamplingStrategy">
    /// Sampling strategy to use for aggregating the training data contained in a <see cref="TrainingDataWrapper"/>.
    /// </typeparam>
    public sealed class GeneticEngineering<TLearnerModel, TPredictorModel, TSamplingStrategy> : IGeneticEngineering
        where TLearnerModel : IGenomeLearner<TPredictorModel, TSamplingStrategy>
        where TPredictorModel : IEnsemblePredictor<GenomePredictionTree>
        where TSamplingStrategy : IEnsembleSamplingStrategy<IGenomePredictor>, new()
    {
        #region Fields

        /// <summary>
        /// The atomic parent counter.
        /// Should only be accessed with <see cref="GetNextParentCounterIndex"/> (or <see cref="Interlocked.Increment(ref int)"/>.
        /// </summary>
        private int _atomicParentCounter;

        /// <summary>
        /// Gets the current generation.
        /// </summary>
        private int _currentGeneration;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneticEngineering{TLearnerModel,TPredictorModel,TSamplingStrategy}"/> class. 
        /// </summary>
        /// <param name="tree">
        /// The parameter tree to work on.
        /// </param>
        /// <param name="configuration">
        /// The configuration.
        /// </param>
        public GeneticEngineering(ParameterTree tree, AlgorithmTunerConfiguration configuration)
        {
            if (tree == null)
            {
                throw new ArgumentNullException(nameof(tree));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // TODO MAYBE #31822: Make encoding in genome transformation configurable.
            this.Configuration = configuration;
            this.GenomeTransformator = new BulkGenomeTransformation<CategoricalBinaryEncoding>(tree);
            this.RandomForestLearner = this.CreateRandomForestLearner();
        }

        #endregion

        #region Explicit Interface properties

        /// <summary>
        /// Gets the interface specific implementation of <see cref="GenomeTransformator" />.
        /// </summary>
        IBulkGenomeTransformation IGeneticEngineering.GenomeTransformator => this.GenomeTransformator;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        private AlgorithmTunerConfiguration Configuration { get; }

        /// <summary>
        /// Gets a transformator, that can transform a <see cref="Genome" /> into its respective <see cref="T:double[]" />
        /// representation, using a <see cref="CategoricalBinaryEncoding" />.
        /// </summary>
        private IBulkGenomeTransformation GenomeTransformator { get; }

        /// <summary>
        /// Gets the Custom random forest learner that uses the <see cref="LinearSplitSearcher{TImpurityCalculator}" /> and
        /// <see cref="TopPerformerFocusImpurityCalculator" />.
        /// </summary>
        private TLearnerModel RandomForestLearner { get; }

        /// <summary>
        /// Gets or sets the random forest predictor that was trained by <see cref="RandomForestLearner" />.
        /// </summary>
        private TPredictorModel RandomForestPredictor { get; set; }

        /// <summary>
        /// Gets the list of the currently learned <see cref="RandomForestPredictor" />'s internal trees.
        /// Required for the <see cref="ConstructOffspring" /> (= Targeted Sampling) method.
        /// </summary>
        private GenomePredictionTree[] Trees => this.RandomForestPredictor.InternalModels;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Engineers an offspring-genomes for each <paramref name="chosenCompetitiveParents"/>, using the targeted sampling algorithm.
        /// </summary>
        /// <param name="chosenCompetitiveParents">
        /// The chosen Competitive Parents.
        /// </param>
        /// <param name="nonCompetitiveMates">
        /// The non Competitive Mates.
        /// </param>
        /// <param name="genomesForDistanceComputation">
        /// The genomes For Distance Computation.
        /// </param>
        /// <returns>
        /// The offspring.
        /// </returns>
        public IEnumerable<Genome> EngineerGenomes(
            List<Genome> chosenCompetitiveParents,
            IReadOnlyList<Genome> nonCompetitiveMates,
            IEnumerable<Genome> genomesForDistanceComputation)
        {
            // sexual selection: rank all non-competitive genomes
            var transformedNonCompetitives = this.GenomeTransformator.ConvertAllGenomesToMatrix(nonCompetitiveMates);
            var attractiveness = this.GetAttractivenessMeasure(transformedNonCompetitives);

            // Prepare data for distance computation
            // handle each individual genome once in algorithm. don't forget actual occurrence count.
            var distanceComputationGenomeCounts = genomesForDistanceComputation.GroupBy(g => g, Genome.GenomeComparer)
                .ToDictionary(g => g.Key, g => g.Count(), Genome.GenomeComparer);
            var orderedUniqueDistanceComputationGenomes = distanceComputationGenomeCounts.Keys.ToArray();
            var distanceGenomeOccurrences = orderedUniqueDistanceComputationGenomes.Select(g => distanceComputationGenomeCounts[g]).ToArray();

            // only use a subset of features to compute distances to.
            var distanceFeatureSubSet = this.SelectColumnIndicesForDistanceComputation(transformedNonCompetitives.ColumnCount);
            var slicedConvertedDistanceComputationGenomes = this.GenomeTransformator
                .ConvertAllGenomesToMatrix(orderedUniqueDistanceComputationGenomes).Columns(distanceFeatureSubSet);

            // reset parent counter
            this._atomicParentCounter = -1;
            var engineeredGenomes = new ConcurrentBag<Genome>();
            var lengthOfFeatures = this.GenomeTransformator.GetFeatureLengths();
            var indexSetsForCategoricalFeaturesInDoubleRepresentation =
                ComputeIndexSetsForCategoricalFeaturesInDoubleRepresentation(lengthOfFeatures);
            var partitionOptions = new ParallelOptions { MaxDegreeOfParallelism = this.Configuration.MaximumNumberParallelThreads };
            var rangePartitioner = Partitioner.Create(chosenCompetitiveParents, true);
            Parallel.ForEach(
                rangePartitioner,
                partitionOptions,
                (compParent, loopState) =>
                    {
                        var transformedCompetitive = this.GenomeTransformator.ConvertGenomeToArray(compParent);

                        // choose a non-competitive mate based on attractiveness
                        var chosenIndex = Randomizer.Instance.RouletteSelect(attractiveness, true);
                        var nonCompParent = transformedNonCompetitives.Row(chosenIndex);
                        var parentGenomes = new ParentGenomesConverted(transformedCompetitive, nonCompParent);
                        var offspring = this.ConstructOffspring(
                            parentGenomes,
                            indexSetsForCategoricalFeaturesInDoubleRepresentation,
                            distanceFeatureSubSet,
                            slicedConvertedDistanceComputationGenomes,
                            distanceGenomeOccurrences);

                        var genome = this.TransformOffspringToGenome(offspring);
                        genome.IsEngineered = true;
                        engineeredGenomes.Add(genome);
                    });

            Debug.Assert(
                engineeredGenomes.Count == chosenCompetitiveParents.Count,
                "In the end we need to have 1 offspring for every competitive parent");
            return engineeredGenomes;
        }

        /// <summary>
        /// Gets the predicted ranks for <paramref name="nonCompetitiveMates"/>, which can be used for weights in sexual
        /// selection.
        /// See <see cref="Randomizer.RouletteSelect"/>.
        /// </summary>
        /// <param name="nonCompetitiveMates">
        /// The non competitive parents to score.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/> attractiveness measure.
        /// Lower numbers (=ranks in this case) indicate higher attractiveness.
        /// </returns>
        public double[] GetAttractivenessMeasure(IEnumerable<Genome> nonCompetitiveMates)
        {
            // forest is not initialized in this case:
            if (this.Configuration.EngineeredPopulationRatio <= 0 || !this.Configuration.EnableSexualSelection)
            {
                return Enumerable.Repeat(1d, nonCompetitiveMates.Count()).ToArray();
            }

            // transform non-comp to F64Matrix
            var transformedNonCompetitives = this.GenomeTransformator.ConvertAllGenomesToMatrix(nonCompetitiveMates);

            return this.GetAttractivenessMeasure(transformedNonCompetitives);
        }

        /// <summary>
        /// Trains a random forest.
        /// Updates <see cref="Trees"/>: After the call, <see cref="Trees"/> will contain all
        /// <see cref="RegressionDecisionTreeModel"/> that were created within the random forest.
        /// </summary>
        /// <param name="trainingData">
        /// The observations to learn.
        /// </param>
        public void TrainForest(TrainingDataWrapper trainingData)
        {
            trainingData.ConvertedGenomes = this.GenomeTransformator.ConvertAllGenomesToMatrix(trainingData.Genomes);
            this.RandomForestPredictor = this.RandomForestLearner.Learn(trainingData);
            this._currentGeneration = trainingData.CurrentGeneration;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Hyperion Serializer "removes" Array-/Genome Comparers from restored Dictionaries, which leads to Key misses.
        /// Explicitly reload/restore dictionaries with correct comparers here.
        /// Calls recursively to subclasses.
        /// </summary>
        internal void RestoreInternalDictionariesWithCorrectComparers()
        {
            this.GenomeTransformator.RestoreInternalDictionariesWithCorrectComparers();
        }

        /// <summary>
        /// Computes the index sets for categorical features in double representation.
        /// </summary>
        /// <param name="lengthOfFeatures">
        /// The lengths of features.
        /// </param>
        /// <returns>
        /// Dictionary that uses all indices of <c>all</c> categorical features and maps them to the set of indices that represent the key's categorical feature.
        /// </returns>
        private static Dictionary<int, HashSet<int>> ComputeIndexSetsForCategoricalFeaturesInDoubleRepresentation(IReadOnlyList<int> lengthOfFeatures)
        {
            var featureIndexSets = new Dictionary<int, HashSet<int>>();
            if (lengthOfFeatures.Count == 0)
            {
                return featureIndexSets;
            }

            // start of current feature in the double[] representation.
            var startIndexInDoubleRepresentation = 0;
            for (var currentFeature = 0; currentFeature < lengthOfFeatures.Count; currentFeature++)
            {
                if (currentFeature > 0)
                {
                    startIndexInDoubleRepresentation += lengthOfFeatures[currentFeature - 1];
                }

                if (lengthOfFeatures[currentFeature] <= 1)
                {
                    // not a categorical feature -> ignore.
                    continue;
                }

                var categoricalFeatureIndices =
                    new HashSet<int>(Enumerable.Range(startIndexInDoubleRepresentation, lengthOfFeatures[currentFeature]));

                foreach (var featureIndex in categoricalFeatureIndices)
                {
                    featureIndexSets.Add(featureIndex, categoricalFeatureIndices);
                }
            }

            return featureIndexSets;
        }

        /// <summary>
        /// Given the <paramref name="normalizedDistanceFactored"/> and the
        /// <paramref name="predictedRankForPotentialOffspring"/>,
        /// this method computes the resulting <c>final score</c> for all potential offspring and returns the
        /// <see cref="GenomeDoubleRepresentation"/>
        /// with best predicted score.
        /// </summary>
        /// <param name="predictedRankForPotentialOffspring">
        /// The rank for every potential offspring, as predicted by the random
        /// forest.
        /// </param>
        /// <param name="normalizedDistanceFactored">
        /// Distances normalized to the range [0, distanceInfluenceFactor].
        /// </param>
        /// <returns>
        /// The (assumed) best offspring for the current pair of competitive- and non-competitive genomes.
        /// </returns>
        private static GenomeDoubleRepresentation DetermineBestOffspringOverAllLeaves(
            List<GenomeDoubleRepresentationWithPredictedRank> predictedRankForPotentialOffspring,
            double[] normalizedDistanceFactored)
        {
            if (predictedRankForPotentialOffspring.Count != normalizedDistanceFactored.Length)
            {
                throw new ArgumentException("Count of offspring does not match count of normalized distances for offspring.");
            }

            GenomeDoubleRepresentation bestOffspring = null;
            var bestScore = double.PositiveInfinity;
            for (var offspringIndex = 0; offspringIndex < normalizedDistanceFactored.Length; offspringIndex++)
            {
                var currentScore = predictedRankForPotentialOffspring[offspringIndex].PredictedRank - normalizedDistanceFactored[offspringIndex];
                if (currentScore < bestScore)
                {
                    bestScore = currentScore;
                    bestOffspring = predictedRankForPotentialOffspring[offspringIndex].ConvertedGenomeRepresentation;
                }
            }

            return bestOffspring;
        }

        /// <summary>
        /// Extract the feature representation of feature with length <paramref name="lengthOfCurrentFeature"/>, 
        /// starting at <paramref name="featureStartColumnIndex"/> from the <see cref="GenomeDoubleRepresentation"/> to work on.
        /// </summary>
        /// <param name="featureStartColumnIndex">
        /// The feature start column index.
        /// </param>
        /// <param name="lengthOfCurrentFeature">
        /// The length of current feature.
        /// </param>
        /// <param name="parentToUse">
        /// The parent to use.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/> representation of the requested feature.
        /// </returns>
        private static double[] ExtractFeatureRepresentationFromDonor(
            int featureStartColumnIndex,
            int lengthOfCurrentFeature,
            GenomeDoubleRepresentation parentToUse)
        {
            var doubleRepresentationOfNextFeature = new double[lengthOfCurrentFeature];
            for (var i = 0; i < lengthOfCurrentFeature; i++)
            {
                doubleRepresentationOfNextFeature[i] = parentToUse[featureStartColumnIndex + i];
            }

            return doubleRepresentationOfNextFeature;
        }

        /// <summary>
        /// Splits the given <paramref name="matrix"/> in rows.
        /// Returns a copy of the rows and does not alter the given object.
        /// </summary>
        /// <param name="matrix">
        /// The matrix.
        /// </param>
        /// <returns>
        /// The <see cref="IReadOnlyList{T}"/>, containing the rows of the given <paramref name="matrix"/>.
        /// </returns>
        private static IReadOnlyList<double[]> SplitMatrixInRows(IMatrix<double> matrix)
        {
            return Enumerable.Range(0, matrix.RowCount).Select(rowIndex => matrix.Row(rowIndex)).ToArray();
        }

        /// <summary>
        /// Extracts a sub array from <paramref name="array"/>.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="indices">The indices which should be copied to the new array.</param>
        /// <returns>A new array containing a subset of the values of <paramref name="array"/>.</returns>
        private static double[] ExtractSubArray(double[] array, IReadOnlyList<int> indices)
        {
            var subArray = new double[indices.Count];
            for (int index = 0; index < indices.Count; index++)
            {
                subArray[index] = array[indices[index]];
            }

            return subArray;
        }

        /// <summary>
        /// Makes sure that input to <see cref="ComputeDistanceForSinglePotentialOffspring"/> is valid.
        /// Logs/Throws otherwise.
        /// </summary>
        /// <param name="slicedConvertedDistanceComputationGenomes">
        /// Features of potential offspring that are relevant for current distance computation.
        /// </param>
        /// <param name="slicedGenomeHistory">
        /// Features of all genomes in current population that are relevant for current distance computation.
        /// </param>
        private static void ValidateAverageDistanceParameters(
            double[] slicedConvertedDistanceComputationGenomes,
            IReadOnlyList<double[]> slicedGenomeHistory)
        {
            if (ReferenceEquals(slicedConvertedDistanceComputationGenomes, null))
            {
                throw new ArgumentNullException(nameof(slicedConvertedDistanceComputationGenomes));
            }

            if (ReferenceEquals(slicedGenomeHistory, null))
            {
                throw new ArgumentNullException(nameof(slicedGenomeHistory));
            }

            // should/cannot happen
            if (slicedGenomeHistory.Count > 0 && slicedConvertedDistanceComputationGenomes.Length != slicedGenomeHistory[0].Length)
            {
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"WARNING - Error in distance-computation. Column Count of slicdOffspring: {slicedConvertedDistanceComputationGenomes.Length}. Column Count of slicedConvertedDistanceComputationGenomes: {slicedGenomeHistory[0].Length}. These numbers should match!");
                Debug.Fail(
                    $"WARNING - Error in distance-computation. Column Count of slicdOffspring: {slicedConvertedDistanceComputationGenomes.Length}. Column Count of slicedConvertedDistanceComputationGenomes: {slicedGenomeHistory[0].Length}. These numbers should match!");
                throw new InvalidOperationException(
                    $"WARNING - Error in distance-computation. Column Count of slicdOffspring: {slicedConvertedDistanceComputationGenomes.Length}. Column Count of slicedConvertedDistanceComputationGenomes: {slicedGenomeHistory[0].Length}. These numbers should match!");
            }
        }

        /// <summary>
        /// Minkowski-Distance is the L1-Norm.
        /// </summary>
        /// <param name="slicedOffspring">
        /// Offspring to compute distance for.
        /// </param>
        /// <param name="slicedConvertedDistanceComputationGenomes">
        /// Unique genomes to consider for distance computation.
        /// </param>
        /// <param name="occurrenceByRow">
        /// Number of occurrences per <paramref name="slicedConvertedDistanceComputationGenomes"/>
        /// before making collection distinct.
        /// </param>
        /// <returns>
        /// Average L1 distance between <paramref name="slicedOffspring"/> and all
        /// <paramref name="slicedConvertedDistanceComputationGenomes"/>.
        /// </returns>
        private static double ComputeAverageMinkowskiDistance(
            GenomeDoubleRepresentation slicedOffspring,
            IReadOnlyList<double[]> slicedConvertedDistanceComputationGenomes,
            int[] occurrenceByRow)
        {
            var distanceSum = 0d;

            for (var row = 0; row < slicedConvertedDistanceComputationGenomes.Count; row++)
            {
                var rowSum = 0d;
                for (var feature = 0; feature < slicedOffspring.Length; feature++)
                {
                    rowSum += Math.Abs(slicedOffspring[feature] - slicedConvertedDistanceComputationGenomes[row][feature]);
                }

                // some rows occur more than once
                distanceSum += rowSum * occurrenceByRow[row];
            }

            // take duplicates into account when computing average
            var averageDistance = distanceSum / occurrenceByRow.Sum();

            return averageDistance;
        }

        /// <summary>
        /// Creates a <typeparamref name="TLearnerModel"/> as specified by <see cref="Configuration"/>.
        /// </summary>
        /// <returns>The created <typeparamref name="TLearnerModel"/>.</returns>
        private TLearnerModel CreateRandomForestLearner()
        {
            var randomForestConfig =
                this.Configuration.ExtractDetailedConfiguration<GenomePredictionRandomForestConfig>(
                    RegressionForestArgumentParser.Identifier);

            var featureCount = this.GenomeTransformator.FeatureCount;
            var featuresPerSplit = (int)Math.Ceiling(featureCount * randomForestConfig.FeaturesPerSplitRatio);

            // TODO MAYBE #31822: Maybe we can use some kind of Factory here? Factory.Create would only be called a single time per run!
            return (TLearnerModel)Activator.CreateInstance(
                typeof(TLearnerModel),
                randomForestConfig,
                featuresPerSplit);
        }

        /// <summary>
        /// Computes distance between <paramref name="slicedOffspring"/> and all <parameref name="slicedConvertedDistanceComputationGenomes"/>.
        /// Method is used according to selected <see cref="DistanceMetric"/> in <see cref="AlgorithmTunerConfiguration.DistanceMetric"/>.
        /// </summary>
        /// <param name="slicedOffspring">
        /// Sliced, potential offspring to compute distances for.
        /// </param>
        /// <param name="slicedConvertedDistanceComputationGenomes">
        /// Sliced population to compute distances to. Each row should be unique.
        /// </param>
        /// <param name="occurrenceByRow">
        /// Occurrence count of duplicate population members.
        /// </param>
        /// <returns>
        /// The computed distance measure.
        /// </returns>
        private double ComputeDistanceForSinglePotentialOffspring(
            GenomeDoubleRepresentation slicedOffspring,
            IReadOnlyList<double[]> slicedConvertedDistanceComputationGenomes,
            int[] occurrenceByRow)
        {
            ValidateAverageDistanceParameters(slicedOffspring, slicedConvertedDistanceComputationGenomes);

            switch (this.Configuration.DistanceMetric)
            {
                case DistanceMetric.L1Average:
                    return GeneticEngineering<TLearnerModel, TPredictorModel, TSamplingStrategy>.ComputeAverageMinkowskiDistance(
                        slicedOffspring,
                        slicedConvertedDistanceComputationGenomes,
                        occurrenceByRow);
                case DistanceMetric.HammingDistance:
                    return this.ComputeHammingDistanceTotalSum(slicedOffspring, slicedConvertedDistanceComputationGenomes, occurrenceByRow);
                default:
                    throw new NotImplementedException($"Distance Metric {this.Configuration.DistanceMetric} is not implemented.");
            }
        }

        /// <summary>
        /// Compute the distances for all potential offspring.
        /// </summary>
        /// <param name="targetedSamplingOffspring">
        /// The targeted sampling offspring.
        /// </param>
        /// <param name="slicedConvertedDistanceComputationGenomes">
        /// The sliced converted distance computation genomes.
        /// Each genome should be unique. Use <paramref name="distancecomputationGenomeOccurrences"/> 
        /// to indicate the total number of occurrences in the base population.
        /// </param>
        /// <param name="distanceComputationFeatureIndices">
        /// The distance computation feature indices.
        /// </param>
        /// <param name="distancecomputationGenomeOccurrences">
        /// The distancecomputation genome occurrences.
        /// I.e. the total number of occurences for each given 
        /// (unique!) <paramref name="slicedConvertedDistanceComputationGenomes"/>.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/> distances.
        /// </returns>
        private double[] ComputeDistancesForAllPotentialOffspring(
            IEnumerable<GenomeDoubleRepresentation> targetedSamplingOffspring,
            IMatrix<double> slicedConvertedDistanceComputationGenomes,
            int[] distanceComputationFeatureIndices,
            int[] distancecomputationGenomeOccurrences)
        {
            // Matrix.Row creates a new double[] on every call. No need to repeat that 250+ times
            var distanceComputationGenomeFeaturesAsRows = SplitMatrixInRows(slicedConvertedDistanceComputationGenomes);
            // Only use relevant columns when computing distances.
            var distances = targetedSamplingOffspring.Select(
                potentialOffspring => this.ComputeDistanceForSinglePotentialOffspring(
                    GeneticEngineering<TLearnerModel, TPredictorModel, TSamplingStrategy>.ExtractSubArray(
                        potentialOffspring,
                        distanceComputationFeatureIndices),
                    distanceComputationGenomeFeaturesAsRows,
                    distancecomputationGenomeOccurrences)).ToArray();
            return distances;
        }

        /// <summary>
        /// Computes the hamming distances between <paramref name="slicedOffspring"/> 
        /// and all. <parameref name="slicedConvertedDistanceComputationGenomes"/>
        /// </summary>
        /// <param name="slicedOffspring">
        /// The sliced potential offspring.
        /// </param>
        /// <param name="slicedConvertedDistanceComputationGenomes">
        /// The sliced population members. Members should be unique.
        /// Use <paramref name="occurrenceByRow"/> to indicate the total number of occurrences for each member.
        /// </param>
        /// <param name="occurrenceByRow">
        /// The number of occurrences for each <paramref name="slicedConvertedDistanceComputationGenomes"/>.
        /// </param>
        /// <returns>
        /// The sum of all computed hamming distances.
        /// </returns>
        private double ComputeHammingDistanceTotalSum(
            GenomeDoubleRepresentation slicedOffspring,
            IReadOnlyList<double[]> slicedConvertedDistanceComputationGenomes,
            int[] occurrenceByRow)
        {
            // TODO MAYBE #31822: Find a nice way to pass and handle this parameter!
            var k = 3;

            // factor in the occurrences per row when sorting.
            var uniqueFeatureCounts = slicedConvertedDistanceComputationGenomes
                .SelectMany((row, idx) => Enumerable.Repeat((double)this.CountUniqueFeatures(slicedOffspring, row), occurrenceByRow[idx]))
                .TakeSmallestSorted(k);

            return uniqueFeatureCounts.Sum();
        }

        /// <summary>
        /// Computes distance for each <paramref name="targetedSamplingOffspring"/> 
        /// to all <paramref name="slicedConvertedDistanceComputationGenomes"/>.
        /// Distance is normalized to be in range [0, <see cref="AlgorithmTunerConfiguration.MaxRanksCompensatedByDistance"/>].
        /// </summary>
        /// <param name="targetedSamplingOffspring">
        /// All potential offspring.
        /// </param>
        /// <param name="slicedConvertedDistanceComputationGenomes">
        /// Unique set of the population members to compute distacnes to.
        /// </param>
        /// <param name="distanceComputationFeatureIndices">
        /// The indexes of features that are relevant for the current distance computation.
        /// </param>
        /// <param name="distanceGenomeOccurrences">
        /// The number of occurrences for each <paramref name="slicedConvertedDistanceComputationGenomes"/>
        /// in the original population.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/> containing the distance influence on the final predicted offspring score.
        /// </returns>
        private double[] ComputeNormalizedDistancesWithInfluenceFactor(
            IEnumerable<GenomeDoubleRepresentation> targetedSamplingOffspring,
            IMatrix<double> slicedConvertedDistanceComputationGenomes,
            int[] distanceComputationFeatureIndices,
            int[] distanceGenomeOccurrences)
        {
            if (this.Configuration.MaxRanksCompensatedByDistance < 1e-6)
            {
                // return 0 as distance for every genome. Rank will not be influenced by "distance = 0".
                var dummyDistances = new double[targetedSamplingOffspring.Count()];
                return dummyDistances;
            }

            // Compute + normalize all distances for all offspring
            var distances = this.ComputeDistancesForAllPotentialOffspring(
                targetedSamplingOffspring,
                slicedConvertedDistanceComputationGenomes,
                distanceComputationFeatureIndices,
                distanceGenomeOccurrences);

            var normalizedDistanceFactored = this.NormalizeDistancesAndApplyInfluenceFactor(distances);
            return normalizedDistanceFactored;
        }

        /// <summary>
        /// Computes the set of all leaves that any offspring of the given <paramref name="parentGenomes"/> can fall into.
        /// </summary>
        /// <param name="parentGenomes">
        /// The parent Genomes.
        /// </param>
        /// <param name="indexSetsForCategoricalFeaturesInDoubleRepresentation">
        /// The index sets for categorical features in double representation.
        /// (<see cref="ComputeIndexSetsForCategoricalFeaturesInDoubleRepresentation"/>).
        /// </param>
        /// <returns>
        /// The set of reachable leaf nodes.
        /// </returns>
        private IEnumerable<TreeNodeAndFixations> ComputeReachableLeavesAndFixations(
            ParentGenomesConverted parentGenomes,
            Dictionary<int, HashSet<int>> indexSetsForCategoricalFeaturesInDoubleRepresentation)
        {
            var reachedTargetLeaves = this.Trees.SelectMany(
                tree => TargetLeafComputation.ComputeReachableTargetLeavesForTree(
                    tree,
                    parentGenomes,
                    indexSetsForCategoricalFeaturesInDoubleRepresentation));

            return reachedTargetLeaves;
        }

        /// <summary>
        /// Generates |targetSampleSize| many parameterizations for every possible leaf node among all forests
        /// and returns the offspring for which the <see cref="RandomForestPredictor"/> predicts the best performance
        /// (in combination with some bonus with respect to the distance between each offspring and the current population).
        /// </summary>
        /// <param name="parentGenomes">
        /// The parent Genomes.
        /// </param>
        /// <param name="indexSetsForCategoricalFeaturesInDoubleRepresentation">
        /// The index sets for categorical features in double representation.
        /// (<see cref="ComputeIndexSetsForCategoricalFeaturesInDoubleRepresentation"/>).
        /// </param>
        /// <param name="distanceComputationFeatureIndices">
        /// Indices of features that should be considered during distance computation.
        /// </param>
        /// <param name="slicedConvertedDistanceComputationGenomes">
        /// Members of the current population to compute distances to.
        /// Already slieced to the set of <paramref name="distanceComputationFeatureIndices"/>.
        /// Only unique genomes required. Total count of occurences is given via <paramref name="distanceGenomeOccurrences"/>.
        /// </param>
        /// <param name="distanceGenomeOccurrences">
        /// The total number of occurences for each <paramref name="slicedConvertedDistanceComputationGenomes"/> in the base population.
        /// </param>
        /// <returns>
        /// The constructed offspring with best predicted score.
        /// </returns>
        private double[] ConstructOffspring(
            ParentGenomesConverted parentGenomes,
            Dictionary<int, HashSet<int>> indexSetsForCategoricalFeaturesInDoubleRepresentation,
            int[] distanceComputationFeatureIndices,
            IMatrix<double> slicedConvertedDistanceComputationGenomes,
            int[] distanceGenomeOccurrences)
        {
            // target leaves will contain all reachable leaves alongside the set of restricted features and the respective parent, that restricts a feature.
            // if a feature index does not appear in the set of restricted features, the value can be chosen from one of the parents at random.
            var reachableLeaves = this.ComputeReachableLeavesAndFixations(parentGenomes, indexSetsForCategoricalFeaturesInDoubleRepresentation);

            // sample k random combinations between comp-/non-comp parent for every leaf and use forest to predict scores
            var predictedRanksForAllPotentialOffspring = this.PerformTargetedSampling(
                parentGenomes,
                indexSetsForCategoricalFeaturesInDoubleRepresentation,
                reachableLeaves);
            var offspringWithChanceOfSelection = this.RemoveTerribleOffspring(predictedRanksForAllPotentialOffspring);
#if DEBUG
            var droppedOffspringCount = predictedRanksForAllPotentialOffspring.Count - offspringWithChanceOfSelection.Count;
            LoggingHelper.WriteLine(
                VerbosityLevel.Debug,
                $"Removed {droppedOffspringCount} duplicate or low performing potential offspring. Remaining: {offspringWithChanceOfSelection.Count}");
#endif

            // normalize distances to range [0, distanceMaxInfluence]
            var normalizedDistancesFactored = this.ComputeNormalizedDistancesWithInfluenceFactor(
                offspringWithChanceOfSelection.Select(pr => pr.ConvertedGenomeRepresentation),
                slicedConvertedDistanceComputationGenomes,
                distanceComputationFeatureIndices,
                distanceGenomeOccurrences);

            // check what offspring is best according to predicted rank - "diversity"/distance [larger distance: better]
            var bestOffspring = DetermineBestOffspringOverAllLeaves(offspringWithChanceOfSelection, normalizedDistancesFactored);
            return bestOffspring;
        }

        /// <summary>
        /// Removes all "terrible" offspring.
        /// Offspring is considered "terrible" if it has no chance to "overtake" the best predicted offspring by using the <see cref="AlgorithmTunerConfiguration.MaxRanksCompensatedByDistance"/>.
        /// I.e. Rank(offspring) > Rank(bestPredictedOffspring) + <see cref="AlgorithmTunerConfiguration.MaxRanksCompensatedByDistance"/> (+ eps.)
        /// Additionally, duplicate offspring will be removed, too.
        /// </summary>
        /// <param name="potentialOffspring">
        /// The potential offspring to filter.
        /// </param>
        /// <returns>
        /// The <see cref="List{GenomeDoubleRepresentationWithPredictedRank}"/> with offspring that has a chance to be selected as final offspring.
        /// </returns>
        private List<GenomeDoubleRepresentationWithPredictedRank> RemoveTerribleOffspring(
            IReadOnlyCollection<GenomeDoubleRepresentationWithPredictedRank> potentialOffspring)
        {
            var bestPredictedRank = potentialOffspring.Min(o => o.PredictedRank);
#if DEBUG
            var worstPredictedRank = potentialOffspring.Max(o => o.PredictedRank);
            LoggingHelper.WriteLine(
                VerbosityLevel.Debug,
                $"PB: {bestPredictedRank:0.000} - PW: {worstPredictedRank:0.000} - Diff: {worstPredictedRank - bestPredictedRank:0.000}");
#endif
            var selectionThreshold = bestPredictedRank + this.Configuration.MaxRanksCompensatedByDistance + 1e-6;
            // the GenomeDoubleRepresentationWithRank overrides Equals + GetHashCode, so that we can use the "default distinct" here.
            return potentialOffspring.Where(o => o.PredictedRank <= selectionThreshold).Distinct().ToList();
        }

        /// <summary>
        /// Counts the number of features that are different between <paramref name="offspring"/> and
        /// <paramref name="populationMember"/>.
        /// A feature is considered to be different when the relative difference (i.e. offspringValue/populationMemberValue)
        /// exceeds 1e-2.
        /// </summary>
        /// <param name="offspring">
        /// The offspring for which the current distance is computed.
        /// </param>
        /// <param name="populationMember">
        /// A member of the current/historical population.
        /// </param>
        /// <returns>
        /// The number of unique features.
        /// </returns>
        private int CountUniqueFeatures(double[] offspring, double[] populationMember)
        {
            // Do not divide by popMember to avoid handling popMember[idx] == 0 Instead: Multiply everything by |popMemberValue|.
            // TODO MAYBE #34847: Maybe it is a good idea to use multiplier |popMember[idx]| on the right side only, if |popMember[idx]| > 1? For values < 1 the resulting absolute difference will be 'stricter' than the current relative difference.
            return offspring
                .Select(
                    (offspringValue, idx) => Math.Abs(offspringValue - populationMember[idx])
                                             > Math.Abs(populationMember[idx] * this.Configuration.HammingDistanceRelativeThreshold))
                .Count(isUnique => isUnique);
        }

        /// <summary>
        /// Creates a single target sampling offspring.
        /// </summary>
        /// <param name="leaf">
        /// The leaf that is targeted.
        /// </param>
        /// <param name="parentGenomes">
        /// The parent genomes.
        /// </param>
        /// <param name="indexSetsForCategoricalFeaturesInDoubleRepresentation">
        /// The index sets for categorical features in double representation.
        /// (<see cref="ComputeIndexSetsForCategoricalFeaturesInDoubleRepresentation"/>).
        /// </param>
        /// <returns>
        /// The <see cref="GenomeDoubleRepresentation"/> of the generated offspring.
        /// </returns>
        private GenomeDoubleRepresentation CreateSingleTargetSampleOffspring(
            TreeNodeAndFixations leaf,
            ParentGenomesConverted parentGenomes,
            Dictionary<int, HashSet<int>> indexSetsForCategoricalFeaturesInDoubleRepresentation)
        {
            var lengthOfGenomeRepresentation = parentGenomes.LengthOfDoubleRepresentation;
            GenomeDoubleRepresentation offspring = new double[lengthOfGenomeRepresentation];

            // construct offspring, taking fixed features into account
            var featureStartColumnIndex = 0;
            while (featureStartColumnIndex < lengthOfGenomeRepresentation)
            {
                var lengthOfCurrentFeature = indexSetsForCategoricalFeaturesInDoubleRepresentation.ContainsKey(featureStartColumnIndex)
                                                 ? indexSetsForCategoricalFeaturesInDoubleRepresentation[featureStartColumnIndex].Count
                                                 : 1;
                var doubleRepresentationOfNextFeature = this.DetermineDoubleRepresentationOfNextFeature(
                    leaf,
                    parentGenomes,
                    featureStartColumnIndex,
                    lengthOfCurrentFeature);

                Debug.Assert(doubleRepresentationOfNextFeature.Length == lengthOfCurrentFeature, "Offspring's length should match parent's length.");
                for (var i = 0; i < lengthOfCurrentFeature; i++)
                {
                    offspring[featureStartColumnIndex + i] = doubleRepresentationOfNextFeature[i];
                }

                // skip additional columns of categorical feature representation.
                featureStartColumnIndex += lengthOfCurrentFeature;

                // last iteration: ==, else: <. > always is an error
                Debug.Assert(
                    featureStartColumnIndex <= lengthOfGenomeRepresentation,
                    "FeatureColumnIndex was incremented out of offspring[]-bounds. Should at most be set to offspring[].Length");
            }

            Debug.Assert(featureStartColumnIndex == lengthOfGenomeRepresentation, "Not all featues were handled during GeneticEngineering.");
            return offspring;
        }

        /// <summary>
        /// Determines the current feature value donor.
        /// Donor depends on whether the current feature is fixed to a certain parent or not.
        /// </summary>
        /// <param name="leaf">
        /// The leaf.
        /// </param>
        /// <param name="parentGenomes">
        /// The parent genomes.
        /// </param>
        /// <param name="featureStartColumnIndex">
        /// The feature start column index.
        /// </param>
        /// <returns>
        /// The <see cref="GenomeDoubleRepresentation"/>.
        /// If feature is not fixed to a specific parent, a biased coin is flipped.
        /// Probability for selecting feature of competitive parent: <see cref="AlgorithmTunerConfiguration.CrossoverProbabilityCompetitive"/>.
        /// </returns>
        private GenomeDoubleRepresentation DetermineCurrentFeatureValueDonor(
            TreeNodeAndFixations leaf,
            ParentGenomesConverted parentGenomes,
            int featureStartColumnIndex)
        {
            // no choice for fixed features
            if (leaf.FixedIndicesInDoubleRepresentation.ContainsKey(featureStartColumnIndex))
            {
                return parentGenomes.GetParentToFollow(leaf.FixedIndicesInDoubleRepresentation[featureStartColumnIndex]);
            }

            // flip a coin for non-fixed features
            if (Randomizer.Instance.Decide(this.Configuration.CrossoverProbabilityCompetitive))
            {
                return parentGenomes.CompetitiveParent;
            }
            else
            {
                return parentGenomes.NonCompetitiveParent;
            }
        }

        /// <summary>
        /// Determine the double representation of next feature.
        /// Depends on fixation (or a biased coin flip for 'free' feature).
        /// </summary>
        /// <param name="leaf">
        /// The leaf.
        /// </param>
        /// <param name="parentGenomes">
        /// The parent genomes.
        /// </param>
        /// <param name="featureStartColumnIndex">
        /// The feature start column index.
        /// </param>
        /// <param name="lengthOfCurrentFeature">
        /// The length of current feature.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/> representation of the given feature to use in the potential offspring.
        /// </returns>
        private double[] DetermineDoubleRepresentationOfNextFeature(
            TreeNodeAndFixations leaf,
            ParentGenomesConverted parentGenomes,
            int featureStartColumnIndex,
            int lengthOfCurrentFeature)
        {
            var parentToUse = this.DetermineCurrentFeatureValueDonor(leaf, parentGenomes, featureStartColumnIndex);

            return ExtractFeatureRepresentationFromDonor(featureStartColumnIndex, lengthOfCurrentFeature, parentToUse);
        }

        /// <summary>
        /// Computes an attractiveness measure for the given non-competitive genomes.
        /// </summary>
        /// <param name="transformedNonCompetitives">
        /// The <see cref="T:double[]"/> representation of the non-competitive genomes.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/> containing the predicted rank for each given genome.
        /// Lower values indicate higher attractiveness.
        /// </returns>
        private double[] GetAttractivenessMeasure(F64Matrix transformedNonCompetitives)
        {
            // no forest trained in first iteration.
            if (this.RandomForestPredictor == null || !this.Configuration.EnableSexualSelection)
            {
                return Enumerable.Repeat(1d, transformedNonCompetitives.RowCount).ToArray();
            }

            // sexual selection: rank all non-competitive genomes
            return this.RandomForestPredictor.Predict(transformedNonCompetitives);
        }

        /// <summary>
        /// Gets the next parent counter index. Used during export of histogram csv data.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/> ID.
        /// </returns>
        private int GetNextParentCounterIndex()
        {
            return Interlocked.Increment(ref this._atomicParentCounter);
        }

        /// <summary>
        /// Normalize given <paramref name="distances"/> so that they are in the range 
        /// [0, <see cref="AlgorithmTunerConfiguration.MaxRanksCompensatedByDistance"/>].
        /// </summary>
        /// <param name="distances">
        /// The distances to normalize.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/> containing the normalized distances.
        /// </returns>
        private double[] NormalizeDistancesAndApplyInfluenceFactor(double[] distances)
        {
            var aggMinMax = distances.Aggregate(
                new { Min = double.PositiveInfinity, Max = double.NegativeInfinity },
                (agg, dist) => new { Min = Math.Min(agg.Min, dist), Max = Math.Max(agg.Max, dist) });

            var minDist = aggMinMax.Min;
            var maxDist = aggMinMax.Max;
            var distRange = maxDist - minDist;
            if (Math.Abs(distRange) < 1e-6)
            {
                return Enumerable.Repeat(0d, distances.Length).ToArray();
            }

            var normalizedDistanceFactored =
                distances.Select(d => this.Configuration.MaxRanksCompensatedByDistance * (d - minDist) / distRange).ToArray();
            return normalizedDistanceFactored;
        }

        /// <summary>
        /// Generates and scores |targetSampleSize| many re-combinations of the given parents for every leaf.
        /// </summary>
        /// <param name="parentGenomes">
        /// The parent Genomes.
        /// </param>
        /// <param name="indexSetsForCategoricalFeaturesInDoubleRepresentation">
        /// The index sets for categorical features in double representation.
        /// (<see cref="ComputeIndexSetsForCategoricalFeaturesInDoubleRepresentation"/>).
        /// </param>
        /// <param name="targetLeaves">
        /// The leaves to sample for.
        /// </param>
        /// <returns>
        /// All generated offspring with predicted rank.
        /// </returns>
        private List<GenomeDoubleRepresentationWithPredictedRank> PerformTargetedSampling(
            ParentGenomesConverted parentGenomes,
            Dictionary<int, HashSet<int>> indexSetsForCategoricalFeaturesInDoubleRepresentation,
            IEnumerable<TreeNodeAndFixations> targetLeaves)
        {
            var targetSampleSize = this.Configuration.TargetSamplingSize;
            var predictedRanksForPotentialOffspring = new List<GenomeDoubleRepresentationWithPredictedRank>();
            var predictionsForSingleLeaf = new double[targetSampleSize][];
            double[][] randomlySelectedLeafPredictions = null;
            var currentLeafCount = 0;

            // only reading access on forest. no need to lock.
            foreach (var leaf in targetLeaves)
            {
                for (var k = 0; k < targetSampleSize; k++)
                {
                    var targetSampleOffspring = this.CreateSingleTargetSampleOffspring(
                        leaf,
                        parentGenomes,
                        indexSetsForCategoricalFeaturesInDoubleRepresentation);

                    // predict offspring performance
                    var individualRanksPredictedByTrees = this.RandomForestPredictor.PredictIndividualTreeValues(targetSampleOffspring);
                    var predictedRank = individualRanksPredictedByTrees.Average();

                    var targetedSampleWithScore = new GenomeDoubleRepresentationWithPredictedRank(targetSampleOffspring, predictedRank);

                    // store offspring with predicted score
                    predictedRanksForPotentialOffspring.Add(targetedSampleWithScore);
                    predictionsForSingleLeaf[k] = individualRanksPredictedByTrees;
                }

                // this choses 1 random target leaf on the fly. uniform random distribution over all 'targetLeaves'
                currentLeafCount++;
                if (Randomizer.Instance.Next(currentLeafCount) == 0)
                {
                    randomlySelectedLeafPredictions = predictionsForSingleLeaf;
                }
            }

            // export the predictions for the single leaf that was chosen for this c/nc couple
            if (randomlySelectedLeafPredictions != null)
            {
                RandomForestHelper.ExportHistogramDataForAllSamplesOfSingleLeaf(
                    randomlySelectedLeafPredictions,
                    this._currentGeneration,
                    this.GetNextParentCounterIndex());
            }

            return predictedRanksForPotentialOffspring;
        }

        /// <summary>
        /// Select a random subset of features that are considered for distance computation.
        /// </summary>
        /// <param name="columnCountOfGenomeDoubleRepresentation">
        /// The number of columns of a transformed <see cref="Genome"/>.
        /// </param>
        /// <returns>
        /// The <see cref="T:int[]"/>.
        /// </returns>
        private int[] SelectColumnIndicesForDistanceComputation(int columnCountOfGenomeDoubleRepresentation)
        {
            var featureSubsetRatioForDistanceComputation = this.Configuration.FeatureSubsetRatioForDistanceComputation;
            var numFeaturesToCompare = (int)Math.Ceiling(columnCountOfGenomeDoubleRepresentation * featureSubsetRatioForDistanceComputation);
            return Randomizer.Instance.ChooseRandomSubset(Enumerable.Range(0, columnCountOfGenomeDoubleRepresentation), numFeaturesToCompare)
                .ToArray();
        }

        /// <summary>
        /// Restores a <see cref="Genome"/> from the <see cref="T:double[]"/>-representation given by
        /// <paramref name="offspring"/>.
        /// </summary>
        /// <param name="offspring">
        /// The <see cref="T:double[]"/> representation of a <see cref="Genome"/>.
        /// </param>
        /// <returns>
        /// The restored <see cref="Genome"/>.
        /// </returns>
        private Genome TransformOffspringToGenome(double[] offspring)
        {
            return this.GenomeTransformator.ConvertBack(offspring);
        }

        #endregion
    }
}