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
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="DataRecord{TResult}"/> class.
    /// </summary>
    public class DataRecordTest : IDisposable
    {
        #region Static Fields

        /// <summary>
        /// The data record, used in tests.
        /// </summary>
        public static readonly DataRecord<RuntimeResult> DataRecord = new DataRecord<RuntimeResult>(
            TunerDataRecordTest.TunerDataRecord,
            AdapterDataRecordTest.AdapterDataRecord);

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRecordTest"/> class.
        /// Behaves like the [TestInitialize] of MSTest framework.
        /// </summary>
        public DataRecordTest()
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
        /// Checks, that <see cref="DataRecord{TResult}.GetHeader"/> returns the correct header.
        /// </summary>
        [Fact]
        public void GetHeaderReturnsCorrectValues()
        {
            var header = DataRecordTest.DataRecord.GetHeader();
            var correctHeader = TunerDataRecordTest.TunerDataRecordHeader.Concat(AdapterDataRecordTest.AdapterDataRecordHeader).ToArray();
            header.SequenceEqual(correctHeader).ShouldBeTrue();
        }

        /// <summary>
        /// Checks, that <see cref="DataRecord{TResult}.ToStringArray"/> returns the correct values.
        /// </summary>
        [Fact]
        public void ToStringArrayReturnsCorrectValues()
        {
            var values = DataRecordTest.DataRecord.ToStringArray();
            var correctValues = TunerDataRecordTest.TunerDataRecordValues.Concat(AdapterDataRecordTest.AdapterDataRecordValues).ToArray();
            values.SequenceEqual(correctValues).ShouldBeTrue();
        }

        #endregion
    }
}