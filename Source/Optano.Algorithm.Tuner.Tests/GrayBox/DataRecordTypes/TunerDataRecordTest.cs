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
    using Optano.Algorithm.Tuner.MachineLearning.GenomeRepresentation;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="TunerDataRecord{TResult}"/> class.
    /// </summary>
    public class TunerDataRecordTest : IDisposable
    {
        #region Static Fields

        /// <summary>
        /// The adapter data record, used in tests.
        /// </summary>
        public static readonly TunerDataRecord<RuntimeResult> TunerDataRecord =
            new TunerDataRecord<RuntimeResult>(
                "TestNodeID",
                0,
                0,
                "TestInstanceID",
                0.5,
                new[] { "GenomeFeature_1", "GenomeFeature_2" },
                (GenomeDoubleRepresentation)new[] { 15.0d, 22.0d },
                new RuntimeResult(TimeSpan.FromSeconds(30)));

        /// <summary>
        /// The string header of <see cref="TunerDataRecordTest.TunerDataRecord"/>.
        /// </summary>
        public static readonly string[] TunerDataRecordHeader = new[]
                                                                    {
                                                                        "NodeID",
                                                                        "GenerationID",
                                                                        "TournamentID",
                                                                        "RunID",
                                                                        "InstanceID",
                                                                        "GenomeID",
                                                                        "GrayBoxConfidence",
                                                                        "Genome_GenomeFeature_1",
                                                                        "Genome_GenomeFeature_2",
                                                                        "FinalResult_TargetAlgorithmStatus",
                                                                        "FinalResult_Runtime",
                                                                    };

        /// <summary>
        /// The string values of <see cref="TunerDataRecordTest.TunerDataRecord"/>.
        /// </summary>
        public static readonly string[] TunerDataRecordValues = new[]
                                                                    {
                                                                        "TestNodeID",
                                                                        "0",
                                                                        "0",
                                                                        "TestInstanceID_[15.0,22.0]",
                                                                        "TestInstanceID",
                                                                        "[15.0,22.0]",
                                                                        "0.5",
                                                                        "15",
                                                                        "22",
                                                                        "Finished",
                                                                        "30000",
                                                                    };

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TunerDataRecordTest"/> class.
        /// Behaves like the [TestInitialize] of MSTest framework.
        /// </summary>
        public TunerDataRecordTest()
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
        /// Checks, that <see cref="TunerDataRecord{TResult}.GetHeader"/> returns the correct header.
        /// </summary>
        [Fact]
        public void GetHeaderReturnsCorrectValues()
        {
            var header = TunerDataRecordTest.TunerDataRecord.GetHeader();
            header.SequenceEqual(TunerDataRecordTest.TunerDataRecordHeader).ShouldBeTrue();
        }

        /// <summary>
        /// Checks, that <see cref="TunerDataRecord{TResult}.ToStringArray"/> returns the correct values.
        /// </summary>
        [Fact]
        public void ToStringArrayReturnsCorrectValues()
        {
            var values = TunerDataRecordTest.TunerDataRecord.ToStringArray();
            values.SequenceEqual(TunerDataRecordTest.TunerDataRecordValues).ShouldBeTrue();
        }

        /// <summary>
        /// Checks that changing values in a copy does not change values in the original.
        /// </summary>
        [Fact]
        public void ChangingValuesInCopyDoesNotChangeValuesInOriginal()
        {
            var original = TunerDataRecordTest.TunerDataRecord;
            var copy = original.Copy();

            const double NewGrayBoxConfidence = 0.7;
            var newFinalResult = new RuntimeResult(TimeSpan.FromSeconds(15));

            copy.GrayBoxConfidence = NewGrayBoxConfidence;
            copy.FinalResult = newFinalResult;

            original.GrayBoxConfidence.ShouldBe(TunerDataRecordTest.TunerDataRecord.GrayBoxConfidence);
            original.FinalResult.TargetAlgorithmStatus.ShouldBe(TunerDataRecordTest.TunerDataRecord.FinalResult.TargetAlgorithmStatus);
            original.FinalResult.IsCancelled.ShouldBe(TunerDataRecordTest.TunerDataRecord.FinalResult.IsCancelled);
            original.FinalResult.Runtime.ShouldBe(TunerDataRecordTest.TunerDataRecord.FinalResult.Runtime);

            copy.GrayBoxConfidence.ShouldBe(NewGrayBoxConfidence);
            copy.FinalResult.TargetAlgorithmStatus.ShouldBe(newFinalResult.TargetAlgorithmStatus);
            copy.FinalResult.IsCancelled.ShouldBe(newFinalResult.IsCancelled);
            copy.FinalResult.Runtime.ShouldBe(newFinalResult.Runtime);
        }

        #endregion
    }
}