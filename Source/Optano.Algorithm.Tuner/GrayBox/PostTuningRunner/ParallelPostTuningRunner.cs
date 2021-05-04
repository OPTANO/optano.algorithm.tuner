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

namespace Optano.Algorithm.Tuner.GrayBox.PostTuningRunner
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GrayBox.CsvRecorder;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Handles post tuning runs of the provided target algorithm for data recording purposes in parallel.
    /// </summary>
    /// <typeparam name="TGrayBoxTargetAlgorithm">The gray box target algorithm type.</typeparam>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class ParallelPostTuningRunner<TGrayBoxTargetAlgorithm, TInstance, TResult>
        where TGrayBoxTargetAlgorithm : IGrayBoxTargetAlgorithm<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// The <see cref="AlgorithmTunerConfiguration"/>.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _tunerConfiguration;

        /// <summary>
        /// The <see cref="PostTuningConfiguration"/>.
        /// </summary>
        private readonly PostTuningConfiguration _postTuningConfiguration;

        /// <summary>
        /// The <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </summary>
        private readonly ITargetAlgorithmFactory<TGrayBoxTargetAlgorithm, TInstance, TResult> _targetAlgorithmFactory;

        /// <summary>
        /// The <see cref="ParameterTree"/>.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// The post tuning file.
        /// </summary>
        private readonly FileInfo _postTuningFile;

        /// <summary>
        /// The list of <see cref="GenomeInstancePairStringRepresentation"/>s.
        /// </summary>
        private readonly List<GenomeInstancePairStringRepresentation> _listOfPostTuningGenomeInstancePairs;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelPostTuningRunner{TTargetAlgorithm,TInstance,TResult}"/> class.
        /// </summary>
        /// <param name="tunerConfiguration">The tuner configuration.</param>
        /// <param name="postTuningConfiguration">The post tuning configuration.</param>
        /// <param name="targetAlgorithmFactory">The target algorithm factory.</param>
        /// <param name="parameterTree">The parameter tree.</param>
        public ParallelPostTuningRunner(
            AlgorithmTunerConfiguration tunerConfiguration,
            PostTuningConfiguration postTuningConfiguration,
            ITargetAlgorithmFactory<TGrayBoxTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            ParameterTree parameterTree)
        {
            ProcessUtils.SetDefaultCultureInfo(CultureInfo.InvariantCulture);
            GrayBoxUtils.ValidatePostTuningConfiguration(postTuningConfiguration);
            GrayBoxUtils.ValidateAdditionalPostTuningParameters(tunerConfiguration, targetAlgorithmFactory, parameterTree);

            this._tunerConfiguration = tunerConfiguration;
            this._postTuningConfiguration = postTuningConfiguration;
            this._targetAlgorithmFactory = targetAlgorithmFactory;
            this._parameterTree = parameterTree;

            this._postTuningFile = new FileInfo(postTuningConfiguration.PathToPostTuningFile);
            if (!GrayBoxUtils.TryToReadGenomeInstancePairsFromFile(this._postTuningFile, out this._listOfPostTuningGenomeInstancePairs))
            {
                throw new ArgumentException($"Cannot read genome instance pairs from {this._postTuningFile.FullName}!");
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Starts the desired post tuning runs in parallel.
        /// </summary>
        public void ExecutePostTuningRunsInParallel()
        {
            var desiredPostTuningRuns = Enumerable.Range(
                this._postTuningConfiguration.IndexOfFirstPostTuningRun,
                this._postTuningConfiguration.NumberOfPostTuningRuns);

            var faultyRunIndices = new ConcurrentBag<int>();

            var settings = new ParallelOptions() { MaxDegreeOfParallelism = this._tunerConfiguration.MaximumNumberParallelEvaluations };
            Parallel.ForEach(
                desiredPostTuningRuns,
                settings,
                index =>
                    {
                        using var singlePostTuningRunner = new SinglePostTuningRunner<TGrayBoxTargetAlgorithm, TInstance, TResult>(
                            this._tunerConfiguration,
                            this._targetAlgorithmFactory,
                            this._parameterTree,
                            this._listOfPostTuningGenomeInstancePairs[index],
                            index);
                        try
                        {
                            singlePostTuningRunner.ExecuteSinglePostTuningRun();
                        }
                        catch (TaskCanceledException exception)
                        {
                            LoggingHelper.WriteLine(VerbosityLevel.Warn, $"The post tuning run #{index} ended in an exception: {exception.Message}");
                            faultyRunIndices.Add(index);
                        }
                    });

            if (faultyRunIndices.Any())
            {
                this.LogFaultyRuns(faultyRunIndices);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes the faulty runs to disk.
        /// </summary>
        /// <param name="faultyRunIndices">The faulty run indices.</param>
        private void LogFaultyRuns(ConcurrentBag<int> faultyRunIndices)
        {
            var file = new FileInfo(
                Path.Combine(this._postTuningFile.DirectoryName!, $"faultyPostTuningRuns_{ProcessUtils.GetCurrentProcessId()}.csv"));

            var recorder = new StringArrayRecorder(
                file,
                GenomeInstancePairStringRepresentation.GetHeader(),
                true);

            var faultyRuns = faultyRunIndices.Select(index => this._listOfPostTuningGenomeInstancePairs[index]);
            recorder.WriteRows(
                faultyRuns.Select(gip => gip.ToStringArray()).ToList());
        }

        #endregion
    }
}