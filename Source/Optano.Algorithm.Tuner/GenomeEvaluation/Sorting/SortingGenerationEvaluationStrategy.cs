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
    using System.Collections.Immutable;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// An evaluation strategy that evaluates all genomes on all instances and returns the global order, using an <see cref="IRunEvaluator{TInstance,TResult}"/>.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class SortingGenerationEvaluationStrategy<TInstance, TResult> : IGenerationEvaluationStrategy<TInstance, TResult>
        where TResult : ResultBase<TResult>, new()
        where TInstance : InstanceBase
    {
        #region Fields

        /// <summary>
        /// The <see cref="IRunEvaluator{TInstance,TResult}"/>.
        /// </summary>
        private readonly IRunEvaluator<TInstance, TResult> _runEvaluator;

        /// <summary>
        /// The genomes.
        /// </summary>
        private readonly IReadOnlyList<ImmutableGenome> _genomes;

        /// <summary>
        /// The genome to genome stats dictionary.
        /// </summary>
        private readonly Dictionary<ImmutableGenome, GenomeStats<TInstance, TResult>> _genomeToGenomeStats;

        /// <summary>
        /// A boolean indicating whether this <see cref="SortingGenerationEvaluationStrategy{TInstance,TResult}"/> has started working.
        /// </summary>
        private bool _hasStartedWorking;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortingGenerationEvaluationStrategy{TInstance,TResult}"/> class.
        /// </summary>
        /// <param name="runEvaluator">The <see cref="IRunEvaluator{TInstance,TResult}"/> for sorting genomes.</param>
        /// <param name="genomes">The genomes for evaluation.</param>
        /// <param name="instances">The instances for evaluation.</param>
        public SortingGenerationEvaluationStrategy(
            IRunEvaluator<TInstance, TResult> runEvaluator,
            IEnumerable<ImmutableGenome> genomes,
            IEnumerable<TInstance> instances)
        {
            this._runEvaluator = runEvaluator ?? throw new ArgumentNullException(nameof(runEvaluator));

            if (genomes == null)
            {
                throw new ArgumentNullException(nameof(genomes));
            }

            this._genomes = genomes.ToList();

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            this._genomeToGenomeStats = this._genomes.Distinct(ImmutableGenome.GenomeComparer).ToDictionary(
                g => g,
                g => new GenomeStats<TInstance, TResult>(g, Enumerable.Empty<TInstance>(), instances),
                ImmutableGenome.GenomeComparer);
        }

        #endregion

        #region Public properties

        /// <inheritdoc />
        public bool IsGenerationFinished => !this._genomeToGenomeStats.Values.Any(gs => gs.HasOpenOrRunningInstances);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Tries to pop the next evaluation to perform.
        /// Only returns evaluations, after <see cref="BecomeWorking"/> has been called.
        /// </summary>
        /// <param name="nextEvaluation">The next evaluation.</param>
        /// <returns><c>true</c>, if an evaluation has been popped.</returns>
        public bool TryPopEvaluation(out GenomeInstancePair<TInstance> nextEvaluation)
        {
            if (!this._hasStartedWorking)
            {
                nextEvaluation = null;
                return false;
            }

            if (this.IsGenerationFinished)
            {
                nextEvaluation = null;
                return false;
            }

            var nextGenomeStats = this._genomeToGenomeStats.Values.FirstOrDefault(gs => gs.HasOpenInstances);
            if (nextGenomeStats == null)
            {
                nextEvaluation = null;
                return false;
            }

            if (!nextGenomeStats.TryStartInstance(out var nextInstance))
            {
                throw new InvalidOperationException(
                    $"The GenomeStats for Genome {Environment.NewLine}{nextGenomeStats.Genome}{Environment.NewLine} reports that it has open instances, but fails to pop the next instance for evaluation.");
            }

            nextEvaluation = new GenomeInstancePair<TInstance>(nextGenomeStats.Genome, nextInstance);
            return true;
        }

        /// <inheritdoc />
        public void GenomeInstanceEvaluationFinished(GenomeInstancePair<TInstance> evaluation, TResult result)
        {
            this._genomeToGenomeStats[evaluation.Genome].FinishInstance(evaluation.Instance, result);
        }

        /// <inheritdoc />
        public void RequeueEvaluation(GenomeInstancePair<TInstance> evaluation)
        {
            this._genomeToGenomeStats[evaluation.Genome].RequeueInstance(evaluation.Instance);
        }

        /// <inheritdoc />
        public object CreateResultMessageForPopulationStrategy()
        {
            if (!this.IsGenerationFinished)
            {
                {
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Warn,
                        $"You cannot create the sort result of the current generation, before finishing it.");
                    throw new InvalidOperationException(
                        $"You cannot create the sort result of the current generation, before finishing it.");
                }
            }

            var expandedImmutableGenomeStats = this._genomes.Select(g => this._genomeToGenomeStats[g].ToImmutable());
            var orderedGenomes = this._runEvaluator.Sort(expandedImmutableGenomeStats).Select(gs => gs.Genome).ToImmutableList();
            var sortResult = new SortResult(orderedGenomes);
            return sortResult;
        }

        /// <summary>
        /// Before the working phase has started, no evaluations will be returned by <see cref="TryPopEvaluation"/>.
        /// </summary>
        public void BecomeWorking()
        {
            this._hasStartedWorking = true;
        }

        #endregion
    }
}