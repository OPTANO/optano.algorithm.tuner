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

namespace Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    using Optano.Algorithm.Tuner.Configuration;

    /// <summary>
    /// Configuration parameters for the differential evolution (DE) variant JADE.
    /// <para>
    /// Based on J. Zhang and A. C. Sanderson, "JADE: Adaptive Differential Evolution With Optional External Archive,"
    /// in IEEE Transactions on Evolutionary Computation, vol. 13, no. 5, pp. 945-958, Oct. 2009.
    /// </para>
    /// </summary>
    public class DifferentialEvolutionConfiguration : ConfigurationBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentialEvolutionConfiguration"/> class.
        /// </summary>
        /// <param name="bestPercentage">
        /// The percentage of population members which may be used as best member in the current-to-pbest mutation
        /// strategy. Often called p in literature.
        /// </param>
        /// <param name="initialMeanMutationFactor">
        /// The initial value of the mean mutation factor, often called mu_F in literature.
        /// </param>
        /// <param name="initialMeanCrossoverRate">
        /// The initial value of the mean crossover rate, often called mu_{CR} in literature.
        /// </param>
        /// <param name="learningRate">
        /// The learning rate for the means, often called c in literature.
        /// </param>
        private DifferentialEvolutionConfiguration(
            double bestPercentage,
            double initialMeanMutationFactor,
            double initialMeanCrossoverRate,
            double learningRate)
        {
            this.BestPercentage = bestPercentage;
            this.InitialMeanMutationFactor = initialMeanMutationFactor;
            this.InitialMeanCrossoverRate = initialMeanCrossoverRate;
            this.LearningRate = learningRate;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the percentage of population members which may be used as best member in the current-to-pbest mutation
        /// strategy. Often called p in literature.
        /// </summary>
        public double BestPercentage { get; }

        /// <summary>
        /// Gets the initial value of the mean mutation factor, often called mu_F in literature.
        /// </summary>
        public double InitialMeanMutationFactor { get; }

        /// <summary>
        /// Gets the initial value of the mean crossover constant, often called mu_{CR} in literature.
        /// </summary>
        public double InitialMeanCrossoverRate { get; }

        /// <summary>
        /// Gets the learning rate for the means, often called c in literature.
        /// </summary>
        public double LearningRate { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks whether two <see cref="ConfigurationBase"/>s are compatible for one to be used in a continued run
        /// based on a run using the other.
        /// </summary>
        /// <param name="other">Configuration used for the start of tuning.</param>
        /// <returns>True iff this configuration can be used for continued run.</returns>
        public override bool IsCompatible(ConfigurationBase other)
        {
            if (!(other is DifferentialEvolutionConfiguration otherConfig))
            {
                return false;
            }

            // Initial rates do not have to be used in a continued run, so they don't have to be checked.
            return
                Math.Abs(this.BestPercentage - otherConfig.BestPercentage) < ConfigurationBase.CompatibilityTolerance
                && Math.Abs(this.LearningRate - otherConfig.LearningRate) < ConfigurationBase.CompatibilityTolerance;
        }

        /// <summary>
        /// Checks whether two <see cref="ConfigurationBase"/>s are compatible in a technical sense for one
        /// to be used in a continued run based on a run using the other.
        /// <para>The difference to <see cref="ConfigurationBase.IsCompatible"/> is that this function only checks for technical
        /// compatibility and does not consider whether the combination of configurations is compatible in the sense
        /// that the continued run looks like a longer single run.</para>
        /// </summary>
        /// <param name="other">Configuration used for the start of run.</param>
        /// <returns>True iff this configuration can be used for continued run.</returns>
        public override bool IsTechnicallyCompatible(ConfigurationBase other)
        {
            return other is DifferentialEvolutionConfiguration;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var descriptionBuilder = new StringBuilder();

            descriptionBuilder.AppendLine($"bestPercentage : {this.BestPercentage}");
            descriptionBuilder.AppendLine($"meanMutationFactor : {this.InitialMeanMutationFactor}");
            descriptionBuilder.AppendLine($"meanCrossoverRate : {this.InitialMeanCrossoverRate}");
            descriptionBuilder.AppendLine($"learningRate : {this.LearningRate}");

            return descriptionBuilder.ToString();
        }

        #endregion

        /// <summary>
        /// The <see cref="DifferentialEvolutionConfiguration"/> builder.
        /// </summary>
        [SuppressMessage(
            "NDepend",
            "ND1305:NestedTypesShouldNotBeVisible",
            Justification = "This type is part of the Builder Pattern.")]
        public class DifferentialEvolutionConfigurationBuilder : IConfigBuilder<DifferentialEvolutionConfiguration>
        {
            #region Constants

            /// <summary>
            /// The default value for <see cref="BestPercentage"/>.
            /// </summary>
            public const double DefaultBestPercentage = 0.1;

            /// <summary>
            /// The default value for <see cref="InitialMeanMutationFactor"/>.
            /// <para>
            /// Taken from J. Zhang and A. C. Sanderson,
            /// "JADE: Adaptive Differential Evolution With Optional External Archive," in IEEE Transactions on Evolutionary
            /// Computation, vol. 13, no. 5, pp. 945-958, Oct. 2009.
            /// </para>
            /// </summary>
            public const double DefaultInitialMeanMutationFactor = 0.5;

            /// <summary>
            /// The default value for <see cref="InitialMeanCrossoverRate"/>.
            /// <para>
            /// Taken from J. Zhang and A. C. Sanderson,
            /// "JADE: Adaptive Differential Evolution With Optional External Archive," in IEEE Transactions on Evolutionary
            /// Computation, vol. 13, no. 5, pp. 945-958, Oct. 2009.
            /// </para>
            /// </summary>
            public const double DefaultInitialMeanCrossoverRate = 0.5;

            /// <summary>
            /// The default value for <see cref="LearningRate"/>.
            /// </summary>
            public const double DefaultLearningRate = 0.1;

            #endregion

            #region Fields

            /// <summary>
            /// The value to set for <see cref="BestPercentage"/>.
            /// </summary>
            private double? _bestPercentage;

            /// <summary>
            /// The value to set for <see cref="InitialMeanMutationFactor"/>.
            /// </summary>
            private double? _initialMeanMutationFactor;

            /// <summary>
            /// The value to set for <see cref="InitialMeanCrossoverRate"/>.
            /// </summary>
            private double? _initialMeanCrossoverRate;

            /// <summary>
            /// The value to set for <see cref="LearningRate"/>.
            /// </summary>
            private double? _learningRate;

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// Builds a <see cref="DifferentialEvolutionConfiguration"/> using the provided
            /// <see cref="ConfigurationBase"/> as fallback.
            /// </summary>
            /// <param name="fallback">Used if a property is not set for this
            /// <see cref="DifferentialEvolutionConfiguration"/>.
            /// May be null. In that case, defaults are used as fallback.</param>
            /// <returns>The built <see cref="DifferentialEvolutionConfiguration"/>.</returns>
            public DifferentialEvolutionConfiguration BuildWithFallback(ConfigurationBase fallback)
            {
                return this.BuildWithFallback(CastToConfigurationType<DifferentialEvolutionConfiguration>(fallback));
            }

            /// <summary>
            /// Sets the percentage of population members which may be used as best member in the current-to-pbest mutation
            /// strategy.
            /// Often called p in literature.
            /// <para>
            /// Default is 0.1.
            /// </para>
            /// <para>
            /// JADE works best with a value between 0.05 and 0.2.
            /// </para>
            /// </summary>
            /// <param name="percentage">Percentage as a value between 0 and 1 (both excluded).</param>
            /// <returns>The <see cref="DifferentialEvolutionConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the percentage is not a value in (0, 1].</exception>
            public DifferentialEvolutionConfigurationBuilder SetBestPercentage(double percentage)
            {
                if (percentage <= 0 || percentage > 1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(percentage),
                        $"Best percentage must be in (0, 1], but was {percentage}.");
                }

                this._bestPercentage = percentage;
                return this;
            }

            /// <summary>
            /// Sets the initial value of the mean mutation factor, often called mu_F in literature.
            /// Default is 0.5.
            /// </summary>
            /// <param name="factor">Factor as a value between 0 and 1 (both excluded).</param>
            /// <returns>The <see cref="DifferentialEvolutionConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the factor is not a value in [0, 1].</exception>
            public DifferentialEvolutionConfigurationBuilder SetInitialMeanMutationFactor(double factor)
            {
                if (factor < 0 || factor > 1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(factor),
                        $"Initial mean mutation factor must be in [0, 1], but was {factor}.");
                }

                this._initialMeanMutationFactor = factor;
                return this;
            }

            /// <summary>
            /// Sets the initial value of the mean crossover rate, often called mu_{CR} in literature.
            /// Default is 0.5.
            /// </summary>
            /// <param name="rate">Rate as a value between 0 and 1 (both excluded).</param>
            /// <returns>The <see cref="DifferentialEvolutionConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the rate is not a value in [0, 1].</exception>
            public DifferentialEvolutionConfigurationBuilder SetInitialMeanCrossoverRate(double rate)
            {
                if (rate < 0 || rate > 1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(rate),
                        $"Initial mean crossover rate must be in [0, 1], but was {rate}.");
                }

                this._initialMeanCrossoverRate = rate;
                return this;
            }

            /// <summary>
            /// Sets the learning rate for the means, often called c in literature.
            /// <para>
            /// Default is 0.1.
            /// </para>
            /// <para>
            /// JADE works best with a value between 0.05 and 0.2.
            /// </para>
            /// </summary>
            /// <param name="rate">Rate as a value between 0 and 1 (both excluded).</param>
            /// <returns>The <see cref="DifferentialEvolutionConfigurationBuilder" /> in its new state.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if the rate is not a value in [0, 1].</exception>
            public DifferentialEvolutionConfigurationBuilder SetLearningRate(double rate)
            {
                if (rate < 0 || rate > 1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(rate),
                        $"Learning rate must be in [0, 1], but was {rate}.");
                }

                this._learningRate = rate;
                return this;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Builds a <see cref="DifferentialEvolutionConfiguration"/> using the provided
            /// <see cref="DifferentialEvolutionConfiguration"/> as fallback.
            /// </summary>
            /// <param name="fallback">Used if a property is not set for this
            /// <see cref="DifferentialEvolutionConfiguration"/>.
            /// May be null. In that case, defaults are used as fallback.</param>
            /// <returns>The build <see cref="DifferentialEvolutionConfiguration"/>.</returns>
            private DifferentialEvolutionConfiguration BuildWithFallback(DifferentialEvolutionConfiguration fallback)
            {
                return new DifferentialEvolutionConfiguration(
                    this._bestPercentage ?? fallback?.BestPercentage ?? DefaultBestPercentage,
                    this._initialMeanMutationFactor ?? fallback?.InitialMeanMutationFactor ?? DefaultInitialMeanMutationFactor,
                    this._initialMeanCrossoverRate ?? fallback?.InitialMeanCrossoverRate ?? DefaultInitialMeanCrossoverRate,
                    this._learningRate ?? fallback?.LearningRate ?? DefaultLearningRate);
            }

            #endregion
        }
    }
}