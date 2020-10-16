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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies
{
    using System;

    using Optano.Algorithm.Tuner.Configuration;

    /// <summary>
    /// Basic structure for <see cref="IConfigBuilder{TConfiguration}"/>s building subclasses of
    /// <see cref="StrategyConfigurationBase{TConfiguration}"/>.
    /// </summary>
    /// <typeparam name="TConfiguration">
    /// The exact type of the <see cref="StrategyConfigurationBase{TConfiguration}"/> which is built.
    /// </typeparam>
    /// <typeparam name="TBuilder">
    /// The type of the implementing class.
    /// </typeparam>
    public abstract class StrategyConfigurationBuilderBase<TConfiguration, TBuilder> : IConfigBuilder<TConfiguration>
        where TConfiguration : StrategyConfigurationBase<TConfiguration>
        where TBuilder : StrategyConfigurationBuilderBase<TConfiguration, TBuilder>
    {
        #region Constants

        /// <summary>
        /// The default value for <see cref="StrategyConfigurationBase{TConfiguration}.FocusOnIncumbent"/>.
        /// </summary>
        public const bool DefaultFocusOnIncumbent = false;

        /// <summary>
        /// The default value for <see cref="StrategyConfigurationBase{TConfiguration}.MaximumNumberGenerations"/>.
        /// </summary>
        public const int DefaultMaximumNumberGenerations = int.MaxValue;

        /// <summary>
        /// The default value for <see cref="StrategyConfigurationBase{TConfiguration}.MinimumDomainSize"/>.
        /// </summary>
        public const int DefaultMinimumDomainSize = 150;

        /// <summary>
        /// The default value for <see cref="StrategyConfigurationBase{TConfiguration}.ReplacementRate"/>.
        /// </summary>
        public const double DefaultReplacementRate = 0;

        /// <summary>
        /// The default value for <see cref="StrategyConfigurationBase{TConfiguration}.FixInstances"/>.
        /// </summary>
        public const bool DefaultFixInstances = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the value to set for <see cref="StrategyConfigurationBase{TConfiguration}.FocusOnIncumbent"/>.
        /// </summary>
        protected bool? FocusOnIncumbent { get; set; }

        /// <summary>
        /// Gets or sets the value to set for
        /// <see cref="StrategyConfigurationBase{TConfiguration}.MaximumNumberGenerations"/>.
        /// </summary>
        protected int? MaximumNumberGenerations { get; set; }

        /// <summary>
        /// Gets or sets the value to set for
        /// <see cref="StrategyConfigurationBase{TConfiguration}.MinimumDomainSize"/>.
        /// </summary>
        protected int? MinimumDomainSize { get; set; }

        /// <summary>
        /// Gets or sets the value to set for
        /// <see cref="StrategyConfigurationBase{TConfiguration}.ReplacementRate"/>.
        /// </summary>
        protected double? ReplacementRate { get; set; }

        /// <summary>
        /// Gets or sets the value to set for <see cref="StrategyConfigurationBase{TConfiguration}.FixInstances"/>.
        /// </summary>
        protected bool? FixInstances { get; set; }

        /// <summary>
        /// Gets the current object.
        /// </summary>
        protected abstract TBuilder Instance { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Builds a <typeparamref name="TConfiguration"/> using the provided <see cref="ConfigurationBase"/> as
        /// fallback.
        /// </summary>
        /// <param name="fallback">Used if a property is not set for this
        /// <see cref="StrategyConfigurationBuilderBase{TBuilder, TConfiguration}"/>.
        /// May be null. In that case, defaults are used as fallback.</param>
        /// <returns>The built <typeparamref name="TConfiguration"/>.</returns>
        public TConfiguration BuildWithFallback(ConfigurationBase fallback)
        {
            return this.BuildWithFallback(ConfigurationBase.CastToConfigurationType<TConfiguration>(fallback));
        }

        /// <summary>
        /// Sets the maximum number of generations per phase.
        /// Default is <see cref="int.MaxValue"/>.
        /// </summary>
        /// <param name="number">Positive number.</param>
        /// <returns>The <typeparamref name="TBuilder"/> in its new state.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the number is not positive.</exception>
        public TBuilder SetMaximumNumberGenerations(int number)
        {
            if (number <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number),
                    $"Maximum number of generations must be positive, but was {number}.");
            }

            this.MaximumNumberGenerations = number;
            return this.Instance;
        }

        /// <summary>
        /// Sets the minimum size an integer domain needs to have to be handled as continuous.
        /// Default is 20.
        /// </summary>
        /// <param name="size">A positive number.</param>
        /// <returns>The <typeparamref name="TBuilder"/> in its new state.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the number is not positive.</exception>
        public TBuilder SetMinimumDomainSize(int size)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(size),
                    $"Minimum domain size must be positive, but was {size}.");
            }

            this.MinimumDomainSize = size;
            return this.Instance;
        }

        /// <summary>
        /// Sets whether the set of instances to evaluate on should stay the same during a phase.
        /// Default is false.
        /// </summary>
        /// <param name="fix">Whether the set of instances to evaluate on should stay the same.</param>
        /// <returns>The <typeparamref name="TBuilder"/> in its new state.</returns>
        public TBuilder SetFixInstances(bool fix)
        {
            this.FixInstances = fix;
            return this.Instance;
        }

        /// <summary>
        /// Sets whether the continuous optimization method should focus on improving the incumbent.
        /// If not, it modifies the complete competitive population.
        /// Default is false.
        /// </summary>
        /// <param name="incumbentFocus">
        /// Whether the continuous optimization method should focus on improving the incumbent.
        /// </param>
        /// <returns>The <typeparamref name="TBuilder"/> in its new state.</returns>
        public TBuilder SetFocusOnIncumbent(bool incumbentFocus)
        {
            this.FocusOnIncumbent = incumbentFocus;
            return this.Instance;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Builds a <typeparamref name="TConfiguration"/> using the provided <typeparamref name="TConfiguration"/> as
        /// fallback.
        /// </summary>
        /// <param name="fallback">Used if a property is not set for this
        /// <see cref="StrategyConfigurationBuilderBase{TBuilder, TConfiguration}"/>.
        /// May be null. In that case, defaults are used as fallback.</param>
        /// <returns>The built <typeparamref name="TConfiguration"/>.</returns>
        protected abstract TConfiguration BuildWithFallback(TConfiguration fallback);

        #endregion
    }
}