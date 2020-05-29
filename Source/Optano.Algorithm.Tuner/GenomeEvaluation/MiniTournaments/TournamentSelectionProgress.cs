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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// Progress of a single tournament selection, i.e. the execution of a <see cref="SelectCommand{TInstance}"/> message.
    /// </summary>
    /// <typeparam name="TInstance">
    /// Type of instance the target algorithm is able to process.
    /// Must be a subtype of <see cref="InstanceBase" />.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// Type of result of a target algorithm run.
    /// Must be a subtype of <see cref="ResultBase{TResultType}" />.
    /// </typeparam>
    internal class TournamentSelectionProgress<TInstance, TResult>
        where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// The <see cref="GenomeToTournamentResults"/> lock.
        /// </summary>
        private readonly object _genomeToTournamentResultsLock = new object();

        /// <summary>
        /// Algorithm tuner configuration parameters.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _configuration;

        /// <summary>
        /// Mapping of actors to the mini tournament that has been assigned to them
        /// and hasn't been returned with a result yet.
        /// </summary>
        private readonly Dictionary<IActorRef, MiniTournament> _assignedMiniTournaments =
            new Dictionary<IActorRef, MiniTournament>();

        /// <summary>
        /// Winners of the <see cref="SelectCommand{TInstance}" />'s mini tournaments and the results
        /// they produced.
        /// </summary>
        private readonly List<MiniTournamentResult<TResult>> _miniTournamentResults = new List<MiniTournamentResult<TResult>>();

        /// <summary>
        /// The <see cref="SelectCommand{TInstance}" />.
        /// </summary>
        private SelectCommand<TInstance> _selectCommand;

        /// <summary>
        /// Mini tournaments that have to be held for the <see cref="SelectCommand{TInstance}" />
        /// and have not been sent to be processed yet.
        /// </summary>
        private List<MiniTournament> _openMiniTournaments;

        /// <summary>
        /// Current mini tournament id. Should only be incremented by <see cref="NextTournamentId"/>.
        /// </summary>
        private int _miniTournamentId = -1;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TournamentSelectionProgress{TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="configuration">Algorithm tuner configuration parameters.</param>
        public TournamentSelectionProgress(AlgorithmTunerConfiguration configuration)
        {
            this._configuration = configuration;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome to tournament results.
        /// </summary>
        public Dictionary<ImmutableGenome, List<GenomeTournamentResult>> GenomeToTournamentResults { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance has open mini tournaments.
        /// </summary>
        public bool HasOpenMiniTournaments => this._openMiniTournaments.Any();

        /// <summary>
        /// Gets a value indicating whether the selection is finished.
        /// </summary>
        public bool IsFinished => !this.HasOpenMiniTournaments && !this._assignedMiniTournaments.Any();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="command">The selection command.</param>
        public void Initialize(SelectCommand<TInstance> command)
        {
            this._selectCommand = command;
            this.GenomeToTournamentResults = new Dictionary<ImmutableGenome, List<GenomeTournamentResult>>(new ImmutableGenome.GeneValueComparer());

            // Balance selection into mini tournaments.
            this._openMiniTournaments = this.CreateMiniTournamentsOutOfSelectionCommand();
        }

        /// <summary>
        /// Creates all required messages to update evaluation instances.
        /// </summary>
        /// <returns>All required messages to update evaluation instances.</returns>
        public IEnumerable<object> CreateInstanceUpdateMessages()
        {
            return UpdateInstances.CreateInstanceUpdateMessages(this._selectCommand.Instances);
        }

        /// <summary>
        /// Pops the next open mini tournament.
        /// </summary>
        /// <returns>The next open mini tournament.</returns>
        public MiniTournament PopOpenMiniTournament()
        {
            var nextMiniTournament = this._openMiniTournaments[0];
            this._openMiniTournaments.RemoveAt(0);

            return nextMiniTournament;
        }

        /// <summary>
        /// Determines whether the specified actor has an assignment.
        /// </summary>
        /// <param name="assignee">The actor.</param>
        /// <returns>
        ///   <c>true</c> if the specified actor has an assignment; otherwise, <c>false</c>.
        /// </returns>
        public bool HasAssignment(IActorRef assignee)
        {
            return this._assignedMiniTournaments.ContainsKey(assignee);
        }

        /// <summary>
        /// Adds an assignment.
        /// </summary>
        /// <param name="assignee">The assignee.</param>
        /// <param name="miniTournament">The mini tournament.</param>
        public void AddAssignment(IActorRef assignee, MiniTournament miniTournament)
        {
            this._assignedMiniTournaments.Add(assignee, miniTournament);
        }

        /// <summary>
        /// Removes the current assignment.
        /// </summary>
        /// <param name="assignee">The assignee.</param>
        /// <returns>Whether there was an assignment to remove.</returns>
        public bool RemoveAssignment(IActorRef assignee)
        {
            return this._assignedMiniTournaments.Remove(assignee);
        }

        /// <summary>
        /// Adds a result.
        /// </summary>
        /// <param name="result">The result.</param>
        public void AddResult(MiniTournamentResult<TResult> result)
        {
            this.StoreTournamentResults(result);
            this._miniTournamentResults.Add(result);
        }

        /// <summary>
        /// Creates all required messages to send the results of a <see cref="SelectCommand{TInstance}"/>.
        /// </summary>
        /// <param name="runEvaluator">Object for evaluating target algorithm runs.</param>
        /// <returns>The created messages.</returns>
        public object CreateSelectionResultMessage(IRunEvaluator<TResult> runEvaluator)
        {
            // Sort all tournament winners to get global winners.
            // assumption: all mini tournaments of a specific generation were held on the same subset of instances.
            var tournamentWinnerResults = this._miniTournamentResults
                .SelectMany(msg => msg.WinnerResults)
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value as IEnumerable<TResult>);

            var orderedTournamentWinners = runEvaluator.Sort(tournamentWinnerResults).ToList();
            var generationBest = orderedTournamentWinners.First();

            return new SelectionResultMessage<TResult>(
                orderedTournamentWinners.ToImmutableList(),
                this.GenomeToTournamentResults,
                generationBest,
                tournamentWinnerResults[generationBest]);
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            this._selectCommand = null;
            this._assignedMiniTournaments.Clear();
            this._openMiniTournaments.Clear();
            this._miniTournamentResults.Clear();
        }

        /// <summary>
        /// Rolls back all assigned tournaments on the specified address.
        /// </summary>
        /// <param name="address">The address.</param>
        public void RollBackOnAddress(Address address)
        {
            IEnumerable<IActorRef> assigneesOnAddress = this._assignedMiniTournaments.Keys
                .Where(actor => actor.Path.Address == address)
                .ToList();

            foreach (var miniTournamentWorker in assigneesOnAddress)
            {
                var withdrawnTournament = this._assignedMiniTournaments[miniTournamentWorker];
                this._openMiniTournaments.Add(withdrawnTournament);
                this.RemoveAssignment(miniTournamentWorker);

                LoggingHelper.WriteLine(
                    VerbosityLevel.Warn,
                    $"Withdrew tournament with ID {withdrawnTournament.MiniTournamentId} from worker with Address {address}, because Akka marked the worker as unreachable.");
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the next tournament id and increments the <see cref="_miniTournamentId"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/> next tournament id.
        /// </returns>
        private int NextTournamentId()
        {
            return Interlocked.Increment(ref this._miniTournamentId);
        }

        /// <summary>
        /// Creates random disjunct mini tournaments covering all genomes that have been part of the
        /// <see cref="_selectCommand" />.
        /// The mini tournament size is at most <see cref="AlgorithmTunerConfiguration.MaximumMiniTournamentSize"/>.
        /// </summary>
        /// <returns>The mini tournaments.</returns>
        private List<MiniTournament> CreateMiniTournamentsOutOfSelectionCommand()
        {
            // Find number of mini tournaments that have to be held
            // s.t. no mini tournament exceeds the number of available cores per node.
            var maximumTournamentSize = this._configuration.MaximumMiniTournamentSize;
            var numberParticipants = this._selectCommand.Participants.Count;
            var numberTournaments = (int)Math.Ceiling((double)numberParticipants / maximumTournamentSize);

            // Balance the tournaments and find how many of them will have to be enlarged by one in order for all the
            // genomes to be mapped to a mini tournament.
            var balancedTournamentSize = numberParticipants / numberTournaments;
            var numberEnlargedTournaments = numberParticipants % numberTournaments;

            // Create list of mini tournaments:
            var miniTournaments = new List<MiniTournament>(numberTournaments);

            // Shuffle participants to get random subsets (for mini tournaments) when slicing.
            List<ImmutableGenome> participants =
                Randomizer.Instance.ChooseRandomSubset(this._selectCommand.Participants, numberParticipants).ToList();

            lock (this._genomeToTournamentResultsLock)
            {
                // For numberTournaments times, ...
                var nextFirstIndex = 0;

                for (var i = 0; i < numberTournaments; i++)
                {
                    // ...determine the tournament size...
                    var tournamentSize = i < numberEnlargedTournaments ? balancedTournamentSize + 1 : balancedTournamentSize;

                    // ...and take the next genomes from participants to create a mini tournament.
                    var tournament = new MiniTournament(participants.GetRange(nextFirstIndex, tournamentSize), this.NextTournamentId());
                    miniTournaments.Add(tournament);

                    // Finally update the index for the first genome in the next tournament.
                    nextFirstIndex += tournamentSize;
                }
            }

            // Return the created list.
            return miniTournaments;
        }

        /// <summary>
        /// Store tournament results.
        /// </summary>
        /// <param name="results">
        /// The results.
        /// </param>
        private void StoreTournamentResults(MiniTournamentResult<TResult> results)
        {
            lock (this._genomeToTournamentResultsLock)
            {
                for (var rank = 0; rank < results.AllFinishedOrdered.Count; rank++)
                {
                    var participant = results.AllFinishedOrdered[rank];

                    // update the rank
                    if (!this.GenomeToTournamentResults.TryGetValue(participant, out var allObservedParticipantResults))
                    {
                        allObservedParticipantResults = new List<GenomeTournamentResult>();
                        this.GenomeToTournamentResults[participant] = allObservedParticipantResults;
                    }

                    var result = new GenomeTournamentResult()
                                     {
                                         TournamentRank = rank + 1,
                                         TournamentId = results.MiniTournamentId,
                                         Generation = this._selectCommand.CurrentGeneration,
                                     };
                    allObservedParticipantResults.Add(result);
                }
            }
        }

        #endregion
    }
}