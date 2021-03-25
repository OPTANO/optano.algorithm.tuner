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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// Message containing all necessary information to start a generation evaluation.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class GenerationEvaluation<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationEvaluation{TInstance,TResult}"/> class.
        /// </summary>
        /// <param name="participants">
        /// Genomes to evaluate.
        /// </param>
        /// <param name="instances">
        /// Instances for evaluation.
        /// </param>
        /// <param name="evaluationStrategyFactory">The evaluation strategy factory.</param>
        public GenerationEvaluation(
            IEnumerable<ImmutableGenome> participants,
            IEnumerable<TInstance> instances,
            Func<IRunEvaluator<TInstance, TResult>, IEnumerable<ImmutableGenome>, IEnumerable<TInstance>,
                IGenerationEvaluationStrategy<TInstance, TResult>> evaluationStrategyFactory)
        {
            // Verify parameters.
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            // Copy the parameters over into fields to make the message immutable.
            this.Participants = participants.ToImmutableList();
            this.Instances = instances.ToImmutableList();

            this.EvaluationStrategyFactory = evaluationStrategyFactory ?? throw new ArgumentNullException(nameof(evaluationStrategyFactory));
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the evaluation strategy factory.
        /// </summary>
        public Func<IRunEvaluator<TInstance, TResult>, IEnumerable<ImmutableGenome>, IEnumerable<TInstance>,
            IGenerationEvaluationStrategy<TInstance, TResult>> EvaluationStrategyFactory { get; }

        /// <summary>
        /// Gets the genomes to evaluate.
        /// </summary>
        public ImmutableList<ImmutableGenome> Participants { get; }

        /// <summary>
        /// Gets the instances to evaluate the genomes with.
        /// </summary>
        public ImmutableList<TInstance> Instances { get; }

        #endregion
    }
}