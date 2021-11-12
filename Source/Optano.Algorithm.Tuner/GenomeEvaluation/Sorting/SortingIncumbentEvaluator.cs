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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Sorting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Responsible for evaluating all incumbent genomes with <see cref="SortingGenerationEvaluationStrategy{TInstance,TResult}"/>.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class SortingIncumbentEvaluator<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// The generation evaluation actor.
        /// </summary>
        private readonly IActorRef _generationEvaluationActor;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortingIncumbentEvaluator{TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="generationEvaluationActor">The generation evaluation actor.</param>
        public SortingIncumbentEvaluator(
            IActorRef generationEvaluationActor)
        {
            this._generationEvaluationActor = generationEvaluationActor ?? throw new ArgumentNullException(nameof(generationEvaluationActor));
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Evaluates all incumbent genomes on the provided instances.
        /// </summary>
        /// <param name="allIncumbentGenomes">All incumbent genomes.</param>
        /// <param name="instances">The instances.</param>
        /// <returns>The <see cref="SortResult"/>.</returns>
        public SortResult EvaluateAllIncumbentGenomes(
            IEnumerable<ImmutableGenome> allIncumbentGenomes,
            IEnumerable<TInstance> instances)
        {
            if (allIncumbentGenomes == null)
            {
                throw new ArgumentNullException(nameof(allIncumbentGenomes));
            }

            if (!allIncumbentGenomes.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(allIncumbentGenomes));
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            if (!instances.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(instances));
            }

            // Set generation to -1 and disable gray box, since this is a "post tuning generation".
            var generationEvaluationMessage = new GenerationEvaluation<TInstance, TResult>(
                allIncumbentGenomes,
                instances,
                (runEvaluator, participantsOfGeneration, instancesOfGeneration) =>
                    new SortingGenerationEvaluationStrategy<TInstance, TResult>(
                        runEvaluator,
                        participantsOfGeneration,
                        instancesOfGeneration,
                        -1,
                        false));

            var generationEvaluationTask = this._generationEvaluationActor.Ask<SortResult>(generationEvaluationMessage).ContinueWith(
                task =>
                    {
                        if (task.IsFaulted)
                        {
                            throw new InvalidOperationException(
                                $"The evaluation of all incumbent genomes resulted in an exception!{Environment.NewLine}Message: {task.Exception?.Message}");
                        }

                        return task.Result;
                    });

            generationEvaluationTask.Wait();
            return generationEvaluationTask.Result;
        }

        #endregion
    }
}