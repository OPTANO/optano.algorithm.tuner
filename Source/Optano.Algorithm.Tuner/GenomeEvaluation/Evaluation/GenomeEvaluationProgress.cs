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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation.Messages;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Progress of evaluating a single genome, i.e. the execution of a <see cref="GenomeEvaluation"/> message.
    /// </summary>
    /// <typeparam name="TInstance">
    /// Type of instance the target algorithm is able to process.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// Type of result of a target algorithm run.
    /// Must be a subtype of <see cref="ResultBase{TResultType}"/>.
    /// </typeparam>
    internal class GenomeEvaluationProgress<TInstance, TResult>
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// Instances the target algorithm still has to be run on.
        /// </summary>
        private readonly List<TInstance> _openInstances = new List<TInstance>();

        /// <summary>
        /// Run results for the evaluated genome.
        /// </summary>
        private readonly List<TResult> _currentRunResults = new List<TResult>();

        /// <summary>
        /// Stores the number of faulted evaluations for the genome, grouped by instance.
        /// </summary>
        private readonly Dictionary<TInstance, int> _faultedEvaluationsByInstance = new Dictionary<TInstance, int>();

        /// <summary>
        /// The evaluation identifier.
        /// </summary>
        private int _currentEvaluationId = -1;

        #endregion

        #region Public properties

        /// <summary>
        /// Gets a value indicating whether this instance has open evaluations.
        /// </summary>
        public bool HasOpenEvaluations => this._openInstances.Any();

        /// <summary>
        /// Gets the total run time up to now.
        /// </summary>
        public TimeSpan TotalRunTime => this._currentRunResults.Sum(result => result.Runtime);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// (Re-)Initializes the instance with the specified evaluation.
        /// </summary>
        /// <param name="evaluation">The evaluation message.</param>
        /// <param name="instancesForEvaluation">The instances for evaluation.</param>
        public void Initialize(GenomeEvaluation evaluation, List<TInstance> instancesForEvaluation)
        {
            this._currentEvaluationId = evaluation.EvaluationId;

            this._openInstances.Clear();
            this._openInstances.AddRange(instancesForEvaluation);
        }

        /// <summary>
        /// Pops the next open instance.
        /// </summary>
        /// <returns>The next open instance.</returns>
        public TInstance PopOpenInstance()
        {
            var currentInstance = this._openInstances[0];
            this._openInstances.RemoveAt(0);

            return currentInstance;
        }

        /// <summary>
        /// Adds a result.
        /// </summary>
        /// <param name="result">The result to add.</param>
        public void AddResult(TResult result)
        {
            this._currentRunResults.Add(result);
        }

        /// <summary>
        /// Adds information about a faulted evaluation.
        /// </summary>
        /// <param name="instance">The instance for which the evaluation failed.</param>
        /// <returns>The number of times the genome has failed on that instance in the current evaluation.</returns>
        public int AddFaultedEvaluation(TInstance instance)
        {
            this._faultedEvaluationsByInstance.TryGetValue(
                instance,
                out var numberFaultedEvaluationsForInstance);

            numberFaultedEvaluationsForInstance++;
            this._faultedEvaluationsByInstance[instance] = numberFaultedEvaluationsForInstance;

            return numberFaultedEvaluationsForInstance;
        }

        /// <summary>
        /// Creates all required messages to send the results of a <see cref="GenomeEvaluation"/>.
        /// </summary>
        /// <returns>The created messages.</returns>
        public IEnumerable<object> CreateEvaluationResultMessages()
        {
            return CompleteGenomeEvaluationResults.CreateEvaluationResultMessages(
                this._currentEvaluationId,
                this._currentRunResults);
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            this._currentEvaluationId = -1;
            this._currentRunResults.Clear();
            this._faultedEvaluationsByInstance.Clear();
        }

        #endregion
    }
}