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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.CovarianceMatrixAdaptation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Akka.Actor;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Global;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="RepairedGenomeSearchPointSorter{TSearchPoint,TInstance,TResult}"/> class.
    /// </summary>
    public class RepairedGenomeSearchPointSorterTest : GenomeAssistedSorterBaseTest<ContinuizedGenomeSearchPoint>
    {
        #region Fields

        /// <summary>
        /// The <see cref="RepairedGenomeSearchPointSorter{TSearchPoint,TInstance,TResult}"/> to test.
        /// </summary>
        private RepairedGenomeSearchPointSorter<ContinuizedGenomeSearchPoint, TestInstance, IntegerResult> _sorter;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="GenomeAssistedSorterBase{TSearchPoint,TInstance,TResult}"/> used in tests.
        /// </summary>
        protected override GenomeAssistedSorterBase<ContinuizedGenomeSearchPoint, TestInstance, IntegerResult> GenomeAssistedSorter => this._sorter;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="RepairedGenomeSearchPointSorter{TSearchPoint,TInstance,TResult}"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without a <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </summary>
        [Fact]
        public override void ConstructorThrowsForMissingGenerationEvaluationActor()
        {
            Assert.Throws<ArgumentNullException>(
                () =>
                    new RepairedGenomeSearchPointSorter<ContinuizedGenomeSearchPoint, TestInstance, IntegerResult>(null));
        }

        /// <summary>
        /// Checks that <see cref="RepairedGenomeSearchPointSorter{TSearchPoint,TInstance,TResult}.Sort"/> sorts by validity as first criterion.
        /// </summary>
        [Fact]
        public void ValidGenomeIsBetterThanInvalidOne()
        {
            // Create a search point which corresponds to a valid genome and one which corresponds to an invalid one.
            // Make sure the repaired genome will have a better value.
            var validPoint = this.CreatePoint(0, correspondsToValidGenome: true);
            var invalidPoint = this.CreatePoint(10, correspondsToValidGenome: false);

            var sorting = this._sorter.Sort(new List<ContinuizedGenomeSearchPoint> { invalidPoint, validPoint });
            int[] expectedSorting = { 1, 0 };
            Assert.Equal(expectedSorting, sorting.ToArray());
        }

        /// <summary>
        /// Checks that <see cref="RepairedGenomeSearchPointSorter{TSearchPoint,TInstance,TResult}.Sort"/> sorts both valid and invalid
        /// <see cref="Genome"/>s by performance.
        /// </summary>
        [Fact]
        public void SecondSortingCriterionIsPerformance()
        {
            var points = new List<ContinuizedGenomeSearchPoint>
                             {
                                 this.CreatePoint(1, correspondsToValidGenome: false),
                                 this.CreatePoint(4.5, correspondsToValidGenome: false),
                                 this.CreatePoint(6d, correspondsToValidGenome: true),
                                 this.CreatePoint(10d, correspondsToValidGenome: true),
                                 this.CreatePoint(8d, correspondsToValidGenome: true),
                             };
            int[] expectedSorting = { 3, 4, 2, 1, 0 };
            Assert.Equal(
                expectedSorting,
                this._sorter.Sort(points).ToArray());
        }

        /// <summary>
        /// Checks that <see cref="ISearchPointSorter{TSearchPoint}.Sort"/> and
        /// <see cref="ISearchPointSorter{TSearchPoint}.DetermineRanks"/> are consistent.
        /// </summary>
        [Fact]
        public override void SortingIsConsistent()
        {
            var points = new List<ContinuizedGenomeSearchPoint>
                             {
                                 this.CreatePoint(1, correspondsToValidGenome: false),
                                 this.CreatePoint(4.5, correspondsToValidGenome: false),
                                 this.CreatePoint(6d, correspondsToValidGenome: true),
                                 this.CreatePoint(10d, correspondsToValidGenome: true),
                                 this.CreatePoint(8d, correspondsToValidGenome: true),
                             };
            this.CheckSortingConsistence(points);
        }

        /// <summary>
        /// Checks that <see cref="RepairedGenomeSearchPointSorter{TSearchPoint,TInstance,TResult}.Sort"/> can handle duplicates.
        /// </summary>
        [Fact]
        public override void SortingCanHandleDuplicates()
        {
            // Duplicates may be due to actual duplicates, rounding, or mapping into bounds.
            var points = new List<ContinuizedGenomeSearchPoint>
                             {
                                 this.CreatePoint(5.001, correspondsToValidGenome: true),
                                 this.CreatePoint(8, correspondsToValidGenome: true),
                                 this.CreatePoint(28, correspondsToValidGenome: true),
                                 this.CreatePoint(1, correspondsToValidGenome: true),
                                 this.CreatePoint(5, correspondsToValidGenome: true),
                                 this.CreatePoint(1, correspondsToValidGenome: true),
                             };

            // Because of the duplicates, it is not clear how the sorting will look by index.
            // Map the result to values instead.
            double[] valuesInExpectedSorting = { 8, 8, 5, 5, 1, 1 };
            var sorting = this._sorter.Sort(points)
                .Select(idx => Math.Round(points[idx].Values[1]) % 10)
                .ToArray();
            Assert.Equal(
                valuesInExpectedSorting,
                sorting);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes <see cref="GenomeAssistedSorterBaseTest{TSearchPoint}.GenomeAssistedSorter"/>.
        /// </summary>
        /// <param name="generationEvaluationActor">
        /// An <see cref="IActorRef" /> to a <see cref="GenerationEvaluationActor{TTargetAlgorithm, TInstance, TResult}" />.
        /// </param>
        protected override void InitializeSorter(IActorRef generationEvaluationActor)
        {
            this._sorter = new RepairedGenomeSearchPointSorter<ContinuizedGenomeSearchPoint, TestInstance, IntegerResult>(generationEvaluationActor);
        }

        /// <summary>
        /// Creates a <see cref="ContinuizedGenomeSearchPoint"/> fitting the <see cref="ParameterTree"/>.
        /// </summary>
        /// <param name="sortingRelevantParameter">Value relevant for sorting, between 0 and 10.</param>
        /// <param name="correspondsToValidGenome">
        /// If true, other values are added s. t. the <see cref="BoundedSearchPoint"/> corresponds to an valid
        /// <see cref="Genome"/>; if false, it will be an invalid <see cref="Genome"/>.
        /// </param>
        /// <returns>The created <see cref="BoundedSearchPoint"/>.</returns>
        private ContinuizedGenomeSearchPoint CreatePoint(double sortingRelevantParameter, bool correspondsToValidGenome)
        {
            var lowerBounds =
                this.ParameterTree.GetNumericalParameters()
                    .Select(parameter => (double)((IntegerDomain)parameter.Domain).Minimum)
                    .ToArray();
            var upperBounds =
                this.ParameterTree.GetNumericalParameters()
                    .Select(parameter => (double)((IntegerDomain)parameter.Domain).Maximum)
                    .ToArray();

            // Free parameter name is first in alphabet, so set that value first, too.
            double freeParameter = correspondsToValidGenome ? 0d : 7d;
            return new ContinuizedGenomeSearchPoint(
                Vector<double>.Build.DenseOfArray(new[] { freeParameter, sortingRelevantParameter }),
                this.ParameterTree,
                this.GenomeBuilder,
                lowerBounds,
                upperBounds);
        }

        #endregion
    }
}