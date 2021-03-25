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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.GeneticAlgorithm
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.Tracking;

    /// <summary>
    /// A gender-based genetic algorithm to update <see cref="Population"/> objects.
    /// <remarks>
    /// Based on
    /// <para>
    /// C. A. Gil, M. Sellmann, K. Tierney: "A Gender-Based Genetic Algorithm for the Automatic Configuration of Algorithms",
    /// Proceedings of the 15th intern. Conference on the Principles and Practice of Constraint Programming (CP-09), Springer LNCS 5732, pp. 142-157, 2009.
    /// </para>
    /// and
    /// <para>
    /// C. Ansótegui, Y. Malitsky, H.Samulowitz, M. Sellmann, K. Tierney: "Model-Based Genetic Algorithms for Algorithm Configuration",
    /// Proceedings of the Twenty-Fourth International Joint Conference on Artificial Intelligence (IJCAI 2015).
    /// </para>
    /// </remarks>
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    public class GgaStrategy<TInstance, TResult> : IPopulationUpdateStrategy<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// A number of options used for this instance.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _configuration;

        /// <summary>
        /// Structure representing the tunable parameters.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// The <see cref="GenomeBuilder" /> used in tuning.
        /// </summary>
        private readonly GenomeBuilder _genomeBuilder;

        /// <summary>
        /// An <see cref="IActorRef" /> to a <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}" /> which decides
        /// which competitive genomes are allowed to reproduce.
        /// </summary>
        private readonly IActorRef _tournamentSelector;

        /// <summary>
        /// Object which trains a model and enables model-based crossovers.
        /// </summary>
        private IGeneticEngineering _geneticEngineering;

        /// <summary>
        /// The current genome population.
        /// </summary>
        private Population _population;

        /// <summary>
        /// The current generation index.
        /// </summary>
        private int _currentGeneration;

        /// <summary>
        /// The number of times <see cref="PerformIteration(int, IEnumerable{TInstance})"/> has been
        /// called in this GGA phase.
        /// </summary>
        private int _iterationCounter;

        /// <summary>
        /// The number of successive generations in which the incumbent has not changed.
        /// </summary>
        private int _incumbentKeptCounter;

        /// <summary>
        /// Information about the genome that was identified as best in the most recent evaluation.
        /// </summary>
        private IncumbentGenomeWrapper<TResult> _mostRecentBest;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GgaStrategy{TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="configuration">Options used for this instance.</param>
        /// <param name="parameterTree">Provides the tuneable parameters.</param>
        /// <param name="genomeBuilder">Responsible for creation, modification and crossover of genomes.
        /// Needs to be compatible with the given parameter tree and configuration.</param>
        /// <param name="tournamentSelector">An <see cref="IActorRef" /> to a
        /// <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}" /> which decides which competitive
        /// genomes are allowed to reproduce.</param>
        /// <param name="geneticEngineering">Object which trains a model and enables model-based crossovers.</param>
        public GgaStrategy(
            AlgorithmTunerConfiguration configuration,
            ParameterTree parameterTree,
            GenomeBuilder genomeBuilder,
            IActorRef tournamentSelector,
            IGeneticEngineering geneticEngineering)
        {
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._parameterTree = parameterTree ?? throw new ArgumentNullException(nameof(parameterTree));
            this._genomeBuilder = genomeBuilder ?? throw new ArgumentNullException(nameof(genomeBuilder));
            this._tournamentSelector = tournamentSelector ?? throw new ArgumentNullException(nameof(tournamentSelector));
            this._geneticEngineering = geneticEngineering ?? throw new ArgumentNullException(nameof(geneticEngineering));

            this.AllKnownRanks = new Dictionary<Genome, List<GenomeTournamentRank>>(
                3 * this._configuration.PopulationSize,
                Genome.GenomeComparer);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets all observed tournament results.
        /// </summary>
        private Dictionary<Genome, List<GenomeTournamentRank>> AllKnownRanks { get; set; }

        /// <summary>
        /// Gets the path to use when working with <see cref="GgaStatus"/>.
        /// </summary>
        private string StatusFilePath => Path.Combine(this._configuration.StatusFileDirectory, GgaStatus.FileName);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Initializes a new phase for the strategy.
        /// </summary>
        /// <param name="basePopulation">Population to start with.</param>
        /// <param name="currentIncumbent">Most recent incumbent genome. Might be <c>null</c>.</param>
        /// <param name="instancesForEvaluation">Instances to use for evaluation.</param>
        public void Initialize(
            Population basePopulation,
            IncumbentGenomeWrapper<TResult> currentIncumbent,
            IEnumerable<TInstance> instancesForEvaluation)
        {
            this._population = basePopulation;
            this._iterationCounter = 0;
            this._incumbentKeptCounter = 0;
        }

        /// <summary>
        /// Updates the current population.
        /// </summary>
        /// <param name="currentGeneration">The current generation index.</param>
        /// <param name="instancesForEvaluation">Instances to use for evaluation.</param>
        public void PerformIteration(int currentGeneration, IEnumerable<TInstance> instancesForEvaluation)
        {
            this._iterationCounter++;
            this._currentGeneration = currentGeneration;

            var tournamentResults = this.PerformSelection(instancesForEvaluation);

            this.UpdateIncumbentKeptCounter(
                previousIncumbent: this._mostRecentBest?.IncumbentGenome,
                newIncumbent: tournamentResults.GenerationBest);
            this._mostRecentBest = new IncumbentGenomeWrapper<TResult>
                                       {
                                           IncumbentGeneration = currentGeneration,
                                           IncumbentGenome = tournamentResults.GenerationBest,
                                           IncumbentInstanceResults = tournamentResults.GenerationBestResult,
                                       };

            bool isLastGeneration = currentGeneration >= this._configuration.Generations - 1;
            if (!isLastGeneration)
            {
                // only perform expensive population update until penultimate generation.
                this.UpdateAllKnownRanks(tournamentResults);
                this.UpdatePopulation(tournamentResults.CompetitiveParents.ToList());
            }
        }

        /// <summary>
        /// Finds an incumbent genome.
        /// <para>
        /// In this class, we simply return <see cref="_mostRecentBest"/>.
        /// </para>
        /// </summary>
        /// <returns>The <see cref="_mostRecentBest"/> field.</returns>
        public IncumbentGenomeWrapper<TResult> FindIncumbentGenome()
        {
            return this._mostRecentBest;
        }

        /// <summary>
        /// Finishes a phase for the strategy.
        /// </summary>
        /// <param name="basePopulation">Population on which this phase was based.</param>
        /// <returns>The <see cref="Population"/> for the next strategy to work with.</returns>
        public Population FinishPhase(Population basePopulation)
        {
            return this._population;
        }

        /// <summary>
        /// Chooses the next population update strategy after this one finished.
        /// </summary>
        /// <param name="populationUpdateStrategies">Possible strategies.</param>
        /// <returns>Index of the chosen strategy.</returns>
        public int NextStrategy(List<IPopulationUpdateStrategy<TInstance, TResult>> populationUpdateStrategies)
        {
            Type nextStrategyType;
            switch (this._configuration.ContinuousOptimizationMethod)
            {
                case ContinuousOptimizationMethod.None:
                    nextStrategyType = typeof(GgaStrategy<TInstance, TResult>);
                    break;
                case ContinuousOptimizationMethod.Jade:
                    nextStrategyType = typeof(DifferentialEvolutionStrategy<TInstance, TResult>);
                    break;
                case ContinuousOptimizationMethod.CmaEs:
                    nextStrategyType =
                        CovarianceMatrixAdaptationInformationFlowSwitch.DetermineCovarianceMatrixAdaptationStrategyType<TInstance, TResult>(
                            this._configuration);
                    break;
                default:
                    throw new NotImplementedException(
                        $"{this._configuration.ContinuousOptimizationMethod} is not mapped to a type in GGA strategy.");
            }

            return populationUpdateStrategies.FindIndex(strategy => strategy.GetType() == nextStrategyType);
        }

        /// <summary>
        /// Returns a value indicating whether the current instantiation of the strategy has terminated.
        /// </summary>
        /// <returns>Whether the current instantiation of the strategy has terminated.</returns>
        public bool HasTerminated()
        {
            bool stagnated = this._incumbentKeptCounter >= this._configuration.MaximumNumberGgaGenerationsWithSameIncumbent;
            if (stagnated)
            {
                LoggingHelper.WriteLine(VerbosityLevel.Info, "GGA: Termination criterion met.");
                LoggingHelper.WriteLine(VerbosityLevel.Debug, "Incumbent kept.");
                return true;
            }

            bool terminated = this._iterationCounter >= this._configuration.MaximumNumberGgaGenerations;
            if (terminated)
            {
                LoggingHelper.WriteLine(VerbosityLevel.Info, "GGA: Termination criterion met.");
                LoggingHelper.WriteLine(VerbosityLevel.Debug, "MaxGenerations");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Logs information about the current population to console.
        /// </summary>
        public void LogPopulationToConsole()
        {
            LoggingHelper.WriteLine(VerbosityLevel.Debug, "Current population:");
            LoggingHelper.WriteLine(
                VerbosityLevel.Debug,
                $"Competitive genomes:\n {string.Join("\n ", this._population.GetCompetitiveIndividuals().Select(genome => genome.ToFilteredGeneString(this._parameterTree)))}");
            LoggingHelper.WriteLine(
                VerbosityLevel.Debug,
                $"Noncompetitive genomes:\n {string.Join("\n ", this._population.GetNonCompetitiveMates().Select(genome => genome.ToFilteredGeneString(this._parameterTree)))}");
        }

        /// <summary>
        /// Exports the standard deviations of the numerical features of the current population's competitive part via
        /// <see cref="RunStatisticTracker.ComputeAndExportNumericalFeatureCoefficientOfVariation"/>.
        /// </summary>
        public void ExportFeatureStandardDeviations()
        {
            RunStatisticTracker.ComputeAndExportNumericalFeatureCoefficientOfVariation(
                this._parameterTree,
                this._population.GetCompetitiveIndividuals(),
                this._currentGeneration);
        }

        /// <summary>
        /// Writes all internal data to file.
        /// </summary>
        public void DumpStatus()
        {
            var status = new GgaStatus(
                this._population,
                this._iterationCounter,
                this._incumbentKeptCounter,
                this.AllKnownRanks);
            status.WriteToFile(this.StatusFilePath);
        }

        /// <summary>
        /// Reads all internal data from file.
        /// </summary>
        /// <param name="evaluationModel">Reference to up-to-date evaluation model.</param>
        public void UseStatusDump(IGeneticEngineering evaluationModel)
        {
            // Update evaluation model.
            this._geneticEngineering = evaluationModel;

            // Read status from file.
            var status = StatusBase.ReadFromFile<GgaStatus>(this.StatusFilePath);
            this._population = status.Population;
            this._iterationCounter = status.IterationCounter;
            this._incumbentKeptCounter = status.IncumbentKeptCounter;
            this.AllKnownRanks = status.AllKnownRanks;

            // somehow, the equality comparer is not restored properly.
            // fix this.
            var restoredRanks = this.AllKnownRanks.GroupBy(kr => kr.Key, Genome.GenomeComparer).ToDictionary(
                grp => grp.Key,
                grp => grp.SelectMany(ranks => ranks.Value).ToList(),
                Genome.GenomeComparer);
            this.AllKnownRanks = restoredRanks;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates <see cref="_incumbentKeptCounter"/>.
        /// </summary>
        /// <param name="previousIncumbent">The previous incumbent.</param>
        /// <param name="newIncumbent">The new incumbent.</param>
        private void UpdateIncumbentKeptCounter(Genome previousIncumbent, Genome newIncumbent)
        {
            if (Genome.GenomeComparer.Equals(previousIncumbent, newIncumbent))
            {
                this._incumbentKeptCounter++;
            }
            else
            {
                this._incumbentKeptCounter = 0;
            }
        }

        /// <summary>
        /// Update all known ranks.
        /// </summary>
        /// <param name="tournamentResults">
        /// The tournament results.
        /// </param>
        /// <exception cref="Exception">
        /// We expect one result for each competitive genome.
        /// </exception>
        private void UpdateAllKnownRanks(TournamentWinnersWithRank<TResult> tournamentResults)
        {
            var totalResultCount = tournamentResults.GenomeToTournamentRank.Sum(gtr => gtr.Value.Count);
            if (totalResultCount != this._population.CompetitiveCount)
            {
                throw new ArgumentException(
                    $"We expect {this._population.CompetitiveCount} individual tournament results. Only found {totalResultCount}.",
                    nameof(tournamentResults));
            }

            foreach (var genome in tournamentResults.GenomeToTournamentRank.Keys)
            {
                if (!this.AllKnownRanks.ContainsKey(genome))
                {
                    this.AllKnownRanks.Add(genome, new List<GenomeTournamentRank>());
                }

                this.AllKnownRanks[genome].AddRange(tournamentResults.GenomeToTournamentRank[genome]);
            }
        }

        /// <summary>
        /// Selects those genomes from the competitive part of the population that are allowed to reproduce.
        /// </summary>
        /// <param name="instancesForEvaluation">
        /// The <typeparamref name="TInstance"/>s to use for evaluation.
        /// </param>
        /// <returns>
        /// Competitive genomes allowed to reproduce.
        /// </returns>
        private TournamentWinnersWithRank<TResult> PerformSelection(IEnumerable<TInstance> instancesForEvaluation)
        {
            var participants = this._population.GetCompetitiveIndividuals().Select(genome => new ImmutableGenome(genome)).ToList();
            var generationEvaluationMessage = new GenerationEvaluation<TInstance, TResult>(
                participants,
                instancesForEvaluation,
                (runEvaluator, participantsOfGeneration, instancesOfGeneration) => new MiniTournamentGenerationEvaluationStrategy<TInstance, TResult>(
                    runEvaluator,
                    participantsOfGeneration,
                    instancesOfGeneration,
                    this._configuration,
                    this._currentGeneration));

            var generationEvaluationTask = this._tournamentSelector.Ask<GgaResult<TResult>>(generationEvaluationMessage).ContinueWith(
                tr =>
                    {
                        if (tr.IsFaulted)
                        {
                            // It was impossible to determine the best genomes, i.e. something really bad happened.
                            // In this case, we throw an exception for the caller to handle.
                            throw new InvalidOperationException(
                                $"The generation evaluation with GGA in generation {this._currentGeneration} resulted in an exception!");
                        }

                        var result = new TournamentWinnersWithRank<TResult>(
                            tr.Result.CompetitiveParents,
                            tr.Result.GenerationBest,
                            tr.Result.GenerationBestResult,
                            tr.Result.GenomeToTournamentRank);
                        return result;
                    });

            generationEvaluationTask.Wait();
            return generationEvaluationTask.Result;
        }

        /// <summary>
        /// Generates natural and engineered offspring,
        /// updates historical genomes data,
        /// adds offspring to population,
        /// ages generated offspring,
        /// and inserts mutants.
        /// </summary>
        /// <param name="competitiveParents">
        /// The current tournament winners.
        /// </param>
        private void UpdatePopulation(List<Genome> competitiveParents)
        {
            // Transition to new population:
            var numberOfDyingCompetitiveIndividuals = this.CountDyingCompetitiveGenomes();
            var numberOfDyingNonCompetitiveIndividuals = this.CountDyingNonCompetitiveGenomes();
            var totalDying = numberOfDyingCompetitiveIndividuals + numberOfDyingNonCompetitiveIndividuals;

            var generatedAndMutatedOffspring = this.GenerateAndMutateAllOffspring(competitiveParents, totalDying);

            this.AddToPopulation(generatedAndMutatedOffspring, numberOfDyingCompetitiveIndividuals, numberOfDyingNonCompetitiveIndividuals);

            this.AgePopulationAndKeepIncumbentAlive();

            // Replace a part of the non-competitive population with mutants
            if (generatedAndMutatedOffspring.Any(o => o.IsEngineered))
            {
                this._population.ReplaceIndividualsWithMutants(this._genomeBuilder);
            }
        }

        /// <summary>
        /// Counts the number of competitive individuals that have reached their
        /// maximum age and will die at the end of the generation.
        /// If the <see cref="IncumbentGenomeWrapper{TResult}.IncumbentGenome"/> has exceeded the <see cref="AlgorithmTunerConfiguration.MaxGenomeAge"/>,
        /// the number will be reduced by 1, as the <see cref="IncumbentGenomeWrapper{TResult}.IncumbentGenome"/> musn't die.
        /// <c>Make sure to exclude it from the set of dying genomes in <see cref="Population.Age"/>!</c>.
        /// </summary>
        /// <returns>The number of competitive individuals that will die at the end of the generation.</returns>
        private int CountDyingCompetitiveGenomes()
        {
            var eliticismDiscount = this.KeepIncumbentArtificiallyAlive() ? 1 : 0;

            return this._population.GetCompetitiveIndividuals().Count(genome => genome.Age >= this._configuration.MaxGenomeAge) - eliticismDiscount;
        }

        /// <summary>
        /// Check if the incumbent needs to be kept alive artificially.
        /// </summary>
        /// <returns>
        /// True, if the <see cref="IncumbentGenomeWrapper{TResult}.IncumbentGenome"/> exceeded <see cref="AlgorithmTunerConfiguration.MaxGenomeAge"/>.
        /// </returns>
        private bool KeepIncumbentArtificiallyAlive()
        {
            return this._mostRecentBest.IncumbentGenome.Age >= this._configuration.MaxGenomeAge;
        }

        /// <summary>
        /// Counts the number of non-competitive individuals that have reached their
        /// maximum age and will die at the end of the generation.
        /// </summary>
        /// <returns>The number of non-competitive individuals that will die at the end of the generation.</returns>
        private int CountDyingNonCompetitiveGenomes()
        {
            return this._population.GetNonCompetitiveMates().Count(genome => genome.Age >= this._configuration.MaxGenomeAge);
        }

        /// <summary>
        /// Generate and mutate all offspring.
        /// I.e.: Determine required number of genomes,
        /// perform natural crossover,
        /// perform model-based crossover,
        /// mutate genomes.
        /// </summary>
        /// <param name="competitiveParents">
        /// The competitive parents.
        /// </param>
        /// <param name="totalDying">
        /// The total number of dying genomes.
        /// </param>
        /// <returns>
        /// The <see cref="List{Genome}"/> with the new offspring.
        /// </returns>
        private List<Genome> GenerateAndMutateAllOffspring(List<Genome> competitiveParents, int totalDying)
        {
            // only begin genetic engineering when specified.
            var naturallyReproducedNumber = this.ComputeNumberOfNaturallyReproducedGenomes(totalDying);
            var engineeredGenomeNumber = totalDying - naturallyReproducedNumber;

            // Perform crossovers.
            var naturalOffspring = this.PerformCrossovers(competitiveParents, naturallyReproducedNumber);
            var engineeredOffspring = this.PerformGeneticEngineering(engineeredGenomeNumber, competitiveParents);

            // Perform mutation on new offspring.
            // join both offspring lists
            var offspring = naturalOffspring.Concat(engineeredOffspring).ToList();
            this.PerformMutation(offspring);
            return offspring;
        }

        /// <summary>
        /// Compute the number of naturally reproduced genomes.
        /// </summary>
        /// <param name="totalDying">
        /// Number of dying genomes.
        /// </param>
        /// <returns>
        /// The <see cref="int"/> number of genomes to reproduce with natural crossover.
        /// </returns>
        private int ComputeNumberOfNaturallyReproducedGenomes(int totalDying)
        {
            var effectiveEngineeredPopProportion = this._configuration.StartEngineeringAtIteration <= this._currentGeneration
                                                       ? this._configuration.EngineeredPopulationRatio
                                                       : 0d;

            // if engineered ratio > 0, produce at least 1 engineered genome
            var naturallyReproducedNumber = (int)Math.Floor((1 - effectiveEngineeredPopProportion) * totalDying);
            return naturallyReproducedNumber;
        }

        /// <summary>
        /// Perfoms crossovers between genomes of different genders.
        /// </summary>
        /// <param name="competitiveParents">
        /// Competitive genomes allowed to reproduce.
        /// </param>
        /// <param name="number">
        /// Number of crossovers to perform.
        /// </param>
        /// <returns>
        /// The offspring.
        /// </returns>
        private IEnumerable<Genome> PerformCrossovers(List<Genome> competitiveParents, int number)
        {
            if (number == 0)
            {
                yield break;
            }

            // make sure that we have enough competitive parents
            var chosenCompetitiveParents = competitiveParents.InflateAndShuffle(number);

            // Randomly select correct number of non-competitive genomes.
            var nonCompetitiveMates = this._population.GetNonCompetitiveMates();

            // needed for roulette wheel selection
            var attractiveness = this._configuration.EnableSexualSelection
                                     ? this._geneticEngineering.GetAttractivenessMeasure(nonCompetitiveMates)
                                     : Enumerable.Repeat(1d, nonCompetitiveMates.Count).ToArray();

            // For each of them:
            foreach (var comp in chosenCompetitiveParents)
            {
                // Randomly select a non-competitive parent allowed to reproduce and do a crossover.
                var nonCompIndex = Randomizer.Instance.RouletteSelect(attractiveness, true);
                var nonComp = nonCompetitiveMates[nonCompIndex];
                yield return this._genomeBuilder.Crossover(comp, nonComp);
            }
        }

        /// <summary>
        /// Performs the genetic engineering.
        /// I.e.: Train the forest,
        /// Engineer requested number of offspring.
        /// </summary>
        /// <param name="engineeredGenomeNumber">
        /// The required number of engineered genomes.
        /// </param>
        /// <param name="competitiveParents">
        /// The competitive parents.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{Genome}"/> containing the engineered genomes.
        /// </returns>
        private IEnumerable<Genome> PerformGeneticEngineering(int engineeredGenomeNumber, List<Genome> competitiveParents)
        {
            // always train, if we may need engineered genomes later on
            // also required, if sexual selection is enabled
            if (this._configuration.TrainModel || this._configuration.EngineeredPopulationRatio > 0 || this._configuration.EnableSexualSelection)
            {
                var trainSet = new TrainingDataWrapper(this.AllKnownRanks, this._currentGeneration);
                this._geneticEngineering.TrainForest(trainSet);
            }

            // Perform engineering, if required
            IEnumerable<Genome> engineeredOffspring;
            if (engineeredGenomeNumber > 0)
            {
                var chosenCompetitiveParents = competitiveParents.InflateAndShuffle(engineeredGenomeNumber);
                engineeredOffspring = this._geneticEngineering.EngineerGenomes(
                    chosenCompetitiveParents,
                    this._population.GetNonCompetitiveMates(),
                    this._population.AllGenomes);
            }
            else
            {
                // just an empty dummy-enumerable
                engineeredOffspring = new Genome[0];
            }

            return engineeredOffspring;
        }

        /// <summary>
        /// Mutates each genome.
        /// </summary>
        /// <param name="children">
        /// Genomes to mutate. Will be modified.
        /// </param>
        private void PerformMutation(IList<Genome> children)
        {
            foreach (var child in children)
            {
                this._genomeBuilder.Mutate(child);
            }
        }

        /// <summary>
        /// Adds the genomes to the current population.
        /// </summary>
        /// <param name="genomes">
        /// The genomes to add to the population.
        /// </param>
        /// <param name="competitive">
        /// The number of genomes to add to the competitive population.
        /// </param>
        /// <param name="nonCompetitive">
        /// The number of genomes to add to the non competitive population.
        /// </param>
        private void AddToPopulation(IEnumerable<Genome> genomes, int competitive, int nonCompetitive)
        {
            // Count how many genomes where added to each gender to prevent assigning too many to one of them.
            var competitiveGenomesAdded = 0;
            var nonCompetitiveGenomesAdded = 0;

            // For each genome:
            foreach (var genome in genomes)
            {
                // Decide to which gender it should belong by first checking if one gender already got enough children
                // and randomizing the decision if that isn't the case.
                var enoughNonCompetitiveGenomesAdded = nonCompetitiveGenomesAdded >= nonCompetitive;
                var enoughCompetitiveGenomesAdded = competitiveGenomesAdded >= competitive;
                var addChildAsCompetitive = enoughNonCompetitiveGenomesAdded || (!enoughCompetitiveGenomesAdded && Randomizer.Instance.Decide());

                // Add to population.
                this._population.AddGenome(genome, addChildAsCompetitive);

                // Update counters.
                if (addChildAsCompetitive)
                {
                    competitiveGenomesAdded++;
                }
                else
                {
                    nonCompetitiveGenomesAdded++;
                }
            }
        }

        /// <summary>
        /// Age the population and keep incumbent alive.
        /// </summary>
        private void AgePopulationAndKeepIncumbentAlive()
        {
            var initialIncumbentAge = this._mostRecentBest.IncumbentGenome.Age;

            // important: call before Age()
            var keepArtificiallyAlive = this.KeepIncumbentArtificiallyAlive();
            this._population.Age();

            if (initialIncumbentAge == this._mostRecentBest.IncumbentGenome.Age)
            {
                this._mostRecentBest.IncumbentGenome.AgeOnce();
            }

            // population.Age dropped the incumbent
            if (keepArtificiallyAlive)
            {
                this._population.AddGenome(this._mostRecentBest.IncumbentGenome, true);
            }
        }

        #endregion
    }
}