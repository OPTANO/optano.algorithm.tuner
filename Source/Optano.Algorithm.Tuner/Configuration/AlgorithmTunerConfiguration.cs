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

namespace Optano.Algorithm.Tuner.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;

    using Akka.Configuration;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning;

    /// <summary>
    /// Relevant parameters for OPTANO Algorithm Tuner.
    /// </summary>
    public class AlgorithmTunerConfiguration : ConfigurationBase
    {
        #region Constants

        /// <summary>
        /// File name to use for serialized data.
        /// </summary>
        public const string FileName = "status.oatstat";

        #endregion

        #region Fields

        /// <summary>
        /// Private field corresponding to <see cref="AlgorithmTunerConfiguration.MaximumNumberParallelEvaluations"/>.
        /// </summary>
        private int _maximumNumberParallelEvaluations;

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the distance metric to use during genetic engineering.
        /// </summary>
        public DistanceMetric DistanceMetric { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the convergence behavior should get evaluated and logged.
        /// </summary>
        public bool TrackConvergenceBehavior { get; private set; }

        /// <summary>
        /// Gets a value indicating whether racing should be enabled.
        /// </summary>
        public bool EnableRacing { get; private set; }

        /// <summary>
        /// Gets the total population size.
        /// </summary>
        public int PopulationSize { get; private set; }

        /// <summary>
        /// Gets the maximum number of evaluations, i. e. runs of configuration - instance combinations, which should
        /// be done. Program is terminated after the first generation which meets the limit.
        /// </summary>
        public int EvaluationLimit { get; private set; }

        /// <summary>
        /// Gets the total number of generations.
        /// </summary>
        public int Generations { get; private set; }

        /// <summary>
        /// Gets the continuous optimization method to combine GGA(++) with.
        /// </summary>
        public ContinuousOptimizationMethod ContinuousOptimizationMethod { get; private set; }

        /// <summary>
        /// Gets the maximum number of generations per GGA phase.
        /// </summary>
        public int MaximumNumberGgaGenerations { get; private set; }

        /// <summary>
        /// Gets the maximum number of consecutive GGA generations in which no new incumbent is found.
        /// </summary>
        public int MaximumNumberGgaGenerationsWithSameIncumbent { get; private set; }

        /// <summary>
        /// Gets the maximum number of generations a genome can survive.
        /// </summary>
        public int MaxGenomeAge { get; private set; }

        /// <summary>
        /// Gets the maximum size of a mini tournament.
        /// </summary>
        public int MaximumMiniTournamentSize { get; private set; }

        /// <summary>
        /// Gets the percentage of winners per mini tournament.
        /// </summary>
        public double TournamentWinnerPercentage { get; private set; }

        /// <summary>
        /// Gets the CPU timeout for a single target algorithm run.
        /// </summary>
        public TimeSpan CpuTimeout { get; private set; }

        /// <summary>
        /// Gets the probability that we switch between parents when doing a crossover and deciding on the value of a
        /// parameter that has different values for both parents and has a parent parameter in the parameter tree which
        /// also has different values for both parents.
        /// </summary>
        public double CrossoverSwitchProbability { get; private set; }

        /// <summary>
        /// Gets the probability that a parameter is mutated.
        /// </summary>
        public double MutationRate { get; private set; }

        /// <summary>
        /// Gets the percentage of the variable's domain that is used to determine the variance for Gaussian mutation.
        /// </summary>
        public double MutationVariancePercentage { get; private set; }

        /// <summary>
        /// Gets the maximum number of attempts to repair a genome.
        /// </summary>
        public int MaxRepairAttempts { get; private set; }

        /// <summary>
        /// Gets the maximum number of tries to evaluate a genome - instance combination and tolerating a failure in a
        /// row. If more failures occur, the whole program will be stopped.
        /// </summary>
        public int MaximumNumberConsecutiveFailuresPerEvaluation { get; private set; }

        /// <summary>
        /// Gets the number of instances to use for evaluation at the start of the tuning.
        /// </summary>
        public int StartNumInstances { get; private set; }

        /// <summary>
        /// Gets the number of instances to use for evaluation at the end of the tuning.
        /// At least at high as <see cref="StartNumInstances" />.
        /// </summary>
        public int EndNumInstances { get; private set; }

        /// <summary>
        /// Gets the generation after which <see cref="EndNumInstances" /> should be used.
        /// At most as high as <see cref="Generations" /> - 1.
        /// </summary>
        public int GoalGeneration { get; private set; }

        /// <summary>
        /// Gets the maximum number of parallel evaluations allowed per node.
        /// </summary>
        public int MaximumNumberParallelEvaluations
        {
            get
            {
                if (this._maximumNumberParallelEvaluations > Environment.ProcessorCount)
                {
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Warn,
                        $"Warning: You specified {this._maximumNumberParallelEvaluations} parallel evaluations, but only have {Environment.ProcessorCount} processors. Processes may fight for resources.");
                }

                return this._maximumNumberParallelEvaluations;
            }
        }

        /// <summary>
        /// Gets the maximum number of parallel threads allowed per node.
        /// </summary>
        public int MaximumNumberParallelThreads { get; private set; }

        /// <summary>
        /// Gets configuration for Akka.NET.
        /// </summary>
        public Config AkkaConfiguration { get; private set; }

        /// <summary>
        /// Gets how detailed the console output should be.
        /// </summary>
        public VerbosityLevel Verbosity { get; private set; }

        /// <summary>
        /// Gets the proportion of engineered genomes to the total genome population.
        /// Default: 0.3.
        /// </summary>
        public double EngineeredPopulationRatio { get; private set; }

        /// <summary>
        /// Gets the iteration at which the genetic engineering crossover is used.
        /// Default: 3.
        /// </summary>
        public int StartEngineeringAtIteration { get; private set; }

        /// <summary>
        /// Gets the percentage of randomly selected non competitive genomes that get replaced by newly generated ones after each generation.
        /// </summary>
        public double PopulationMutantRatio { get; private set; }

        /// <summary>
        /// Gets the threshold (percentage wise), below which a genome is considered to be a top performer.
        /// </summary>
        public double TopPerformerThreshold { get; private set; }

        /// <summary>
        /// Gets the path to the status file directory.
        /// </summary>
        public string StatusFileDirectory { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to zip old status files.
        /// If set to <c>false</c>, old status files are overwritten.
        /// </summary>
        public bool ZipOldStatusFiles { get; private set; }

        /// <summary>
        /// Gets the path to the directory holding zip files containing old states of the status directory.
        /// </summary>
        public string ZippedStatusFileDirectory =>
            Path.Combine(Directory.GetParent(this.StatusFileDirectory).FullName, "old_status_files");

        /// <summary>
        /// Gets the path to the log file.
        /// </summary>
        public string LogFilePath { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the generation history logged at the end of the tuning should include
        /// average scores on the complete instance sets.
        /// </summary>
        public bool ScoreGenerationHistory { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a performance model should be trained even if genetic engineering and
        /// sexual selection are turned off.
        /// </summary>
        public bool TrainModel { get; private set; }

        /// <summary>
        /// Gets a value indicating whether an attractiveness measure should be considered during the selection of
        /// non-competitive mates.
        /// </summary>
        public bool EnableSexualSelection { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the compatibility check executed in case of a continued run should check
        /// for logical continuity.
        /// If this is set to false, OPTANO Algorithm Tuner only validates if the configurations fit together in a
        /// technical sense.
        /// </summary>
        public bool StrictCompatibilityCheck { get; private set; }

        /// <summary>
        /// Gets the probability with which a non-fixed parameter will be selected from the competitive genome during
        /// genetic engineering.
        /// </summary>
        public double CrossoverProbabilityCompetitive { get; private set; }

        /// <summary>
        /// Gets the relative difference threshold of a parameter value between an engineered offspring candidate and
        /// an existing genome above which the feature of will count towards the
        /// <see cref="Configuration.DistanceMetric.HammingDistance"/>.
        /// </summary>
        public double HammingDistanceRelativeThreshold { get; private set; }

        /// <summary>
        /// Gets the number of random samples to generate per reachable leaf during GeneticEngineering.
        /// </summary>
        public int TargetSamplingSize { get; private set; }

        /// <summary>
        /// Gets the influence factor for the 'distance' between a potential offspring and the existing population when scoring potential offspring.
        /// Potential offspring with lowest combination of 'Predicted Rank' - 'DistanceCompensation' is selected as offspring. (For each TournamentWinner).
        /// Default: 0.2 * <see cref="MaximumMiniTournamentSize"/>.
        /// </summary>
        public double MaxRanksCompensatedByDistance { get; private set; }

        /// <summary>
        /// Gets the number of randomly selected features to use to compute distances between genomes during genetic engineering.
        /// </summary>
        public double FeatureSubsetRatioForDistanceComputation { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to include a <see cref="Genome"/> that uses the target algorithm's default values (if specified).
        /// </summary>
        public bool AddDefaultGenome { get; private set; }

        /// <summary>
        /// Gets further configurations which are relevant for specific parts of OPTANO Algorithm Tuner.
        /// </summary>
        public Dictionary<string, ConfigurationBase> DetailedConfigurations { get; } =
            new Dictionary<string, ConfigurationBase>();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Extracts a configuration from <see cref="DetailedConfigurations"/>.
        /// </summary>
        /// <typeparam name="TConfiguration">The expected type of the configuration.</typeparam>
        /// <param name="identifier">The configuration's identifier.</param>
        /// <returns>The extracted configuration.</returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if there is no fitting key in <see cref="DetailedConfigurations"/>.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// Thrown if the configuration with identifier <paramref name="identifier"/> is not of the expected type.
        /// </exception>
        public TConfiguration ExtractDetailedConfiguration<TConfiguration>(string identifier)
            where TConfiguration : ConfigurationBase
        {
            if (!this.DetailedConfigurations.TryGetValue(identifier, out var detailedConfig))
            {
                throw new KeyNotFoundException($"Did not find a configuration at {identifier}");
            }

            if (!(detailedConfig is TConfiguration correctTypeConfiguration))
            {
                throw new InvalidCastException(
                    $"Configuration {identifier} was not a {typeof(TConfiguration)} but a {detailedConfig.GetType()}.");
            }

            return correctTypeConfiguration;
        }

        /// <summary>
        /// Checks whether two <see cref="AlgorithmTunerConfiguration"/>s are compatible for one to be used in a
        /// continued tuning based on a tuning using the other.
        /// </summary>
        /// <param name="other">Configuration used for the start of tuning.</param>
        /// <returns>True iff this configuration can be used for continued tuning.</returns>
        [SuppressMessage(
            "NDepend",
            "ND1001:MethodsTooComplexCritical",
            Justification = "The method needs to check all relevant configuration parameters.")]
        public override bool IsCompatible(ConfigurationBase other)
        {
            if (!(other is AlgorithmTunerConfiguration))
            {
                return false;
            }

            var otherConfig = (AlgorithmTunerConfiguration)other;

            // Check detailed configurations of elements that have been used at the start of the tuning
            foreach (var detailedConfig in this.DetailedConfigurations)
            {
                // If they are not there anymore, nothing can break them.
                if (!otherConfig.DetailedConfigurations.TryGetValue(detailedConfig.Key, out var otherDetailedConfig))
                {
                    continue;
                }

                // Else check compatibility.
                if (!detailedConfig.Value.IsCompatible(otherDetailedConfig))
                {
                    return false;
                }
            }

            return this.IsTechnicallyCompatible(otherConfig)
                   && this.EnableRacing == otherConfig.EnableRacing
                   && this.PopulationSize == otherConfig.PopulationSize
                   && this.EvaluationLimit >= otherConfig.EvaluationLimit
                   && this.Generations == otherConfig.Generations
                   && this.ContinuousOptimizationMethod == otherConfig.ContinuousOptimizationMethod
                   && this.MaximumNumberGgaGenerations == otherConfig.MaximumNumberGgaGenerations
                   && this.MaximumNumberGgaGenerationsWithSameIncumbent == otherConfig.MaximumNumberGgaGenerationsWithSameIncumbent
                   && this.MaxGenomeAge == otherConfig.MaxGenomeAge
                   && this.MaximumMiniTournamentSize == otherConfig.MaximumMiniTournamentSize
                   && Math.Abs(this.TournamentWinnerPercentage - otherConfig.TournamentWinnerPercentage) < ConfigurationBase.CompatibilityTolerance
                   && this.CpuTimeout == otherConfig.CpuTimeout
                   && Math.Abs(this.CrossoverSwitchProbability - otherConfig.CrossoverSwitchProbability) < ConfigurationBase.CompatibilityTolerance
                   && Math.Abs(this.MutationRate - otherConfig.MutationRate) < ConfigurationBase.CompatibilityTolerance
                   && Math.Abs(this.MutationVariancePercentage - otherConfig.MutationVariancePercentage) < ConfigurationBase.CompatibilityTolerance
                   && this.StartNumInstances == otherConfig.StartNumInstances
                   && this.EndNumInstances == otherConfig.EndNumInstances
                   && this.GoalGeneration == otherConfig.GoalGeneration
                   && Math.Abs(this.EngineeredPopulationRatio - otherConfig.EngineeredPopulationRatio) < ConfigurationBase.CompatibilityTolerance
                   && Math.Abs(this.PopulationMutantRatio - otherConfig.PopulationMutantRatio) < ConfigurationBase.CompatibilityTolerance
                   && this.StartEngineeringAtIteration == otherConfig.StartEngineeringAtIteration
                   && Math.Abs(this.TopPerformerThreshold - otherConfig.TopPerformerThreshold) < ConfigurationBase.CompatibilityTolerance
                   && this.EnableSexualSelection == otherConfig.EnableSexualSelection
                   && Math.Abs(this.CrossoverProbabilityCompetitive - otherConfig.CrossoverProbabilityCompetitive)
                   < ConfigurationBase.CompatibilityTolerance
                   && Math.Abs(this.HammingDistanceRelativeThreshold - otherConfig.HammingDistanceRelativeThreshold)
                   < ConfigurationBase.CompatibilityTolerance
                   && this.TargetSamplingSize == otherConfig.TargetSamplingSize
                   && Math.Abs(this.MaxRanksCompensatedByDistance - otherConfig.MaxRanksCompensatedByDistance)
                   < ConfigurationBase.CompatibilityTolerance
                   && Math.Abs(this.FeatureSubsetRatioForDistanceComputation - otherConfig.FeatureSubsetRatioForDistanceComputation)
                   < ConfigurationBase.CompatibilityTolerance
                   && this.DistanceMetric == otherConfig.DistanceMetric
                   && this.AddDefaultGenome == otherConfig.AddDefaultGenome;
        }

        /// <summary>
        /// Checks whether two <see cref="AlgorithmTunerConfiguration"/>s are compatible in a technical sense for one
        /// to be used in a continued tuning based on a tuning using the other.
        /// <para>The difference to <see cref="IsCompatible"/> is that this function only checks for technical
        /// compatibility and does not consider whether the combination of configurations is compatible in the sense
        /// that the continued tuning looks like a longer single tuning.</para>
        /// </summary>
        /// <param name="other">Configuration used for the start of tuning.</param>
        /// <returns>True iff this configuration can be used for continued tuning.</returns>
        public override bool IsTechnicallyCompatible(ConfigurationBase other)
        {
            if (!(other is AlgorithmTunerConfiguration otherConfig))
            {
                return false;
            }

            if (this.TrainModel && !otherConfig.TrainModel)
            {
                return false;
            }

            if (this.ContinuousOptimizationMethod != ContinuousOptimizationMethod.None
                && this.ContinuousOptimizationMethod != otherConfig.ContinuousOptimizationMethod)
            {
                return false;
            }

            // Check detailed configurations of elements that have been used at the start of the tuning
            foreach (var detailedConfig in this.DetailedConfigurations)
            {
                // If they are not there anymore, nothing can break them.
                if (!otherConfig.DetailedConfigurations.TryGetValue(detailedConfig.Key, out var otherDetailedConfig))
                {
                    continue;
                }

                // Else check compatibility.
                if (!detailedConfig.Value.IsTechnicallyCompatible(otherDetailedConfig))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            var descriptionBuilder = new StringBuilder();

            descriptionBuilder.AppendLine("Target algorithm specific : {");
            descriptionBuilder.AppendLine(Indent + $"racing : {this.EnableRacing}");
            descriptionBuilder.AppendLine(Indent + $"cpuTimeout : {this.CpuTimeout}");
            descriptionBuilder.AppendLine(Indent + $"maxParallelEvaluations : {this._maximumNumberParallelEvaluations}");
            descriptionBuilder.AppendLine(Indent + $"instanceNumbers.Minimum : {this.StartNumInstances}");
            descriptionBuilder.AppendLine(Indent + $"instanceNumbers.Maximum : {this.EndNumInstances}");
            descriptionBuilder.AppendLine(Indent + $"addDefaultGenome : {this.AddDefaultGenome}");
            descriptionBuilder.AppendLine("}");

            descriptionBuilder.AppendLine("Population-based algorithm : {");
            descriptionBuilder.AppendLine(Indent + $"popSize : {this.PopulationSize}");
            descriptionBuilder.AppendLine(Indent + $"numGens : {this.Generations}");
            descriptionBuilder.AppendLine(Indent + $"goalGen : {this.GoalGeneration}");
            descriptionBuilder.AppendLine(Indent + $"evaluationLimit : {this.EvaluationLimit}");
            descriptionBuilder.AppendLine(Indent + $"maxParallelThreads : {this.MaximumNumberParallelThreads}");
            descriptionBuilder.AppendLine("}");

            descriptionBuilder.AppendLine("Strategy : {");
            descriptionBuilder.AppendLine(Indent + $"continuousOptimizationMethod : {this.ContinuousOptimizationMethod}");
            descriptionBuilder.AppendLine(Indent + $"maxGenerationsPerGgaPhase : {this.MaximumNumberGgaGenerations}");
            descriptionBuilder.AppendLine(Indent + $"maxGgaGenerationsWithSameIncumbent : {this.MaximumNumberGgaGenerationsWithSameIncumbent}");
            descriptionBuilder.AppendLine("}");

            descriptionBuilder.AppendLine("Logging : {");
            descriptionBuilder.AppendLine(Indent + $"verbosity : {this.Verbosity}");
            descriptionBuilder.AppendLine(Indent + $"logFile : {this.LogFilePath}");
            descriptionBuilder.AppendLine(Indent + $"statusFileDir : {this.StatusFileDirectory}");
            descriptionBuilder.AppendLine(Indent + $"zipOldStatus : {this.ZipOldStatusFiles}");
            descriptionBuilder.AppendLine(Indent + $"trackConvergenceBehavior : {this.TrackConvergenceBehavior}");
            descriptionBuilder.AppendLine(Indent + $"scoreGenerationHistory : {this.ScoreGenerationHistory}");
            descriptionBuilder.AppendLine("}");

            descriptionBuilder.AppendLine("Fault tolerance : {");
            descriptionBuilder.AppendLine(Indent + $"maxRepair : {this.MaxRepairAttempts}");
            descriptionBuilder.AppendLine(Indent + $"faultTolerance : {this.MaximumNumberConsecutiveFailuresPerEvaluation}");
            descriptionBuilder.AppendLine(Indent + $"strictCompatibilityCheck : {this.StrictCompatibilityCheck}");
            descriptionBuilder.AppendLine("}");

            descriptionBuilder.AppendLine("Genetic gender-based algorithm : {");
            descriptionBuilder.AppendLine(Indent + $"maxGenomeAge : {this.MaxGenomeAge}");
            descriptionBuilder.AppendLine(Indent + $"miniTournamentSize : {this.MaximumMiniTournamentSize}");
            descriptionBuilder.AppendLine(Indent + $"winnerPercentage : {this.TournamentWinnerPercentage}");
            descriptionBuilder.AppendLine(Indent + $"crossoverSwitchProbability : {this.CrossoverSwitchProbability}");
            descriptionBuilder.AppendLine(Indent + $"mutationRate : {this.MutationRate}");
            descriptionBuilder.AppendLine(Indent + $"mutationVariance : {this.MutationVariancePercentage}");
            descriptionBuilder.AppendLine(Indent + $"enableSexualSelection : {this.EnableSexualSelection}");
            descriptionBuilder.AppendLine(Indent + $"populationMutantRatio : {this.PopulationMutantRatio}");
            descriptionBuilder.AppendLine("}");

            descriptionBuilder.AppendLine("Genetic engineering : {");
            descriptionBuilder.AppendLine(Indent + $"trainModel : {this.TrainModel}");
            descriptionBuilder.AppendLine(Indent + $"engineeredProportion : {this.EngineeredPopulationRatio}");
            descriptionBuilder.AppendLine(Indent + $"startIterationEngineering : {this.StartEngineeringAtIteration}");
            descriptionBuilder.AppendLine(Indent + $"targetSampleSize : {this.TargetSamplingSize}");
            descriptionBuilder.AppendLine(Indent + $"crossoverProbabilityCompetitive : {this.CrossoverProbabilityCompetitive}");
            descriptionBuilder.AppendLine(Indent + $"topPerformerThreshold : {this.TopPerformerThreshold}");
            descriptionBuilder.AppendLine(Indent + $"distanceMetric : {this.DistanceMetric}");
            descriptionBuilder.AppendLine(Indent + $"hammingDistanceRelativeThreshold : {this.HammingDistanceRelativeThreshold}");
            descriptionBuilder.AppendLine(Indent + $"featureSubsetRatioForDistance : {this.FeatureSubsetRatioForDistanceComputation}");
            descriptionBuilder.AppendLine(Indent + $"maxRanksCompensatedByDistance : {this.MaxRanksCompensatedByDistance}");
            descriptionBuilder.AppendLine("}");

            foreach (var detailedConfig in this.DetailedConfigurations)
            {
                descriptionBuilder.Append(DescribeSubConfiguration(detailedConfig.Key, detailedConfig.Value));
            }

            descriptionBuilder.AppendLine("Akka.NET configuration:");
            descriptionBuilder.Append(this.AkkaConfiguration);

            return descriptionBuilder.ToString();
        }

        /// <summary>
        /// Checks relationships between properties.
        /// May write warnings or throw <see cref="ArgumentException"/>s.
        /// </summary>
        public void Validate()
        {
            if (this.ContinuousOptimizationMethod == ContinuousOptimizationMethod.None)
            {
                if (this.MaximumNumberGgaGenerations < this.Generations)
                {
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Warn,
                        $"Warning: You specified a maximum number of {this.MaximumNumberGgaGenerations} GGA generations per phase, which is smaller than the generation limit of {this.Generations}). However, you did not specify an alternative tuning method to switch to.");
                }

                if (this.MaximumNumberGgaGenerationsWithSameIncumbent < this.Generations - 1)
                {
                    LoggingHelper.WriteLine(
                        VerbosityLevel.Warn,
                        $"Warning: You specified a maximum number of {this.MaximumNumberGgaGenerations} consecutive GGA generations not finding a new incumbent, while using a generation limit of {this.Generations}). However, you did not specify an alternative tuning method to switch to.");
                }
            }

            if (this.GoalGeneration >= this.Generations)
            {
                throw new ArgumentException(
                    $"Max number instances should be reached at (0-indexed) generation {this.GoalGeneration}, but only {this.Generations} generations will be executed.");
            }

            var sizeOfBiggerPopulationPart = (int)Math.Ceiling(this.PopulationSize / 2.0);
            if (this.MaxGenomeAge > sizeOfBiggerPopulationPart)
            {
                throw new ArgumentException(
                    $"With a maximum genome age of {this.MaxGenomeAge} higher than the size {sizeOfBiggerPopulationPart} of the bigger part of the population, sometimes no genomes would be replaced in a generation.");
            }
        }

        #endregion

        /// <summary>
        /// Builder for <see cref="AlgorithmTunerConfiguration" />.
        /// </summary>
        [SuppressMessage(
            "NDepend",
            "ND1305:NestedTypesShouldNotBeVisible",
            Justification = "This type is part of the Builder Pattern.")]
        public class AlgorithmTunerConfigurationBuilder : IConfigBuilder<AlgorithmTunerConfiguration>
        {
            #region Static Fields

            /// <summary>
            /// By default, the status files should be written to a folder 'status' in the current directory.
            /// </summary>
            public static readonly string DefaultStatusFileDirectory = PathUtils.GetAbsolutePathFromCurrentDirectory("status");

            /// <summary>
            /// The default value for <see cref="AlgorithmTunerConfiguration.ContinuousOptimizationMethod"/>.
            /// </summary>
            public static readonly ContinuousOptimizationMethod DefaultContinuousOptimizationMethod
                = ContinuousOptimizationMethod.None;

            /// <summary>
            /// The default value for <see cref="AlgorithmTunerConfiguration.MaximumNumberGgaGenerations"/>.
            /// </summary>
            public static readonly int DefaultMaximumNumberGgaGenerations = int.MaxValue;

            /// <summary>
            /// The default value for <see cref="AlgorithmTunerConfiguration.MaximumNumberGgaGenerationsWithSameIncumbent"/>.
            /// </summary>
            public static readonly int DefaultMaximumNumberGgaGenerationsWithSameIncumbent = int.MaxValue;

            /// <summary>
            /// The default value for <see cref="AlgorithmTunerConfiguration.EnableRacing"/>.
            /// </summary>
            public static readonly bool DefaultEnableRacing = false;

            /// <summary>
            /// The default value for <see cref="AlgorithmTunerConfiguration.MutationRate"/>.
            /// </summary>
            public static readonly double DefaultMutationRate = 0.1;

            /// <summary>
            /// The default value for <see cref="AlgorithmTunerConfiguration.PopulationMutantRatio"/>.
            /// </summary>
            public static readonly double DefaultPopulationMutantRatio = 0.25;

            /// <summary>
            /// The default value for <see cref="AlgorithmTunerConfiguration.TargetSamplingSize"/>.
            /// </summary>
            public static readonly int DefaultTargetSamplingSize = 125;

            /// <summary>
            /// The default value for <see cref="_populationSize"/>.
            /// </summary>
            private static readonly int DefaultPopulationSize = 128;

            /// <summary>
            /// The default value for <see cref="_evaluationLimit"/>.
            /// </summary>
            private static readonly int DefaultEvaluationLimit = int.MaxValue;

            /// <summary>
            /// The default value for <see cref="_maximumMiniTournamentSize"/>.
            /// </summary>
            private static readonly int DefaultMaximumMiniTournamentSize = 8;

            /// <summary>
            /// The default value for <see cref="_maxRanksCompensatedByDistance"/>.
            /// </summary>
            private static readonly double DefaultMaxRanksCompensatedByDistance =
                0.2 * DefaultMaximumMiniTournamentSize;

            /// <summary>
            /// The default value for <see cref="_addDefaultGenome"/>.
            /// </summary>
            private static readonly bool DefaultAddDefaultGenome = true;

            #endregion

            #region Fields

            /// <summary>
            /// Builders which create <see cref="AlgorithmTunerConfiguration.DetailedConfigurations"/>.
            /// </summary>
            private readonly Dictionary<string, IConfigBuilder<ConfigurationBase>> _detailedConfigurationBuilders =
                new Dictionary<string, IConfigBuilder<ConfigurationBase>>();

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.AkkaConfiguration" />.
            /// </summary>
            private Config _akkaConfiguration;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.CpuTimeout" />.
            /// </summary>
            private TimeSpan? _cpuTimeout;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.CrossoverSwitchProbability" />.
            /// </summary>
            private double? _crossoverSwitchProbability;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.EndNumInstances" />.
            /// </summary>
            private int? _endNumInstances;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.Generations" />.
            /// </summary>
            private int? _generations;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.EvaluationLimit"/>.
            /// </summary>
            private int? _evaluationLimit;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.GoalGeneration" />.
            /// </summary>
            private int? _goalGeneration;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.ContinuousOptimizationMethod"/>.
            /// </summary>
            private ContinuousOptimizationMethod? _continuousOptimizationMethod;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.MaximumNumberGgaGenerations"/>.
            /// </summary>
            private int? _maximumNumberGgaGenerations;

            /// <summary>
            /// The value to set for
            /// <see cref="AlgorithmTunerConfiguration.MaximumNumberGgaGenerationsWithSameIncumbent"/>.
            /// </summary>
            private int? _maximumNumberGgaGenerationsWithSameIncumbent;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.MaxGenomeAge" />.
            /// </summary>
            private int? _maxGenomeAge;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.MaximumMiniTournamentSize"/>.
            /// </summary>
            private int? _maximumMiniTournamentSize;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.MaxRepairAttempts" />.
            /// </summary>
            private int? _maxRepairAttempts;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.MaximumNumberConsecutiveFailuresPerEvaluation"/>.
            /// </summary>
            private int? _maximumNumberConsecutiveFailuresPerEvaluation;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.MutationRate" />.
            /// </summary>
            private double? _mutationRate;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.MutationVariancePercentage" />.
            /// </summary>
            private double? _mutationVariancePercentage;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.PopulationSize" />.
            /// </summary>
            private int? _populationSize;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.StartNumInstances" />.
            /// </summary>
            private int? _startNumInstances;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.TournamentWinnerPercentage" />.
            /// </summary>
            private double? _tournamentWinnerPercentage;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.EnableRacing" />.
            /// </summary>
            private bool? _enableRacing;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.Verbosity" />.
            /// </summary>
            private VerbosityLevel? _verbosity;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.TrainModel"/>.
            /// </summary>
            private bool? _trainModel;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.EngineeredPopulationRatio" />.
            /// The proportion of engineered genomes of the total genome population.
            /// </summary>
            private double? _engineeredPopulationRatio;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.PopulationMutantRatio"/>.
            /// </summary>
            private double? _populationMutantRatio;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.StartEngineeringAtIteration" />.
            /// The iteration at which the model based approach should be incorporated in the search.
            /// </summary>
            private int? _startEngineeringAtIteration;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.TopPerformerThreshold" />.
            /// </summary>
            private double? _topPerformerThreshold;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.StatusFileDirectory"/>.
            /// </summary>
            private string _statusFileDirectory;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.ZipOldStatusFiles"/>.
            /// </summary>
            private bool? _zipOldStatusFiles;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.EnableSexualSelection"/>.
            /// </summary>
            private bool? _enableSexualSelection;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.CrossoverProbabilityCompetitive"/>.
            /// </summary>
            private double? _crossoverProbabilityCompetitive;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.HammingDistanceRelativeThreshold"/>.
            /// </summary>
            private double? _hammingDistanceRelativeThreshold;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.TargetSamplingSize"/>.
            /// </summary>
            private int? _targetSampleSize;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.MaxRanksCompensatedByDistance"/>.
            /// </summary>
            private double? _maxRanksCompensatedByDistance;

            /// <summary>
            /// The value to set for
            /// <see cref="AlgorithmTunerConfiguration.FeatureSubsetRatioForDistanceComputation"/>.
            /// </summary>
            private double? _featureSubsetRatioForDistance;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.TrackConvergenceBehavior"/>.
            /// </summary>
            private bool? _trackConvergenceBehavior;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.DistanceMetric"/>.
            /// </summary>
            private DistanceMetric? _distanceMetric;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.LogFilePath"/>.
            /// </summary>
            private string _logFilePath;

            /// <summary>
            /// The value to set for <see cref="ScoreGenerationHistory"/>.
            /// </summary>
            private bool? _scoreGenerationHistory;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.MaximumNumberParallelEvaluations"/>.
            /// </summary>
            private int? _maximumNumberParallelEvaluations;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.MaximumNumberParallelThreads"/>.
            /// </summary>
            private int? _maximumNumberParallelThreads;

            /// <summary>
            /// The value to set for <see cref="AlgorithmTunerConfiguration.AddDefaultGenome"/>.
            /// </summary>
            private bool? _addDefaultGenome;

            #endregion

            #region Public properties

            /// <summary>
            /// Gets the current value to set for <see cref="AlgorithmTunerConfiguration.Verbosity"/>.
            /// </summary>
            public VerbosityLevel Verbosity => this._verbosity ?? VerbosityLevel.Info;

            #endregion

            #region Properties

            /// <summary>
            /// Gets the value to set for <see cref="AlgorithmTunerConfiguration.StrictCompatibilityCheck"/>.
            /// </summary>
            internal bool? StrictCompatibilityCheck { get; private set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// Builds a <see cref="AlgorithmTunerConfiguration" />.
            /// </summary>
            /// <param name="maximumNumberParallelEvaluations">
            /// The maximum number of parallel evaluations allowed per node.
            /// </param>
            /// <returns>The build <see cref="AlgorithmTunerConfiguration" />.</returns>
            public AlgorithmTunerConfiguration Build(int maximumNumberParallelEvaluations)
            {
                this.SetMaximumNumberParallelEvaluations(maximumNumberParallelEvaluations);
                return this.Build();
            }

            /// <summary>
            /// Builds a <see cref="AlgorithmTunerConfiguration" />.
            /// </summary>
            /// Make sure to call <see cref="SetMaximumNumberParallelEvaluations"/>, as the value needs to be larger than 0.
            /// <returns>The build <see cref="AlgorithmTunerConfiguration" />.</returns>
            public AlgorithmTunerConfiguration Build()
            {
                return this.BuildWithFallback(null);
            }

            /// <summary>
            /// Builds a <see cref="AlgorithmTunerConfiguration"/> using the provided
            /// <see cref="ConfigurationBase"/> as fallback.
            /// </summary>
            /// <param name="fallback">Used if a property is not set for this
            /// <see cref="AlgorithmTunerConfigurationBuilder"/>.
            /// May be null. In that case, defaults are used as fallback.
            /// Needs to be of type <see cref="AlgorithmTunerConfiguration"/> if it is not null.</param>
            /// <returns>The build <see cref="AlgorithmTunerConfiguration"/>.</returns>
            public AlgorithmTunerConfiguration BuildWithFallback(ConfigurationBase fallback)
            {
                return this.BuildWithFallback(CastToConfigurationType<AlgorithmTunerConfiguration>(fallback));
            }

            /// <summary>
            /// Adds a <see cref="IConfigBuilder{ConfigurationBase}"/> building a <see cref="ConfigurationBase"/> which
            /// is relevant for a specific part of OPTANO Algorithm Tuner.
            /// </summary>
            /// <param name="key">The key to add the builder (and later the configuration) at.</param>
            /// <param name="detailedConfigurationBuilder">
            /// The <see cref="IConfigBuilder{ConfigurationBase}"/> to add.
            /// </param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder AddDetailedConfigurationBuilder(
                string key,
                IConfigBuilder<ConfigurationBase> detailedConfigurationBuilder)
            {
                if (detailedConfigurationBuilder == null)
                {
                    throw new ArgumentNullException(nameof(detailedConfigurationBuilder));
                }

                this._detailedConfigurationBuilders.Add(key, detailedConfigurationBuilder);
                return this;
            }

            /// <summary>
            /// Sets the maximum number of parallel evaluations allowed per computing node.
            /// </summary>
            /// <param name="maximumNumberParallelEvaluations">The positive number of allowed parallel evalutations.
            /// </param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetMaximumNumberParallelEvaluations(int maximumNumberParallelEvaluations)
            {
                if (maximumNumberParallelEvaluations <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"At least one evaluation must be able to run at a time, but {maximumNumberParallelEvaluations} was given at the maximum number of parallel evaluations.");
                }

                this._maximumNumberParallelEvaluations = maximumNumberParallelEvaluations;
                return this;
            }

            /// <summary>
            /// Sets the maximum number of parallel threads allowed per computing node.
            /// </summary>
            /// <param name="maximumNumberParallelThreads">The positive number of allowed parallel threads.
            /// </param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetMaximumNumberParallelThreads(int maximumNumberParallelThreads)
            {
                if (maximumNumberParallelThreads <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"At least one thread must be allowed at a time, but {maximumNumberParallelThreads} was given at the maximum number of parallel threads.");
                }

                this._maximumNumberParallelThreads = maximumNumberParallelThreads;
                return this;
            }

            /// <summary>
            /// Sets whether racing should be enabled. Default is false.
            /// </summary>
            /// <param name="enableRacing">Whether or not enable racing.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetEnableRacing(bool enableRacing)
            {
                this._enableRacing = enableRacing;
                return this;
            }

            /// <summary>
            /// Sets the total population size. Default is 100.
            /// </summary>
            /// <param name="populationSize">The population size, at least 2.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the population size is less than 2.</exception>
            public AlgorithmTunerConfigurationBuilder SetPopulationSize(int populationSize)
            {
                if (populationSize < 2)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Population size must be at least 2, but was {populationSize}.");
                }

                this._populationSize = populationSize;
                return this;
            }

            /// <summary>
            /// Sets the maximum number of evaluations, i. e. runs of configuration - instance combinations, which should
            /// be done. Program is terminated after the first generation which meets the limit.
            /// <para>Default is <see cref="int.MaxValue"/>.</para>
            /// </summary>
            /// <param name="limit">Maximum number of evaluations.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// Thrown if the number of evaluations is negative.
            /// </exception>
            public AlgorithmTunerConfigurationBuilder SetEvaluationLimit(int limit)
            {
                if (limit < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Maximum number of evaluation must not be negative, but was {limit}.");
                }

                this._evaluationLimit = limit;
                return this;
            }

            /// <summary>
            /// Sets the total number of generations. Default is 100.
            /// </summary>
            /// <param name="generations">Total number of generations, at least 1.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the number of generations is less than 1.</exception>
            public AlgorithmTunerConfigurationBuilder SetGenerations(int generations)
            {
                if (generations < 1)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Number of generations must be at least 1, but was {generations}.");
                }

                this._generations = generations;
                return this;
            }

            /// <summary>
            /// Sets the continuous optimization method to combine GGA(++) with.
            /// <para>
            /// Default is <see cref="ContinuousOptimizationMethod.None"/>.
            /// </para>
            /// </summary>
            /// <param name="method">The <see cref="ContinuousOptimizationMethod"/> to use.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the number of generations is less than 1.</exception>
            public AlgorithmTunerConfigurationBuilder SetContinuousOptimizationMethod(ContinuousOptimizationMethod method)
            {
                if (this._continuousOptimizationMethod != null)
                {
                    throw new InvalidOperationException(
                        $"Tried to set continuous optimization method to {method} when it was already set to {this._continuousOptimizationMethod}.");
                }

                if (!Enum.IsDefined(typeof(ContinuousOptimizationMethod), method))
                {
                    throw new ArgumentOutOfRangeException($"{method} is an undefined continuous optimization method.");
                }

                this._continuousOptimizationMethod = method;
                return this;
            }

            /// <summary>
            /// Sets the maximum number of generations per GGA phase. Default is <see cref="int.MaxValue"/>.
            /// </summary>
            /// <param name="number">Maximum number of generations per GGA phase, at least 0.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the number of generations is negative.</exception>
            public AlgorithmTunerConfigurationBuilder SetMaximumNumberGgaGenerations(int number)
            {
                if (number < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Number of generations must be at least 0, but was {number}.");
                }

                this._maximumNumberGgaGenerations = number;
                return this;
            }

            /// <summary>
            /// Sets the maximum number of consecutive GGA generations in which no new incumbent is found.
            /// Default is <see cref="int.MaxValue"/>.
            /// </summary>
            /// <param name="number">
            /// Maximum number of consecutive GGA generations in which no new incumbent is found, at least 1.
            /// </param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the number of generations is negative.</exception>
            public AlgorithmTunerConfigurationBuilder SetMaximumNumberGgaGenerationsWithSameIncumbent(int number)
            {
                if (number < 1)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Maximum number of consecutive GGA generations in which no new incumbent is found must be at least 1, but was {number}.");
                }

                this._maximumNumberGgaGenerationsWithSameIncumbent = number;
                return this;
            }

            /// <summary>
            /// Sets the maximum number of generations a genome can survive. Default is 3.
            /// </summary>
            /// <param name="maxGenomeAge">Maximum genome age, at least 1.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the maximum genome age is set to less than 1.</exception>
            public AlgorithmTunerConfigurationBuilder SetMaxGenomeAge(int maxGenomeAge)
            {
                if (maxGenomeAge < 1)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Genomes must be able to survive for at least 1 generation, but {maxGenomeAge} was given as maximum genome age.");
                }

                this._maxGenomeAge = maxGenomeAge;
                return this;
            }

            /// <summary>
            /// Sets the maximum size of a mini tournament. Default is 8.
            /// </summary>
            /// <param name="maxMiniTournamentSize">Maximum size of a mini tournament, at least 1.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the maximum mini tournament size is set to less
            /// than 1.</exception>
            public AlgorithmTunerConfigurationBuilder SetMaximumMiniTournamentSize(int maxMiniTournamentSize)
            {
                if (maxMiniTournamentSize < 1)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Mini tournaments must have a positive size, but given size was  {maxMiniTournamentSize}.");
                }

                this._maximumMiniTournamentSize = maxMiniTournamentSize;
                return this;
            }

            /// <summary>
            /// Sets the percentage of winners per mini tournament. Default is 0.125.
            /// </summary>
            /// <param name="percentage">Percentage as a value between 0 and 1 (both excluded).</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the percentage is not a value between 0 and 1, both excluded.</exception>
            public AlgorithmTunerConfigurationBuilder SetTournamentWinnerPercentage(double percentage)
            {
                if ((percentage <= 0) || (percentage >= 1))
                {
                    throw new ArgumentOutOfRangeException(
                        $"Tournament winner percentage must be between 0 and 1 (both excluded), but {percentage} was given.");
                }

                this._tournamentWinnerPercentage = percentage;
                return this;
            }

            /// <summary>
            /// Sets the CPU timeout for a single target algorithm run.
            /// Default is <see cref="int.MaxValue" /> milliseconds.
            /// </summary>
            /// <param name="cpuTimeout">CPU timeout. Must be positive.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// Thrown if the provided CPU timeout is nonpositive.
            /// </exception>
            public AlgorithmTunerConfigurationBuilder SetCpuTimeout(TimeSpan cpuTimeout)
            {
                if (cpuTimeout <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(cpuTimeout),
                        $"CPU timeout must be positive, but {cpuTimeout.TotalSeconds} seconds was provided.");
                }

                if (cpuTimeout.TotalMilliseconds > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(cpuTimeout), $"CPU timeout must be less than {int.MaxValue} milliseconds.");
                }

                this._cpuTimeout = cpuTimeout;
                return this;
            }

            /// <summary>
            /// Sets the probability that we switch between parents when doing a crossover and deciding on the value of a
            /// parameter that has different values for both parents and has a parent parameter in the parameter tree which
            /// also has different values for both parents. Default is 0.1.
            /// </summary>
            /// <param name="probability">Probability as value between 0 and 1.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the probability is not a value between 0 and 1.</exception>
            public AlgorithmTunerConfigurationBuilder SetCrossoverSwitchProbability(double probability)
            {
                if ((probability < 0) || (probability > 1))
                {
                    throw new ArgumentOutOfRangeException(
                        $"Crossover switch probability must be between 0 and 1, but was {probability}.");
                }

                this._crossoverSwitchProbability = probability;
                return this;
            }

            /// <summary>
            /// Sets the probability that a new individual's gene gets mutated. Default is 0.1.
            /// </summary>
            /// <param name="mutationRate">Mutation rate as a value between 0 and 1.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// Thrown if the mutation rate is not given as a value
            /// between 0 and 1.
            /// </exception>
            public AlgorithmTunerConfigurationBuilder SetMutationRate(double mutationRate)
            {
                if ((mutationRate < 0) || (mutationRate > 1))
                {
                    throw new ArgumentOutOfRangeException(
                        $"Mutation rate must be between 0 and 1, but {mutationRate} was given.");
                }

                this._mutationRate = mutationRate;
                return this;
            }

            /// <summary>
            /// Sets the percentage of a variable's domain that is used to determine the variance for Gaussian mutation.
            /// </summary>
            /// <param name="percentage">Percentage as a positive value of at most 1.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetMutationVariancePercentage(double percentage)
            {
                if ((percentage <= 0) || (percentage > 1))
                {
                    throw new ArgumentOutOfRangeException(
                        $"Mutation variance percentage must be positive and at most 1, but is {percentage}.");
                }

                this._mutationVariancePercentage = percentage;
                return this;
            }

            /// <summary>
            /// Sets the maximum number of attempts to repair a genome. Default is 20.
            /// </summary>
            /// <param name="maxRepairAttempts">Maximum number of attempts, at least 0.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the maximum number of repair attempts is a negative value.</exception>
            public AlgorithmTunerConfigurationBuilder SetMaxRepairAttempts(int maxRepairAttempts)
            {
                if (maxRepairAttempts < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Maximum number of repair attempts must be at least 0, but was {maxRepairAttempts}.");
                }

                this._maxRepairAttempts = maxRepairAttempts;
                return this;
            }

            /// <summary>
            /// Sets the maximum number of tries to evaluate a genome - instance combination and tolerating a failure
            /// in a row. Default is 3.
            /// </summary>
            /// <param name="maximumNumberFailures">Maximum number of failures, at least 0.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the maximum number of tries is a negative value.</exception>
            public AlgorithmTunerConfigurationBuilder SetMaximumNumberConsecutiveFailuresPerEvaluation(int maximumNumberFailures)
            {
                if (maximumNumberFailures < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"A maximum number of failures should be at least 0, but is {maximumNumberFailures}.");
                }

                this._maximumNumberConsecutiveFailuresPerEvaluation = maximumNumberFailures;
                return this;
            }

            /// <summary>
            /// Sets the number of instances to use for evaluation at the start and the end of the tuning.
            /// Default numbers are 5 and 100.
            /// Number will be increased linearly until generation <see cref="AlgorithmTunerConfiguration.GoalGeneration" />.
            /// </summary>
            /// <param name="startNumInstances">Number of instances at the beginning.</param>
            /// <param name="endNumInstances">
            /// Number of instances at the end. Has to be at least as large as
            /// <see cref="_startNumInstances" />.
            /// </param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified number of instances is negative.</exception>
            /// <exception cref="ArgumentException">
            /// Thrown if the specified number of instances at the end is less
            /// than the one at the start.
            /// </exception>
            public AlgorithmTunerConfigurationBuilder SetInstanceNumbers(int startNumInstances, int endNumInstances)
            {
                if (startNumInstances <= 0)
                {
                    throw new ArgumentOutOfRangeException($"Number of instances must always be positive.");
                }

                if (endNumInstances < startNumInstances)
                {
                    throw new ArgumentException(
                        $"Number of instances must increase, but {endNumInstances} is smaller than {startNumInstances}.");
                }

                this._startNumInstances = startNumInstances;
                this._endNumInstances = endNumInstances;
                return this;
            }

            /// <summary>
            /// Sets the generation after which <see cref="AlgorithmTunerConfiguration.EndNumInstances" /> should be used.
            /// Default is 74.
            /// </summary>
            /// <param name="goalGeneration">The generation number, at least 0.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified generation number is negative.</exception>
            public AlgorithmTunerConfigurationBuilder SetGoalGeneration(int goalGeneration)
            {
                if (goalGeneration < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Generation number should always be nonnegative, but is {goalGeneration}!");
                }

                this._goalGeneration = goalGeneration;
                return this;
            }

            /// <summary>
            /// Sets configuration for Akka.NET. Default is the one found in the current application's config file.
            /// </summary>
            /// <param name="akkaConfiguration">The Akka.NET configuration.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetAkkaConfiguration(Config akkaConfiguration)
            {
                this._akkaConfiguration = akkaConfiguration ?? throw new ArgumentNullException("akkaConfiguration");
                return this;
            }

            /// <summary>
            /// Sets how detailed the console output should be. Default is <see cref="VerbosityLevel.Info" />.
            /// </summary>
            /// <param name="verbosityLevel">The level of verbosity.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetVerbosity(VerbosityLevel verbosityLevel)
            {
                if (!Enum.IsDefined(typeof(VerbosityLevel), verbosityLevel))
                {
                    throw new ArgumentOutOfRangeException($"{verbosityLevel} is an undefined level of verbosity.");
                }

                this._verbosity = verbosityLevel;
                return this;
            }

            /// <summary>
            /// Sets whether a performance model should be trained even if genetic engineering and
            /// sexual selection are turned off.
            /// Default is false.
            /// </summary>
            /// <param name="train">True to train the model.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetTrainModel(bool train)
            {
                this._trainModel = train;
                return this;
            }

            /// <summary>
            /// Sets the percentage of genomes that should be genetically engineered in every generation.
            /// Default is 0.
            /// </summary>
            /// <param name="engineeredProportion">Percentage as a positive value of at most 1.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetEngineeredProportion(double engineeredProportion)
            {
                if ((engineeredProportion < 0) || (engineeredProportion > 1))
                {
                    throw new ArgumentOutOfRangeException(
                        $"Proportion of engineered genomes must be in the range of [0, 1], but is {engineeredProportion}.");
                }

                this._engineeredPopulationRatio = engineeredProportion;
                return this;
            }

            /// <summary>
            /// Sets the ratio of randomly selected <c>non competitive</c> genomes that get replaced by newly generated random genomes after each generation. Default is 0.25.
            /// </summary>
            /// <param name="percentage">Percentage as value between 0 and 1.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder"/> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if percentage is not between 0 and 1.</exception>
            public AlgorithmTunerConfigurationBuilder SetPopulationMutantRatio(double percentage)
            {
                if (percentage < 0 || percentage > 1)
                {
                    throw new ArgumentOutOfRangeException($"Population mutant ratio must be in the range of [0, 1], but {percentage} was given.");
                }

                this._populationMutantRatio = percentage;
                return this;
            }

            /// <summary>
            /// Sets the iteration number after which the genetic engineering should be incorporated in the tuning.
            /// </summary>
            /// <param name="startEngineeringAtIteration">Percentage as a positive value of at most 1.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetStartEngineeringAtIteration(int startEngineeringAtIteration)
            {
                if (startEngineeringAtIteration < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Iteration to start engineering genomes in must be greater or equal than 0, but is {startEngineeringAtIteration}.");
                }

                this._startEngineeringAtIteration = startEngineeringAtIteration;
                return this;
            }

            /// <summary>
            /// Sets the percentage of genomes that are considered to be 'top performers' during model based approach.
            /// </summary>
            /// <param name="topThreshold">Percentage as a positive value of at most 1.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetTopPerformerThreshold(double topThreshold)
            {
                if ((topThreshold < 0) || (topThreshold > 1))
                {
                    throw new ArgumentOutOfRangeException($"Proportion of engineered genomes must be in the range of [0, 1], but is {topThreshold}.");
                }

                this._topPerformerThreshold = topThreshold;
                return this;
            }

            /// <summary>
            /// Set a value indicating whether an attractiveness measure should be considered during the selection of non-competitive mates.
            /// The attractiveness of a genome refers to the rank that is predicted for it by the <see cref="GeneticEngineering{TLearnerModel,TPredictorModel,TSamplingStrategy}"/> random forest.
            /// </summary>
            /// <param name="sexualSelection">True to enable attractiveness-based selection.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetEnableSexualSelection(bool sexualSelection)
            {
                this._enableSexualSelection = sexualSelection;
                return this;
            }

            /// <summary>
            /// Sets whether the compatibility check executed in case of a continued run should check for logical
            /// continuity.
            /// If this is set to false, OPTANO Algorithm Tuner only validates if the configurations fit together in a
            /// technical sense.
            /// Default is true. Only change if you know what you are doing.
            /// </summary>
            /// <param name="useStrictCheck">False to disable continuity check.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetStrictCompatibilityCheck(bool useStrictCheck)
            {
                this.StrictCompatibilityCheck = useStrictCheck;
                return this;
            }

            /// <summary>
            /// Sets the probability with which a non-fixed parameter will be selected from the <c>competitive</c>
            /// genome during the targeted sampling of
            /// <see cref="GeneticEngineering{TLearnerModel, TPredictorModel, TSamplingStrategy}"/>. Default is 0.5.
            /// </summary>
            /// <param name="crossoverProbabilityCompetitive">Probability as value between 0 and 1.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetCrossoverProbabilityCompetitive(double crossoverProbabilityCompetitive)
            {
                if (crossoverProbabilityCompetitive < 0 || crossoverProbabilityCompetitive > 1)
                {
                    throw new ArgumentException(
                        $"Probability for selecting the value of a competitive genome value during genetic engineering must be in the range of [0, 1], but is {crossoverProbabilityCompetitive}.");
                }

                this._crossoverProbabilityCompetitive = crossoverProbabilityCompetitive;
                return this;
            }

            /// <summary>
            /// Sets the relative threshold above which 2 compared features are considered to be different.
            /// </summary>
            /// <param name="hammingDistanceRelativeThreshold">Threshold as a value between 0 and 1.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetHammingDistanceRelativeThreshold(double hammingDistanceRelativeThreshold)
            {
                if (hammingDistanceRelativeThreshold < 0 || hammingDistanceRelativeThreshold > 1)
                {
                    throw new ArgumentException(
                        $"Unique Threshold for Hamming Distance must be in the range of [0, 1], but is {hammingDistanceRelativeThreshold}.");
                }

                this._hammingDistanceRelativeThreshold = hammingDistanceRelativeThreshold;
                return this;
            }

            /// <summary>
            /// Number of samples to generate per leaf dring targeted sampling.
            /// Default: <see cref="DefaultTargetSamplingSize"/>.
            /// </summary>
            /// <param name="targetSampleSize">Positive number of samples.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetTargetSampleSize(int targetSampleSize)
            {
                if (targetSampleSize <= 0)
                {
                    throw new ArgumentException($"targetSampleSize needs to be larger than 0. Given value: {targetSampleSize}");
                }

                this._targetSampleSize = targetSampleSize;
                return this;
            }

            /// <summary>
            /// Number of samples to generate per leaf dring targeted sampling.
            /// Default: <see cref="DefaultMaxRanksCompensatedByDistance"/> (= 1.6).
            /// Min value: 0.
            /// </summary>
            /// <param name="maxRanksCompensatedByDistance">
            /// The maximum number Ranks Compensated By Distance.
            /// </param>
            /// <returns>
            /// The <see cref="AlgorithmTunerConfigurationBuilder"/> in its new state.
            /// </returns>
            public AlgorithmTunerConfigurationBuilder SetMaxRanksCompensatedByDistance(double maxRanksCompensatedByDistance)
            {
                if (maxRanksCompensatedByDistance < 0)
                {
                    throw new ArgumentException(
                        $"maxRanksCompensatedByDistance needs to be 0 or above. Given value: {maxRanksCompensatedByDistance}");
                }

                this._maxRanksCompensatedByDistance = maxRanksCompensatedByDistance;
                return this;
            }

            /// <summary>
            /// Ratio of features to use for distance computations during GeneticEngineering.
            /// Default: 0.3.
            /// </summary>
            /// <param name="featureSubsetRatioForDistance">Ratio as value between 0 and 1.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetFeatureSubsetRatioForDistance(double featureSubsetRatioForDistance)
            {
                if (featureSubsetRatioForDistance < 0 || featureSubsetRatioForDistance > 1)
                {
                    throw new ArgumentException(
                        $"featureSubsetRatioForDistance needs to be in the range [0, 1]. Given value: {featureSubsetRatioForDistance}");
                }

                this._featureSubsetRatioForDistance = featureSubsetRatioForDistance;
                return this;
            }

            /// <summary>
            /// True for computing convergence behavior after a tournaments are finished.
            /// Default: false.
            /// </summary>
            /// <param name="trackConvergenceBehavior">True to enable tracking convergence behavior.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetTrackConvergenceBehavior(bool trackConvergenceBehavior)
            {
                this._trackConvergenceBehavior = trackConvergenceBehavior;
                return this;
            }

            /// <summary>
            /// Converts the given <paramref name="distanceMetricName"/> to a member of the
            /// <see cref="DistanceMetric"/> enum.
            /// Throws an <see cref="ArgumentException"/> if conversion fails.
            /// </summary>
            /// <param name="distanceMetricName">String representation of a <see cref="DistanceMetric"/>
            /// value.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetDistanceMetric(string distanceMetricName)
            {
                if (!Enum.TryParse(distanceMetricName, out DistanceMetric metric))
                {
                    var knownMetricMembers = string.Join("; ", Enum.GetNames(typeof(DistanceMetric)));
                    throw new ArgumentException(
                        $"Cannot convert {distanceMetricName} to a member of the DistanceMetric enum. Known values are: {{{knownMetricMembers}}}");
                }

                this._distanceMetric = metric;
                return this;
            }

            /// <summary>
            /// Sets the path to the status file directory. Default is PATH_TO_CURRENT_DIRECTORY/status.
            /// </summary>
            /// <param name="directory">Absolute path to status file directory.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder"/> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetStatusFileDirectory(string directory)
            {
                this._statusFileDirectory = directory;
                return this;
            }

            /// <summary>
            /// Sets the value indicating whether old status files should be zipped instead of overwritten.
            /// Default is <c>false</c>.
            /// </summary>
            /// <param name="zip">Whether old status files should be zipped instead of overwritten.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder"/> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetZipOldStatusFiles(bool zip)
            {
                this._zipOldStatusFiles = zip;
                return this;
            }

            /// <summary>
            /// Sets the path to the log file. Default is PATH_TO_CURRENT_DIRECTORY/tunerLog.txt.
            /// </summary>
            /// <param name="logFilePath">Absolute path where the log file should be written to.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder"/> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetLogFilePath(string logFilePath)
            {
                this._logFilePath = logFilePath;
                return this;
            }

            /// <summary>
            /// Sets a value indicating whether the generation history logged at the end of the tuning should include
            /// average scores on the complete instance sets. Default is false.
            /// </summary>
            /// <param name="score">Whether to include average scores on the complete instance sets.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder"/> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetScoreGenerationHistory(bool score)
            {
                this._scoreGenerationHistory = score;
                return this;
            }

            /// <summary>
            /// Sets whether a default value genome is added to the population.
            /// </summary>
            /// <param name="addDefaultGenome">Whether or not to add a default value genome in the population.</param>
            /// <returns>The <see cref="AlgorithmTunerConfigurationBuilder" /> in its new state.</returns>
            public AlgorithmTunerConfigurationBuilder SetAddDefaultGenome(bool addDefaultGenome)
            {
                this._addDefaultGenome = addDefaultGenome;
                return this;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Builds a <see cref="AlgorithmTunerConfiguration"/> using the provided
            /// <see cref="AlgorithmTunerConfiguration"/> as fallback.
            /// </summary>
            /// <param name="fallback">Used if a property is not set for this
            /// <see cref="AlgorithmTunerConfigurationBuilder"/>.
            /// May be null. In that case, defaults are used as fallback.</param>
            /// <returns>The build <see cref="AlgorithmTunerConfiguration"/>.</returns>
            private AlgorithmTunerConfiguration BuildWithFallback(AlgorithmTunerConfiguration fallback)
            {
                // Create new configuration.
                var configuration = new AlgorithmTunerConfiguration();

                // Set all properties.
                // If the builder does not specify a property, use fallback. If fallback is null, use default.
                configuration.StrictCompatibilityCheck =
                    this.StrictCompatibilityCheck ?? fallback?.StrictCompatibilityCheck ?? true;
                configuration.EnableRacing = this._enableRacing ?? fallback?.EnableRacing ?? DefaultEnableRacing;

                configuration.PopulationSize =
                    this._populationSize ?? fallback?.PopulationSize ?? DefaultPopulationSize;
                configuration.EvaluationLimit =
                    this._evaluationLimit ?? fallback?.EvaluationLimit ?? DefaultEvaluationLimit;
                configuration.Generations = this._generations ?? fallback?.Generations ?? 100;
                configuration.MaxGenomeAge = this._maxGenomeAge ?? fallback?.MaxGenomeAge ?? 3;
                configuration.MaximumMiniTournamentSize =
                    this._maximumMiniTournamentSize ?? fallback?.MaximumMiniTournamentSize ?? DefaultMaximumMiniTournamentSize;

                configuration.ContinuousOptimizationMethod =
                    this._continuousOptimizationMethod ?? fallback?.ContinuousOptimizationMethod ?? DefaultContinuousOptimizationMethod;
                configuration.MaximumNumberGgaGenerations =
                    this._maximumNumberGgaGenerations ?? fallback?.MaximumNumberGgaGenerations ?? DefaultMaximumNumberGgaGenerations;
                configuration.MaximumNumberGgaGenerationsWithSameIncumbent =
                    this._maximumNumberGgaGenerationsWithSameIncumbent
                    ?? fallback?.MaximumNumberGgaGenerationsWithSameIncumbent
                    ?? DefaultMaximumNumberGgaGenerationsWithSameIncumbent;

                configuration.TournamentWinnerPercentage =
                    this._tournamentWinnerPercentage ?? fallback?.TournamentWinnerPercentage ?? 0.125;
                configuration.CpuTimeout =
                    this._cpuTimeout ?? fallback?.CpuTimeout ?? TimeSpan.FromMilliseconds(int.MaxValue);

                configuration.CrossoverSwitchProbability =
                    this._crossoverSwitchProbability ?? fallback?.CrossoverSwitchProbability ?? 0.1;
                configuration.MutationRate = this._mutationRate ?? fallback?.MutationRate ?? DefaultMutationRate;
                configuration.MutationVariancePercentage =
                    this._mutationVariancePercentage ?? fallback?.MutationVariancePercentage ?? 0.1;
                configuration.MaxRepairAttempts = this._maxRepairAttempts ?? fallback?.MaxRepairAttempts ?? 20;

                configuration.MaximumNumberConsecutiveFailuresPerEvaluation =
                    this._maximumNumberConsecutiveFailuresPerEvaluation ?? fallback?.MaximumNumberConsecutiveFailuresPerEvaluation ?? 3;

                configuration.StartNumInstances = this._startNumInstances ?? fallback?.StartNumInstances ?? 5;
                configuration.EndNumInstances = this._endNumInstances ?? fallback?.EndNumInstances ?? 100;
                configuration.GoalGeneration = this._goalGeneration ?? fallback?.GoalGeneration ?? 74;

                var fallbackHoconFile = new FileInfo("app.hocon");
                var hoconFallbackString = fallbackHoconFile.Exists ? File.ReadAllText(fallbackHoconFile.FullName) : "";
                configuration.AkkaConfiguration =
                    this._akkaConfiguration ?? fallback?.AkkaConfiguration ?? ConfigurationFactory.ParseString(hoconFallbackString);

                configuration.Verbosity = this._verbosity ?? fallback?.Verbosity ?? VerbosityLevel.Info;
                configuration.StatusFileDirectory = this._statusFileDirectory ?? fallback?.StatusFileDirectory ?? DefaultStatusFileDirectory;
                configuration.ZipOldStatusFiles = this._zipOldStatusFiles ?? fallback?.ZipOldStatusFiles ?? false;

                configuration.LogFilePath =
                    this._logFilePath ?? fallback?.LogFilePath ?? PathUtils.GetAbsolutePathFromCurrentDirectory("tunerLog.txt");
                configuration.ScoreGenerationHistory =
                    this._scoreGenerationHistory ?? fallback?.ScoreGenerationHistory ?? false;

                configuration.TrainModel = this._trainModel ?? fallback?.TrainModel ?? false;
                configuration.EngineeredPopulationRatio =
                    this._engineeredPopulationRatio ?? fallback?.EngineeredPopulationRatio ?? 0;
                configuration.TopPerformerThreshold =
                    this._topPerformerThreshold ?? fallback?.TopPerformerThreshold ?? 0.1;
                configuration.StartEngineeringAtIteration =
                    this._startEngineeringAtIteration ?? fallback?.StartEngineeringAtIteration ?? 3;
                configuration.PopulationMutantRatio =
                    this._populationMutantRatio ?? fallback?.PopulationMutantRatio ?? DefaultPopulationMutantRatio;
                configuration.EnableSexualSelection =
                    this._enableSexualSelection ?? fallback?.EnableSexualSelection ?? false;
                configuration.CrossoverProbabilityCompetitive =
                    this._crossoverProbabilityCompetitive ?? fallback?.CrossoverProbabilityCompetitive ?? 0.5;
                configuration.HammingDistanceRelativeThreshold =
                    this._hammingDistanceRelativeThreshold ?? fallback?.HammingDistanceRelativeThreshold ?? 0.01;
                configuration.TargetSamplingSize = this._targetSampleSize ?? fallback?.TargetSamplingSize ?? DefaultTargetSamplingSize;
                configuration.MaxRanksCompensatedByDistance =
                    this._maxRanksCompensatedByDistance ?? fallback?.MaxRanksCompensatedByDistance ?? DefaultMaxRanksCompensatedByDistance;
                configuration.FeatureSubsetRatioForDistanceComputation =
                    this._featureSubsetRatioForDistance ?? fallback?.FeatureSubsetRatioForDistanceComputation ?? 0.3;
                configuration.TrackConvergenceBehavior =
                    this._trackConvergenceBehavior ?? fallback?.TrackConvergenceBehavior ?? false;
                configuration.DistanceMetric =
                    this._distanceMetric ?? fallback?.DistanceMetric ?? DistanceMetric.HammingDistance;

                configuration._maximumNumberParallelEvaluations =
                    this._maximumNumberParallelEvaluations
                    ?? fallback?._maximumNumberParallelEvaluations
                    ?? throw new InvalidOperationException("You must set a maximum number of parallel evaluations!");

                configuration.MaximumNumberParallelThreads =
                    this._maximumNumberParallelThreads
                    ?? fallback?.MaximumNumberParallelThreads
                    ?? configuration._maximumNumberParallelEvaluations;

                configuration.AddDefaultGenome =
                    this._addDefaultGenome
                    ?? fallback?.AddDefaultGenome
                    ?? AlgorithmTunerConfigurationBuilder.DefaultAddDefaultGenome;

                this.CreateDetailedConfigurations(fallback, configuration);

                // Make sure new configuration is fine.
                configuration.Validate();

                // Return the new configuration.
                return configuration;
            }

            /// <summary>
            /// Creates the detailed configurations.
            /// </summary>
            /// <param name="fallback">The fallback.</param>
            /// <param name="configuration">The configuration.</param>
            private void CreateDetailedConfigurations(AlgorithmTunerConfiguration fallback, AlgorithmTunerConfiguration configuration)
            {
                // First, the ones that were specified right now
                foreach (var key in this._detailedConfigurationBuilders.Keys)
                {
                    ConfigurationBase detailedFallback = null;
                    fallback?.DetailedConfigurations.TryGetValue(key, out detailedFallback);
                    var detailedConfig = this._detailedConfigurationBuilders[key].BuildWithFallback(detailedFallback);
                    configuration.DetailedConfigurations.Add(key, detailedConfig);
                }

                // Then add fallback configurations
                foreach (var element in fallback?.DetailedConfigurations ?? new Dictionary<string, ConfigurationBase>())
                {
                    if (!configuration.DetailedConfigurations.ContainsKey(element.Key))
                    {
                        configuration.DetailedConfigurations.Add(element.Key, element.Value);
                    }
                }
            }

            #endregion
        }
    }
}