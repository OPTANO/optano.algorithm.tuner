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

namespace Optano.Algorithm.Tuner.Tracking
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using MathNet.Numerics.Statistics;

    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// The run statistic tracker.
    /// </summary>
    public static class RunStatisticTracker
    {
        #region Public Methods and Operators

        /// <summary>
        /// Compute and export numerical feature coefficient of variation.
        /// </summary>
        /// <param name="paramTree">
        /// The parameter tree.
        /// </param>
        /// <param name="genomes">
        /// The genomes.
        /// </param>
        /// <param name="currGeneration">
        /// The current generation.
        /// </param>
        public static void ComputeAndExportNumericalFeatureCoefficientOfVariation(
            ParameterTree paramTree,
            IEnumerable<Genome> genomes,
            int currGeneration)
        {
            var numericalAlleleKeys = paramTree.GetNumericalParameters().Select(p => p.Identifier).ToArray();
            var competitiveByVariable = numericalAlleleKeys.ToDictionary(
                k => k,
                k => genomes.Select(c => Convert.ToDouble(c.GetGeneValue(k).GetValue())).ToArray());

            var coeffOfVariation = competitiveByVariable.OrderBy(cv => cv.Key)
                .Select(cv => new { Feature = cv.Key, CoeffOfVariation = cv.Value.PopulationStandardDeviation() / cv.Value.Mean() }).ToArray();

            var filePath = "standardDeviation.csv";
            if (coeffOfVariation.Length == 0)
            {
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(
                        filePath,
                        "Did not find any Features for which a Coefficient of Variation can be computed. Coefficient of Variation only is computed for numerical columns.");
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Warn,
                        "Did not find any Features for which a Coefficient of Variation can be computed. Coefficient of Variation only is computed for numerical columns.");
                }

                return;
            }

            var avgCoeffOfVariation = coeffOfVariation.Where(cv => !double.IsNaN(cv.CoeffOfVariation) && !double.IsInfinity(cv.CoeffOfVariation))
                .Average(d => d.CoeffOfVariation);

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "Generation;" + string.Join(";", coeffOfVariation.Select(d => d.Feature)) + ";Average\r\n");
            }

            File.AppendAllText(
                filePath,
                $"{currGeneration};" + string.Join(";", coeffOfVariation.Select(d => d.CoeffOfVariation)) + $";{avgCoeffOfVariation}\r\n");

            var coeffOfVariationLine = string.Join(" - ", coeffOfVariation.Select(d => $"{d.Feature}: {d.CoeffOfVariation:0.000}"))
                                       + $" - Average: {avgCoeffOfVariation:0.000}";
            LoggingHelper.WriteLine(VerbosityLevel.Debug, "Coefficient of Variation Competitive Pool: " + coeffOfVariationLine);
        }

        /// <summary>
        /// Track convergence behavior.
        /// </summary>
        /// <param name="incumbentGenomeWrapper">
        /// The incumbent genome wrapper.
        /// </param>
        /// <param name="runEvaluator">
        /// A <see cref="IMetricRunEvaluator{TResult}"/> to evaluate the incumbent's results.
        /// </param>
        /// <typeparam name="TResult">
        /// Type of single instance evaluation result.
        /// </typeparam>
        /// <returns>
        /// The <see cref="double"/> average <see cref="IMetricRunEvaluator{TResult}.GetMetricRepresentation"/> of the
        /// current incumbent.
        /// </returns>
        public static double TrackConvergenceBehavior<TResult>(
            IncumbentGenomeWrapper<TResult> incumbentGenomeWrapper,
            IMetricRunEvaluator<TResult> runEvaluator)
            where TResult : ResultBase<TResult>, new()
        {
            if (runEvaluator == null)
            {
                throw new ArgumentNullException(nameof(runEvaluator));
            }

            var currentAverage = incumbentGenomeWrapper.IncumbentInstanceResults.Average(r => runEvaluator.GetMetricRepresentation(r));
            LoggingHelper.WriteLine(
                VerbosityLevel.Info,
                $"Incumbent solved {incumbentGenomeWrapper.IncumbentInstanceResults.Count(i => !i.IsCancelled)}/{incumbentGenomeWrapper.IncumbentInstanceResults.Count} instances.");
            LoggingHelper.WriteLine(
                VerbosityLevel.Info,
                $"Average compare-value score: {currentAverage}.");
            return currentAverage;
        }

        /// <summary>
        /// Export convergence behavior.
        /// </summary>
        /// <param name="incumbentQuality">
        /// The incumbent quality.
        /// </param>
        public static void ExportConvergenceBehavior(List<double> incumbentQuality)
        {
            // export observed convergence
            var filePath = "averageConvergence.csv";
            if (!File.Exists(filePath))
            {
                File.WriteAllLines(filePath, incumbentQuality.Select(i => FormattableString.Invariant($"{i}")));
            }
            else
            {
                var csv = File.ReadLines(filePath).ToList();

                // fill existing csv with dummy rows to match IncumbentQuality's row count
                if (csv.Count < incumbentQuality.Count)
                {
                    var requiredPrefixColumns = csv.Max(line => line.Count(c => c == ';')) + 1;
                    var prefixColumns = string.Join(";", Enumerable.Repeat("Undef", requiredPrefixColumns));
                    for (var i = csv.Count; i < incumbentQuality.Count; i++)
                    {
                        csv.Add(prefixColumns);
                    }
                }

                // append new observation for row (=generation) or undef, if run was aborted
                var joinedCsv = csv.Select(
                    (line, index) =>
                        line + ";" + ((incumbentQuality.Count > index) ? incumbentQuality[index].ToString(CultureInfo.InvariantCulture) : "Undef"));
                File.WriteAllLines(filePath, joinedCsv);
            }
        }

        /// <summary>
        /// Exports the generation information history to file.
        /// </summary>
        /// <param name="informationHistory">The information history.</param>
        public static void ExportGenerationHistory(List<GenerationInformation> informationHistory)
        {
            if (informationHistory == null)
            {
                throw new ArgumentNullException(nameof(informationHistory));
            }

            File.WriteAllText(
                "generationHistory.csv",
                GenerationInformation.LegendOfGenerationInformation + Environment.NewLine);
            File.AppendAllLines("generationHistory.csv", informationHistory.Select(information => information.ToString()));
        }

        /// <summary>
        /// Exports the average incumbent scores by evaluation count to file.
        /// </summary>
        /// <param name="informationHistory">The information history.</param>
        /// <param name="evaluationLimit">
        /// The evaluation limit after which further generation information is ignored.
        /// </param>
        public static void ExportAverageIncumbentScores(
            List<GenerationInformation> informationHistory,
            int evaluationLimit)
        {
            if (informationHistory == null)
            {
                throw new ArgumentNullException(nameof(informationHistory));
            }

            // If no evaluation limit is set, do not write a huge file, but just write out things until last
            // generation.
            if (evaluationLimit == int.MaxValue)
            {
                // Round to last full 100.
                evaluationLimit = (int)(Math.Ceiling(informationHistory.Last().TotalNumberOfEvaluations / 100d) * 100);
            }

            using (var evaluationToScoreFile = File.CreateText("scores.csv"))
            {
                evaluationToScoreFile.WriteLine("# Evaluations;Average Train Incumbent;Average Test Incumbent");

                int firstKnowledgeCount = informationHistory.First().TotalNumberOfEvaluations;
                for (int evaluationCount = 100; evaluationCount <= evaluationLimit; evaluationCount += 100)
                {
                    if (evaluationCount < firstKnowledgeCount)
                    {
                        // The first generation might take more than 100 evaluations, so write empty lines until we know something.
                        evaluationToScoreFile.WriteLine(FormattableString.Invariant($"{evaluationCount};;"));
                        continue;
                    }

                    var latestInfo = informationHistory.Last(info => info.TotalNumberOfEvaluations <= evaluationCount);
                    evaluationToScoreFile.WriteLine(
                        FormattableString.Invariant(
                            $"{evaluationCount};{latestInfo.IncumbentTrainingScore};{latestInfo.IncumbentTestScore}"));
                }
            }
        }

        #endregion
    }
}