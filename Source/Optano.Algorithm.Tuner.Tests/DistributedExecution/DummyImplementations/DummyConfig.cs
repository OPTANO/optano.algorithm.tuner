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

namespace Optano.Algorithm.Tuner.Tests.DistributedExecution.DummyImplementations
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    using Optano.Algorithm.Tuner.Configuration;

    /// <summary>
    /// A dummy config, used in tests.
    /// </summary>
    public class DummyConfig : ConfigurationBase
    {
        #region Public properties

        /// <summary>
        /// Gets a value.
        /// </summary>
        public int Value { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public override bool IsCompatible(ConfigurationBase other)
        {
            return true;
        }

        /// <inheritdoc />
        public override bool IsTechnicallyCompatible(ConfigurationBase other)
        {
            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var builder = new StringBuilder("DummyConfig:\r\n");
            builder.AppendLine($"{nameof(DummyConfig.Value)}: {this.Value}");
            return builder.ToString();
        }

        #endregion

        /// <summary>
        /// A dummy config builder, used in tests.
        /// </summary>
        [SuppressMessage(
            "NDepend",
            "ND1305:NestedTypesShouldNotBeVisible",
            Justification = "This type is part of the Builder pattern.")]
        public class DummyConfigBuilder : IConfigBuilder<DummyConfig>
        {
            #region Fields

            /// <summary>
            /// The value to set for <see cref="DummyConfig.Value"/>.
            /// </summary>
            private int? _value;

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// Sets <see cref="DummyConfig.Value"/>.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <returns>The <see cref="DummyConfigBuilder"/> in its new state.</returns>
            public DummyConfigBuilder SetValue(int value)
            {
                this._value = value;
                return this;
            }

            /// <summary>
            /// Builds the configuration.
            /// </summary>
            /// <returns>The configuration.</returns>
            public DummyConfig Build()
            {
                return this.BuildWithFallback(null);
            }

            /// <inheritdoc />
            public DummyConfig BuildWithFallback(ConfigurationBase fallback)
            {
                var cast = ConfigurationBase.CastToConfigurationType<DummyConfig>(fallback);
                return this.BuildWithFallback(cast);
            }

            #endregion

            #region Methods

            /// <summary>
            /// Builds the configuration using a fallback configuration.
            /// </summary>
            /// <param name="fallback">The fallback. May be null. In that case, defaults are used as fallback.</param>
            /// <returns>The configuration.</returns>
            private DummyConfig BuildWithFallback(DummyConfig fallback)
            {
                return new DummyConfig
                           {
                               Value = this._value ?? fallback?.Value ?? 0,
                           };
            }

            #endregion
        }
    }
}