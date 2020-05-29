#region Copyright (c) OPTANO GmbH

// ////////////////////////////////////////////////////////////////////////////////
// 
//        OPTANO GmbH Source Code
//        Copyright (c) 2010-2020 OPTANO GmbH
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

namespace Optano.Algorithm.Tuner.Tracking
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Responsible for rewriting a log file after every generation.
    /// </summary>
    /// <typeparam name="TInstance">Instance type the target algorithm can deal with.
    /// Must extend <see cref="InstanceBase"/>.</typeparam>
    /// <typeparam name="TResult">Type of the results. Must extend <see cref="ResultBase{TResultType}"/>.</typeparam>
    public class LogWriter<TInstance, TResult>
        where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Constants

        /// <summary>
        /// Suffix added to the file name while writing is in progress.
        /// The file will lose the suffix once writing completes.
        /// </summary>
        public const string WorkInProgressSuffix = "_wip";

        #endregion

        #region Fields

        /// <summary>
        /// Time at which the program was started.
        /// </summary>
        private readonly DateTime _startTime;

        /// <summary>
        /// Structure representing the tunable parameters.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// The algorithm tuner configuration parameters.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _configuration;

        /// <summary>
        /// Offset for the <see cref="TotalElapsedTime"/>. Can be used when tuning is continued from existing status file.
        /// </summary>
        private TimeSpan _elapsedTimeFromPreviousTuningSession;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LogWriter{TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="parameterTree">
        /// The parameter tree used for tuning.
        /// </param>
        /// <param name="configuration">
        /// The algorithm tuner configuration parameters.
        /// </param>
        public LogWriter(ParameterTree parameterTree, AlgorithmTunerConfiguration configuration)
        {
            if (parameterTree == null)
            {
                throw new ArgumentNullException(nameof(parameterTree));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this._startTime = Process.GetCurrentProcess().StartTime.ToUniversalTime();
            this._parameterTree = parameterTree;
            this._configuration = configuration;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the elapsed time.
        /// </summary>
        public TimeSpan TotalElapsedTime => DateTime.Now.ToUniversalTime() - this._startTime + this._elapsedTimeFromPreviousTuningSession;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Logs a finished generation to file.
        /// </summary>
        /// <param name="numberFinishedGenerations">The number of finished generations so far.</param>
        /// <param name="totalEvaluationCount">The total number of evaluations so far.</param>
        /// <param name="fittestGenome">The fittest genome found in the generation.</param>
        /// <param name="results">All results of the fittest genome so far.</param>
        public void LogFinishedGeneration(
            int numberFinishedGenerations,
            int totalEvaluationCount,
            Genome fittestGenome,
            GenomeResults<TInstance, TResult> results)
        {
            if (totalEvaluationCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalEvaluationCount),
                    $"Evaluation count must be positive, but was {totalEvaluationCount}.");
            }

            if (fittestGenome == null)
            {
                throw new ArgumentNullException(nameof(fittestGenome));
            }

            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            // Write to file.
            var wipFilePath = this._configuration.LogFilePath + WorkInProgressSuffix;
            using (var file = new StreamWriter(wipFilePath))
            {
                file.WriteLine($"Finished generation {numberFinishedGenerations} / {this._configuration.Generations}");
                if (this._configuration.EvaluationLimit != int.MaxValue)
                {
                    file.WriteLine($"Evaluations: {totalEvaluationCount} / {this._configuration.EvaluationLimit}");
                }

                file.WriteLine($"Elapsed (d:hh:mm:ss): {this.TotalElapsedTime:G}");
                file.WriteLine($"Age of fittest genome: {fittestGenome.Age}");
                file.WriteLine("Fittest genome according to last tournament:");
                foreach (var gene in fittestGenome.GetFilteredGenes(this._parameterTree))
                {
                    file.WriteLine($"\t{gene.Key}: {gene.Value}");
                }

                file.WriteLine("Fittest genome's results on instances so far:");
                foreach (var result in results.RunResults.OrderBy(r => r.Key.ToString()))
                {
                    file.WriteLine($"\t{result.Key}:\t{result.Value}");
                }
            }

            // Replace last log file after writing has completed.
            if (File.Exists(this._configuration.LogFilePath))
            {
                File.Delete(this._configuration.LogFilePath);
            }

            File.Move(wipFilePath, this._configuration.LogFilePath);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Can be used to add an offset to the <see cref="TotalElapsedTime"/>.
        /// </summary>
        /// <param name="elapsedTimeFromPreviousSession">
        /// A non-negative offset. I.e. the elapsed time of a previous tuning session that is continued.
        /// </param>
        internal void SetElapsedTimeOffset(TimeSpan elapsedTimeFromPreviousSession)
        {
            if (elapsedTimeFromPreviousSession < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedTimeFromPreviousSession),
                    "Make sure to pass a non-negative offset as elapsed time from previous sessions.");
            }

            this._elapsedTimeFromPreviousTuningSession = elapsedTimeFromPreviousSession;
        }

        #endregion
    }
}