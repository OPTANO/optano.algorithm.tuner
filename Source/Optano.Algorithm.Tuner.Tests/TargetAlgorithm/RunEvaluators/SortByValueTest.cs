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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.RunEvaluators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="SortByValue{TInstance}"/> class.
    /// </summary>
    public class SortByValueTest
    {
        #region Fields

        /// <summary>
        /// A set of 10 test instances.
        /// </summary>
        private readonly List<TestInstance> _instances = Enumerable.Range(0, 10).Select(i => new TestInstance($"{i}")).ToList();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="SortByValue{TInstance}.GetMetricRepresentation"/> returns <see cref="ContinuousResult.Value"/>.
        /// </summary>
        /// <param name="sortAscending">Indicates whether to sort ascending or descending.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetMetricRepresentationReturnsValue(bool sortAscending)
        {
            var sorter = new SortByValue<TestInstance>(sortAscending);

            var result = new ContinuousResult(100, TimeSpan.Zero);
            Assert.Equal(
                100,
                sorter.GetMetricRepresentation(result));
        }

        /// <summary>
        /// Checks that ascending <see cref="SortByValue{TInstance}.Sort"/>
        /// returns the genome with lower average value first.
        /// And vice versa for descending <see cref="SortByValue{TInstance}.Sort"/>.
        /// </summary>
        /// <param name="sortAscending">Indicates whether to sort ascending or descending.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AscendingSorterReturnsGenomeWithLowerAverageValueFirst(bool sortAscending)
        {
            var sorter = new SortByValue<TestInstance>(sortAscending);

            var betterGenome = new ImmutableGenome(new Genome(1));
            var worseGenome = new ImmutableGenome(new Genome(2));

            var betterResults = this.CreateStats(betterGenome, this._instances, i => sortAscending ? i : 10);
            var worseResults = this.CreateStats(worseGenome, this._instances, i => sortAscending ? 10 : i);

            var combinedImmutableResults = new[] { betterResults.ToImmutable(), worseResults.ToImmutable() };

            var sortedGenomes = sorter.Sort(combinedImmutableResults).ToList();

            sortedGenomes.Count.ShouldBe(2);
            Assert.Equal(betterGenome, sortedGenomes.First().Genome);
            Assert.Equal(worseGenome, sortedGenomes.Last().Genome);
        }

        /// <summary>
        /// Checks that ascending <see cref="SortByValue{TInstance}.Sort"/>
        /// returns the genome with the higher number of valid results first, even if it has a higher average value.
        /// </summary>
        /// <param name="sortAscending">Indicates whether to sort ascending or descending.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AscendingSorterHandlesDifferentNumberOfRunsPerGenomeCorrectly(bool sortAscending)
        {
            var sorter = new SortByValue<TestInstance>(sortAscending);
            var worseGenome = new ImmutableGenome(new Genome(1));
            var betterGenome = new ImmutableGenome(new Genome(2));

            // worse, because less valid results.
            var worseStats = this.CreateStats(worseGenome, this._instances, i => sortAscending ? 1 : 2, 1);
            var betterStats = this.CreateStats(betterGenome, this._instances, i => sortAscending ? 2 : 1);

            var allStats = new[] { worseStats.ToImmutable(), betterStats.ToImmutable() };
            var sortedGenomes = sorter.Sort(allStats).ToList();

            sortedGenomes.Count.ShouldBe(2);

            Assert.Equal(betterGenome, sortedGenomes.First().Genome);
            Assert.Equal(worseGenome, sortedGenomes.Last().Genome);
        }

        /// <summary>
        /// Checks that ascending/descending <see cref="SortByValue{TInstance}.Sort"/> handles invalid results correctly.
        /// </summary>
        /// <param name="sortAscending">Indicates whether to sort ascending or descending.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SortByValueHandlesInvalidResultsCorrectly(bool sortAscending)
        {
            var sorter = new SortByValue<TestInstance>(sortAscending);

            var worstGenome = new ImmutableGenome(new Genome(1));
            var middleWorseGenome = new ImmutableGenome(new Genome(2));
            var middleBetterGenome = new ImmutableGenome(new Genome(3));
            var bestGenome = new ImmutableGenome(new Genome(4));

            var stats4 = this.CreateStats(worstGenome, this._instances, i => double.NaN, 4);
            var stats3 = this.CreateStats(middleWorseGenome, this._instances, i => double.NaN, 2);
            var stats2 = this.CreateStats(middleBetterGenome, this._instances, i => double.PositiveInfinity, 2);
            var stats1 = this.CreateStats(bestGenome, this._instances, i => 10, 4);

            var runResults = new[] { stats1.ToImmutable(), stats2.ToImmutable(), stats3.ToImmutable(), stats4.ToImmutable() };
            var sortedGenomes = sorter.Sort(runResults).ToList();

            sortedGenomes.Count.ShouldBe(4);

            Assert.Equal(bestGenome, sortedGenomes.First().Genome);
            Assert.Equal(middleBetterGenome, sortedGenomes.Skip(1).First().Genome);
            Assert.Equal(middleWorseGenome, sortedGenomes.Skip(2).First().Genome);
            Assert.Equal(worstGenome, sortedGenomes.Last().Genome);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates genome stats for testing.
        /// </summary>
        /// <param name="genome">The genome.</param>
        /// <param name="instances">The instances.</param>
        /// <param name="indexToResult">A function to provide a result by instance index.</param>
        /// <param name="numInstancesToUse">The number of instances to use from the list.</param>
        /// <returns>The genome stats.</returns>
        private GenomeStats<TestInstance, ContinuousResult> CreateStats(
            ImmutableGenome genome,
            IReadOnlyList<TestInstance> instances,
            Func<int, double> indexToResult,
            int? numInstancesToUse = null)
        {
            if (numInstancesToUse == null || numInstancesToUse > instances.Count)
            {
                numInstancesToUse = instances.Count;
            }

            var stats = new GenomeStats<TestInstance, ContinuousResult>(genome, Enumerable.Empty<TestInstance>(), instances);
            for (var i = 0; i < numInstancesToUse; i++)
            {
                var result = new ContinuousResult(indexToResult(i), TimeSpan.Zero);
                stats.FinishInstance(instances[i], result);
            }

            return stats;
        }

        #endregion
    }
}