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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;

    /// <summary>
    /// Relevant parameters for <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/>.
    /// </summary>
    /// <seealso cref="Optano.Algorithm.Tuner.Configuration.ConfigurationBase" />
    public class DifferentialEvolutionStrategyConfiguration : StrategyConfigurationBase<DifferentialEvolutionStrategyConfiguration>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentialEvolutionStrategyConfiguration"/> class.
        /// </summary>
        /// <param name="maximumNumberGenerations">
        /// The maximum number of generations per differential evolution phase.
        /// </param>
        /// <param name="focusOnIncumbent">
        /// Whether JADE should focus on improving the incumbent.
        /// If not, it works on the complete population.
        /// </param>
        /// <param name="minimumDomainSize">
        /// The minimum size an integer domain needs to have to be handled as continuous.
        /// </param>
        /// <param name="replacementRate">
        /// The percentage of competitive genomes which get replaced by the best search points found by
        /// the continuous optimization method at the end of a phase if <paramref name="focusOnIncumbent"/> is
        /// <c>true</c>, expressed by a value in [0, 1].
        /// </param>
        /// <param name="fixInstances">
        /// Whether the set of instances to evaluate on should stay the same during a
        /// differential evolution phase.
        /// </param>
        /// <param name="differentialEvolutionConfiguration">
        /// The configuration for the differential evolution runner.
        /// </param>
        private DifferentialEvolutionStrategyConfiguration(
            bool focusOnIncumbent,
            int maximumNumberGenerations,
            int minimumDomainSize,
            double replacementRate,
            bool fixInstances,
            DifferentialEvolutionConfiguration differentialEvolutionConfiguration)
            : base(focusOnIncumbent, maximumNumberGenerations, minimumDomainSize, replacementRate, fixInstances)
        {
            this.DifferentialEvolutionConfiguration = differentialEvolutionConfiguration;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the configuration for the differential evolution runner.
        /// </summary>
        public DifferentialEvolutionConfiguration DifferentialEvolutionConfiguration { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks whether two <see cref="DifferentialEvolutionStrategyConfiguration"/>s are compatible for one to be
        /// used in a continued tuning based on a tuning using the other.
        /// </summary>
        /// <param name="other">Configuration used for the start of tuning.</param>
        /// <returns>True iff this configuration can be used for continued tuning.</returns>
        public override bool IsCompatible(ConfigurationBase other)
        {
            if (!(other is DifferentialEvolutionStrategyConfiguration otherConfig))
            {
                return false;
            }

            return base.IsCompatible(other)
                   && this.DifferentialEvolutionConfiguration.IsCompatible(otherConfig.DifferentialEvolutionConfiguration);
        }

        /// <summary>
        /// Checks whether two <see cref="DifferentialEvolutionStrategyConfiguration"/>s are compatible in a technical
        /// sense for one to be used in a continued tuning based on a tuning using the other.
        /// <para>The difference to <see cref="IsCompatible"/> is that this function only checks for technical
        /// compatibility and does not consider whether the combination of configurations is compatible in the sense
        /// that the continued tuning looks like a longer single tuning.</para>
        /// </summary>
        /// <param name="other">Configuration used for the start of tuning.</param>
        /// <returns>True iff this configuration can be used for continued tuning.</returns>
        public override bool IsTechnicallyCompatible(ConfigurationBase other)
        {
            if (!base.IsTechnicallyCompatible(other))
            {
                return false;
            }

            if (!(other is DifferentialEvolutionStrategyConfiguration otherConfig))
            {
                return false;
            }

            // If we are in the middle of a JADE phase running on (quasi-)continuous parameters only,
            // a change in definition is a problem.
            return this.MinimumDomainSize == otherConfig.MinimumDomainSize
                   && this.DifferentialEvolutionConfiguration.IsTechnicallyCompatible(otherConfig.DifferentialEvolutionConfiguration);
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

            descriptionBuilder.AppendLine($"maxGenerationsPerDePhase : {this.MaximumNumberGenerations}");
            descriptionBuilder.AppendLine($"minDomainSize : {this.MinimumDomainSize}");
            descriptionBuilder.AppendLine($"focusOnIncumbent : {this.FocusOnIncumbent}");
            descriptionBuilder.AppendLine(Indent + $"replacementRate : {this.ReplacementRate}");
            descriptionBuilder.AppendLine($"fixInstances : {this.FixInstances}");
            descriptionBuilder.Append(DescribeSubConfiguration("JADE", this.DifferentialEvolutionConfiguration));

            return descriptionBuilder.ToString();
        }

        #endregion

        /// <summary>
        /// The <see cref="DifferentialEvolutionStrategyConfiguration"/> builder.
        /// </summary>
        [SuppressMessage(
            "NDepend",
            "ND1305:NestedTypesShouldNotBeVisible",
            Justification = "This type is part of the Builder Pattern.")]
        public class DifferentialEvolutionStrategyConfigurationBuilder
            : StrategyConfigurationBuilderBase<DifferentialEvolutionStrategyConfiguration, DifferentialEvolutionStrategyConfigurationBuilder>
        {
            #region Fields

            /// <summary>
            /// Builder which creates <see cref="DifferentialEvolutionConfiguration"/>.
            /// </summary>
            private DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder _differentialEvolutionConfigurationBuilder;

            #endregion

            #region Properties

            /// <inheritdoc />
            protected override DifferentialEvolutionStrategyConfigurationBuilder Instance => this;

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// Sets the percentage of competitive genomes which get replaced by the best search points found by
            /// the continuous optimization method at the end of a phase, expressed by a value in [0, 0.5].
            /// <para>A replacement rate of 0 indicates that only the incumbent itself should be replaced.</para>
            /// <para>Default is 0.</para>
            /// </summary>
            /// <param name="rate">A rate in [0, 0.5].</param>
            /// <returns>The <see cref="DifferentialEvolutionStrategyConfigurationBuilder"/> in its new state.</returns>
            public DifferentialEvolutionStrategyConfigurationBuilder SetReplacementRate(double rate)
            {
                if (rate < 0 || rate > 0.5)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(rate),
                        $"Replacement rate must be in [0, 0.5], but was {rate}.");
                }

                this.ReplacementRate = rate;
                return this;
            }

            /// <summary>
            /// Sets relevant parameters for the differential evolution algorithm.
            /// </summary>
            /// <param name="builder">
            /// A <see cref="DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder"/>.
            /// </param>
            /// <returns>The <see cref="DifferentialEvolutionStrategyConfigurationBuilder" /> in its new state.</returns>
            public DifferentialEvolutionStrategyConfigurationBuilder SetDifferentialEvolutionConfigurationBuilder(
                DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder builder)
            {
                this._differentialEvolutionConfigurationBuilder = builder ?? throw new ArgumentNullException(nameof(builder));
                return this;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Builds a <see cref="DifferentialEvolutionStrategyConfiguration"/> using the provided
            /// Builds a <see cref="DifferentialEvolutionStrategyConfiguration"/> using the provided
            /// <see cref="DifferentialEvolutionStrategyConfiguration"/> as fallback.
            /// </summary>
            /// <param name="fallback">Used if a property is not set for this
            /// <see cref="DifferentialEvolutionStrategyConfiguration"/>.
            /// May be null. In that case, defaults are used as fallback.</param>
            /// <returns>The build <see cref="DifferentialEvolutionStrategyConfiguration"/>.</returns>
            protected override DifferentialEvolutionStrategyConfiguration BuildWithFallback(DifferentialEvolutionStrategyConfiguration fallback)
            {
                var fallbackAlgorithmConfiguration = fallback?.DifferentialEvolutionConfiguration;
                var algorithmConfiguration =
                    this._differentialEvolutionConfigurationBuilder?.BuildWithFallback(fallbackAlgorithmConfiguration)
                    ?? fallbackAlgorithmConfiguration;

                if (algorithmConfiguration == null)
                {
                    throw new InvalidOperationException(
                        "Either current builder should have a differential evolution configuration, or fallback should exist.");
                }

                return new DifferentialEvolutionStrategyConfiguration(
                    this.FocusOnIncumbent ?? fallback?.FocusOnIncumbent ?? DefaultFocusOnIncumbent,
                    this.MaximumNumberGenerations ?? fallback?.MaximumNumberGenerations ?? DefaultMaximumNumberGenerations,
                    this.MinimumDomainSize ?? fallback?.MinimumDomainSize ?? DefaultMinimumDomainSize,
                    this.ReplacementRate ?? fallback?.ReplacementRate ?? DefaultReplacementRate,
                    this.FixInstances ?? fallback?.FixInstances ?? DefaultFixInstances,
                    algorithmConfiguration);
            }

            #endregion
        }
    }
}