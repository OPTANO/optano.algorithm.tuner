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

namespace Optano.Algorithm.Tuner.MachineLearning.RandomForest
{
    using SharpLearning.Containers.Views;
    using SharpLearning.DecisionTrees.ImpurityCalculators;

    /// <summary>
    /// An impurity calculator that tries to reach a high resolution on "high performing" areas of the parameter space.
    /// </summary>
    public interface ITopPerformerFocusImpurityCalculator : IImpurityCalculator
    {
        #region Public Methods and Operators

        /// <summary>
        /// Updates the interval and targets.
        /// Triggers a re-computation of the top performers.
        /// </summary>
        /// <param name="newInterval">The new interval.</param>
        /// <param name="targets">The targets.</param>
        void UpdateIntervalAndTargets(Interval1D newInterval, double[] targets);

        #endregion
    }
}