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

namespace Optano.Algorithm.Tuner.GrayBox.DataRecordTypes
{
    using System;
    using System.Linq;

    using Optano.Algorithm.Tuner.GrayBox.CsvRecorder;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Contains data, that is given to the <see cref="DataRecorder{TResult}"/> from the target algorithm adapter.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class AdapterDataRecord<TResult> : EventArgs
        where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterDataRecord{TResult}"/> class.
        /// </summary>
        /// <param name="targetAlgorithmName">The target algorithm name.</param>
        /// <param name="targetAlgorithmStatus">The target algorithm status.</param>
        /// <param name="expendedCpuTime">The expended cpu time.</param>
        /// <param name="expendedWallClockTime">The expended wall clock time.</param>
        /// <param name="timeStamp">The time stamp.</param>
        /// <param name="adapterFeaturesHeader">The adapter features header.</param>
        /// <param name="adapterFeatures">The adapter features.</param>
        /// <param name="currentGrayBoxResult">The current gray box result.</param>
        public AdapterDataRecord(
            string targetAlgorithmName,
            TargetAlgorithmStatus targetAlgorithmStatus,
            TimeSpan expendedCpuTime,
            TimeSpan expendedWallClockTime,
            DateTime timeStamp,
            string[] adapterFeaturesHeader,
            double[] adapterFeatures,
            TResult currentGrayBoxResult)
        {
            this.TargetAlgorithmName = targetAlgorithmName;
            this.TargetAlgorithmStatus = targetAlgorithmStatus;
            this.ExpendedCpuTime = expendedCpuTime;
            this.ExpendedWallClockTime = expendedWallClockTime;
            this.TimeStamp = timeStamp;
            this.AdapterFeaturesHeader = adapterFeaturesHeader;
            this.AdapterFeatures = adapterFeatures;
            this.CurrentGrayBoxResult = currentGrayBoxResult;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the adapter features header prefix.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        public static string AdapterFeaturesHeaderPrefix => "AdapterFeature_";

        /// <summary>
        /// Gets the current gray box result header prefix.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        public static string CurrentGrayBoxResultHeaderPrefix => "CurrentGrayBoxResult_";

        /// <summary>
        /// Gets the other header.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        public static string[] OtherHeader => new[]
                                                  {
                                                      "TargetAlgorithmName",
                                                      "TargetAlgorithmStatus",
                                                      "ExpendedCpuTime",
                                                      "ExpendedWallClockTime",
                                                      "TimeStamp",
                                                  };

        /// <summary>
        /// Gets the target algorithm name.
        /// </summary>
        public string TargetAlgorithmName { get; }

        /// <summary>
        /// Gets the target algorithm status.
        /// </summary>
        public TargetAlgorithmStatus TargetAlgorithmStatus { get; }

        /// <summary>
        /// Gets the expended CPU time.
        /// </summary>
        public TimeSpan ExpendedCpuTime { get; }

        /// <summary>
        /// Gets the expended wall clock time.
        /// </summary>
        public TimeSpan ExpendedWallClockTime { get; }

        /// <summary>
        /// Gets the time stamp.
        /// </summary>
        public DateTime TimeStamp { get; }

        /// <summary>
        /// Gets the adapter feature specific part of the header.
        /// </summary>
        public string[] AdapterFeaturesHeader { get; }

        /// <summary>
        /// Gets the adapter features.
        /// </summary>
        public double[] AdapterFeatures { get; }

        /// <summary>
        /// Gets the current gray box result.
        /// </summary>
        public TResult CurrentGrayBoxResult { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns the header.
        /// </summary>
        /// <returns>The header.</returns>
        public string[] GetHeader()
        {
            var adapterFeaturesHeader =
                this.AdapterFeaturesHeader.Select(header => $"{AdapterDataRecord<TResult>.AdapterFeaturesHeaderPrefix}{header}");
            var currentGrayBoxResultHeader = this.CurrentGrayBoxResult.GetHeader()
                .Select(header => $"{AdapterDataRecord<TResult>.CurrentGrayBoxResultHeaderPrefix}{header}");
            return AdapterDataRecord<TResult>.OtherHeader.Concat(adapterFeaturesHeader).Concat(currentGrayBoxResultHeader).ToArray();
        }

        /// <summary>
        /// Returns the string array representation.
        /// </summary>
        /// <returns>The string array representation.</returns>
        public string[] ToStringArray()
        {
            var adapterFeatureValues = this.AdapterFeatures.Select(d => $"{d:0.######}");
            var currentGrayBoxResultValues = this.CurrentGrayBoxResult.ToStringArray();
            var otherValues = new[]
                                  {
                                      this.TargetAlgorithmName,
                                      $"{this.TargetAlgorithmStatus}",
                                      $"{this.ExpendedCpuTime.TotalMilliseconds:0}",
                                      $"{this.ExpendedWallClockTime.TotalMilliseconds:0}",
                                      $"{this.TimeStamp:yyyyMMddHHmmss}",
                                  };
            return otherValues.Concat(adapterFeatureValues).Concat(currentGrayBoxResultValues).ToArray();
        }

        #endregion
    }
}