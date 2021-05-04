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
    using System;
    using System.Collections.Immutable;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// The immutable version of <see cref="GenomeStats{TInstance,TResult}"/>.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class ImmutableGenomeStats<TInstance, TResult>
        where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// The underlying <see cref="GenomeStats{TInstance,TResult}"/>.
        /// </summary>
        private readonly GenomeStats<TInstance, TResult> _genomeStats;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableGenomeStats{TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="genomeStats">The underlying <see cref="GenomeStats{TInstance,TResult}"/>.</param>
        public ImmutableGenomeStats(GenomeStats<TInstance, TResult> genomeStats)
        {
            this._genomeStats = new GenomeStats<TInstance, TResult>(genomeStats);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome.
        /// </summary>
        public ImmutableGenome Genome => this._genomeStats.Genome;

        /// <summary>
        /// Gets the open instances.
        /// </summary>
        public ImmutableList<TInstance> OpenInstances => this._genomeStats.OpenInstances.ToImmutableList();

        /// <summary>
        /// Gets the running instances.
        /// </summary>
        public ImmutableList<TInstance> RunningInstances => this._genomeStats.RunningInstances.ToImmutableList();

        /// <summary>
        /// Gets the finished instances with results.
        /// </summary>
        public ImmutableDictionary<TInstance, TResult> FinishedInstances => this._genomeStats.FinishedInstances.ToImmutableDictionary();

        /// <summary>
        /// Gets the instances that were cancelled by racing.
        /// </summary>
        public ImmutableList<TInstance> CancelledByRacingInstances => this._genomeStats.CancelledByRacingInstances.ToImmutableList();

        /// <summary>
        /// Gets the total instance count.
        /// </summary>
        public int TotalInstanceCount => this._genomeStats.TotalInstanceCount;

        /// <summary>
        /// Gets a value indicating whether there are any open or running instances.
        /// </summary>
        public bool HasOpenOrRunningInstances => this._genomeStats.HasOpenOrRunningInstances;

        /// <summary>
        /// Gets the sum of the run times of the <see cref="FinishedInstances"/>.
        /// Note: Instances that are <see cref="CancelledByRacingInstances"/> are not included.
        /// </summary>
        public TimeSpan RuntimeOfFinishedInstances => this._genomeStats.RuntimeOfFinishedInstances;

        /// <summary>
        /// Gets a value indicating whether this genome evaluation was cancelled by racing.
        /// </summary>
        public bool IsCancelledByRacing => this._genomeStats.IsCancelledByRacing;

        /// <summary>
        /// Gets a value indicating whether this genome is a candidate to use for killing other evaluations, when performing racing.
        /// </summary>
        public bool AllInstancesFinishedWithoutCancelledResult => this._genomeStats.AllInstancesFinishedWithoutCancelledResult;

        #endregion
    }
}