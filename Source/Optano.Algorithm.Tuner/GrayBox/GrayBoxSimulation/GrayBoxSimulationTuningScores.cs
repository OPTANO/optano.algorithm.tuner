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

namespace Optano.Algorithm.Tuner.GrayBox.GrayBoxSimulation
{
    using System;

    /// <summary>
    /// Contains the tuning scores of a <see cref="GrayBoxSimulation{TTargetAlgorithm, TInstance, TResult}"/>.
    /// </summary>
    public class GrayBoxSimulationTuningScores
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GrayBoxSimulationTuningScores" /> class.
        /// </summary>
        /// <param name="generationId">The generation ID.</param>
        /// <param name="numberOfEvaluations">The number of evaluations.</param>
        /// <param name="averagedBlackBoxEvaluationRuntime">The averaged black box evaluation runtime.</param>
        /// <param name="averagedGrayBoxEvaluationRuntime">The averaged gray box evaluation runtime.</param>
        /// <param name="averagedPercentageOfTournamentWinnerChanges">The averaged percentage of tournament winner changes.</param>
        /// <param name="averagedAdaptedWsCoefficient">The averaged adapted WS coefficient.</param>
        public GrayBoxSimulationTuningScores(
            int generationId,
            int numberOfEvaluations,
            TimeSpan averagedBlackBoxEvaluationRuntime,
            TimeSpan averagedGrayBoxEvaluationRuntime,
            double averagedPercentageOfTournamentWinnerChanges,
            double averagedAdaptedWsCoefficient)
        {
            this.GenerationId = generationId;
            this.NumberOfEvaluations = numberOfEvaluations;
            this.AveragedBlackBoxEvaluationRuntime = averagedBlackBoxEvaluationRuntime;
            this.AveragedGrayBoxEvaluationRuntime = averagedGrayBoxEvaluationRuntime;
            this.AveragedPercentageOfTournamentWinnerChanges = averagedPercentageOfTournamentWinnerChanges;
            this.AveragedAdaptedWsCoefficient = averagedAdaptedWsCoefficient;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the generation id.
        /// </summary>
        public int GenerationId { get; }

        /// <summary>
        /// Gets the number of genome instance pairs.
        /// </summary>
        public int NumberOfEvaluations { get; }

        /// <summary>
        /// Gets the averaged black box genome instance pair runtime.
        /// </summary>
        public TimeSpan AveragedBlackBoxEvaluationRuntime { get; }

        /// <summary>
        /// Gets the averaged gray box genome instance pair runtime.
        /// </summary>
        public TimeSpan AveragedGrayBoxEvaluationRuntime { get; }

        /// <summary>
        /// Gets the averaged runtime savings.
        /// </summary>
        public double AveragedRuntimeSavings => 1 - (this.AveragedGrayBoxEvaluationRuntime.TotalMilliseconds
                                                     / this.AveragedBlackBoxEvaluationRuntime.TotalMilliseconds);

        /// <summary>
        /// Gets the averaged percentage of tournament winner changes.
        /// </summary>
        public double AveragedPercentageOfTournamentWinnerChanges { get; }

        /// <summary>
        /// Gets the averaged adapted WS coefficient.
        /// </summary>
        public double AveragedAdaptedWsCoefficient { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns the header.
        /// </summary>
        /// <returns>The header.</returns>
        public static string[] GetHeader()
        {
            return new[]
                       {
                           "Generation",
                           "NumberOfEvaluations",
                           "AveragedBlackBoxEvaluationRuntime",
                           "AveragedGrayBoxEvaluationRuntime",
                           "AveragedRuntimeSavings",
                           "AveragedPercentageOfTournamentWinnerChanges",
                           "AveragedAdaptedWsCoefficient",
                       };
        }

        /// <summary>
        /// Returns the string array representation.
        /// </summary>
        /// <returns>The string array representation.</returns>
        public string[] ToStringArray()
        {
            return this.ToStringArray(this.GenerationId.ToString());
        }

        /// <summary>
        /// Returns the string array representation.
        /// </summary>
        /// <param name="generationReplacement">The string to replace the generation with, e.g.: "total".</param>
        /// <returns>The string array representation.</returns>
        public string[] ToStringArray(string generationReplacement)
        {
            return new[]
                       {
                           generationReplacement,
                           this.NumberOfEvaluations.ToString(),
                           $"{this.AveragedBlackBoxEvaluationRuntime.TotalMilliseconds:0}",
                           $"{this.AveragedGrayBoxEvaluationRuntime.TotalMilliseconds:0}",
                           $"{this.AveragedRuntimeSavings:0.######}",
                           $"{this.AveragedPercentageOfTournamentWinnerChanges:0.######}",
                           $"{this.AveragedAdaptedWsCoefficient:0.######}",
                       };
        }

        #endregion
    }
}