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

namespace Optano.Algorithm.Tuner.Tests.GrayBox.CsvRecorder
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Optano.Algorithm.Tuner.GrayBox.CsvRecorder;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="CsvRecorderBase{TData}"/> class.
    /// </summary>
    public class CsvRecorderBaseTest : IDisposable
    {
        #region Static Fields

        /// <summary>
        /// The file, used in tests.
        /// </summary>
        private static readonly string TestFile = PathUtils.GetAbsolutePathFromExecutableFolderRelative("testFile.csv");

        #endregion

        #region Fields

        /// <summary>
        /// The csv recorder, used in tests.
        /// </summary>
        private readonly DummyCsvRecorder _csvRecorder;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvRecorderBaseTest"/> class.
        /// Behaves like the [TestInitialize] of MSTest framework.
        /// </summary>
        public CsvRecorderBaseTest()
        {
            this._csvRecorder = new DummyCsvRecorder(new FileInfo(CsvRecorderBaseTest.TestFile));
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public void Dispose()
        {
            if (File.Exists(CsvRecorderBaseTest.TestFile))
            {
                File.Delete(CsvRecorderBaseTest.TestFile);
            }
        }

        /// <summary>
        /// Checks, that <see cref="CsvRecorderBase{TData}.WriteRow"/> writes the correct row.
        /// </summary>
        [Fact]
        public void WriteRowWritesCorrectRow()
        {
            File.Exists(CsvRecorderBaseTest.TestFile).ShouldBeFalse();

            this._csvRecorder.WriteRow(new[] { "element_1", "element_2" });
            File.Exists(CsvRecorderBaseTest.TestFile).ShouldBeTrue();
            var firstLinesInFile = File.ReadLines(CsvRecorderBaseTest.TestFile).ToList();
            Assert.Equal(2, firstLinesInFile.Count());
            Assert.Equal(
                "Header A,Header B",
                firstLinesInFile.ElementAt(0));
            Assert.Equal(
                "element_1,element_2",
                firstLinesInFile.ElementAt(1));

            this._csvRecorder.WriteRow(new[] { "element_3", "element_4" });
            File.Exists(CsvRecorderBaseTest.TestFile).ShouldBeTrue();
            var secondLinesInFile = File.ReadLines(CsvRecorderBaseTest.TestFile).ToList();
            Assert.Equal(3, secondLinesInFile.Count());
            Assert.Equal(
                "Header A,Header B",
                secondLinesInFile.ElementAt(0));
            Assert.Equal(
                "element_1,element_2",
                secondLinesInFile.ElementAt(1));
            Assert.Equal(
                "element_3,element_4",
                secondLinesInFile.ElementAt(2));
        }

        /// <summary>
        /// Checks, that <see cref="CsvRecorderBase{TData}.WriteRow"/> handles null correctly.
        /// </summary>
        [Fact]
        public void WriteRowHandlesNullCorrectly()
        {
            Assert.Throws<ArgumentNullException>(() => this._csvRecorder.WriteRow(null));
        }

        /// <summary>
        /// Checks, that <see cref="CsvRecorderBase{TData}.WriteRows"/> writes correct rows.
        /// </summary>
        [Fact]
        public void WriteRowsWritesCorrectRows()
        {
            File.Exists(CsvRecorderBaseTest.TestFile).ShouldBeFalse();
            var listOfData = new List<string[]>
                                 {
                                     new[] { "element_1", "element_2" },
                                     new[] { "element_3", "element_4" },
                                 };

            this._csvRecorder.WriteRows(listOfData);
            File.Exists(CsvRecorderBaseTest.TestFile).ShouldBeTrue();
            var linesInFile = File.ReadLines(CsvRecorderBaseTest.TestFile).ToList();
            Assert.Equal(3, linesInFile.Count());
            Assert.Equal(
                "Header A,Header B",
                linesInFile.ElementAt(0));
            Assert.Equal(
                "element_1,element_2",
                linesInFile.ElementAt(1));
            Assert.Equal(
                "element_3,element_4",
                linesInFile.ElementAt(2));
        }

        /// <summary>
        /// Checks, that <see cref="CsvRecorderBase{TData}.WriteRows"/> throws a <see cref="CsvDelimiterException"/>, if an element contains the delimiter.
        /// </summary>
        [Fact]
        public void WriteRowsThrowsIfElementContainsDelimiter()
        {
            File.Exists(CsvRecorderBaseTest.TestFile).ShouldBeFalse();
            var listOfData = new List<string[]> { new[] { "element_1", "ele,ment_2" } };

            Assert.Throws<CsvDelimiterException>(() => this._csvRecorder.WriteRows(listOfData));
            File.Exists(CsvRecorderBaseTest.TestFile).ShouldBeTrue();
            var linesInFile = File.ReadLines(CsvRecorderBaseTest.TestFile).ToList();
            Assert.Single(linesInFile);
            Assert.Equal(
                "Header A,Header B",
                linesInFile.ElementAt(0));
        }

        /// <summary>
        /// Checks, that <see cref="CsvRecorderBase{TData}.WriteRows"/> handles empty lists correctly.
        /// </summary>
        [Fact]
        public void WriteRowsHandlesEmptyListsCorrectly()
        {
            File.Exists(CsvRecorderBaseTest.TestFile).ShouldBeFalse();
            var listOfData = new List<string[]>();

            this._csvRecorder.WriteRows(listOfData);
            File.Exists(CsvRecorderBaseTest.TestFile).ShouldBeFalse();
        }

        /// <summary>
        /// Checks, that <see cref="CsvRecorderBase{TData}.WriteRows"/> handles null correctly.
        /// </summary>
        [Fact]
        public void WriteRowsHandlesNullCorrectly()
        {
            Assert.Throws<ArgumentNullException>(() => this._csvRecorder.WriteRows(null));
        }

        #endregion

        /// <summary>
        /// A dummy implementation of the <see cref="CsvRecorderBase{TData}"/> class.
        /// </summary>
        public class DummyCsvRecorder : CsvRecorderBase<string[]>
        {
            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="DummyCsvRecorder"/> class.
            /// </summary>
            /// <param name="csvFileInfo"> The current file.</param>
            public DummyCsvRecorder(FileInfo csvFileInfo)
                : base(csvFileInfo, ',', true)
            {
            }

            #endregion

            #region Public Methods and Operators

            /// <inheritdoc />
            public override string[] GetHeaderFromObject(string[] data)
            {
                return new[] { "Header A", "Header B" };
            }

            /// <inheritdoc />
            public override string[] GetValuesFromObject(string[] data)
            {
                return data;
            }

            #endregion
        }
    }
}