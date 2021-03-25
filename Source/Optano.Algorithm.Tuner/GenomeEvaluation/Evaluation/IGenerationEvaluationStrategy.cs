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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation
{
    using Optano.Algorithm.Tuner.GenomeEvaluation.Messages;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.Tuning;

    /// <summary>
    /// Interface for strategies that are used for evaluating a single generation during a run of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}"/>.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public interface IGenerationEvaluationStrategy<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Public properties

        /// <summary>
        /// Gets a value indicating whether the evaluation of the generation is finished.
        /// </summary>
        bool IsGenerationFinished { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Tries to pop the next evaluation (Genome x Instance) from a list (e.g. queue) of open evaluations.
        /// </summary>
        /// <param name="nextEvaluation">The next evaluation.</param>
        /// <returns><c>true</c>, if an open evaluation is popped.</returns>
        bool TryPopEvaluation(out GenomeInstancePair<TInstance> nextEvaluation);

        /// <summary>
        /// Updates the result for the given evaluation.
        /// </summary>
        /// <param name="evaluation">The evaluation.</param>
        /// <param name="result">The result.</param>
        void GenomeInstanceEvaluationFinished(GenomeInstancePair<TInstance> evaluation, TResult result);

        /// <summary>
        /// Requeues an evaluation, e.g. if it failed due to an actor crash.
        /// </summary>
        /// <param name="evaluation">The evaluation to requeue.</param>
        void RequeueEvaluation(GenomeInstancePair<TInstance> evaluation);

        /// <summary>
        /// Creates the result message that is sent as reply to the <see cref="GenerationEvaluation{TInstance,TResult}"/> back to the <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/>.
        /// The returned message type needs to match the \"expected\" result message when awaiting the answer from the <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>.
        /// </summary>
        /// <returns>The generation evaluation result message.</returns>
        object CreateResultMessageForPopulationStrategy();

        /// <summary>
        /// Tells the <see cref="IGenerationEvaluationStrategy{TInstance,TResult}"/> that the result fetching phase has ended.
        /// Can be used to indicate that, e.g. priority updates in the global priority queue, should now be performed when updating results or requeuing an evaluation.
        /// </summary>
        void BecomeWorking();

        #endregion
    }
}