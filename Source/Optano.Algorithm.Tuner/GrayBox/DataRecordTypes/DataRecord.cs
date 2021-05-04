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
    using System.Linq;

    using Optano.Algorithm.Tuner.GrayBox.CsvRecorder;
    using Optano.Algorithm.Tuner.GrayBox.GrayBoxSimulation;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Contains the data, that is given to the <see cref="DataRecorder{TResult}"/>.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class DataRecord<TResult>
        where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRecord{TResult}"/> class.
        /// </summary>
        public DataRecord()
        {
            this.IsCancelledByGrayBoxDuringGrayBoxSimulation = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRecord{TResult}"/> class.
        /// </summary>
        /// <param name="tunerDataRecord">The tuner data record.</param>
        /// <param name="adapterDataRecord">The adapter data record.</param>
        public DataRecord(TunerDataRecord<TResult> tunerDataRecord, AdapterDataRecord<TResult> adapterDataRecord)
        {
            this.TunerDataRecord = tunerDataRecord;
            this.AdapterDataRecord = adapterDataRecord;
            this.IsCancelledByGrayBoxDuringGrayBoxSimulation = false;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets the adapter data.
        /// </summary>
        public AdapterDataRecord<TResult> AdapterDataRecord { get; set; }

        /// <summary>
        /// Gets or sets the tuner data.
        /// </summary>
        public TunerDataRecord<TResult> TunerDataRecord { get; set; }

        /// <summary>
        /// Gets the genome instance pair.
        /// </summary>
        public GenomeInstancePairStringRepresentation GenomeInstancePair =>
            new GenomeInstancePairStringRepresentation(this.TunerDataRecord.GenomeId, this.TunerDataRecord.InstanceId);

        /// <summary>
        /// Gets the gray box label.
        /// </summary>
        public int GrayBoxLabel => this.TunerDataRecord.FinalResult.IsCancelled
                                       ? GrayBoxUtils.GrayBoxLabelOfTimeouts
                                       : GrayBoxUtils.GrayBoxLabelOfNonTimeouts;

        /// <summary>
        /// Gets or sets a value indicating whether the current <see cref="DataRecord{TResult}"/> was cancelled by gray box during a <see cref="GrayBoxSimulation{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </summary>
        public bool IsCancelledByGrayBoxDuringGrayBoxSimulation { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns the header.
        /// </summary>
        /// <returns>The header.</returns>
        public string[] GetHeader()
        {
            return this.TunerDataRecord.GetHeader().Concat(this.AdapterDataRecord.GetHeader()).ToArray();
        }

        /// <summary>
        /// Returns the string array representation.
        /// </summary>
        /// <returns>The string array representation.</returns>
        public string[] ToStringArray()
        {
            return this.TunerDataRecord.ToStringArray().Concat(this.AdapterDataRecord.ToStringArray()).ToArray();
        }

        #endregion
    }
}