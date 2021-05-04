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
    using System.Globalization;
    using System.Linq;

    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    using TinyCsvParser.TypeConverter;

    /// <inheritdoc />
    public class AdapterDataRecordCsvMapping<TTargetAlgorithm, TInstance, TResult> : IArrayTypeConverter<AdapterDataRecord<TResult>>
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// The target algorithm factory.
        /// </summary>
        private readonly ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> _targetAlgorithmFactory;

        /// <summary>
        /// The adapter features header.
        /// </summary>
        private readonly string[] _adapterFeaturesHeader;

        /// <summary>
        /// The number of result columns.
        /// </summary>
        private readonly int _numberOfResultColumns;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterDataRecordCsvMapping{TTargetAlgorithm, TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="targetAlgorithmFactory">The target algorithm factory.</param>
        /// <param name="adapterFeaturesHeader">The adapter features header.</param>
        /// <param name="numberOfResultColumns">The number of result columns.</param>
        public AdapterDataRecordCsvMapping(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            string[] adapterFeaturesHeader,
            int numberOfResultColumns)
        {
            this._targetAlgorithmFactory = targetAlgorithmFactory;
            this._adapterFeaturesHeader = adapterFeaturesHeader;
            this._numberOfResultColumns = numberOfResultColumns;
        }

        #endregion

        #region Public properties

        /// <inheritdoc />
        public Type TargetType => typeof(AdapterDataRecord<TResult>);

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public bool TryConvert(string[] value, out AdapterDataRecord<TResult> result)
        {
            result = null;
            if (value.Length != AdapterDataRecord<TResult>.OtherHeader.Length + this._adapterFeaturesHeader.Length + this._numberOfResultColumns)
            {
                return false;
            }

            var targetAlgorithmName = value[0];

            if (!Enum.TryParse(value[1], true, out TargetAlgorithmStatus targetAlgorithmStatus))
            {
                return false;
            }

            if (!int.TryParse(value[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var cpuTime))
            {
                return false;
            }

            if (!int.TryParse(value[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var wallClockTime))
            {
                return false;
            }

            if (!DateTime.TryParseExact(value[4], "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeStamp))
            {
                return false;
            }

            // Try to read adapter features.
            double[] adapterFeatures;
            try
            {
                adapterFeatures = value[new Range(
                    AdapterDataRecord<TResult>.OtherHeader.Length,
                    AdapterDataRecord<TResult>.OtherHeader.Length + this._adapterFeaturesHeader.Length)].Select(double.Parse).ToArray();
            }
            catch
            {
                return false;
            }

            // Try to read current gray box result.
            var currentGrayBoxResultString = value[new Range(
                AdapterDataRecord<TResult>.OtherHeader.Length + this._adapterFeaturesHeader.Length,
                AdapterDataRecord<TResult>.OtherHeader.Length + this._adapterFeaturesHeader.Length + this._numberOfResultColumns)];

            if (!this._targetAlgorithmFactory.TryToGetResultFromStringArray(currentGrayBoxResultString, out var currentGrayBoxResult))
            {
                return false;
            }

            result = new AdapterDataRecord<TResult>(
                targetAlgorithmName,
                targetAlgorithmStatus,
                TimeSpan.FromMilliseconds(cpuTime),
                TimeSpan.FromMilliseconds(wallClockTime),
                timeStamp,
                this._adapterFeaturesHeader,
                adapterFeatures,
                currentGrayBoxResult);
            return true;
        }

        #endregion
    }
}