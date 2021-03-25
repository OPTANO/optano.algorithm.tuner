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

namespace Optano.Algorithm.Tuner.Tracking
{
    using System;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Tuning;

    /// <summary>
    /// Relevant information about a generation which will be written to an evaluation file at the end of the tuning.
    /// </summary>
    public class GenerationInformation
    {
        #region Constants

        /// <summary>
        /// Explanation of <see cref="ToString"/>.
        /// </summary>
        public const string LegendOfGenerationInformation =
            "Generation;Elapsed(d:hh:mm:ss);Total # Evaluations;Average Train Incumbent;Average Test Incumbent;Strategy;Incumbent";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationInformation"/> class.
        /// </summary>
        /// <param name="generation">The generation index.</param>
        /// <param name="totalElapsedTime"> The total elapsed time.</param>
        /// <param name="totalNumberOfEvaluations">
        /// The total number of evaluations at the end of the generation.
        /// </param>
        /// <param name="strategy">The strategy type used in the generation.</param>
        /// <param name="incumbent">The incumbent.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="generation"/> or <paramref name="totalNumberOfEvaluations"/> are negative.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="strategy"/> or <paramref name="incumbent"/> are <c>null</c>.
        /// </exception>
        public GenerationInformation(
            int generation,
            TimeSpan totalElapsedTime,
            int totalNumberOfEvaluations,
            Type strategy,
            ImmutableGenome incumbent)
        {
            if (generation < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(generation),
                    $"Generation must not be negative, but was {generation}.");
            }

            if (totalNumberOfEvaluations < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalNumberOfEvaluations),
                    $"Number of evaluations must not be negative, but was {totalNumberOfEvaluations}.");
            }

            if (totalElapsedTime.TotalMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalElapsedTime),
                    $"The total elapsed time must not be negative, but was {totalElapsedTime.TotalMilliseconds} ms.");
            }

            this.Generation = generation;
            this.TotalElapsedTime = totalElapsedTime;
            this.TotalNumberOfEvaluations = totalNumberOfEvaluations;
            this.Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            this.Incumbent = incumbent ?? throw new ArgumentNullException(nameof(incumbent));
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the generation index.
        /// </summary>
        public int Generation { get; }

        /// <summary>
        /// Gets the total elapsed time.
        /// </summary>
        public TimeSpan TotalElapsedTime { get; }

        /// <summary>
        /// Gets the total number of evaluations at the end of the generation.
        /// </summary>
        public int TotalNumberOfEvaluations { get; }

        /// <summary>
        /// Gets the strategy type used in that generation.
        /// </summary>
        public Type Strategy { get; }

        /// <summary>
        /// Gets the incumbent as determined by
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}"/>.
        /// </summary>
        public ImmutableGenome Incumbent { get; }

        /// <summary>
        /// Gets or sets <see cref="Incumbent"/>'s average score over the complete training set.
        /// </summary>
        public double? IncumbentTrainingScore { get; set; }

        /// <summary>
        /// Gets or sets <see cref="Incumbent"/>'s average score over the complete test set.
        /// </summary>
        public double? IncumbentTestScore { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <seealso cref="LegendOfGenerationInformation"/>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return FormattableString.Invariant(
                $"{this.Generation};{this.TotalElapsedTime:G};{this.TotalNumberOfEvaluations};{this.IncumbentTrainingScore};{this.IncumbentTestScore};{this.Strategy.Name};{this.Incumbent}");
        }

        #endregion
    }
}