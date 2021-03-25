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

namespace Optano.Algorithm.Tuner.MachineLearning.RandomForest
{
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData;

    using SharpLearning.Containers.Matrices;
    using SharpLearning.DecisionTrees.Nodes;

    /// <summary>
    /// The random forest helper.
    /// </summary>
    internal static class RandomForestHelper
    {
        #region Methods

        /// <summary>
        /// Checks if the <paramref name="node"/> is a leaf in its tree.
        /// </summary>
        /// <param name="node">
        /// The node.
        /// </param>
        /// <returns>
        /// <c>True</c>, iff the node is a leaf.
        /// I.e. its <see cref="Node.FeatureIndex"/> &lt; 0.
        /// </returns>
        internal static bool IsLeafNode(this Node node)
        {
            return node.FeatureIndex < 0;
        }

        /// <summary>
        /// Writes all training data.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <param name="pathAndFile">
        /// The path and file.
        /// </param>
        internal static void WriteAllTrainingData(TrainingDataWrapper data, string pathAndFile)
        {
            var filePath = new FileInfo(pathAndFile);
            if (filePath.DirectoryName != null)
            {
                Directory.CreateDirectory(filePath.DirectoryName);
            }

            var convertedGenomes = data.ConvertedGenomes;

            // build header: generation, tournament id, features..
            var featureColumnNames = string.Join(";", Enumerable.Range(1, convertedGenomes.ColumnCount).Select(r => $"Feature_{r}"));
            var csvBuilder = new StringBuilder(string.Concat("UniqueGenomeId;", "Generation;", "TournamentId;", featureColumnNames, ";Rank"))
                .AppendLine();

            Debug.Assert(
                data.Genomes.GroupBy(g => g, g => g, Genome.GenomeComparer).Count() == data.Count,
                "Found 2 separate genomes in list data.Genomes that are equal. This should not occur.");

            // indices/order:
            // 0-2: genome id, generation, tournament id
            // 3: "genome double representation" as separated string
            // 4: Rank
            var formatTemplate = "{0};{1};{2};{3};{4}";
            var genomes = data.Genomes.ToArray();
            for (var rowIndex = 0; rowIndex < data.Count; rowIndex++)
            {
                // repeat the genome data for every observed tournament result
                // assumption: data.Genomes is a distinct list of genomes (with respect to Genome.GeneValueComparer)
                var currentGenome = genomes[rowIndex];
                var genomeMatrixString = convertedGenomes.GetRowAsCsv(rowIndex, ";");
                var genomeResults = data.TournamentResults[currentGenome];

                foreach (var currentResult in genomeResults)
                {
                    var rowText = string.Format(
                        CultureInfo.InvariantCulture,
                        formatTemplate,
                        rowIndex,
                        currentResult.GenerationId,
                        currentResult.TournamentId,
                        genomeMatrixString,
                        currentResult.TournamentRank);
                    csvBuilder.AppendLine(rowText);
                }
            }

            File.WriteAllText(filePath.FullName, csvBuilder.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Exports the training data that was used to train the current tree.
        /// </summary>
        /// <param name="observations">
        /// The genome.
        /// </param>
        /// <param name="ranks">
        /// The target performance.
        /// </param>
        /// <param name="path">
        /// The path and file name. Add '{0}' in path, if you want an auto-incremented counter in the file name.
        /// </param>
        internal static void WriteAggregatedTrainingData(F64Matrix observations, double[] ranks, string path)
        {
            var filePath = new FileInfo(path);
            if (filePath.DirectoryName != null)
            {
                Directory.CreateDirectory(filePath.DirectoryName);
            }

            var csvBuilder = new StringBuilder(
                string.Concat(string.Join(";", Enumerable.Range(1, observations.ColumnCount).Select(r => $"Feature_{r}")), ";Rank")).AppendLine();
            for (var row = 0; row < observations.RowCount; row++)
            {
                var rowString = observations.GetRowAsCsv(row, ";");
                csvBuilder.Append(rowString);
                csvBuilder.AppendFormat(CultureInfo.InvariantCulture, ";{0}", ranks[row]).AppendLine();
            }

            File.WriteAllText(filePath.FullName, csvBuilder.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Get a row as csv.
        /// </summary>
        /// <param name="matrix">
        /// The matrix.
        /// </param>
        /// <param name="row">
        /// The row.
        /// </param>
        /// <param name="sep">
        /// The seperator.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        internal static string GetRowAsCsv(this F64Matrix matrix, int row, string sep)
        {
            return string.Join(sep, matrix.Row(row).Select(o => string.Format(CultureInfo.InvariantCulture, "{0}", o)));
        }

        /// <summary>
        /// Export histogram data for all samples of single leaf.
        /// </summary>
        /// <param name="predictedRanksForTargetSample">
        /// The predicted ranks for target sample.
        /// </param>
        /// <param name="currentGeneration">
        /// The current generation.
        /// </param>
        /// <param name="parentNumber">
        /// The parent id.
        /// </param>
        internal static void ExportHistogramDataForAllSamplesOfSingleLeaf(
            double[][] predictedRanksForTargetSample,
            int currentGeneration,
            int parentNumber)
        {
#if !DEBUG
            return;
#endif
            try
            {
                // each element represents a rows, containing all individual tree predictions for a target sample.
                var allTargetSampleRows = predictedRanksForTargetSample.Select(
                        predictedRanks => string.Join(";", predictedRanks.Select(r => string.Format(CultureInfo.InvariantCulture, "{0:0.00}", r))))
                    .ToList();
                var path = $"export/histogramdata/generation_{currentGeneration}/";
                var fileName = $"targetSampleTreePredictions_{parentNumber}.csv";
                var fileInfo = new FileInfo(path + fileName);
                if (File.Exists(fileInfo.FullName))
                {
                    fileName = Path.GetRandomFileName() + $"_{fileName}";
                    fileInfo = new FileInfo(path + fileName);
                }

                if (fileInfo.DirectoryName != null)
                {
                    Directory.CreateDirectory(fileInfo.DirectoryName);
                }

                File.WriteAllLines(fileInfo.FullName, allTargetSampleRows, Encoding.UTF8);
            }
            catch
            {
                // ignored
            }
        }

        #endregion
    }
}