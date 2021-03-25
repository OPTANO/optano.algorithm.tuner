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
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Manages the evaluation progress of a single <see cref="ImmutableGenome"/>.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class GenomeStats<TInstance, TResult>
        where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// A lock object.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The open instances.
        /// </summary>
        private readonly HashSet<TInstance> _openInstances;

        /// <summary>
        /// The running instances.
        /// </summary>
        private readonly HashSet<TInstance> _runningInstances;

        /// <summary>
        /// The finished instances with results.
        /// </summary>
        private readonly Dictionary<TInstance, TResult> _finishedInstances;

        /// <summary>
        /// The instances that were cancelled by racing.
        /// </summary>
        private readonly HashSet<TInstance> _cancelledByRacingInstances;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeStats{TInstance,TResult}"/> class.
        /// </summary>
        /// <param name="genome">The genome.</param>
        /// <param name="openInstances">The instances that are initially in the open list.</param>
        /// <param name="runningInstances">
        /// The instances that are initialized in the running instances.
        /// Can be used for managing the <see cref="ResultStorageActor{TInstance,TResult}"/> update phase,
        /// by setting all instances as running and then updating all result hits, and requeueing all storage misses.</param>
        public GenomeStats(ImmutableGenome genome, IEnumerable<TInstance> openInstances, IEnumerable<TInstance> runningInstances)
        {
            this.Genome = genome;
            this._openInstances = new HashSet<TInstance>(openInstances);
            this._runningInstances = new HashSet<TInstance>(runningInstances);
            this._finishedInstances = new Dictionary<TInstance, TResult>();
            this._cancelledByRacingInstances = new HashSet<TInstance>();
            this.TotalInstanceCount = this._openInstances.Count
                                      + this._runningInstances.Count
                                      + this._finishedInstances.Count
                                      + this._cancelledByRacingInstances.Count;
            if (this.TotalInstanceCount == 0)
            {
                throw new ArgumentException($"You need to provide at least one instance in the GenomeStats constructor.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeStats{TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="otherStats">The genome stats to copy from.</param>
        internal GenomeStats(GenomeStats<TInstance, TResult> otherStats)
        {
            this.Genome = otherStats.Genome;
            this._openInstances = new HashSet<TInstance>(otherStats._openInstances);
            this._runningInstances = new HashSet<TInstance>(otherStats._runningInstances);
            this._finishedInstances = otherStats._finishedInstances.ToDictionary(i => i.Key, i => i.Value);
            this._cancelledByRacingInstances = new HashSet<TInstance>(otherStats._cancelledByRacingInstances);
            this.TotalInstanceCount = otherStats.TotalInstanceCount;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the Genome.
        /// </summary>
        public ImmutableGenome Genome { get; }

        /// <summary>
        /// Gets the total instance count.
        /// </summary>
        public int TotalInstanceCount { get; }

        /// <summary>
        /// Gets the open instances.
        /// </summary>
        public IEnumerable<TInstance> OpenInstances
        {
            get
            {
                lock (this._lock)
                {
                    return this._openInstances.ToList();
                }
            }
        }

        /// <summary>
        /// Gets the running instances.
        /// </summary>
        public IEnumerable<TInstance> RunningInstances
        {
            get
            {
                lock (this._lock)
                {
                    return this._runningInstances.ToList();
                }
            }
        }

        /// <summary>
        /// Gets the finished instances.
        /// </summary>
        public IReadOnlyDictionary<TInstance, TResult> FinishedInstances
        {
            get
            {
                lock (this._lock)
                {
                    return this._finishedInstances.ToImmutableDictionary();
                }
            }
        }

        /// <summary>
        /// Gets the instances that were cancelled by racing.
        /// </summary>
        public IEnumerable<TInstance> CancelledByRacingInstances
        {
            get
            {
                lock (this._lock)
                {
                    return this._cancelledByRacingInstances.ToList();
                }
            }
        }

        /// <summary>
        /// Gets the sum of the run times of the <see cref="FinishedInstances"/>.
        /// Note: Instances that are <see cref="CancelledByRacingInstances"/> are not included.
        /// </summary>
        public TimeSpan RuntimeOfFinishedInstances
        {
            get
            {
                lock (this._lock)
                {
                    return this._finishedInstances.Values.Sum(r => r.Runtime);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the genome stats do have <see cref="OpenInstances"/>.
        /// </summary>
        public bool HasOpenInstances
        {
            get
            {
                lock (this._lock)
                {
                    return this._openInstances.Any();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether there are any open or running instances.
        /// </summary>
        public bool HasOpenOrRunningInstances
        {
            get
            {
                lock (this._lock)
                {
                    return this.HasOpenInstances || this._runningInstances.Any();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this genome evaluation was cancelled by racing.
        /// </summary>
        public bool IsCancelledByRacing
        {
            get
            {
                lock (this._lock)
                {
                    return this._cancelledByRacingInstances.Any();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this genome is a candidate to use for killing other evaluations, when performing racing.
        /// </summary>
        public bool AllInstancesFinishedWithoutCancelledResult
        {
            get
            {
                lock (this._lock)
                {
                    return !this.HasOpenOrRunningInstances && !this.IsCancelledByRacing
                                                           && this._finishedInstances.Values.Count(result => result.IsCancelled) == 0;
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Tries to add the result for the given instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="result">The result.</param>
        /// <returns>
        /// <c>false</c>, if the instance was not contained in <see cref="RunningInstances"/>, or if it was contained in <see cref="OpenInstances"/>, or if we already recorded a result in <see cref="FinishedInstances"/>.
        /// </returns>
        public bool FinishInstance(TInstance instance, TResult result)
        {
            lock (this._lock)
            {
                if (this.IsCancelledByRacing)
                {
                    return true;
                }

                if (!this._runningInstances.Remove(instance) || this._openInstances.Contains(instance))
                {
                    return false;
                }

                return this._finishedInstances.TryAdd(instance, result);
            }
        }

        /// <summary>
        ///  Tries to requeue the instance.
        /// Only returns true, if an instance for which we did not already store a result (<see cref="FinishedInstances"/>) is moved from <see cref="RunningInstances"/> to <see cref="OpenInstances"/>.
        /// </summary>
        /// <param name="instance">The instance to requeue.</param>
        /// <returns><c>true</c>, if the instance was requeued.</returns>
        public bool RequeueInstance(TInstance instance)
        {
            lock (this._lock)
            {
                if (!this._runningInstances.Remove(instance) || this._openInstances.Contains(instance)
                                                             || this._finishedInstances.ContainsKey(instance))
                {
                    return false;
                }

                return this._openInstances.Add(instance);
            }
        }

        /// <summary>
        /// Computes and updates the <see cref="IsCancelledByRacing"/>.
        /// Moves all <see cref="RunningInstances"/> and <see cref="OpenInstances"/> to <see cref="CancelledByRacingInstances"/>.
        /// </summary>
        /// <returns><c>true</c>, if <see cref="IsCancelledByRacing"/> was set from <c>false</c> to <c>true</c>.</returns>
        public bool UpdateCancelledByRacing()
        {
            lock (this._lock)
            {
                if (this.IsCancelledByRacing || !this.HasOpenOrRunningInstances)
                {
                    return false;
                }

                foreach (var unfinishedInstance in this._openInstances.Concat(this._runningInstances))
                {
                    this._cancelledByRacingInstances.Add(unfinishedInstance);
                }

                LoggingHelper.WriteLine(
                    VerbosityLevel.Info,
                    $"The evaluation of {this._cancelledByRacingInstances.Count} instances (Open: {this._openInstances.Count}, Running: {this._runningInstances.Count}) of the following genome is stopped by racing.{Environment.NewLine}{this.Genome}");

                this._openInstances.Clear();
                this._runningInstances.Clear();

                return true;
            }
        }

        /// <summary>
        /// Returns an immutable version of the <see cref="GenomeStats{TInstance,TResult}"/>.
        /// </summary>
        /// <returns>The <see cref="ImmutableGenomeStats{TInstance,TResult}"/>.</returns>
        public ImmutableGenomeStats<TInstance, TResult> ToImmutable()
        {
            lock (this._lock)
            {
                return new ImmutableGenomeStats<TInstance, TResult>(this);
            }
        }

        /// <summary>
        /// Tries to pop the next instance from <see cref="OpenInstances"/>.
        /// </summary>
        /// <param name="nextInstance">The next instance that should be evaluated.</param>
        /// <returns><c>true</c>, if an instance was popped from <see cref="OpenInstances"/> and moved to <see cref="RunningInstances"/>.</returns>
        public bool TryStartInstance(out TInstance nextInstance)
        {
            lock (this._lock)
            {
                if (!this.HasOpenInstances)
                {
                    nextInstance = null;
                    return false;
                }

                nextInstance = this._openInstances.First();

                if (!this.StartInstance(nextInstance))
                {
                    nextInstance = null;
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Handles an instance that was started in another tournament for an equal genome.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public void NotifyInstanceStarted(TInstance instance)
        {
            this.StartInstance(instance);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Moves the given instance from open to running.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>True, if successful.</returns>
        private bool StartInstance(TInstance instance)
        {
            lock (this._lock)
            {
                if (!this._openInstances.Remove(instance) || this._finishedInstances.ContainsKey(instance))
                {
                    return false;
                }

                return this._runningInstances.Add(instance);
            }
        }

        #endregion
    }
}