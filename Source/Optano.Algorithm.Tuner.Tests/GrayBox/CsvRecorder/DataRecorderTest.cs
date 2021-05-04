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
    using System.IO;

    using Optano.Algorithm.Tuner.GrayBox.CsvRecorder;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="DataRecorder{TResult}"/> class.
    /// </summary>
    public class DataRecorderTest : IDisposable
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRecorderTest"/> class.
        /// Behaves like the [TestInitialize] of MSTest framework.
        /// </summary>
        public DataRecorderTest()
        {
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <summary>
        /// Checks, that calling the constructor throws an <see cref="ArgumentException"/>, if the file name does not match the regex.
        /// </summary>
        [Fact]
        public void ConstructorThrowsIfFileNameDoesNotMatchRegex()
        {
            Assert.Throws<ArgumentException>(() => new DataRecorder<RuntimeResult>(new FileInfo("dataLog.csv")));
        }

        #endregion
    }
}