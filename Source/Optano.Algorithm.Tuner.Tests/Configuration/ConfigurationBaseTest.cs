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

namespace Optano.Algorithm.Tuner.Tests.Configuration
{
    using System;
    using System.Globalization;

    using Optano.Algorithm.Tuner.Configuration;

    using Xunit;

    /// <summary>
    /// Contains tests that should be executed for all test classes for
    /// <see cref="ConfigurationBase"/>s and their <see cref="IConfigBuilder{TConfiguration}"/>s. 
    /// </summary>
    public abstract class ConfigurationBaseTest
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationBaseTest"/> class.
        /// </summary>
        public ConfigurationBaseTest()
        {
            ProcessUtils.SetDefaultCultureInfo(CultureInfo.InvariantCulture);
            TestUtils.InitializeLogger();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that all values get transferred from builder to configuration.
        /// </summary>
        [Fact]
        public abstract void AllValuesGetTransferred();

        /// <summary>
        /// Checks that the configuration has correct default values.
        /// </summary>
        [Fact]
        public abstract void DefaultsAreSetCorrectly();

        /// <summary>
        /// Checks that all values are copied if
        /// <see cref="IConfigBuilder{TConfiguration}.BuildWithFallback"/>
        /// is called on a builder without anything set.
        /// </summary>
        [Fact]
        public abstract void BuildWithFallbackUsesFallbacks();

        /// <summary>
        /// Checks that <see cref="ConfigurationBase.IsCompatible"/> returns false if the argument is not of the same
        /// type as the caller.
        /// </summary>
        [Fact]
        public void IsCompatibleReturnsFalseForWrongType()
        {
            var configuration = this.CreateTestConfiguration();
            Assert.False(
                configuration.IsCompatible(new WrongTypeConfiguration()),
                $"Configuration of type {configuration.GetType()} should not be compatible with one of type {typeof(WrongTypeConfiguration)}");
        }

        /// <summary>
        /// Checks that <see cref="ConfigurationBase.IsTechnicallyCompatible"/> returns false if the argument is not of
        /// the same type as the caller.
        /// </summary>
        [Fact]
        public void IsTechnicallyCompatibleReturnsFalseForWrongType()
        {
            var configuration = this.CreateTestConfiguration();
            Assert.False(
                configuration.IsTechnicallyCompatible(new WrongTypeConfiguration()),
                $"Configuration of type {configuration.GetType()} should not be technically compatible with one of type {typeof(WrongTypeConfiguration)}");
        }

        /// <summary>
        /// Checks that <see cref="IConfigBuilder{TConfiguration}.BuildWithFallback"/> throws when given a wrong type
        /// as argument.
        /// </summary>
        [Fact]
        public void BuildWithFallbackThrowsForWrongType()
        {
            var configurationBuilder = this.CreateTestConfigurationBuilder();
            Assert.Throws<ArgumentException>(() => configurationBuilder.BuildWithFallback(new WrongTypeConfiguration()));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks that two <see cref="ConfigurationBase"/>s are incompatible with each other using
        /// <see cref="ConfigurationBase.IsCompatible"/>.
        /// </summary>
        /// <param name="config1">The first <see cref="ConfigurationBase"/>.</param>
        /// <param name="config2">The second <see cref="ConfigurationBase"/>.</param>
        protected static void CheckIncompatibility(ConfigurationBase config1, ConfigurationBase config2)
        {
            Assert.False(config1.IsCompatible(config2), "Configurations should not be compatible.");
            Assert.Equal(config1.IsCompatible(config2), config2.IsCompatible(config1));
        }

        /// <summary>
        /// Checks that two <see cref="ConfigurationBase"/>s are technically incompatible with each other 
        /// using <see cref="ConfigurationBase.IsTechnicallyCompatible"/>.
        /// </summary>
        /// <param name="config1">The first <see cref="ConfigurationBase"/>.</param>
        /// <param name="config2">The second <see cref="ConfigurationBase"/>.</param>
        protected static void CheckTechnicalIncompatibility(ConfigurationBase config1, ConfigurationBase config2)
        {
            Assert.False(config1.IsTechnicallyCompatible(config2), "Configurations should not be technically compatible.");
            Assert.Equal(config1.IsTechnicallyCompatible(config2), config2.IsTechnicallyCompatible(config1));
        }

        /// <summary>
        /// Creates a valid configuration object.
        /// </summary>
        /// <returns>The created object.</returns>
        protected abstract ConfigurationBase CreateTestConfiguration();

        /// <summary>
        /// Creates a valid configuration builder object.
        /// </summary>
        /// <returns>The created object.</returns>
        protected abstract IConfigBuilder<ConfigurationBase> CreateTestConfigurationBuilder();

        #endregion

        /// <summary>
        /// <see cref="ConfigurationBase"/> subtype used for tests of wrong type.
        /// </summary>
        internal class WrongTypeConfiguration : ConfigurationBase
        {
            #region Public Methods and Operators

            /// <summary>
            /// Checks whether two <see cref="ConfigurationBase"/>s are compatible for one to be used in a continued run
            /// based on a run using the other.
            /// </summary>
            /// <param name="other">Configuration used for the start of tuning.</param>
            /// <returns>True iff this configuration can be used for continued run.</returns>
            public override bool IsCompatible(ConfigurationBase other)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Checks whether two <see cref="ConfigurationBase"/>s are compatible in a technical sense for one
            /// to be used in a continued run based on a run using the other.
            /// <para>The difference to <see cref="IsCompatible"/> is that this function only checks for technical
            /// compatibility and does not consider whether the combination of configurations is compatible in the sense
            /// that the continued run looks like a longer single run.</para>
            /// </summary>
            /// <param name="other">Configuration used for the start of run.</param>
            /// <returns>True iff this configuration can be used for continued run.</returns>
            public override bool IsTechnicallyCompatible(ConfigurationBase other)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Returns a <see cref="string" /> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="string" /> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }
}