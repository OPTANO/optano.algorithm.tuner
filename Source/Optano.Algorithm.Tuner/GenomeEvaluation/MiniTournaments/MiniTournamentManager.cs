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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    using Priority_Queue;

    /// <summary>
    /// Handles a single mini tournament. Thereby the mini tournament manager is responsible for queuing its genome instance evaluations, handling of racing and reporting its mini tournament result.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class MiniTournamentManager<TInstance, TResult>
        where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// The generation id.
        /// </summary>
        private readonly int _generationId;

        /// <summary>
        /// The <see cref="IRunEvaluator{TInstance,TResult}"/>.
        /// </summary>
        private readonly IRunEvaluator<TInstance, TResult> _runEvaluator;

        /// <summary>
        /// The algorithm tuner configuration.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _configuration;

        /// <summary>
        /// The desired number of winners.
        /// </summary>
        private readonly int _desiredNumberOfWinners;

        /// <summary>
        /// The genome to genome tournament key dictionary.
        /// </summary>
        private readonly Dictionary<ImmutableGenome, GenomeTournamentKey> _genomeToGenomeTournamentKey;

        /// <summary>
        /// The genome to genome stats dictionary.
        /// </summary>
        private readonly IReadOnlyDictionary<ImmutableGenome, GenomeStats<TInstance, TResult>> _genomeToGenomeStats;

        /// <summary>
        /// A lock object.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The global evaluation priority queue.
        /// </summary>
        private IPriorityQueue<GenomeTournamentKey, double> _globalPriorityQueue;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniTournamentManager{I, R}"/> class.
        /// </summary>
        /// <param name="participants">The tournament's participants.</param>
        /// <param name="instances">The tournament's instances.</param>
        /// <param name="miniTournamentId">The mini tournament id.</param>
        /// <param name="generationId">The generation id.</param>
        /// <param name="runEvaluator">The <see cref="IRunEvaluator{TInstance,TResult}"/>.</param>
        /// <param name="configuration">The algorithm tuner configuration.</param>
        public MiniTournamentManager(
            IEnumerable<ImmutableGenome> participants,
            IEnumerable<TInstance> instances,
            int miniTournamentId,
            int generationId,
            IRunEvaluator<TInstance, TResult> runEvaluator,
            AlgorithmTunerConfiguration configuration)
        {
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            this.Participants = participants.ToList();
            this.MiniTournamentId = miniTournamentId;
            this._generationId = generationId;

            this._runEvaluator = runEvaluator ?? throw new ArgumentNullException(nameof(runEvaluator));
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            this._desiredNumberOfWinners = (int)Math.Ceiling(this.Participants.Count() * this._configuration.TournamentWinnerPercentage);

            this._genomeToGenomeTournamentKey =
                this.Participants.Distinct(ImmutableGenome.GenomeComparer).ToDictionary(
                    genome => genome,
                    genome => new GenomeTournamentKey(genome, this.MiniTournamentId),
                    ImmutableGenome.GenomeComparer);

            this._genomeToGenomeStats = this._genomeToGenomeTournamentKey.Keys.ToDictionary(
                    genome => genome,
                    genome => new GenomeStats<TInstance, TResult>(genome, Enumerable.Empty<TInstance>(), instances),
                    ImmutableGenome.GenomeComparer)
                .ToImmutableDictionary(ImmutableGenome.GenomeComparer);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the tournament's participants.
        /// </summary>
        public IEnumerable<ImmutableGenome> Participants { get; }

        /// <summary>
        /// Gets the mini tournament id.
        /// </summary>
        public int MiniTournamentId { get; }

        /// <summary>
        /// Gets a value indicating whether the tournament is finished.
        /// </summary>
        public bool IsTournamentFinished
        {
            get
            {
                lock (this._lock)
                {
                    return !this._genomeToGenomeStats.Any(gs => gs.Value.HasOpenOrRunningInstances);
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Handles a result update by updating the corresponding genome stats and evaluation queue and checking for racing.
        /// </summary>
        /// <param name="evaluation">The evaluation.</param>
        /// <param name="result">The result.</param>
        public void UpdateResult(GenomeInstancePair<TInstance> evaluation, TResult result)
        {
            lock (this._lock)
            {
                if (!this._genomeToGenomeStats.TryGetValue(evaluation.Genome, out var genomeStats))
                {
                    return;
                }

                if (!genomeStats.FinishInstance(evaluation.Instance, result))
                {
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Warn,
                        $"Trying to finish instance {evaluation.Instance} with result {result}, but it was not in GenomeStats.RunningInstances.");
                }

                if (this._configuration.EnableRacing)
                {
                    var currentExpandedImmutableGenomeStats = this.GetExpandedImmutableGenomeStats().ToList();
                    var canBeCancelledByRacing = this._runEvaluator
                        .GetGenomesThatCanBeCancelledByRacing(currentExpandedImmutableGenomeStats, this._desiredNumberOfWinners)
                        .ToHashSet(ImmutableGenome.GenomeComparer);

                    if (canBeCancelledByRacing.Any())
                    {
                        var numberOfCancelledByRacing =
                            currentExpandedImmutableGenomeStats.Count(gs => gs.IsCancelledByRacing || canBeCancelledByRacing.Contains(gs.Genome));
                        if (numberOfCancelledByRacing > this.Participants.Count() - this._desiredNumberOfWinners)
                        {
                            throw new InvalidOperationException(
                                $"You cannot cancel more genomes by racing than number of participants - desired number of winners!{Environment.NewLine}Number of genomes cancelled by racing: {numberOfCancelledByRacing}{Environment.NewLine}Number of participants: {this.Participants.Count()}{Environment.NewLine}Desired number of winners: {this._desiredNumberOfWinners}");
                        }

                        foreach (var genome in canBeCancelledByRacing)
                        {
                            if (this._genomeToGenomeStats[genome].UpdateCancelledByRacing())
                            {
                                try
                                {
                                    if (this._globalPriorityQueue == null)
                                    {
                                        // We are in result storage update phase!
                                        continue;
                                    }

                                    this._globalPriorityQueue.Remove(this._genomeToGenomeTournamentKey[genome]);
                                }
                                catch (InvalidOperationException)
                                {
                                }
                            }
                        }
                    }
                }

                var genomeKey = this._genomeToGenomeTournamentKey[evaluation.Genome];
                this.RemoveOrUpdateInPriorityQueue(genomeKey);
            }
        }

        /// <summary>
        /// Starts the synchronization of the global evaluation queue.
        /// </summary>
        /// <param name="globalPriorityQueue">The global evaluation queue.</param>
        public void StartSynchronizingQueue(IPriorityQueue<GenomeTournamentKey, double> globalPriorityQueue)
        {
            lock (this._lock)
            {
                this._globalPriorityQueue = globalPriorityQueue;
                this.InitializeQueue();
            }
        }

        /// <summary>
        /// Tries to get the next instance of the given genome tournament key and updates the evaluation queue.
        /// </summary>
        /// <param name="nextGenomeKey">The next genome tournament key.</param>
        /// <param name="nextInstance">The next instance.</param>
        /// <returns>True, if there is a next instance.</returns>
        public bool TryGetNextInstanceAndUpdateGenomePriority(GenomeTournamentKey nextGenomeKey, out TInstance nextInstance)
        {
            lock (this._lock)
            {
                var genomeStats = this._genomeToGenomeStats[nextGenomeKey.Genome];
                var hasStartedInstance = genomeStats.TryStartInstance(out nextInstance);

                this.RemoveOrUpdateInPriorityQueue(nextGenomeKey);

                return hasStartedInstance;
            }
        }

        /// <summary>
        /// Requeues the given evaluation, if relevant for this mini tournament.
        /// </summary>
        /// <param name="evaluation">The evaluation.</param>
        public void RequeueEvaluationIfRelevant(GenomeInstancePair<TInstance> evaluation)
        {
            lock (this._lock)
            {
                if (!this._genomeToGenomeStats.TryGetValue(evaluation.Genome, out var genomeStats))
                {
                    return;
                }

                var genomeKey = new GenomeTournamentKey(evaluation.Genome, this.MiniTournamentId);
                var genomePriority = this._runEvaluator.ComputeEvaluationPriorityOfGenome(genomeStats.ToImmutable(), this._configuration.CpuTimeout);

                if (genomeStats.RequeueInstance(evaluation.Instance))
                {
                    if (this._globalPriorityQueue != null)
                    {
                        if (!this._globalPriorityQueue.Contains(genomeKey))
                        {
                            this._globalPriorityQueue.Enqueue(
                                genomeKey,
                                genomePriority);
                        }
                        else
                        {
                            this._globalPriorityQueue.UpdatePriority(genomeKey, genomePriority);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates the final mini tournament result.
        /// </summary>
        /// <returns>The mini tournament result.</returns>
        public MiniTournamentResult<TInstance, TResult> CreateMiniTournamentResult()
        {
            lock (this._lock)
            {
                if (!this.IsTournamentFinished)
                {
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Warn,
                        $"You cannot create the mini tournament result of the mini tournament {this.MiniTournamentId}, before finishing it.");
                    throw new InvalidOperationException(
                        $"You cannot create the mini tournament result of the mini tournament {this.MiniTournamentId}, before finishing it.");
                }

                var orderedGenomeStats = this._runEvaluator.Sort(this.GetExpandedImmutableGenomeStats()).ToList();

                var winnerStats = orderedGenomeStats.Take(this._desiredNumberOfWinners).ToList();

                var genomeToTournamentRank = orderedGenomeStats.Select((genomeStats, index) => new { genomeStats.Genome, Rank = index + 1 })
                    .GroupBy(x => x.Genome, ImmutableGenome.GenomeComparer)
                    .ToDictionary(
                        group => group.Key,
                        group => (IReadOnlyList<GenomeTournamentRank>)group.Select(
                            x => new GenomeTournamentRank()
                                     {
                                         TournamentRank = x.Rank,
                                         TournamentId = this.MiniTournamentId,
                                         GenerationId = this._generationId,
                                     }).ToList(),
                        ImmutableGenome.GenomeComparer);

                return new MiniTournamentResult<TInstance, TResult>(this.MiniTournamentId, winnerStats, genomeToTournamentRank);
            }
        }

        /// <summary>
        /// Handles the information that a evaluation has started by another mini tournament manager, if relevant.
        /// </summary>
        /// <param name="evaluation">The evaluation.</param>
        public void NotifyEvaluationStarted(GenomeInstancePair<TInstance> evaluation)
        {
            lock (this._lock)
            {
                if (!this._genomeToGenomeStats.TryGetValue(evaluation.Genome, out var genomeStats))
                {
                    return;
                }

                genomeStats.NotifyInstanceStarted(evaluation.Instance);
                this.RemoveOrUpdateInPriorityQueue(new GenomeTournamentKey(evaluation.Genome, this.MiniTournamentId));
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the expanded immutable version of the current genome stats that includes every genome as often as it participates in the current mini tournament.
        /// </summary>
        /// <returns>The expanded immutable genome stats.</returns>
        internal IEnumerable<ImmutableGenomeStats<TInstance, TResult>> GetExpandedImmutableGenomeStats()
        {
            return this.Participants.Select(genome => this._genomeToGenomeStats[genome].ToImmutable());
        }

        /// <summary>
        /// Initializes the evaluation queue.
        /// </summary>
        private void InitializeQueue()
        {
            foreach (var genomeTournament in this._genomeToGenomeTournamentKey)
            {
                var genomeStats = this._genomeToGenomeStats[genomeTournament.Key];
                if (!genomeStats.HasOpenInstances)
                {
                    continue;
                }

                this._globalPriorityQueue.Enqueue(
                    genomeTournament.Value,
                    this._runEvaluator
                        .ComputeEvaluationPriorityOfGenome(
                            this._genomeToGenomeStats[genomeTournament.Key].ToImmutable(),
                            this._configuration.CpuTimeout));
            }
        }

        /// <summary>
        /// Removes or updates the given genome tournament key in the evaluation queue.
        /// </summary>
        /// <param name="genomeTournamentKey">The genome tournament key.</param>
        private void RemoveOrUpdateInPriorityQueue(GenomeTournamentKey genomeTournamentKey)
        {
            if (this._globalPriorityQueue == null)
            {
                // We are in result storage update phase!
                return;
            }

            var genomeStats = this._genomeToGenomeStats[genomeTournamentKey.Genome];
            if (!genomeStats.HasOpenInstances)
            {
                if (this._globalPriorityQueue.Contains(genomeTournamentKey))
                {
                    this._globalPriorityQueue.Remove(genomeTournamentKey);
                }
            }
            else
            {
                var genomePriority = this._runEvaluator
                    .ComputeEvaluationPriorityOfGenome(
                        this._genomeToGenomeStats[genomeTournamentKey.Genome].ToImmutable(),
                        this._configuration.CpuTimeout);
                this._globalPriorityQueue.UpdatePriority(genomeTournamentKey, genomePriority);
            }
        }

        #endregion
    }
}