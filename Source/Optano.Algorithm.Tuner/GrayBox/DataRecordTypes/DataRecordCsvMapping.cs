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
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    using TinyCsvParser.Mapping;
    using TinyCsvParser.Ranges;

    /// <inheritdoc />
    public class DataRecordCsvMapping<TTargetAlgorithm, TInstance, TResult> : CsvMapping<DataRecord<TResult>>
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRecordCsvMapping{TTargetAlgorithm, TInstance, TResult}" /> class.
        /// </summary>
        /// <param name="targetAlgorithmFactory">The target algorithm factory.</param>
        /// <param name="genomeHeader">The genome header.</param>
        /// <param name="adapterFeaturesHeader">The adapter features header.</param>
        /// <param name="numberOfResultColumns">The number of result columns.</param>
        public DataRecordCsvMapping(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            string[] genomeHeader,
            string[] adapterFeaturesHeader,
            int numberOfResultColumns)
        {
            var lastTunerDataRecordColumn = TunerDataRecord<TResult>.OtherHeader.Length + genomeHeader.Length + numberOfResultColumns - 1;
            var lastAdapterDataRecordColumn = lastTunerDataRecordColumn + AdapterDataRecord<TResult>.OtherHeader.Length + adapterFeaturesHeader.Length
                                              + numberOfResultColumns;

            this.MapProperty(
                new RangeDefinition(0, lastTunerDataRecordColumn),
                x => x.TunerDataRecord,
                new TunerDataRecordCsvMapping<TTargetAlgorithm, TInstance, TResult>(
                    targetAlgorithmFactory,
                    genomeHeader,
                    numberOfResultColumns));
            this.MapProperty(
                new RangeDefinition(lastTunerDataRecordColumn + 1, lastAdapterDataRecordColumn),
                x => x.AdapterDataRecord,
                new AdapterDataRecordCsvMapping<TTargetAlgorithm, TInstance, TResult>(
                    targetAlgorithmFactory,
                    adapterFeaturesHeader,
                    numberOfResultColumns));
        }

        #endregion
    }
}