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
    /// Contains tests for the <see cref="SortByUnpenalizedRuntime{TInstance}"/> class.
    /// </summary>
    public class SortByUnpenalizedRuntimeTest
    {
        #region Static Fields

        /// <summary>
        /// The <see cref="SortByUnpenalizedRuntime{TInstance}"/> sorter used in tests.
        /// </summary>
        private static readonly SortByUnpenalizedRuntime<TestInstance> Sorter = new SortByUnpenalizedRuntime<TestInstance>(TimeSpan.FromSeconds(30));

        /// <summary>
        /// A set of test instances.
        /// </summary>
        private static readonly List<TestInstance> Instances = Enumerable.Range(0, 10).Select(i => new TestInstance($"{i}")).ToList();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="SortByUnpenalizedRuntime{TInstance}.Sort"/>
        /// returns the genome with lower average runtime first if the number of provided results is the same for both
        /// genomes.
        /// </summary>
        [Fact]
        public void SortReturnsGenomeWithLowerAverageRuntimeFirst()
        {
            var worseGenome = new ImmutableGenome(new Genome(1));
            var betterGenome = new ImmutableGenome(new Genome(2));

            var worseStats = this.CreateStats(worseGenome, SortByUnpenalizedRuntimeTest.Instances, i => TimeSpan.FromSeconds(i + 1));
            var betterStats = this.CreateStats(betterGenome, SortByUnpenalizedRuntimeTest.Instances, i => TimeSpan.FromSeconds(i));

            var runResults = new[] { worseStats.ToImmutable(), betterStats.ToImmutable() };
            var sortedGenomes = SortByUnpenalizedRuntimeTest.Sorter.Sort(runResults).ToList();

            sortedGenomes.Count.ShouldBe(2);
            Assert.Equal(betterGenome, sortedGenomes.First().Genome);
            Assert.Equal(worseGenome, sortedGenomes.Last().Genome);
        }

        /// <summary>
        /// Checks that <see cref="SortByUnpenalizedRuntime{TInstance}.Sort"/>
        /// returns the genome with lower average runtime first even if the number of provided results is different
        /// for the genomes and the one with lower average runtime has a higher runtime sum.
        /// </summary>
        [Fact]
        public void SortCanHandleDifferentNumberOfRunsPerGenome()
        {
            var worseGenome = new ImmutableGenome(new Genome(1));
            var betterGenome = new ImmutableGenome(new Genome(2));

            var worseStats = this.CreateStats(worseGenome, SortByUnpenalizedRuntimeTest.Instances, i => TimeSpan.FromMilliseconds(100), 1);
            var betterStats = this.CreateStats(betterGenome, SortByUnpenalizedRuntimeTest.Instances, i => TimeSpan.FromMilliseconds(20));

            var runResults = new[] { worseStats.ToImmutable(), betterStats.ToImmutable() };
            var sortedGenomes = SortByUnpenalizedRuntimeTest.Sorter.Sort(runResults).ToList();

            sortedGenomes.Count.ShouldBe(2);
            Assert.Equal(betterGenome, sortedGenomes.First().Genome);
            Assert.Equal(worseGenome, sortedGenomes.Last().Genome);
        }

        /// <summary>
        /// Checks that <see cref="SortByUnpenalizedRuntime{TInstance}.Sort"/>
        /// returns the genome with more solved instances first, even if they are on average slower than the results of a genome with less solved instances.
        /// </summary>
        [Fact]
        public void SortCanHandleDifferentNumberOfRunsPerGenome2()
        {
            var worseGenome = new ImmutableGenome(new Genome(1));
            var betterGenome = new ImmutableGenome(new Genome(2));

            var worseStats = this.CreateStats(worseGenome, SortByUnpenalizedRuntimeTest.Instances, i => TimeSpan.FromMilliseconds(100), 1);
            var betterStats = this.CreateStats(betterGenome, SortByUnpenalizedRuntimeTest.Instances, i => TimeSpan.FromMilliseconds(200));

            var runResults = new[] { worseStats.ToImmutable(), betterStats.ToImmutable() };
            var sortedGenomes = SortByUnpenalizedRuntimeTest.Sorter.Sort(runResults).ToList();

            sortedGenomes.Count.ShouldBe(2);
            Assert.Equal(betterGenome, sortedGenomes.First().Genome);
            Assert.Equal(worseGenome, sortedGenomes.Last().Genome);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates genome stats for testing.
        /// </summary>
        /// <param name="genome">The genome.</param>
        /// <param name="instances">The instances.</param>
        /// <param name="indexToRuntime">The function to generate a runtime by <paramref name="instances"/> index.</param>
        /// <param name="numInstancesToUse">The number of instances to add to the stats from the given list.</param>
        /// <returns>The genome stats.</returns>
        private GenomeStats<TestInstance, RuntimeResult> CreateStats(
            ImmutableGenome genome,
            IReadOnlyList<TestInstance> instances,
            Func<int, TimeSpan> indexToRuntime,
            int? numInstancesToUse = null)
        {
            if (numInstancesToUse == null || numInstancesToUse > instances.Count)
            {
                numInstancesToUse = instances.Count;
            }

            var stats = new GenomeStats<TestInstance, RuntimeResult>(genome, Enumerable.Empty<TestInstance>(), instances);
            for (var i = 0; i < numInstancesToUse; i++)
            {
                var result = new RuntimeResult(indexToRuntime(i));
                stats.FinishInstance(instances[i], result);
            }

            return stats;
        }

        #endregion
    }
}