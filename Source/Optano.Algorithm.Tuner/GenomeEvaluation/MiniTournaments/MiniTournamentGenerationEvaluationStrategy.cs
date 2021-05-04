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
    using System.Linq;

    using Akka.Util.Internal;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    using Priority_Queue;

    /// <summary>
    /// An evaluation strategy that sorts the genomes by performing a series of mini tournaments.
    /// Only the tournament winners are returned as crossover candidates.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class MiniTournamentGenerationEvaluationStrategy<TInstance, TResult> : IGenerationEvaluationStrategy<TInstance, TResult>
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
        private readonly List<ImmutableGenome> _genomes;

        /// <summary>
        /// The generation number.
        /// </summary>
        private readonly int _generation;

        /// <summary>
        /// A boolean indicating whether to use gray box tuning in current generation.
        /// </summary>
        private readonly bool _useGrayBoxInGeneration;

        /// <summary>
        /// The <see cref="MiniTournamentManager{TInstance,TResult}"/>s.
        /// </summary>
        private readonly List<MiniTournamentManager<TInstance, TResult>> _tournamentManagers;

        /// <summary>
        /// The evaluation priority queue.
        /// </summary>
        private readonly IPriorityQueue<GenomeTournamentKey, double> _priorityQueue;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniTournamentGenerationEvaluationStrategy{TInstance,TResult}"/> class.
        /// </summary>
        /// <param name="runEvaluator">The <see cref="IRunEvaluator{TInstance,TResult}"/> for sorting genomes.</param>
        /// <param name="genomes">The genomes for evaluation.</param>
        /// <param name="instances">The instances for evaluation.</param>
        /// <param name="generation">The generation number.</param>
        /// <param name="configuration">The algorithm tuner configuration.</param>
        /// <param name="useGrayBoxInGeneration">Boolean indicating whether to use gray box tuning in current generation.</param>
        public MiniTournamentGenerationEvaluationStrategy(
            IRunEvaluator<TInstance, TResult> runEvaluator,
            IEnumerable<ImmutableGenome> genomes,
            IEnumerable<TInstance> instances,
            int generation,
            AlgorithmTunerConfiguration configuration,
            bool useGrayBoxInGeneration)
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

            this._generation = generation;

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this._useGrayBoxInGeneration = useGrayBoxInGeneration;

            var nextTournamentId = (int)Math.Ceiling((configuration.PopulationSize / 2.0) / configuration.MaximumMiniTournamentSize)
                                   * generation;

            this._tournamentManagers
                = Randomizer.Instance.SplitIntoRandomBalancedSubsets(this._genomes, configuration.MaximumMiniTournamentSize)
                    .Select(
                        p => new MiniTournamentManager<TInstance, TResult>(
                            p,
                            instances,
                            nextTournamentId++,
                            this._generation,
                            this._runEvaluator,
                            configuration)).ToList();

            // The generation evaluation actor requeues all required evaluations.
            this._priorityQueue = new SimplePriorityQueue<GenomeTournamentKey, double>();
        }

        #endregion

        #region Public properties

        /// <inheritdoc />
        public bool IsGenerationFinished => this._tournamentManagers.All(tm => tm.IsTournamentFinished);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Instructs all tournament managers to <see cref="MiniTournamentManager{TInstance,TResult}.StartSynchronizingQueue"/>.
        /// </summary>
        public void BecomeWorking()
        {
            foreach (var tm in this._tournamentManagers)
            {
                tm.StartSynchronizingQueue(this._priorityQueue);
            }
        }

        /// <inheritdoc />
        public bool TryPopEvaluation(out GenomeInstancePairEvaluation<TInstance> nextEvaluation)
        {
            if (this._priorityQueue.Count <= 0)
            {
                nextEvaluation = null;
                return false;
            }

            var nextGenomeKey = this._priorityQueue.First;
            var associatedTournamentManager = this._tournamentManagers.FirstOrDefault(tm => tm.MiniTournamentId == nextGenomeKey.TournamentId);
            if (associatedTournamentManager == null)
            {
                var exception = new InvalidOperationException($"Cannot find a MiniTournamentManager with ID {nextGenomeKey.TournamentId}!");
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Error: {exception.Message}");
                throw exception;
            }

            if (!associatedTournamentManager.TryGetNextInstanceAndUpdateGenomePriority(nextGenomeKey, out var nextInstance))
            {
                nextEvaluation = null;
                return false;
            }

            var nextGenomeInstancePair = new GenomeInstancePair<TInstance>(nextGenomeKey.Genome, nextInstance);

            foreach (var manager in this._tournamentManagers.Where(tm => tm.MiniTournamentId != associatedTournamentManager.MiniTournamentId))
            {
                manager.NotifyEvaluationStarted(nextGenomeInstancePair);
            }

            nextEvaluation = new GenomeInstancePairEvaluation<TInstance>(
                nextGenomeInstancePair,
                this._generation,
                associatedTournamentManager.MiniTournamentId,
                this._useGrayBoxInGeneration);
            return true;
        }

        /// <inheritdoc />
        public void GenomeInstanceEvaluationFinished(GenomeInstancePair<TInstance> evaluation, TResult result)
        {
            foreach (var manager in this._tournamentManagers)
            {
                manager.UpdateResult(evaluation, result);
            }
        }

        /// <inheritdoc />
        public void RequeueEvaluation(GenomeInstancePair<TInstance> evaluation)
        {
            foreach (var manager in this._tournamentManagers)
            {
                manager.RequeueEvaluationIfRelevant(evaluation);
            }
        }

        /// <inheritdoc />
        public object CreateResultMessageForPopulationStrategy()
        {
            if (!this.IsGenerationFinished)
            {
                var exception = new InvalidOperationException(
                    $"You cannot create the gga result of the generation {this._generation}, before finishing it.");
                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Error: {exception.Message}");
                throw exception;
            }

            var genomeToTournamentRank = this._genomes.Distinct(ImmutableGenome.GenomeComparer)
                .ToDictionary(g => g, g => new List<GenomeTournamentRank>(), ImmutableGenome.GenomeComparer);

            // Get all tournament results and update genome to tournament rank dictionary.
            var tournamentResults = new List<MiniTournamentResult<TInstance, TResult>>();
            foreach (var manager in this._tournamentManagers)
            {
                var currentTournamentResult = manager.CreateMiniTournamentResult();
                currentTournamentResult.GenomeToTournamentRank.ForEach(genome => genomeToTournamentRank[genome.Key].AddRange(genome.Value));
                tournamentResults.Add(currentTournamentResult);
            }

            // Sort all tournament winners to get global winners.
            // Assumption: All mini tournaments of a specific generation were held on the same subset of instances.
            var orderedTournamentWinnerStats =
                this._runEvaluator.Sort(tournamentResults.SelectMany(tournamentResult => tournamentResult.WinnerStats)).ToList();
            var generationBest = orderedTournamentWinnerStats.First();

            var ggaResult = new GgaResult<TResult>(
                orderedTournamentWinnerStats.Select(x => x.Genome).ToList(),
                genomeToTournamentRank,
                generationBest.Genome,
                generationBest.FinishedInstances.Values);

            return ggaResult;
        }

        #endregion
    }
}