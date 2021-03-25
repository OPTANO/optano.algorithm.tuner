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

namespace Optano.Algorithm.Tuner.Tests.MachineLearning
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.MachineLearning.GenomeRepresentation;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;

    /// <summary>
    /// Useful methods for testing data objects.
    /// </summary>
    internal static class TestDataUtils
    {
        #region Public Methods and Operators

        /// <summary>
        /// Generates <paramref name="count"/> random genomes adhering to the given <see cref="ParameterTree"/>.
        /// </summary>
        /// <param name="tree"><see cref="ParameterTree"/> to base genomes on.</param>
        /// <param name="config"><see cref="AlgorithmTunerConfiguration"/> to use when creating genomes.</param>
        /// <param name="count">The number of genomes to create.</param>
        /// <returns>The created genomes.</returns>
        public static List<Genome> GenerateGenomes(ParameterTree tree, AlgorithmTunerConfiguration config, int count)
        {
            var result = new List<Genome>(count);

            var builder = new GenomeBuilder(tree, config);
            for (var i = 0; i < count; i++)
            {
                result.Add(builder.CreateRandomGenome(0));
            }

            return result;
        }

        /// <summary>
        /// Simulates a tuner run for the specified number of generations and stores results in a new <see cref="TrainingDataWrapper"/>.
        /// </summary>
        /// <param name="tree"><see cref="ParameterTree"/> to base genomes on.</param>
        /// <param name="encoder">Strategy to convert genomes to double arrays.</param>
        /// <param name="genomeCount">Number of genomes to add to result per generation.</param>
        /// <param name="generations">Number of generations to simulate.</param>
        /// <param name="config"><see cref="AlgorithmTunerConfiguration"/>, required to generate new genomes.</param>
        /// <returns>The created <see cref="TrainingDataWrapper"/>.</returns>
        public static TrainingDataWrapper GenerateTrainingData(
            ParameterTree tree,
            IBulkGenomeTransformation encoder,
            int genomeCount,
            int generations,
            AlgorithmTunerConfiguration config)
        {
            var result = new TrainingDataWrapper(
                new Dictionary<Genome, List<GenomeTournamentRank>>(Genome.GenomeComparer),
                generations - 1);

            // Start with correct number of random genomes.
            var randomGenomes = TestDataUtils.GenerateGenomes(tree, config, genomeCount);

            // Then simulate the correct number of generations.
            for (var currentGen = 0; currentGen < generations; currentGen++)
            {
                var fitness = TestDataUtils.EvaluateTargetFunction(encoder, randomGenomes);

                // add result for every genome
                for (var genomeIndex = 0; genomeIndex < genomeCount; genomeIndex++)
                {
                    var currentGenome = randomGenomes[genomeIndex];
                    if (!result.TournamentResults.ContainsKey(currentGenome))
                    {
                        result.TournamentResults[currentGenome] = new List<GenomeTournamentRank>();
                    }

                    var tournamentResult = new GenomeTournamentRank()
                                               {
                                                   GenerationId = currentGen,
                                                   TournamentId = currentGen,
                                                   TournamentRank = fitness[genomeIndex],
                                               };

                    result.TournamentResults[currentGenome].Add(tournamentResult);
                }

                // swap out some genomes
                var replaceCount = (int)Math.Ceiling(0.3 * genomeCount);
                var indiciesToReplace = Randomizer.Instance.ChooseRandomSubset(
                    Enumerable.Range(0, genomeCount),
                    replaceCount);

                var newGenomes = TestDataUtils.GenerateGenomes(tree, config, replaceCount);
                var replacementIndex = 0;
                foreach (var indexToReplace in indiciesToReplace)
                {
                    randomGenomes[indexToReplace] = newGenomes[replacementIndex++];
                }
            }

            return result;
        }

        /// <summary>
        /// Evaluates target function given by <see cref="TestDataUtils"/> on every genome provided.
        /// </summary>
        /// <param name="encoder">Encoder to convert genomes to double arrays.</param>
        /// <param name="genomes">Genomes to evaluate.</param>
        /// <returns>Results for every genome.</returns>
        public static List<double> EvaluateTargetFunction(IGenomeTransformation encoder, List<Genome> genomes)
        {
            var encodedGenomes = genomes.Select(g => encoder.ConvertGenomeToArray(g));
            var result = encodedGenomes.Select(eg => TestDataUtils.EvaluateTargetFunction(eg)).ToList();

            return result;
        }

        /// <summary>
        /// Evaluates x[0] ^ 3 - 3 * x[0] + x[1] ^ 2 + \sum_{i=2}{(-1) ^ i * x[i]} on the provided variables.
        /// </summary>
        /// <param name="genomeEncoding">The variables to evaluate the function on.</param>
        /// <returns>The value obtained by evaluation.</returns>
        public static double EvaluateTargetFunction(double[] genomeEncoding)
        {
            var rank = 0d;

            for (var i = 0; i < genomeEncoding.Length; i++)
            {
                rank += TestDataUtils.EvaluateSingleIndex(genomeEncoding[i], i);
            }

            return rank;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Evaluates a single index of x[0] ^ 3 - 3 * x[0] + x[1] ^ 2 + \sum_{i=2}{(-1) ^ i * x[i]}.
        /// </summary>
        /// <param name="parameterValue">The x value.</param>
        /// <param name="index">The index.</param>
        /// <returns>The value obtained by evaluating the single index.</returns>
        private static double EvaluateSingleIndex(double parameterValue, int index)
        {
            if (index == 0)
            {
                return Math.Pow(parameterValue, 3) - (3 * parameterValue);
            }
            else if (index == 1)
            {
                return parameterValue * parameterValue;
            }
            else
            {
                return Math.Pow(-1, index) * parameterValue;
            }
        }

        #endregion
    }
}