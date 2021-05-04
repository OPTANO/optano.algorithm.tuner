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

namespace Optano.Algorithm.Tuner.GrayBox.CsvRecorder
{
    using System;
    using System.IO;

    using Optano.Algorithm.Tuner.GrayBox.DataRecordTypes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Handles the recording of <see cref="DataRecord{TResult}"/>s.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class DataRecorder<TResult> : CsvRecorderBase<DataRecord<TResult>>
        where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRecorder{TResult}"/> class.
        /// </summary>
        /// <param name="csvFileInfo"> The file.</param>
        public DataRecorder(FileInfo csvFileInfo)
            : base(csvFileInfo, GrayBoxUtils.DataRecorderDelimiter, false)
        {
            if (!GrayBoxUtils.DataLogFileNameRegex.IsMatch(csvFileInfo.Name))
            {
                throw new ArgumentException(
                    $"The name of the data log file must match the regex {GrayBoxUtils.DataLogFileNameRegex}, but was {csvFileInfo.Name}!");
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public override string[] GetHeaderFromObject(DataRecord<TResult> data)
        {
            return data.GetHeader();
        }

        /// <inheritdoc />
        public override string[] GetValuesFromObject(DataRecord<TResult> data)
        {
            return data.ToStringArray();
        }

        #endregion
    }
}