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
    /// Contains tests for the <see cref="SortByRuntime"/> class.
    /// </summary>
    public class SortByRuntimeTest
    {
        #region Static Fields

        /// <summary>
        /// The <see cref="SortByRuntime"/> sorter used in tests.
        /// </summary>
        private static readonly SortByRuntime sorter = new SortByRuntime(1);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="SortByRuntime.Sort(System.Collections.Generic.Dictionary{Optano.Algorithm.Tuner.Genomes.ImmutableGenome,System.Collections.Generic.IEnumerable{Optano.Algorithm.Tuner.TargetAlgorithm.Results.RuntimeResult}})"/>
        /// returns the genome with lower average runtime first if the number of provided results is the same for both
        /// genomes.
        /// </summary>
        [Fact]
        public void SortReturnsGenomeWithLowerAverageRuntimeFirst()
        {
            var worseGenome = new ImmutableGenome(new Genome());
            var betterGenome = new ImmutableGenome(new Genome());

            var runResults = new Dictionary<ImmutableGenome, IEnumerable<RuntimeResult>>(capacity: 2);
            runResults.Add(worseGenome, Enumerable.Range(1, 10).Select(i => new RuntimeResult(TimeSpan.FromMilliseconds(i))));
            runResults.Add(betterGenome, Enumerable.Range(0, 10).Select(i => new RuntimeResult(TimeSpan.FromMilliseconds(i))));

            var sortedGenomes = SortByRuntimeTest.sorter.Sort(runResults);
            Assert.Equal(betterGenome, sortedGenomes.First());
            Assert.Equal(worseGenome, sortedGenomes.Last());
        }

        /// <summary>
        /// Checks that <see cref="SortByRuntime.Sort(System.Collections.Generic.Dictionary{Optano.Algorithm.Tuner.Genomes.ImmutableGenome,System.Collections.Generic.IEnumerable{Optano.Algorithm.Tuner.TargetAlgorithm.Results.RuntimeResult}})"/>
        /// returns the genome with lower average runtime first even if the number of provided results is different
        /// for the genomes and the one with lower average runtime has a higher runtime sum.
        /// </summary>
        [Fact]
        public void SortCanHandleDifferentNumberOfRunsPerGenome()
        {
            var worseGenome = new ImmutableGenome(new Genome());
            var betterGenome = new ImmutableGenome(new Genome());

            var runResults = new Dictionary<ImmutableGenome, IEnumerable<RuntimeResult>>(capacity: 2);
            runResults.Add(worseGenome, new RuntimeResult[] { new RuntimeResult(TimeSpan.FromMilliseconds(100)) });
            runResults.Add(betterGenome, Enumerable.Range(0, 100).Select(i => new RuntimeResult(TimeSpan.FromMilliseconds(2))));

            var sortedGenomes = SortByRuntimeTest.sorter.Sort(runResults);
            Assert.Equal(betterGenome, sortedGenomes.First());
            Assert.Equal(worseGenome, sortedGenomes.Last());
        }

        /// <summary>
        /// Checks that <see cref="SortByRuntime.GetMetricRepresentation"/> returns the total number of seconds for a
        /// successful <see cref="RuntimeResult"/>.
        /// </summary>
        [Fact]
        public void GetMetricRepresentationReturnsSecondsForSuccessfulRun()
        {
            var parSorter = new SortByRuntime(factorPar: 7);
            var result = new RuntimeResult(TimeSpan.FromSeconds(1234.78));
            Assert.Equal(
                1234.78,
                parSorter.GetMetricRepresentation(result));
        }

        /// <summary>
        /// Checks that <see cref="SortByRuntime.GetMetricRepresentation"/> returns the total number of seconds
        /// penalized with the corresponding penalization factor for a cancelled <see cref="RuntimeResult"/>.
        /// </summary>
        [Fact]
        public void GetMetricRepresentationAddsPenalizationForCancelledRun()
        {
            var parSorter = new SortByRuntime(factorPar: 7);
            var result = ResultBase<RuntimeResult>.CreateCancelledResult(TimeSpan.FromSeconds(3.1));
            Assert.Equal(
                21.7,
                parSorter.GetMetricRepresentation(result),
                8);
        }

        #endregion
    }
}