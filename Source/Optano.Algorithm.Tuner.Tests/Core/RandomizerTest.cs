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

namespace Optano.Algorithm.Tuner.Tests.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Accord.Statistics.Distributions.Univariate;
    using Accord.Statistics.Testing;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="Randomizer"/>.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class RandomizerTest : IDisposable
    {
        #region Fields

        /// <summary>
        /// Arbitrary list used in some tests.
        /// </summary>
        private readonly List<int> _numberList = new List<int> { 3, -1, 2, 5 };

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomizerTest"/> class.
        /// </summary>
        public RandomizerTest()
        {
            Randomizer.Configure();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Resets the <see cref="Randomizer"/>.
        /// </summary>
        public void Dispose()
        {
            Randomizer.Reset();
        }

        /// <summary>
        /// Checks that a second configuration call throws an exception.
        /// </summary>
        [Fact]
        public void SecondConfigureThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => Randomizer.Configure(0));
        }

        /// <summary>
        /// Checks that calling <see cref="Randomizer.Instance"/> on an unconfigured <see cref="Randomizer"/> throws 
        /// an <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void UnconfiguredRandomizerThrowsException()
        {
            Randomizer.Reset();
            Assert.Throws<InvalidOperationException>(() => Randomizer.Instance);
        }

        /// <summary>
        /// Checks that it is possible to call <see cref="Randomizer.Configure(int)"/> several times if 
        /// <see cref="Randomizer.Reset"/> is called in-between.
        /// </summary>
        [Fact]
        public void ResetAllowsSecondConfigure()
        {
            // Start test in neutral state.
            Randomizer.Reset();

            // Then call configure twice with a reset in-between.
            Randomizer.Configure(0);
            Randomizer.Reset();
            Randomizer.Configure(1);

            // Check randomizer instance can be grabbed.
            Assert.NotNull(Randomizer.Instance);
        }

        /// <summary>
        /// Checks that two calls to <see cref="Randomizer.Instance"/> return the same objects.
        /// </summary>
        [Fact]
        public void SameInstanceIsReturnedEveryTime()
        {
            var instance1 = Randomizer.Instance;
            var instance2 = Randomizer.Instance;
            Assert.Equal(instance1, instance2);
        }

        /// <summary>
        /// Checks that the size of the subset returned by
        /// <see cref="Randomizer.ChooseRandomSubset{T}(IEnumerable{T}, int)"/> is correct.
        /// </summary>
        [Fact]
        public void ChooseRandomSubsetReturnsCorrectNumberOfItems()
        {
            var randomChoice = Randomizer.Instance.ChooseRandomSubset(this._numberList, 2);
            Assert.Equal(2, randomChoice.Count());
        }

        /// <summary>
        /// Checks that an exception is thrown if a subset with a negative size is requested.
        /// </summary>
        [Fact]
        public void ChooseRandomSubsetThrowsForNegativeCount()
        {
            Assert.Throws<ArgumentException>(() => Randomizer.Instance.ChooseRandomSubset(this._numberList, -1).ToList());
        }

        /// <summary>
        /// Checks that an empty collection is returned if a subset with size 0 is requested.
        /// </summary>
        [Fact]
        public void ChooseRandomSubsetReturnsEmptyForZeroCount()
        {
            var randomChoice = Randomizer.Instance.ChooseRandomSubset(this._numberList, 0);
            Assert.Empty(randomChoice);
        }

        /// <summary>
        /// Checks that the items stay the same if a subset of the same size as the set is requested.
        /// </summary>
        [Fact]
        public void ChooseRandomSubsetReturnsAllForTotalCount()
        {
            var randomChoice = Randomizer.Instance.ChooseRandomSubset(this._numberList, this._numberList.Count);
            Assert.True(
                TestUtils.SetsAreEquivalent(randomChoice, this._numberList),
                $"Shuffle changed items from {TestUtils.PrintList(this._numberList)} to {TestUtils.PrintList(randomChoice)}.");
        }

        /// <summary>
        /// Checks that an exception is thrown if a subset larger than the set is requested.
        /// </summary>
        [Fact]
        public void ChooseRandomSubsetThrowsForCountHigherListCount()
        {
            Assert.Throws<ArgumentException>(() => Randomizer.Instance.ChooseRandomSubset(this._numberList, this._numberList.Count + 1).ToList());
        }

        /// <summary>
        /// Calls <see cref="Randomizer.ChooseRandomSubset{T}(IEnumerable{T}, int)"/> many times for a three item list and checks
        /// that the distribution of permutations does not depart from the uniform distribution with the help of
        /// the Chi-Squared test.
        /// </summary>
        [Fact]
        public void ChooseRandomSubsetForThreeOfThreeDoesNotDepartFromUniformPermutationDistribution()
        {
            var testList = new List<int> { 1, 2, 3 };
            var hitPermutation = new Dictionary<string, int>();

            int numberRuns = 10000;
            for (int i = 0; i < numberRuns; i++)
            {
                var result = Randomizer.Instance.ChooseRandomSubset(testList, number: 3);
                string resultDescription = TestUtils.PrintList(result);
                if (hitPermutation.ContainsKey(resultDescription))
                {
                    hitPermutation[resultDescription]++;
                }
                else
                {
                    hitPermutation.Add(resultDescription, 1);
                }
            }

            // Apply the Chi-Squared test.
            double[] observed = hitPermutation.Select(keyValuePair => (double)keyValuePair.Value).ToArray();
            double[] expected = Enumerable.Range(0, 6).Select(i => numberRuns / 6.0).ToArray();
            ChiSquareTest uniformTest = new ChiSquareTest(expected, observed, degreesOfFreedom: 5);
            Assert.False(
                uniformTest.Significant,
                $"Choice of random 3-item subset out of 3 items was found to not produce a uniform distribution over the 6 permutations by the Chi-Squared test with significance level {uniformTest.Size}.");
        }

        /// <summary>
        /// Checks that <see cref="Randomizer.ChooseRandomSubset{T}(IEnumerable{T}, int)"/> does not change the input list.
        /// </summary>
        [Fact]
        public void ChooseRandomSubsetDoesNotChangeList()
        {
            // Copy list for later comparison.
            var listCopy = new List<int>(this._numberList);

            // Call choose random & evaluate the return.
            var permutationResult = Randomizer.Instance.ChooseRandomSubset(this._numberList, this._numberList.Count).ToList();

            // Check that list was not changed.
            Assert.True(
                Enumerable.SequenceEqual(listCopy, this._numberList),
                $"Started off with {TestUtils.PrintList(listCopy)}, but had {TestUtils.PrintList(this._numberList)} after using Randomizer.");
        }

        /// <summary>
        /// Uses the Kolmogorov-Smirnov test to verify that the results of
        /// <see cref="Randomizer.SampleFromNormal"/> produce a distribution that does not depart
        /// from the respective normal distribution.
        /// </summary>
        [Fact]
        public void SampleFromNormalDoesNotDepartFromNormalDistribution()
        {
            // Fix the value to mean and the variance.
            double mean = 3.4;
            double variance = 0.2;

            // Collect a large set of samples.
            int numberRuns = 10000;
            double[] results = new double[numberRuns];
            for (int i = 0; i < numberRuns; i++)
            {
                results[i] = Randomizer.Instance.SampleFromNormal(
                    mean,
                    standardDeviation: Math.Sqrt(variance));
            }

            // Apply the Kolmogorov-Smirnov test.
            NormalDistribution expected = new NormalDistribution(mean, stdDev: Math.Sqrt(variance));
            KolmogorovSmirnovTest normalityTest = new KolmogorovSmirnovTest(results, expected);
            Assert.False(
                double.IsNaN(normalityTest.PValue) || normalityTest.Significant,
                $"Sampled normal was identified as non normal by the Kolmogorov-Smirnov test with significance level of {normalityTest.Size}.");
        }

        /// <summary>
        /// Uses the Kolmogorov-Smirnov test to verify that the results of
        /// <see cref="Randomizer.SampleFromTruncatedNormal(double, double, double, double)"/> with borders set to
        /// minimum and maximum double values produce a distribution that does not depart
        /// from the respective normal distribution.
        /// </summary>
        [Fact]
        public void SampleFromTruncatedNormalDoesNotDepartFromNormalDistributionForNoTruncation()
        {
            // Fix the value to mean and the variance.
            double mean = 3.4;
            double variance = 0.2;

            // Collect a large set of samples.
            int numberRuns = 10000;
            double[] results = new double[numberRuns];
            for (int i = 0; i < numberRuns; i++)
            {
                results[i] = Randomizer.Instance.SampleFromTruncatedNormal(
                    mean,
                    standardDeviation: Math.Sqrt(variance),
                    minimum: double.MinValue,
                    maximum: double.MaxValue);
            }

            // Apply the Kolmogorov-Smirnov test.
            NormalDistribution expected = new NormalDistribution(mean, stdDev: Math.Sqrt(variance));
            KolmogorovSmirnovTest normalityTest = new KolmogorovSmirnovTest(results, expected);
            Assert.False(
                double.IsNaN(normalityTest.PValue) || normalityTest.Significant,
                $"Truncated normal without truncation was identified as non normal by the Kolmogorov-Smirnov test with significance level of {normalityTest.Size}.");
        }

        /// <summary>
        /// Checks that results from <see cref="Randomizer.SampleFromTruncatedNormal(double, double, double, double)"/>
        /// are contained in the interval used for the call.
        /// </summary>
        [Fact]
        public void SampleFromTruncatedNormalStaysInInterval()
        {
            // Fix bounds.
            double minimum = -1;
            double maximum = 1067;

            // Fix mean and standard deviation.
            double mean = 1050;
            double standardDeviation = 500;

            // For a lot of tries:
            int numberRuns = 1000;
            for (int i = 0; i < numberRuns; i++)
            {
                // Draw a sample and check it is in the specified interval.
                double sample = Randomizer.Instance.SampleFromTruncatedNormal(
                    mean,
                    standardDeviation,
                    minimum,
                    maximum);
                Assert.True(
                    minimum <= sample,
                    $"Truncated distribution generated sample {sample} which is smaller than minimum {minimum}.");
                Assert.True(
                    maximum >= sample,
                    $"Truncated distribution generated sample {sample} which is larger than maximum {maximum}.");
            }
        }

        /// <summary>
        /// Uses the Anderson-Darling test to verify that the results of
        /// <see cref="Randomizer.SampleFromUniformDistribution(double, double)"/> produce a distribution that does
        /// not depart from the respective uniform distribution.
        /// </summary>
        [Fact]
        public void SampleFromUniformDistributionDoesNotDepartFromUniformDistribution()
        {
            // Fix the minimum and maximum values.
            double minimum = -3.3;
            double maximum = double.MaxValue;

            // Collect a large set of samples.
            int numberRuns = 1000;
            double[] results = new double[numberRuns];
            for (int i = 0; i < numberRuns; i++)
            {
                results[i] = Randomizer.Instance.SampleFromUniformDistribution(minimum, maximum);
            }

            // Apply the Anderson-Darling test.
            AndersonDarlingTest uniformTest = new AndersonDarlingTest(results, new UniformContinuousDistribution(minimum, maximum));
            Assert.False(
                uniformTest.Significant,
                $"Random uniform sampling was found to be not uniform by the Anderson-Darling test with significance level of {uniformTest.Size}.");
        }

        /// <summary>
        /// Uses the Anderson-Darling test to verify that the results of
        /// <see cref="Randomizer.SampleFromCauchyDistribution(double, double)"/> produce a distribution that does
        /// not depart from the respective Cauchy distribution.
        /// </summary>
        [Fact]
        public void SampleFromCauchyDistributionDoesNotDepartFromCauchyDistribution()
        {
            // Fix the location and scale.
            double location = -3.3;
            double scale = 0.22;

            // Collect a large set of samples.
            int numberRuns = 1000;
            double[] results = new double[numberRuns];
            for (int i = 0; i < numberRuns; i++)
            {
                results[i] = Randomizer.Instance.SampleFromCauchyDistribution(location, scale);
            }

            // Apply the Anderson-Darling test.
            AndersonDarlingTest cauchyTest = new AndersonDarlingTest(results, new CauchyDistribution(location, scale));
            Assert.False(
                cauchyTest.Significant,
                $"Cauchy sampling was found not to be correctly distributed by the Anderson-Darling test with significance level of {cauchyTest.Size}.");
        }

        /// <summary>
        /// Verifies that calling <see cref="Randomizer.Decide(double)"/> with a negative value throws an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void DecideWithNegativeProbabilityThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Randomizer.Instance.Decide(-0.1));
        }

        /// <summary>
        /// Checks that calling <see cref="Randomizer.Decide(double)"/> with 0 does not throw an error.
        /// </summary>
        [Fact]
        public void DecideWithZeroProbabilityDoesNotThrowError()
        {
            Randomizer.Instance.Decide(0);
        }

        /// <summary>
        /// Verifies that calling <see cref="Randomizer.Decide(double)"/> with a value higher than 1 throws an
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void DecideWithProbabilityHigherThan1ThrowsError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Randomizer.Instance.Decide(1.1));
        }

        /// <summary>
        /// Checks that calling <see cref="Randomizer.Decide(double)"/> with 1 does not throw an error.
        /// </summary>
        [Fact]
        public void DecideWithProbabilityOf1DoesNotThrowError()
        {
            Randomizer.Instance.Decide(1);
        }

        /// <summary>
        /// Calls <see cref="Randomizer.Decide(double)"/> many times for a specific probability and verifies that the
        /// amount of positive decisions conforms to a distribution where positive decisions happen with the
        /// given probability. Departion from that distribution is detected using a Chi-Squared test.
        /// </summary>
        [Fact]
        public void DecideRespectsGivenProbability()
        {
            double probability = 0.2;

            // Remember number of positive decisions...
            int positiveDecisions = 0;
            // ...in a lot of runs.
            int numberRuns = 1000;
            for (int i = 0; i < numberRuns; i++)
            {
                if (Randomizer.Instance.Decide(probability))
                {
                    positiveDecisions++;
                }
            }

            // Apply the Chi-Squared test.
            double[] observed = { positiveDecisions, numberRuns - positiveDecisions };
            double[] expected = { probability * numberRuns, numberRuns - (probability * numberRuns) };
            ChiSquareTest correctProbabilityTest = new ChiSquareTest(expected, observed, degreesOfFreedom: 5);
            Assert.False(
                correctProbabilityTest.Significant,
                $"Decide was found to not produce the correct probability by the Chi-Squared test with significance level {correctProbabilityTest.Size}.");
        }

        #endregion
    }
}