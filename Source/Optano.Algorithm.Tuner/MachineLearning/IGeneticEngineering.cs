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

namespace Optano.Algorithm.Tuner.MachineLearning
{
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.MachineLearning.GenomeRepresentation;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData;

    /// <summary>
    /// Genetic engineering is an alternative crossover operator that uses a model-based approach for constructing new offspring.
    /// </summary>
    public interface IGeneticEngineering
    {
        #region Public properties

        /// <summary>
        /// Gets the genome transformator.
        /// </summary>
        IBulkGenomeTransformation GenomeTransformator { get; }

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
        IEnumerable<Genome> EngineerGenomes(
            List<Genome> chosenCompetitiveParents,
            IReadOnlyList<Genome> nonCompetitiveMates,
            IEnumerable<Genome> genomesForDistanceComputation);

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
        double[] GetAttractivenessMeasure(IEnumerable<Genome> nonCompetitiveMates);

        /// <summary>
        /// Trains a random forest.
        /// </summary>
        /// <param name="trainingData">
        /// The observations to learn.
        /// </param>
        void TrainForest(TrainingDataWrapper trainingData);

        #endregion
    }
}