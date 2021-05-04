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

namespace Optano.Algorithm.Tuner.GrayBox
{
    using Optano.Algorithm.Tuner.GrayBox.DataRecordTypes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Interface that is implemented by the target algorithm adapters to customize some gray box methods.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    public interface ICustomGrayBoxMethods<TResult>
        where TResult : ResultBase<TResult>, new()
    {
        #region Public Methods and Operators

        /// <summary>
        /// Gets the gray box features from the given <see cref="DataRecord{TResult}"/>.
        /// </summary>
        /// <param name="dataRecord">The <see cref="DataRecord{TResult}"/>.</param>
        /// <returns>The gray box features.</returns>
        double[] GetGrayBoxFeaturesFromDataRecord(DataRecord<TResult> dataRecord);

        /// <summary>
        /// Gets the gray box feature names from the given <see cref="DataRecord{TResult}"/>.
        /// </summary>
        /// <param name="dataRecord">The <see cref="DataRecord{TResult}"/>.</param>
        /// <returns>The gray box feature names.</returns>
        string[] GetGrayBoxFeatureNamesFromDataRecord(DataRecord<TResult> dataRecord);

        #endregion
    }
}