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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GrayBox.CsvRecorder;
    using Optano.Algorithm.Tuner.GrayBox.DataRecordTypes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    using SharpLearning.RandomForest.Models;

    /// <summary>
    /// Responsible for data recording and gray box cancellation during target algorithm evaluations.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class GrayBoxHandler<TInstance, TResult> : IDisposable
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// A lock object.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The <see cref="AlgorithmTunerConfiguration"/>.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _configuration;

        /// <summary>
        /// The <see cref="IGrayBoxTargetAlgorithm{TInstance, TResult}"/>.
        /// </summary>
        private readonly IGrayBoxTargetAlgorithm<TInstance, TResult> _grayBoxTargetAlgorithm;

        /// <summary>
        /// The <see cref="TunerDataRecord{TResult}"/>.
        /// </summary>
        private readonly TunerDataRecord<TResult> _tunerDataRecord;

        /// <summary>
        /// The list of <see cref="DataRecord{TResult}"/>s.
        /// </summary>
        private readonly List<DataRecord<TResult>> _listOfDataRecords;

        /// <summary>
        /// The actor ID.
        /// </summary>
        private readonly int _actorId;

        /// <summary>
        /// Boolean indicating whether to use gray box in current evaluation.
        /// </summary>
        private readonly bool _useGrayBoxInCurrentEvaluation = false;

        /// <summary>
        /// The <see cref="ICustomGrayBoxMethods{TResult}"/>.
        /// </summary>
        private readonly ICustomGrayBoxMethods<TResult> _customGrayBoxMethods;

        /// <summary>
        /// The gray box random forest.
        /// </summary>
        private readonly ClassificationForestModel _grayBoxRandomForest;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GrayBoxHandler{TInstance, TResult}" /> class.
        /// </summary>
        /// <param name="configuration">The <see cref="AlgorithmTunerConfiguration"/>.</param>
        /// <param name="grayBoxTargetAlgorithm">The gray box target algorithm.</param>
        /// <param name="actorId">The actor ID.</param>
        /// <param name="tunerDataRecord">The tuner data record.</param>
        /// <param name="useGrayBoxInCurrentEvaluation">Boolean indicating whether to use gray box tuning in current evaluation.</param>
        /// <param name="customGrayBoxMethods">The <see cref="ICustomGrayBoxMethods{TResult}"/>.</param>
        /// <param name="grayBoxRandomForest">The gray box random forest.</param>
        public GrayBoxHandler(
            AlgorithmTunerConfiguration configuration,
            IGrayBoxTargetAlgorithm<TInstance, TResult> grayBoxTargetAlgorithm,
            int actorId,
            TunerDataRecord<TResult> tunerDataRecord,
            bool useGrayBoxInCurrentEvaluation,
            ICustomGrayBoxMethods<TResult> customGrayBoxMethods,
            ClassificationForestModel grayBoxRandomForest)
        {
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._grayBoxTargetAlgorithm = grayBoxTargetAlgorithm ?? throw new ArgumentNullException(nameof(grayBoxTargetAlgorithm));
            this._actorId = actorId;
            this._tunerDataRecord = tunerDataRecord ?? throw new ArgumentNullException(nameof(tunerDataRecord));

            if (useGrayBoxInCurrentEvaluation)
            {
                this._customGrayBoxMethods = customGrayBoxMethods ?? throw new ArgumentNullException(nameof(customGrayBoxMethods));
                this._grayBoxRandomForest = grayBoxRandomForest ?? throw new ArgumentNullException(nameof(grayBoxRandomForest));
                this._useGrayBoxInCurrentEvaluation = true;
            }

            this._grayBoxTargetAlgorithm.OnNewDataRecord += this.HandleDataRecordUpdate;

            lock (this._lock)
            {
                this._listOfDataRecords = new List<DataRecord<TResult>>();
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Sets the final results of the list of current data records and writes this list to the corresponding data log file.
        /// </summary>
        /// <param name="finalResult">The final result.</param>
        public void WriteListOfDataRecordsToFile(TResult finalResult)
        {
            lock (this._lock)
            {
                this.AddFinalDataRecordIfMissing(finalResult);

                // Set final results.
                this._listOfDataRecords.ForEach(x => x.TunerDataRecord.FinalResult = finalResult);

                // Write data records to corresponding data log file.
                var dataLogFileName = GrayBoxUtils.GetDataLogFileName(
                    this._tunerDataRecord.GenerationId,
                    ProcessUtils.GetCurrentProcessId(),
                    this._actorId,
                    finalResult.TargetAlgorithmStatus);
                var dataRecorder =
                    new DataRecorder<TResult>(new FileInfo(Path.Combine(this._configuration.DataRecordDirectoryPath, dataLogFileName)));
                dataRecorder.WriteRows(this._listOfDataRecords);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this._grayBoxTargetAlgorithm.OnNewDataRecord -= this.HandleDataRecordUpdate;

            lock (this._lock)
            {
                this._listOfDataRecords.Clear();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the final data record to the list of data records, if missing.
        /// </summary>
        /// <param name="finalResult">The final result.</param>
        private void AddFinalDataRecordIfMissing(TResult finalResult)
        {
            var lastDataRecord = this._listOfDataRecords.Last();
            if (lastDataRecord.AdapterDataRecord.TargetAlgorithmStatus == TargetAlgorithmStatus.Running)
            {
                var newLastTunerDataRecord = lastDataRecord.TunerDataRecord.Copy();
                newLastTunerDataRecord.GrayBoxConfidence = double.NaN;

                var newLastAdapterDataRecord = new AdapterDataRecord<TResult>(
                    lastDataRecord.AdapterDataRecord.TargetAlgorithmName,
                    finalResult.TargetAlgorithmStatus,
                    lastDataRecord.AdapterDataRecord.ExpendedCpuTime,
                    lastDataRecord.AdapterDataRecord.ExpendedWallClockTime,
                    DateTime.Now,
                    lastDataRecord.AdapterDataRecord.AdapterFeaturesHeader,
                    lastDataRecord.AdapterDataRecord.AdapterFeatures,
                    finalResult);

                var newLastDataRecord = new DataRecord<TResult>(newLastTunerDataRecord, newLastAdapterDataRecord);
                this._listOfDataRecords.Add(newLastDataRecord);
            }
        }

        /// <summary>
        /// Handles a data record update by adding an element to the list of data records and checking for gray box cancellation, if desired.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="adapterDataRecord">The adapter data record.</param>
        private void HandleDataRecordUpdate(object sender, AdapterDataRecord<TResult> adapterDataRecord)
        {
            lock (this._lock)
            {
                var currentDataRecord = new DataRecord<TResult>(this._tunerDataRecord.Copy(), adapterDataRecord);

                if (this._useGrayBoxInCurrentEvaluation && currentDataRecord.AdapterDataRecord.TargetAlgorithmStatus
                                                        == TargetAlgorithmStatus.Running
                                                        && currentDataRecord.AdapterDataRecord.ExpendedWallClockTime
                                                        >= this._configuration.GrayBoxStartTimePoint)
                {
                    var grayBoxFeatures = this._customGrayBoxMethods.GetGrayBoxFeaturesFromDataRecord(currentDataRecord);
                    var grayBoxConfidence = this._grayBoxRandomForest.PredictProbability(grayBoxFeatures)
                        .Probabilities[GrayBoxUtils.GrayBoxLabelOfTimeouts];
                    currentDataRecord.TunerDataRecord.GrayBoxConfidence = grayBoxConfidence;

                    if (grayBoxConfidence >= this._configuration.GrayBoxConfidenceThreshold)
                    {
                        this._grayBoxTargetAlgorithm.CancelByGrayBox();
                        LoggingHelper.WriteLine(
                            VerbosityLevel.Info,
                            $"Cancelled the current evaluation by gray box tuning with confidence {grayBoxConfidence:0.######}!");
                    }
                }

                this._listOfDataRecords.Add(currentDataRecord);
            }
        }

        #endregion
    }
}