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
    /// Contains all relevant parameters for a <see cref="GrayBoxSimulation{TTargetAlgorithm,TInstance,TResult}"/>.
    /// </summary>
    public class GrayBoxSimulationConfiguration
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GrayBoxSimulationConfiguration" /> class.
        /// </summary>
        /// <param name="grayBoxStartGeneration">The gray box start generation.</param>
        /// <param name="grayBoxStartTimePoint">The gray box start time point.</param>
        /// <param name="grayBoxConfidenceThreshold">The gray box confidence threshold.</param>
        /// <param name="numberOfDrawsPerGeneration">The number of draws per generation. Default is 100.</param>
        /// <param name="randomSeed">The random seed. Default is 42.</param>
        public GrayBoxSimulationConfiguration(
            int grayBoxStartGeneration,
            TimeSpan grayBoxStartTimePoint,
            double grayBoxConfidenceThreshold,
            int numberOfDrawsPerGeneration = 100,
            int randomSeed = 42)
        {
            GrayBoxUtils.CheckGrayBoxStartGeneration(grayBoxStartGeneration);
            GrayBoxUtils.CheckGrayBoxStartTimePoint(grayBoxStartTimePoint);
            GrayBoxUtils.CheckGrayBoxConfidenceThreshold(grayBoxConfidenceThreshold);

            if (numberOfDrawsPerGeneration <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(numberOfDrawsPerGeneration),
                    $"The number of draws per generation should always be positive, but {numberOfDrawsPerGeneration} was provided.");
            }

            this.GrayBoxStartGeneration = grayBoxStartGeneration;
            this.GrayBoxStartTimePoint = grayBoxStartTimePoint;
            this.GrayBoxConfidenceThreshold = grayBoxConfidenceThreshold;
            this.NumberOfDrawsPerGeneration = numberOfDrawsPerGeneration;
            this.RandomSeed = randomSeed;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the gray box start generation.
        /// </summary>
        public int GrayBoxStartGeneration { get; }

        /// <summary>
        /// Gets the gray box start time point.
        /// </summary>
        public TimeSpan GrayBoxStartTimePoint { get; }

        /// <summary>
        /// Gets the gray box confidence threshold.
        /// </summary>
        public double GrayBoxConfidenceThreshold { get; }

        /// <summary>
        /// Gets the number draws per generation.
        /// </summary>
        public int NumberOfDrawsPerGeneration { get; }

        /// <summary>
        /// Gets the random seed.
        /// </summary>
        public int RandomSeed { get; }

        #endregion
    }
}