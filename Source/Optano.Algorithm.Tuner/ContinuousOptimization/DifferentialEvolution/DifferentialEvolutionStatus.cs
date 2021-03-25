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

namespace Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution
{
    using System;
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Serialization;

    /// <summary>
    /// An object wrapping the current status of <see cref="DifferentialEvolution{TSearchPoint}"/>.
    /// Can be serialized to a file and deserialized from one.
    /// </summary>
    /// <typeparam name="TSearchPoint">
    /// Type of <see cref="SearchPoint"/>s handled by the <see cref="DifferentialEvolution{TSearchPoint}"/> instance.
    /// </typeparam>
    public class DifferentialEvolutionStatus<TSearchPoint> : StatusBase
        where TSearchPoint : SearchPoint, IDeserializationRestorer<TSearchPoint>
    {
        #region Constants

        /// <summary>
        /// File name to use for serialized data.
        /// </summary>
        public const string FileName = "status.de";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentialEvolutionStatus{TSearchPoint}"/> class.
        /// </summary>
        /// <param name="sortedPopulation">
        /// The current population sorted by performance, best <typeparamref name="TSearchPoint"/>s first.
        /// </param>
        /// <param name="currentGeneration">The current generation.</param>
        /// <param name="maxGenerations">The maximum number of generations to run.</param>
        /// <param name="meanMutationFactor">
        /// The current value of the mean mutation factor, often called mu_F in literature.
        /// </param>
        /// <param name="meanCrossoverRate">
        /// The current value of the mean crossover constant, often called mu_{CR} in literature.
        /// </param>
        public DifferentialEvolutionStatus(
            List<TSearchPoint> sortedPopulation,
            int currentGeneration,
            int maxGenerations,
            double meanMutationFactor,
            double meanCrossoverRate)
        {
            ValidateParameters(
                sortedPopulation,
                currentGeneration,
                maxGenerations,
                meanMutationFactor,
                meanCrossoverRate);

            this.SortedPopulation = sortedPopulation;
            this.CurrentGeneration = currentGeneration;
            this.MaxGenerations = maxGenerations;
            this.MeanMutationFactor = meanMutationFactor;
            this.MeanCrossoverRate = meanCrossoverRate;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the current population sorted by performance, best <typeparamref name="TSearchPoint"/>s first.
        /// </summary>
        public List<TSearchPoint> SortedPopulation { get; }

        /// <summary>
        /// Gets the current generation.
        /// </summary>
        public int CurrentGeneration { get; }

        /// <summary>
        /// Gets the maximum number of generations to run.
        /// </summary>
        public int MaxGenerations { get; }

        /// <summary>
        /// Gets the current value of the mean mutation factor, often called mu_F in literature.
        /// </summary>
        public double MeanMutationFactor { get; }

        /// <summary>
        /// Gets the current value of the mean crossover constant, often called mu_{CR} in literature.
        /// </summary>
        public double MeanCrossoverRate { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Validates parameters handed to constructor.
        /// </summary>
        /// <param name="sortedPopulation">
        /// The current population sorted by performance, best <typeparamref name="TSearchPoint"/>s first.
        /// </param>
        /// <param name="currentGeneration">The current generation.</param>
        /// <param name="maxGenerations">The maximum number of generations to run.</param>
        /// <param name="meanMutationFactor">
        /// The current value of the mean mutation factor, often called mu_F in literature.
        /// </param>
        /// <param name="meanCrossoverRate">
        /// The current value of the mean crossover constant, often called mu_{CR} in literature.
        /// </param>
        private static void ValidateParameters(
            List<TSearchPoint> sortedPopulation,
            int currentGeneration,
            int maxGenerations,
            double meanMutationFactor,
            double meanCrossoverRate)
        {
            if (currentGeneration < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentGeneration),
                    $"Generation must be nonnegative, but was {currentGeneration}.");
            }

            if (maxGenerations < currentGeneration)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxGenerations),
                    $"Maximum number of generations must not be smaller than current generation {currentGeneration}, but was {maxGenerations}.");
            }

            if (meanMutationFactor < 0 || meanMutationFactor > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(meanMutationFactor),
                    $"Mean mutation factor must be in [0, 1], but was {meanMutationFactor}.");
            }

            if (meanCrossoverRate < 0 || meanCrossoverRate > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(meanCrossoverRate),
                    $"Mean crossover rate must be in [0, 1], but was {meanCrossoverRate}.");
            }
        }

        #endregion
    }
}