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

namespace Optano.Algorithm.Tuner.Tests.GrayBox.DataRecordTypes
{
    using System;

    using Optano.Algorithm.Tuner.GrayBox.DataRecordTypes;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="AdapterFeaturesBase"/> class.
    /// </summary>
    public class AdapterFeaturesBaseTest : IDisposable
    {
        #region Fields

        /// <summary>
        /// The <see cref="DummyAdapterFeatures"/>, used in tests.
        /// </summary>
        private readonly DummyAdapterFeatures _features = new DummyAdapterFeatures()
                                                              {
                                                                  FeatureA = 0D,
                                                                  FeatureB = 2D,
                                                                  FeatureC = 1D,
                                                              };

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterFeaturesBaseTest"/> class.
        /// Behaves like the [TestInitialize] of MSTest framework.
        /// </summary>
        public AdapterFeaturesBaseTest()
        {
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <summary>
        /// Checks, that <see cref="AdapterFeaturesBase.GetHeader"/> returns ordered header with correct prefix and suffix.
        /// </summary>
        /// <param name="prefix"> The given prefix.</param>
        /// <param name="suffix"> The given suffix.</param>
        [Theory]
        [InlineData("Prefix_", "_Suffix")]
        [InlineData("", "")]
        public void GetHeaderReturnsOrderedHeaderWithCorrectPrefixAndSuffix(string prefix, string suffix)
        {
            var header = this._features.GetHeader(prefix, suffix);
            header.Length.ShouldBe(3);
            header[0].ShouldBe($"{prefix}FeatureA{suffix}");
            header[1].ShouldBe($"{prefix}FeatureB{suffix}");
            header[2].ShouldBe($"{prefix}FeatureC{suffix}");
        }

        /// <summary>
        /// Checks, that <see cref="AdapterFeaturesBase.ToArray"/> returns ordered array.
        /// </summary>
        [Fact]
        public void ToArrayReturnsOrderedArray()
        {
            var values = this._features.ToArray();
            values.Length.ShouldBe(3);
            values[0].ShouldBe(0);
            values[1].ShouldBe(2);
            values[2].ShouldBe(1);
        }

        #endregion

        /// <summary>
        /// A dummy implementation of the <see cref="AdapterFeaturesBase"/> class.
        /// </summary>
        public class DummyAdapterFeatures : AdapterFeaturesBase
        {
            #region Public properties

            /// <summary>
            /// Gets or sets the dummy feature A.
            /// </summary>
            public double FeatureA { get; set; }

            /// <summary>
            /// Gets or sets the dummy feature B.
            /// </summary>
            public double FeatureB { get; set; }

            /// <summary>
            /// Gets or sets the dummy feature C.
            /// </summary>
            public double FeatureC { get; set; }

            #endregion
        }
    }
}