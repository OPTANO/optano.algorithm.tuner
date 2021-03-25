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

namespace Optano.Algorithm.Tuner.Tests.MachineLearning.RandomForest.RandomForestOutOfBox
{
    using System;

    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.RandomForestOutOfBox;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData.SamplingStrategies;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="StandardRandomForestLearner{TSamplingStrategy}"/>.
    /// </summary>
    public class StandardRandomForestLearnerTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="StandardRandomForestLearnerTest"/>s throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a negative value for features per split.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForNegativeFeaturesPerSplit()
        {
            var forestConfig = new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder().SetTreeCount(1)
                .SetRunParallel(false).BuildWithFallback(null);
            Assert.Throws<ArgumentOutOfRangeException>(() => new StandardRandomForestLearner<ReuseOldTreesStrategy>(forestConfig, -1));
        }

        #endregion
    }
}