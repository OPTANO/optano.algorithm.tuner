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

    using Optano.Algorithm.Tuner.MachineLearning.GenomeRepresentation;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    using TinyCsvParser.TypeConverter;

    /// <inheritdoc />
    public class TunerDataRecordCsvMapping<TTargetAlgorithm, TInstance, TResult> : IArrayTypeConverter<TunerDataRecord<TResult>>
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
        /// The genome header.
        /// </summary>
        private readonly string[] _genomeHeader;

        /// <summary>
        /// The number of result columns.
        /// </summary>
        private readonly int _numberOfResultColumns;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TunerDataRecordCsvMapping{TTargetAlgorithm, TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="targetAlgorithmFactory">The target algorithm factory.</param>
        /// <param name="genomeHeader">The genome header.</param>
        /// <param name="numberOfResultColumns">The number of result columns.</param>
        public TunerDataRecordCsvMapping(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            string[] genomeHeader,
            int numberOfResultColumns)
        {
            this._targetAlgorithmFactory = targetAlgorithmFactory;
            this._genomeHeader = genomeHeader;
            this._numberOfResultColumns = numberOfResultColumns;
        }

        #endregion

        #region Public properties

        /// <inheritdoc />
        public Type TargetType => typeof(TunerDataRecord<TResult>);

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public bool TryConvert(string[] value, out TunerDataRecord<TResult> result)
        {
            result = null;
            if (value.Length != TunerDataRecord<TResult>.OtherHeader.Length + this._genomeHeader.Length + this._numberOfResultColumns)
            {
                return false;
            }

            var nodeId = value[0];

            if (!int.TryParse(value[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var generationId))
            {
                return false;
            }

            if (!int.TryParse(value[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var tournamentId))
            {
                return false;
            }

            // Skip value[3] and value[5], since runID and configID are set automatically.
            var instanceId = value[4];

            if (!double.TryParse(value[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var grayBoxConfidence))
            {
                return false;
            }

            // Try to read genome double representation.
            GenomeDoubleRepresentation genome;
            try
            {
                var genomeDoubleValues = value[new Range(
                    TunerDataRecord<TResult>.OtherHeader.Length,
                    TunerDataRecord<TResult>.OtherHeader.Length + this._genomeHeader.Length)].Select(double.Parse).ToArray();
                genome = (GenomeDoubleRepresentation)genomeDoubleValues;
            }
            catch
            {
                return false;
            }

            // Try to read final result.
            var finalResultString = value[new Range(
                TunerDataRecord<TResult>.OtherHeader.Length + this._genomeHeader.Length,
                TunerDataRecord<TResult>.OtherHeader.Length + this._genomeHeader.Length + this._numberOfResultColumns)];

            if (!this._targetAlgorithmFactory.TryToGetResultFromStringArray(finalResultString, out var finalResult))
            {
                return false;
            }

            result = new TunerDataRecord<TResult>(
                nodeId,
                generationId,
                tournamentId,
                instanceId,
                grayBoxConfidence,
                this._genomeHeader,
                genome,
                finalResult);
            return true;
        }

        #endregion
    }
}