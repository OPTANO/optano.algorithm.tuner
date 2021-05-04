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
    using System.Globalization;
    using System.Linq;

    using Optano.Algorithm.Tuner.GrayBox.DataRecordTypes;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="AdapterDataRecord{TResult}"/> class.
    /// </summary>
    public class AdapterDataRecordTest : IDisposable
    {
        #region Static Fields

        /// <summary>
        /// The time stamp, used in tests.
        /// </summary>
        public static readonly DateTime Now = DateTime.MinValue;

        /// <summary>
        /// The adapter data record, used in tests.
        /// </summary>
        public static readonly AdapterDataRecord<RuntimeResult> AdapterDataRecord =
            new AdapterDataRecord<RuntimeResult>(
                "TestAlgorithm",
                TargetAlgorithmStatus.Running,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(5),
                AdapterDataRecordTest.Now,
                new[] { "Feature_1", "Feature_2" },
                new[] { 5.0d, 12.0d },
                new RuntimeResult(TimeSpan.FromSeconds(10)));

        /// <summary>
        /// The string header of <see cref="AdapterDataRecordTest.AdapterDataRecord"/>.
        /// </summary>
        public static readonly string[] AdapterDataRecordHeader = new[]
                                                                      {
                                                                          "TargetAlgorithmName",
                                                                          "TargetAlgorithmStatus",
                                                                          "ExpendedCpuTime",
                                                                          "ExpendedWallClockTime",
                                                                          "TimeStamp",
                                                                          "AdapterFeature_Feature_1",
                                                                          "AdapterFeature_Feature_2",
                                                                          "CurrentGrayBoxResult_TargetAlgorithmStatus",
                                                                          "CurrentGrayBoxResult_Runtime",
                                                                      };

        /// <summary>
        /// The string values of <see cref="AdapterDataRecordTest.AdapterDataRecord"/>.
        /// </summary>
        public static readonly string[] AdapterDataRecordValues = new[]
                                                                      {
                                                                          "TestAlgorithm",
                                                                          "Running",
                                                                          "10000",
                                                                          "5000",
                                                                          "00010101000000",
                                                                          "5",
                                                                          "12",
                                                                          "Finished",
                                                                          "10000",
                                                                      };

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterDataRecordTest"/> class.
        /// Behaves like the [TestInitialize] of MSTest framework.
        /// </summary>
        public AdapterDataRecordTest()
        {
            ProcessUtils.SetDefaultCultureInfo(CultureInfo.InvariantCulture);
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <summary>
        /// Checks, that <see cref="AdapterDataRecord{TResult}.GetHeader"/> returns the correct header.
        /// </summary>
        [Fact]
        public void GetHeaderReturnsCorrectValues()
        {
            var header = AdapterDataRecordTest.AdapterDataRecord.GetHeader();
            header.SequenceEqual(AdapterDataRecordTest.AdapterDataRecordHeader).ShouldBeTrue();
        }

        /// <summary>
        /// Checks, that <see cref="AdapterDataRecord{TResult}.ToStringArray"/> returns the correct values.
        /// </summary>
        [Fact]
        public void ToStringArrayReturnsCorrectValues()
        {
            var values = AdapterDataRecordTest.AdapterDataRecord.ToStringArray();
            values.SequenceEqual(AdapterDataRecordTest.AdapterDataRecordValues).ShouldBeTrue();
        }

        #endregion
    }
}