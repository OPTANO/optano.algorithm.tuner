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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.DifferentialEvolution.SearchPoint
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Akka.Actor;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.SearchPoint;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GenomeSearchPointSorter{TInstance,TResult}"/> class.
    /// </summary>
    public class GenomeSearchPointSorterTest : GenomeAssistedSorterBaseTest<GenomeSearchPoint>
    {
        #region Fields

        /// <summary>
        /// The <see cref="GenomeSearchPointSorter{TInstance,TResult}"/> to test.
        /// </summary>
        private GenomeSearchPointSorter<TestInstance, IntegerResult> _sorter;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="GenomeAssistedSorterBase{TSearchPoint,TInstance,TResult}"/> used in tests.
        /// </summary>
        protected override GenomeAssistedSorterBase<GenomeSearchPoint, TestInstance, IntegerResult> GenomeAssistedSorter => this._sorter;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="GenomeSearchPointSorter{TInstance, TResult}"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without a <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </summary>
        [Fact]
        public override void ConstructorThrowsForMissingGenerationEvaluationActor()
        {
            Assert.Throws<ArgumentNullException>(() => new GenomeSearchPointSorter<TestInstance, TestResult>(null));
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPointSorter{TInstance, TResult}.Sort"/> sorts <see cref="Genome"/>s by
        /// performance.
        /// </summary>
        [Fact]
        public void SorterSortsByGenomePerformance()
        {
            var points = new List<GenomeSearchPoint>
                             {
                                 this.CreatePoint(1),
                                 this.CreatePoint(6d),
                                 this.CreatePoint(4.5),
                             };
            int[] expectedSorting = { 1, 2, 0 };
            Assert.Equal(
                expectedSorting,
                this._sorter.Sort(points).ToArray());
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPointSorter{TInstance, TResult}.Sort"/> and
        /// <see cref="ISearchPointSorter{TSearchPoint}.DetermineRanks"/> are consistent.
        /// </summary>
        [Fact]
        public override void SortingIsConsistent()
        {
            var points = new List<GenomeSearchPoint>
                             {
                                 this.CreatePoint(1),
                                 this.CreatePoint(6d),
                                 this.CreatePoint(4.5),
                             };
            this.CheckSortingConsistence(points);
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPointSorter{TInstance, TResult}.Sort"/> can handle duplicates.
        /// </summary>
        [Fact]
        public override void SortingCanHandleDuplicates()
        {
            // Duplicates may be due to actual duplicates or rounding.
            var points = new List<GenomeSearchPoint>
                             {
                                 this.CreatePoint(0),
                                 this.CreatePoint(1),
                                 this.CreatePoint(5),
                                 this.CreatePoint(0.2),
                                 this.CreatePoint(1),
                             };

            // Because of the duplicates, it is not clear how the sorting will look by index.
            // Map the result to values instead.
            double[] valuesInExpectedSorting = { 5, 1, 1, 0, 0 };
            var sorting = this._sorter.Sort(points)
                .Select(idx => Math.Round(points[idx].Values[1]) % 10)
                .ToArray();
            Assert.Equal(valuesInExpectedSorting, sorting);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes <see cref="GenomeAssistedSorterBaseTest{TSearchPoint}.GenomeAssistedSorter"/>.
        /// </summary>
        /// <param name="generationEvaluationActor">
        /// An <see cref="IActorRef" /> to a <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}" />.
        /// </param>
        protected override void InitializeSorter(IActorRef generationEvaluationActor)
        {
            this._sorter = new GenomeSearchPointSorter<TestInstance, IntegerResult>(generationEvaluationActor);
        }

        /// <summary>
        /// Creates a <see cref="GenomeSearchPoint"/> fitting the <see cref="ParameterTree"/>.
        /// </summary>
        /// <param name="sortingRelevantParameter">Value relevant for sorting.</param>
        /// <returns>The created <see cref="GenomeSearchPoint"/>.</returns>
        private GenomeSearchPoint CreatePoint(double sortingRelevantParameter)
        {
            // Create some search point to determine the minimum domain size.
            // Values do not matter because we will overwrite them anyway.
            var arbitrarySearchPoint = GenomeSearchPoint.CreateFromGenome(
                this.GenomeBuilder.CreateRandomGenome(age: 0),
                this.ParameterTree,
                minimumDomainSize: 5,
                genomeBuilder: this.GenomeBuilder);

            // Overwrite values with provided relevant value.
            return new GenomeSearchPoint(
                values: Vector<double>.Build.DenseOfArray(new[] { 0d, sortingRelevantParameter }),
                parent: arbitrarySearchPoint,
                genomeBuilder: this.GenomeBuilder);
        }

        #endregion
    }
}