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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;

    /// <summary>
    /// Wraps relevant parameters for strategies inheriting from
    /// <see cref="CovarianceMatrixAdaptationStrategyBase{TSearchPoint,TInstance,TResult}"/>.
    /// </summary>
    public class CovarianceMatrixAdaptationStrategyConfiguration : StrategyConfigurationBase<CovarianceMatrixAdaptationStrategyConfiguration>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CovarianceMatrixAdaptationStrategyConfiguration"/> class.
        /// </summary>
        /// <param name="focusOnIncumbent">
        /// Whether the CMA-ES should focus on improving the continuous parameters of the incumbent.
        /// If not, it modifies all parameters of the complete competitive population.
        /// </param>
        /// <param name="maximumNumberGenerations">
        /// The maximum number of generations per CMA-ES phase.
        /// </param>
        /// <param name="minimumDomainSize">
        /// The minimum size an integer domain needs to have to be handled as continuous.
        /// </param>
        /// <param name="replacementRate">
        /// The percentage of competitive genomes which get replaced by the best search points found by
        /// the continuous optimization method at the end of a phase, expressed by a value in [0, 1].
        /// </param>
        /// <param name="fixInstances">
        /// Whether the set of instances to evaluate on should stay the same during a
        /// CMA-ES phase.
        /// </param>
        /// <param name="initialStepSize">
        /// The step size with which to start CMA-ES phases.
        /// </param>
        private CovarianceMatrixAdaptationStrategyConfiguration(
            bool focusOnIncumbent,
            int maximumNumberGenerations,
            int minimumDomainSize,
            double replacementRate,
            bool fixInstances,
            double initialStepSize)
            : base(focusOnIncumbent, maximumNumberGenerations, minimumDomainSize, replacementRate, fixInstances)
        {
            this.InitialStepSize = initialStepSize;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the step size with which to start CMA-ES phases.
        /// A larger step size results in exploration, a smaller one in exploitation.
        /// <para>
        /// Internally, all values are mapped into a [0,10] hypercube.
        /// </para>
        /// </summary>
        public double InitialStepSize { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks whether two <see cref="CovarianceMatrixAdaptationStrategyConfiguration"/>s are compatible for one to be
        /// used in a continued tuning based on a tuning using the other.
        /// </summary>
        /// <param name="other">Configuration used for the start of tuning.</param>
        /// <returns>True iff this configuration can be used for continued tuning.</returns>
        public override bool IsCompatible(ConfigurationBase other)
        {
            if (!(other is CovarianceMatrixAdaptationStrategyConfiguration otherConfig))
            {
                return false;
            }

            return base.IsCompatible(other)
                   && Math.Abs(this.InitialStepSize - otherConfig.InitialStepSize) < CompatibilityTolerance;
        }

        /// <inheritdoc />
        public override bool IsTechnicallyCompatible(ConfigurationBase other)
        {
            if (!base.IsTechnicallyCompatible(other))
            {
                return false;
            }

            if (!(other is CovarianceMatrixAdaptationStrategyConfiguration otherConfig))
            {
                return false;
            }

            // It is not possible to change search point type.
            if (this.FocusOnIncumbent != otherConfig.FocusOnIncumbent)
            {
                return false;
            }

            // If we are in the middle of a CMA-ES phase running on (quasi-)continuous parameters only,
            // a change in definition is a problem.
            if (this.FocusOnIncumbent && this.MinimumDomainSize != otherConfig.MinimumDomainSize)
            {
                return false;
            }

            return true;
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

            descriptionBuilder.AppendLine($"maxGenerationsPerCmaEsPhase : {this.MaximumNumberGenerations}");
            descriptionBuilder.AppendLine($"focusOnIncumbent : {this.FocusOnIncumbent}");
            descriptionBuilder.AppendLine(Indent + $"minDomainSize : {this.MinimumDomainSize}");
            descriptionBuilder.AppendLine(Indent + $"replacementRate : {this.ReplacementRate}");
            descriptionBuilder.AppendLine($"fixInstances : {this.FixInstances}");
            descriptionBuilder.AppendLine($"initialStepSize : {this.InitialStepSize}");

            return descriptionBuilder.ToString();
        }

        #endregion

        /// <summary>
        /// The <see cref="CovarianceMatrixAdaptationStrategyConfiguration"/> builder.
        /// </summary>
        [SuppressMessage(
            "NDepend",
            "ND1305:NestedTypesShouldNotBeVisible",
            Justification = "This type is part of the Builder Pattern.")]
        public class CovarianceMatrixAdaptationStrategyConfigurationBuilder
            : StrategyConfigurationBuilderBase<CovarianceMatrixAdaptationStrategyConfiguration, CovarianceMatrixAdaptationStrategyConfigurationBuilder
            >
        {
            #region Constants

            // TODO #23945: Find good value.
            /// <summary>
            /// The default value for <see cref="InitialStepSize"/>.
            /// </summary>
            /// <remarks>
            /// <see cref="BoundedSearchPoint"/> looks into [0, 10] and we aim to have the optimum in the initial cube
            /// distributionMean +- 3 * step size.
            /// If improving a given solution (e.g. when hybridizing), a smaller step size, e.g. 0.5, is useful.
            /// </remarks>
            public const double DefaultInitialStepSize = 3;

            #endregion

            #region Fields

            /// <summary>
            /// The value to set for <see cref="InitialStepSize"/>.
            /// </summary>
            private double? _initialStepSize;

            #endregion

            #region Properties

            /// <inheritdoc />
            protected override CovarianceMatrixAdaptationStrategyConfigurationBuilder Instance => this;

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// Sets the percentage of competitive genomes which get replaced by the best search points found by
            /// the continuous optimization method at the end of a phase, expressed by a value in [0, 1].
            /// <para>A replacement rate of 0 indicates that only the incumbent itself should be replaced.</para>
            /// <para>Default is 0.</para>
            /// </summary>
            /// <param name="rate">A rate in [0, 1].</param>
            /// <returns>The <see cref="CovarianceMatrixAdaptationStrategyConfigurationBuilder"/> in its new state.</returns>
            public CovarianceMatrixAdaptationStrategyConfigurationBuilder SetReplacementRate(double rate)
            {
                if (rate <= 0 || rate > 1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(rate),
                        $"Replacement rate must be in (0, 1], but was {rate}.");
                }

                this.ReplacementRate = rate;
                return this;
            }

            /// <summary>
            /// Sets the step size with which to start CMA-ES phases.
            /// A larger step size results in exploration, a smaller one in exploitation.
            /// <para>
            /// Internally, all values are mapped into a [0,10] hypercube.
            /// </para>
            /// <para>Default is 1.</para>
            /// </summary>
            /// <param name="size">The positive step size to start with.</param>
            /// <returns>The <see cref="CovarianceMatrixAdaptationStrategyConfigurationBuilder"/> in its new state.</returns>
            public CovarianceMatrixAdaptationStrategyConfigurationBuilder SetInitialStepSize(double size)
            {
                if (size <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(size),
                        $"Initial step size must be positive, but was {size}.");
                }

                this._initialStepSize = size;
                return this;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Builds a <see cref="CovarianceMatrixAdaptationStrategyConfiguration"/> using the provided
            /// <see cref="CovarianceMatrixAdaptationStrategyConfiguration"/> as fallback.
            /// </summary>
            /// <param name="fallback">Used if a property is not set for this
            /// <see cref="CovarianceMatrixAdaptationStrategyConfiguration"/>.
            /// May be null. In that case, defaults are used as fallback.</param>
            /// <returns>The build <see cref="CovarianceMatrixAdaptationStrategyConfiguration"/>.</returns>
            protected override CovarianceMatrixAdaptationStrategyConfiguration BuildWithFallback(
                CovarianceMatrixAdaptationStrategyConfiguration fallback)
            {
                return new CovarianceMatrixAdaptationStrategyConfiguration(
                    this.FocusOnIncumbent ?? fallback?.FocusOnIncumbent ?? DefaultFocusOnIncumbent,
                    this.MaximumNumberGenerations ?? fallback?.MaximumNumberGenerations ?? DefaultMaximumNumberGenerations,
                    this.MinimumDomainSize ?? fallback?.MinimumDomainSize ?? DefaultMinimumDomainSize,
                    this.ReplacementRate ?? fallback?.ReplacementRate ?? DefaultReplacementRate,
                    this.FixInstances ?? fallback?.FixInstances ?? DefaultFixInstances,
                    this._initialStepSize ?? fallback?.InitialStepSize ?? DefaultInitialStepSize);
            }

            #endregion
        }
    }
}