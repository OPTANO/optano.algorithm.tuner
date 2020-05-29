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

namespace Optano.Algorithm.Tuner
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using MathNet.Numerics.Distributions;

    /// <summary>
    /// Basis for all randomness in the project.
    /// </summary>
    public class Randomizer
    {
        #region Static Fields

        /// <summary>
        /// Object to lock changes happening on the <see cref="instance"/> field.
        /// </summary>
        private static readonly object instanceLock = new object();

        /// <summary>
        /// The single <see cref="Randomizer" /> instance.
        /// </summary>
        [SuppressMessage(
            "NDepend",
            "ND1905:DontAssignAFieldFromManyMethods",
            Justification = "All access is synchronized with instanceLock. instance is only set in 2 places, 1 of which is the Reset-function that simply sets it to null.")]
        private static Randomizer instance;

        #endregion

        #region Fields

        /// <summary>
        /// A pseudo-random number generator.
        /// Never call directly, but use the thread-safe <see cref="Randomizer.Next(int)" />,
        /// <see cref="Randomizer.Next(int, int)" /> and <see cref="Randomizer.NextDouble" /> methods.
        /// </summary>
        private readonly Random _random;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Prevents a default instance of the <see cref="Randomizer" /> class from being created.
        /// </summary>
        private Randomizer()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Randomizer" /> class.
        /// </summary>
        /// <param name="seed">A number that is used as seed.</param>
        [SuppressMessage(
            "NDepend",
            "ND3101:DontUseSystemRandomForSecurityPurposes",
            Justification = "The generated random numbers are not used in security purposes. Reproducability (through seed) is a desired feature.")]
        private Randomizer(int seed)
        {
            this._random = new Random(seed);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the <see cref="Randomizer" /> instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if called before the instance was configured.</exception>
        public static Randomizer Instance
        {
            get
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        throw new InvalidOperationException("No randomizer was configured.");
                    }

                    return instance;
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates the <see cref="Randomizer"/> instance with a time-dependent default seed value.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if called a second time.</exception>
        [SuppressMessage(
            "NDepend",
            "ND3101:DontUseSystemRandomForSecurityPurposes",
            Justification = "The generated random numbers are not used in security purposes. Randomly generated seed here is a feature.")]
        public static void Configure()
        {
            Configure(new Random().Next());
        }

        /// <summary>
        /// Creates the <see cref="Randomizer" /> instance with a certain seed.
        /// </summary>
        /// <param name="seed">A number that is used as seed.</param>
        /// <exception cref="InvalidOperationException">Thrown if called a second time.</exception>
        public static void Configure(int seed)
        {
            lock (instanceLock)
            {
                if (instance != null)
                {
                    throw new InvalidOperationException("A randomizer instance already exists.");
                }

                instance = new Randomizer(seed);
            }
        }

        /// <summary>
        /// Destroys the <see cref="Randomizer"/> instance s.t. <see cref="Randomizer.Configure(int)"/> must be called
        /// again.
        /// </summary>
        public static void Reset()
        {
            lock (instanceLock)
            {
                instance = null;
            }
        }

        /// <summary>
        /// A thread-safe <see cref="Random.Next(int, int)" /> implementation.
        /// <para>It returns a random integer that is within a specified range.</para>
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">
        /// The exclusive upper bound of the random number returned. maxValue must be greater
        /// than or equal to minValue.
        /// </param>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to minValue and less than maxValue;
        /// that is, the range of return values includes minValue but not maxValue. If minValue
        /// equals maxValue, minValue is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">minValue is greater than maxValue.</exception>
        public int Next(int minValue, int maxValue)
        {
            lock (this._random)
            {
                return this._random.Next(minValue, maxValue);
            }
        }

        /// <summary>
        /// A thread-safe <see cref="Random.Next(int)" /> implementation.
        /// <para>
        /// It returns a nonnegative random integer that is less than the specified maximum.
        /// </para>
        /// </summary>
        /// <param name="maxValue">
        /// The exclusive upper bound of the random number to be generated. maxValue must
        /// be greater than or equal to zero.
        /// By default, <paramref name="maxValue"/> is set to <see cref="int.MaxValue"/>.
        /// </param>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to zero, and less than maxValue;
        /// that is, the range of return values ordinarily includes zero but not maxValue.
        /// However, if maxValue equals zero, maxValue is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">maxValue is less than zero.</exception>
        public int Next(int maxValue = int.MaxValue)
        {
            lock (this._random)
            {
                return this._random.Next(maxValue);
            }
        }

        /// <summary>
        /// A thread-safe <see cref="Random.NextDouble" /> implementation.
        /// <para>It returns a random floating-point number between 0.0 and 1.0.</para>
        /// </summary>
        /// <returns>
        /// A double-precision floating point number greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        public double NextDouble()
        {
            lock (this._random)
            {
                return this._random.NextDouble();
            }
        }

        /// <summary>
        /// Selects a random subset from the given collection.
        /// </summary>
        /// <remarks>Based on Fisher-Yates shuffle.</remarks>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <param name="from">The set to return a subset from.</param>
        /// <param name="number">Size of subset.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if a subset with negative size or size larger than the
        /// collection itself is requested.
        /// </exception>
        /// <returns>A random subset from the given collection.</returns>
        public IEnumerable<T> ChooseRandomSubset<T>(IEnumerable<T> from, int number)
        {
            var sourceList = from.ToList();
            if ((number < 0) || (number > sourceList.Count))
            {
                throw new ArgumentException(
                    $"Can only return between 0 and {sourceList.Count} items of a list with a total length of {sourceList.Count}, but the requested number was {number}");
            }

            // Use an index list to randomly choose indices.
            var indexPermutation = Enumerable.Range(0, sourceList.Count).ToList();
            // Until the subset is completed:
            for (var i = 0; i < number; i++)
            {
                // Select random index of remaining indices.
                var randomIndex = this.Next(i, indexPermutation.Count);

                // Update remaining indices by item swapping in index list.
                var indexToSwap = indexPermutation[i];
                indexPermutation[i] = indexPermutation[randomIndex];
                indexPermutation[randomIndex] = indexToSwap;

                // Return item at the random index.
                yield return sourceList[indexPermutation[i]];
            }
        }

        /// <summary>
        /// Samples from a normal distribution.
        /// </summary>
        /// <param name="mean">The distribution's mean.</param>
        /// <param name="standardDeviation">The distribution's standard deviation.</param>
        /// <returns>The generated sample.</returns>
        public double SampleFromNormal(double mean, double standardDeviation)
        {
            lock (this._random)
            {
                var distribution = new Normal(mean, standardDeviation, this._random);
                return distribution.Sample();
            }
        }

        /// <summary>
        /// Samples from a truncated normal distribution.
        /// </summary>
        /// <param name="mean">The distribution's mean.</param>
        /// <param name="standardDeviation">The distribution's standard deviation.</param>
        /// <param name="minimum">Minimum value.</param>
        /// <param name="maximum">Maximum value.</param>
        /// <returns>The generated sample.</returns>
        public double SampleFromTruncatedNormal(
            double mean,
            double standardDeviation,
            double minimum,
            double maximum)
        {
            // Create the underlying normal distribution to make use of inversion technique.
            var originalDistribution = new Normal(mean, standardDeviation);

            // Select a random value of the distribution function whose inverse is part of the interval.
            var randomFactor = this.SampleFromUniformDistribution(0, 1);
            var distributionValueWidth = originalDistribution.CumulativeDistribution(maximum)
                                         - originalDistribution.CumulativeDistribution(minimum);
            var randomDistributionValueInInterval = originalDistribution.CumulativeDistribution(minimum)
                                                    + (randomFactor * distributionValueWidth);

            // Return that inverse.
            return originalDistribution.InverseCumulativeDistribution(randomDistributionValueInInterval);
        }

        /// <summary>
        /// Samples from a uniform distribution.
        /// </summary>
        /// <param name="minimum">Minimum value.</param>
        /// <param name="maximum">Maximum value.</param>
        /// <returns>The generated sample.</returns>
        public double SampleFromUniformDistribution(double minimum, double maximum)
        {
            // Go from average instead of minimum to prevent overflows.
            var average = (minimum / 2) + (maximum / 2);
            var randomDifference = ((2 * this.NextDouble()) - 1) * (maximum - average);
            return average + randomDifference;
        }

        /// <summary>
        /// Samples from a Cauchy distribution.
        /// </summary>
        /// <param name="location">The distribution's location.</param>
        /// <param name="scale">The distribution's scale.</param>
        /// <returns>The generated sample.</returns>
        public double SampleFromCauchyDistribution(double location, double scale)
        {
            lock (this._random)
            {
                var distribution = new Cauchy(location, scale, this._random);
                return distribution.Sample();
            }
        }

        /// <summary>
        /// Makes a random binary decision.
        /// </summary>
        /// <param name="probability">The probabiity to return true.</param>
        /// <returns>The decision.</returns>
        public bool Decide(double probability = 0.5)
        {
            if ((probability < 0) || (probability > 1))
            {
                throw new ArgumentOutOfRangeException(
                    $"A probability parameter {probability} lower than 0 or higher than 1 was given.");
            }

            return this.NextDouble() < probability;
        }

        /// <summary>
        /// Performs a roulette wheel selection, based on the given weights.
        /// Each index i will be chosen with a probability of weight[i] / sum(weights).
        /// </summary>
        /// <param name="weight">The (un-/normalized) weights.</param>
        /// <param name="interpreteWeightAsRank">If set to true, all weights will be reversed. I.e. w'[i] &lt;- max(weight) - weight[i].
        /// Use this, if you are using Ranks instead of weights.</param>
        /// <returns>The chosen index.</returns>
        public int RouletteSelect(double[] weight, bool interpreteWeightAsRank = false)
        {
            // rank 1 is better than rank 8 -> switch order
            double[] signCorrectedWeight;
            if (interpreteWeightAsRank)
            {
                var min = weight.Min();
                var max = weight.Max();

                // Example for Rank 1: 8 - 1 + 1 = 8. Without 'min', this would become 7 and Rank 8 would be chosen with 0 probability.
                signCorrectedWeight = weight.Select(w => max - w + min).ToArray();
            }
            else
            {
                signCorrectedWeight = weight;
            }

            var weightSum = signCorrectedWeight.Sum();

            // get a threshold value
            var thresholdValue = this.NextDouble() * weightSum;

            // locate the random value based on the weights
            for (var i = 0; i < weight.Length; i++)
            {
                thresholdValue -= weight[i];
                if (thresholdValue <= 0)
                {
                    return i;
                }
            }

            // when rounding errors occur, we return the last item's index
            return weight.Length - 1;
        }

        #endregion
    }
}