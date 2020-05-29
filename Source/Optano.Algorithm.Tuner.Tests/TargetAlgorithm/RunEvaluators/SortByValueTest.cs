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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.RunEvaluators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="SortByValue"/> class.
    /// </summary>
    public class SortByValueTest
    {
        #region Static Fields

        /// <summary>
        /// The ascending <see cref="SortByValue"/> sorter used in tests.
        /// </summary>
        private static readonly SortByValue ascendingSorter = new SortByValue(@ascending: true);

        /// <summary>
        /// The descending <see cref="SortByValue"/> sorter used in tests.
        /// </summary>
        private static readonly SortByValue descendingSorter = new SortByValue(@ascending: false);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="SortByValue.GetMetricRepresentation"/> returns <see cref="ContinuousResult.Value"/>.
        /// </summary>
        [Fact]
        public void GetMetricRepresentationReturnsValue()
        {
            var result = new ContinuousResult(100, TimeSpan.Zero);
            Assert.Equal(
                100,
                SortByValueTest.ascendingSorter.GetMetricRepresentation(result));
        }

        /// <summary>
        /// Checks that ascending <see cref="SortByValue.Sort(System.Collections.Generic.Dictionary{Optano.Algorithm.Tuner.Genomes.ImmutableGenome,System.Collections.Generic.IEnumerable{Optano.Algorithm.Tuner.TargetAlgorithm.Results.ContinuousResult}})"/>
        /// returns the genome with lower average value first.
        /// </summary>
        [Fact]
        public void AscendingSorterReturnsGenomeWithLowerAverageValueFirst()
        {
            var worseGenome = new ImmutableGenome(new Genome(1));
            var betterGenome = new ImmutableGenome(new Genome(2));

            var runResults = new Dictionary<ImmutableGenome, IEnumerable<ContinuousResult>>(capacity: 2);
            runResults.Add(worseGenome, Enumerable.Range(1, 10).Select(i => new ContinuousResult(10, TimeSpan.Zero)));
            runResults.Add(betterGenome, Enumerable.Range(1, 10).Select(i => new ContinuousResult(i, TimeSpan.Zero)));

            var sortedGenomes = SortByValueTest.ascendingSorter.Sort(runResults);
            Assert.Equal(betterGenome, sortedGenomes.First());
            Assert.Equal(worseGenome, sortedGenomes.Last());
        }

        /// <summary>
        /// Checks that descending <see cref="SortByValue.Sort(System.Collections.Generic.Dictionary{Optano.Algorithm.Tuner.Genomes.ImmutableGenome,System.Collections.Generic.IEnumerable{Optano.Algorithm.Tuner.TargetAlgorithm.Results.ContinuousResult}})"/>
        /// returns the genome with higher average value first.
        /// </summary>
        [Fact]
        public void DescendingSorterReturnsGenomeWithHigherAverageValueFirst()
        {
            var worseGenome = new ImmutableGenome(new Genome(1));
            var betterGenome = new ImmutableGenome(new Genome(2));

            var runResults = new Dictionary<ImmutableGenome, IEnumerable<ContinuousResult>>(capacity: 2);
            runResults.Add(worseGenome, Enumerable.Range(1, 10).Select(i => new ContinuousResult(i, TimeSpan.Zero)));
            runResults.Add(betterGenome, Enumerable.Range(1, 10).Select(i => new ContinuousResult(10, TimeSpan.Zero)));

            var sortedGenomes = SortByValueTest.descendingSorter.Sort(runResults);
            Assert.Equal(betterGenome, sortedGenomes.First());
            Assert.Equal(worseGenome, sortedGenomes.Last());
        }

        /// <summary>
        /// Checks that ascending <see cref="SortByValue.Sort(Dictionary{ImmutableGenome, IEnumerable{ContinuousResult}})"/>
        /// returns the genome with the higher number of valid results first, even if it has a higher average value.
        /// </summary>
        [Fact]
        public void AscendingSorterHandlesDifferentNumberOfRunsPerGenomeCorrectly()
        {
            var worseGenome = new ImmutableGenome(new Genome(1));
            var betterGenome = new ImmutableGenome(new Genome(2));

            var runResults = new Dictionary<ImmutableGenome, IEnumerable<ContinuousResult>>(capacity: 2);
            runResults.Add(worseGenome, new ContinuousResult[] { new ContinuousResult(1, TimeSpan.Zero) });
            runResults.Add(betterGenome, Enumerable.Range(1, 10).Select(i => new ContinuousResult(2, TimeSpan.Zero)));

            var sortedGenomes = SortByValueTest.ascendingSorter.Sort(runResults);
            Assert.Equal(betterGenome, sortedGenomes.First());
            Assert.Equal(worseGenome, sortedGenomes.Last());
        }

        /// <summary>
        /// Checks that descending <see cref="SortByValue.Sort(Dictionary{ImmutableGenome, IEnumerable{ContinuousResult}})"/>
        /// returns the genome with the higher number of valid results first, even if it has a lower average value.
        /// </summary>
        [Fact]
        public void DescendingSorterHandlesDifferentNumberOfRunsPerGenomeCorrectly()
        {
            var worseGenome = new ImmutableGenome(new Genome(1));
            var betterGenome = new ImmutableGenome(new Genome(2));

            var runResults = new Dictionary<ImmutableGenome, IEnumerable<ContinuousResult>>(capacity: 2);
            runResults.Add(worseGenome, new ContinuousResult[] { new ContinuousResult(2, TimeSpan.Zero) });
            runResults.Add(betterGenome, Enumerable.Range(1, 10).Select(i => new ContinuousResult(1, TimeSpan.Zero)));

            var sortedGenomes = SortByValueTest.descendingSorter.Sort(runResults);
            Assert.Equal(betterGenome, sortedGenomes.First());
            Assert.Equal(worseGenome, sortedGenomes.Last());
        }

        /// <summary>
        /// Checks that ascending <see cref="SortByValue.Sort(Dictionary{ImmutableGenome, IEnumerable{ContinuousResult}})"/> handles invalid results correctly.
        /// </summary>
        [Fact]
        public void AscendingSorterHandlesInvalidResultsCorrectly()
        {
            var worstGenome = new ImmutableGenome(new Genome(1));
            var middleWorseGenome = new ImmutableGenome(new Genome(2));
            var middleBetterGenome = new ImmutableGenome(new Genome(3));
            var bestGenome = new ImmutableGenome(new Genome(4));

            var worstResults = Enumerable.Range(1, 4).Select(i => new ContinuousResult(double.NaN, TimeSpan.Zero));

            var middleWorseResults = Enumerable.Range(1, 2).Select(i => new ContinuousResult(double.NaN, TimeSpan.Zero))
                .Concat(Enumerable.Range(1, 2).Select(i => new ContinuousResult(10, TimeSpan.Zero)));

            var middleBetterResults = Enumerable.Range(1, 2).Select(i => new ContinuousResult(double.PositiveInfinity, TimeSpan.Zero))
                .Concat(Enumerable.Range(1, 2).Select(i => new ContinuousResult(5, TimeSpan.Zero)));

            var bestResults = Enumerable.Range(1, 4).Select(i => new ContinuousResult(10, TimeSpan.Zero));

            var runResults = new Dictionary<ImmutableGenome, IEnumerable<ContinuousResult>>(capacity: 4);
            runResults.Add(worstGenome, worstResults);
            runResults.Add(middleWorseGenome, middleWorseResults);
            runResults.Add(middleBetterGenome, middleBetterResults);
            runResults.Add(bestGenome, bestResults);

            var sortedGenomes = SortByValueTest.ascendingSorter.Sort(runResults);
            Assert.Equal(bestGenome, sortedGenomes.First());
            Assert.Equal(middleBetterGenome, sortedGenomes.Skip(1).First());
            Assert.Equal(middleWorseGenome, sortedGenomes.Skip(2).First());
            Assert.Equal(worstGenome, sortedGenomes.Last());
        }

        /// <summary>
        /// Checks that descending <see cref="SortByValue.Sort(Dictionary{ImmutableGenome, IEnumerable{ContinuousResult}})"/> handles invalid results correctly.
        /// </summary>
        [Fact]
        public void DescendingSorterHandlesInvalidResultsCorrectly()
        {
            var worstGenome = new ImmutableGenome(new Genome(1));
            var middleWorseGenome = new ImmutableGenome(new Genome(2));
            var middleBetterGenome = new ImmutableGenome(new Genome(3));
            var bestGenome = new ImmutableGenome(new Genome(4));

            var worstResults = Enumerable.Range(1, 4).Select(i => new ContinuousResult(double.NaN, TimeSpan.Zero));

            var middleWorseResults = Enumerable.Range(1, 2).Select(i => new ContinuousResult(double.NaN, TimeSpan.Zero))
                .Concat(Enumerable.Range(1, 2).Select(i => new ContinuousResult(5, TimeSpan.Zero)));

            var middleBetterResults = Enumerable.Range(1, 2).Select(i => new ContinuousResult(double.PositiveInfinity, TimeSpan.Zero))
                .Concat(Enumerable.Range(1, 2).Select(i => new ContinuousResult(10, TimeSpan.Zero)));

            var bestResults = Enumerable.Range(1, 4).Select(i => new ContinuousResult(5, TimeSpan.Zero));

            var runResults = new Dictionary<ImmutableGenome, IEnumerable<ContinuousResult>>(capacity: 4);
            runResults.Add(worstGenome, worstResults);
            runResults.Add(middleWorseGenome, middleWorseResults);
            runResults.Add(middleBetterGenome, middleBetterResults);
            runResults.Add(bestGenome, bestResults);

            var sortedGenomes = SortByValueTest.descendingSorter.Sort(runResults);
            Assert.Equal(bestGenome, sortedGenomes.First());
            Assert.Equal(middleBetterGenome, sortedGenomes.Skip(1).First());
            Assert.Equal(middleWorseGenome, sortedGenomes.Skip(2).First());
            Assert.Equal(worstGenome, sortedGenomes.Last());
        }

        #endregion
    }
}