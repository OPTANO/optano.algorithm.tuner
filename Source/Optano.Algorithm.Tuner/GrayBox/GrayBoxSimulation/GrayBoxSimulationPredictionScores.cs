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
    /// Contains the prediction scores of a <see cref="GrayBoxSimulation{TTargetAlgorithm, TInstance, TResult}"/>.
    /// </summary>
    public class GrayBoxSimulationPredictionScores
    {
        #region Fields

        /// <summary>
        /// The generation.
        /// </summary>
        private readonly int _generation;

        /// <summary>
        /// The current time point.
        /// </summary>
        private readonly TimeSpan _timePoint;

        /// <summary>
        /// The positive train data count.
        /// </summary>
        private readonly int _positiveTrainDataCount;

        /// <summary>
        /// The negative train data count.
        /// </summary>
        private readonly int _negativeTrainDataCount;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GrayBoxSimulationPredictionScores" /> class.
        /// </summary>
        /// <param name="generation">The generation.</param>
        /// <param name="timePoint">The current time point.</param>
        /// <param name="positiveTrainDataCount">The positive train data count.</param>
        /// <param name="negativeTrainDataCount">The negative train data count.</param>
        public GrayBoxSimulationPredictionScores(int generation, TimeSpan timePoint, int positiveTrainDataCount, int negativeTrainDataCount)
        {
            this._generation = generation;
            this._timePoint = timePoint;
            this._positiveTrainDataCount = positiveTrainDataCount;
            this._negativeTrainDataCount = negativeTrainDataCount;
            this.TruePositiveCount = 0;
            this.FalsePositiveCount = 0;
            this.TrueNegativeCount = 0;
            this.FalseNegativeCount = 0;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets the true positive count.
        /// </summary>
        public int TruePositiveCount { get; set; }

        /// <summary>
        /// Gets or sets the false positive count.
        /// </summary>
        public int FalsePositiveCount { get; set; }

        /// <summary>
        /// Gets or sets the true negative count.
        /// </summary>
        public int TrueNegativeCount { get; set; }

        /// <summary>
        /// Gets or sets the false negative count.
        /// </summary>
        public int FalseNegativeCount { get; set; }

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
                           "TimePoint",
                           "PositiveTrainData",
                           "NegativeTrainData",
                           "TruePositive",
                           "FalsePositive",
                           "TrueNegative",
                           "FalseNegative",
                       };
        }

        /// <summary>
        /// Returns the string array representation.
        /// </summary>
        /// <returns>The string array representation.</returns>
        public string[] ToStringArray()
        {
            return this.ToStringArray($"{this._timePoint.TotalMilliseconds:0}");
        }

        /// <summary>
        /// Returns the string array representation.
        /// </summary>
        /// <param name="timePointReplacement">The string to replace the time point with, e.g.: "total".</param>
        /// <returns>The string array representation.</returns>
        public string[] ToStringArray(string timePointReplacement)
        {
            return new[]
                       {
                           this._generation.ToString(),
                           timePointReplacement,
                           this._positiveTrainDataCount.ToString(),
                           this._negativeTrainDataCount.ToString(),
                           this.TruePositiveCount.ToString(),
                           this.FalsePositiveCount.ToString(),
                           this.TrueNegativeCount.ToString(),
                           this.FalseNegativeCount.ToString(),
                       };
        }

        #endregion
    }
}