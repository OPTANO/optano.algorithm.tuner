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
    /// Contains common configuration options.
    /// </summary>
    /// <typeparam name="TConfiguration">The type of the configuration.</typeparam>
    public abstract class StrategyConfigurationBase<TConfiguration> : ConfigurationBase
        where TConfiguration : StrategyConfigurationBase<TConfiguration>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StrategyConfigurationBase{TConfiguration}"/> class.
        /// </summary>
        /// <param name="focusOnIncumbent">
        /// Whether the continuous optimization method should focus on improving the incumbent.
        /// If not, it modifies the complete competitive population.
        /// </param>
        /// <param name="maximumNumberGenerations">
        /// The maximum number of generations per phase.
        /// </param>
        /// <param name="minimumDomainSize">
        /// The minimum size an integer domain needs to have to be handled as continuous.
        /// </param>
        /// <param name="replacementRate">
        /// The percentage of competitive genomes which get replaced by the best search points found by 
        /// the continuous optimization method at the end of a phase, expressed by a value in [0, 1].
        /// </param>
        /// <param name="fixInstances">
        /// Whether the set of instances to evaluate on should stay the same during a phase.
        /// </param>
        protected StrategyConfigurationBase(
            bool focusOnIncumbent,
            int maximumNumberGenerations,
            int minimumDomainSize,
            double replacementRate,
            bool fixInstances)
        {
            this.FocusOnIncumbent = focusOnIncumbent;
            this.MaximumNumberGenerations = maximumNumberGenerations;
            this.MinimumDomainSize = minimumDomainSize;
            this.ReplacementRate = replacementRate;
            this.FixInstances = fixInstances;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets a value indicating whether the continuous optimization method should focus on improving the incumbent.
        /// If not, it modifies the complete competitive population.
        /// </summary>
        public bool FocusOnIncumbent { get; }

        /// <summary>
        /// Gets the maximum number of generations per phase.
        /// </summary>
        public int MaximumNumberGenerations { get; }

        /// <summary>
        /// Gets the minimum size an integer domain needs to have to be handled as continuous.
        /// </summary>
        public int MinimumDomainSize { get; }

        /// <summary>
        /// Gets the percentage of competitive genomes which get replaced by the best search points found by 
        /// the continuous optimization method at the end of a phase, expressed by a value in [0, 1].
        /// <para>A replacement rate of 0 indicates that only the incumbent itself should be replaced.</para>
        /// </summary>
        public double ReplacementRate { get; }

        /// <summary>
        /// Gets a value indicating whether the set of instances to evaluate on should stay the same during a phase.
        /// </summary>
        public bool FixInstances { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks whether two <typeref name="TConfiguration"/>s are compatible for one
        /// to be used in a continued tuning based on a tuning using the other.
        /// </summary>
        /// <param name="other">Configuration used for the start of tuning.</param>
        /// <returns>True iff this configuration can be used for continued tuning.</returns>
        public override bool IsCompatible(ConfigurationBase other)
        {
            if (!(other is TConfiguration otherConfig))
            {
                return false;
            }

            // Replacement rate is only relevant if we focus on the incumbent.
            if (this.FocusOnIncumbent && Math.Abs(this.ReplacementRate - otherConfig.ReplacementRate) > CompatibilityTolerance)
            {
                return false;
            }

            // For CMA-ES, minimum domain size is only relevant if we focus on the incumbent --> handle in subclasses.
            return this.IsTechnicallyCompatible(otherConfig)
                   && this.FocusOnIncumbent == otherConfig.FocusOnIncumbent
                   && this.MaximumNumberGenerations == otherConfig.MaximumNumberGenerations
                   && this.FixInstances == otherConfig.FixInstances;
        }

        /// <summary>
        /// Checks whether two <typeref name="TConfiguration"/>s are compatible in a technical
        /// sense for one to be used in a continued tuning based on a tuning using the other.
        /// <para>The difference to <see cref="IsCompatible"/> is that this function only checks for technical
        /// compatibility and does not consider whether the combination of configurations is compatible in the sense
        /// that the continued tuning looks like a longer single tuning.</para>
        /// </summary>
        /// <param name="other">Configuration used for the start of tuning.</param>
        /// <returns>True iff this configuration can be used for continued tuning.</returns>
        public override bool IsTechnicallyCompatible(ConfigurationBase other)
        {
            return other is TConfiguration;
        }

        #endregion
    }
}