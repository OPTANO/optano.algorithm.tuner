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
    using System.Text;

    /// <summary>
    /// Base Class to derive all other Configurations from.
    /// </summary>
    public abstract class ConfigurationBase
    {
        #region Constants

        /// <summary>
        /// Tolerance used when comparing continuous parameters between 0 and 1 while deciding whether two
        /// <see cref="ConfigurationBase"/>s are compatible for one to be used in a continued run based on a run
        /// using the other.
        /// </summary>
        protected const double CompatibilityTolerance = 0.00001;

        #endregion

        #region Static Fields

        /// <summary>
        /// The indent to use for sub configurations in <see cref="ToString"/>.
        /// </summary>
        public static readonly string Indent = new string(' ', 2);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Casts a configuration of type <see cref="ConfigurationBase"/> to the provided type.
        /// </summary>
        /// <typeparam name="TConfiguration">The type of the configuration.</typeparam>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The cast configuration.</returns>
        /// <exception cref="ArgumentException">Thrown if the cast is unsuccessful.</exception>
        public static TConfiguration CastToConfigurationType<TConfiguration>(ConfigurationBase configuration)
            where TConfiguration : ConfigurationBase
        {
            switch (configuration)
            {
                case null:
                    return null;
                case TConfiguration correctTypeFallback:
                    return correctTypeFallback;
                default:
                    throw new ArgumentException(
                        $"Configuration should be {typeof(TConfiguration)} but is {configuration.GetType()}.",
                        nameof(configuration));
            }
        }

        /// <summary>
        /// Checks whether two <see cref="ConfigurationBase"/>s are compatible for one to be used in a continued run
        /// based on a run using the other.
        /// </summary>
        /// <param name="other">Configuration used for the start of tuning.</param>
        /// <returns>True iff this configuration can be used for continued run.</returns>
        public abstract bool IsCompatible(ConfigurationBase other);

        /// <summary>
        /// Checks whether two <see cref="ConfigurationBase"/>s are compatible in a technical sense for one
        /// to be used in a continued run based on a run using the other.
        /// <para>The difference to <see cref="IsCompatible"/> is that this function only checks for technical
        /// compatibility and does not consider whether the combination of configurations is compatible in the sense
        /// that the continued run looks like a longer single run.</para>
        /// </summary>
        /// <param name="other">Configuration used for the start of run.</param>
        /// <returns>True iff this configuration can be used for continued run.</returns>
        public abstract bool IsTechnicallyCompatible(ConfigurationBase other);

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public abstract override string ToString();

        #endregion

        #region Methods

        /// <summary>
        /// Returns a <see cref="string"/> that represents the provided configuration.
        /// </summary>
        /// <param name="name">The configuration's name.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>A <see cref="string"/> that represents the provided configuration.</returns>
        protected static string DescribeSubConfiguration(string name, ConfigurationBase configuration)
        {
            var descriptionBuilder = new StringBuilder();

            descriptionBuilder.AppendLine($"{name} : {{");
            foreach (var line in configuration.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                descriptionBuilder.AppendLine(Indent + line);
            }

            descriptionBuilder.AppendLine("}");

            return descriptionBuilder.ToString();
        }

        #endregion
    }
}