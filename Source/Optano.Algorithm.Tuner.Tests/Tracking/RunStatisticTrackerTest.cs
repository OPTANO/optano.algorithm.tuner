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

namespace Optano.Algorithm.Tuner.Tests.Tracking
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.GeneticAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tracking;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="RunStatisticTracker"/> class.
    /// </summary>
    public class RunStatisticTrackerTest : TestBase
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="RunStatisticTracker.TrackConvergenceBehavior"/> throws a
        /// <see cref="ArgumentNullException"/> if called without a <see cref="IRunEvaluator{TResult}"/>.
        /// </summary>
        [Fact]
        public void TrackConvergenceBehaviorThrowsForMissingRunEvaluator()
        {
            var incumbentWrapper = new IncumbentGenomeWrapper<TestResult>
                                       {
                                           IncumbentGeneration = 0,
                                           IncumbentGenome = new Genome(),
                                           IncumbentInstanceResults = new List<TestResult>().ToImmutableList(),
                                       };
            Assert.Throws<ArgumentNullException>(() => RunStatisticTracker.TrackConvergenceBehavior(incumbentWrapper, runEvaluator: null));
        }

        /// <summary>
        /// Checks that <see cref="RunStatisticTracker.ExportGenerationHistory"/> correctly writes out all information
        /// to a file named 'generationHistory.csv'.
        /// </summary>
        [Fact]
        public void ExportGenerationHistoryWritesOutAllInformation()
        {
            var firstGeneration = new GenerationInformation(
                0,
                34,
                typeof(GgaStrategy<TestInstance, TestResult>),
                new ImmutableGenome(new Genome()));
            firstGeneration.IncumbentTrainingScore = -34.5;
            firstGeneration.IncumbentTestScore = -20;
            var secondGeneration = new GenerationInformation(
                1,
                2587,
                typeof(DifferentialEvolutionStrategy<TestInstance, TestResult>),
                new ImmutableGenome(new Genome()));
            secondGeneration.IncumbentTrainingScore = -104;
            secondGeneration.IncumbentTestScore = -100;

            RunStatisticTracker.ExportGenerationHistory(new List<GenerationInformation> { firstGeneration, secondGeneration });

            var exported = File.ReadAllLines("generationHistory.csv");
            Assert.True(3 == exported.Length, "Expected three lines: One legend and two generations.");
            Assert.True(
                "Generation;Total # Evaluations;Average Train Incumbent;Average Test Incumbent;Strategy;Incumbent" == exported[0],
                "Legend is not as expected.");
            Assert.True(
                "0;34;-34.5;-20;GgaStrategy`2;[](Age: 0)[Engineered: no]" == exported[1],
                "First generation information is not as expected.");
            Assert.True(
                "1;2587;-104;-100;DifferentialEvolutionStrategy`2;[](Age: 0)[Engineered: no]" == exported[2],
                "Second generation information is not as expected.");
        }

        /// <summary>
        /// Checks that <see cref="RunStatisticTracker.ExportGenerationHistory"/> throws a
        /// <see cref="ArgumentNullException"/> if called without such a history.
        /// </summary>
        [Fact]
        public void ExportGenerationHistoryThrowsForMissingHistory()
        {
            Assert.Throws<ArgumentNullException>(() => RunStatisticTracker.ExportGenerationHistory(informationHistory: null));
        }

        /// <summary>
        /// Checks that <see cref="RunStatisticTracker.ExportAverageIncumbentScores"/> correctly identifies the latest
        /// scores by evaluation number and writes them out to a file named 'scores.csv'.
        /// </summary>
        [Fact]
        public void ExportAverageIncumbentScoresDeterminesScoresCorrectly()
        {
            var incumbent = new ImmutableGenome(new Genome());
            var strategy = typeof(GgaStrategy<TestInstance, TestResult>);

            // Check what happens if the first generation takes more than 100 evaluations.
            var generation0 = new GenerationInformation(0, 150, strategy, incumbent);
            generation0.IncumbentTrainingScore = -34.5;
            generation0.IncumbentTestScore = -20;

            // Check what happens for multiple information objects within one evaluation level.
            var generation1 = new GenerationInformation(1, 199, strategy, incumbent);
            generation1.IncumbentTrainingScore = 12.34;
            generation1.IncumbentTestScore = 28.6;

            // Check what happens for an evaluation number equal to a bound.
            var generation2 = new GenerationInformation(2, 300, strategy, incumbent);
            generation2.IncumbentTrainingScore = 12.01;
            generation2.IncumbentTestScore = 29;

            // Check what happens if there is no information object in a certain level (301-400).
            var generation3 = new GenerationInformation(2, 401, strategy, incumbent);
            generation3.IncumbentTrainingScore = 14;
            generation3.IncumbentTestScore = 286;

            // Make sure to try an evaluation limit higher than the last total number of evaluations.
            RunStatisticTracker.ExportAverageIncumbentScores(
                new List<GenerationInformation> { generation0, generation1, generation2, generation3 },
                600);

            var exported = File.ReadAllLines("scores.csv");
            Assert.True(7 == exported.Length, "Expected seven lines: One legend and six evaluation levels.");
            Assert.True(
                "# Evaluations;Average Train Incumbent;Average Test Incumbent" == exported[0],
                "Legend is not as expected.");
            Assert.True(
                "100;;" == exported[1],
                "There should be an empty line as first information is only gathered at 150 evaluations.");
            Assert.True(
                "200;12.34;28.6" == exported[2],
                "First score line should use latest information.");
            Assert.True(
                "300;12.01;29" == exported[3],
                "Second score line should use information with evaluation number equal to the bound.");
            Assert.True(
                "400;12.01;29" == exported[4],
                "Third score line should not change scores.");
            Assert.True(
                "500;14;286" == exported[5],
                "Fourth score line should use the newest data again.");
            Assert.True(
                "600;14;286" == exported[6],
                "Fifth score line should be written to have scores until the limit.");
        }

        /// <summary>
        /// Checks that <see cref="RunStatisticTracker.ExportAverageIncumbentScores"/> throws a
        /// <see cref="ArgumentNullException"/> if called without a generation information history.
        /// </summary>
        [Fact]
        public void ExportAverageIncumbentScoresThrowsForMissingHistory()
        {
            Assert.Throws<ArgumentNullException>(
                () => RunStatisticTracker.ExportAverageIncumbentScores(informationHistory: null, evaluationLimit: 200));
        }

        #endregion
    }
}