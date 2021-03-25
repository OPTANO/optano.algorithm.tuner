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

namespace Optano.Algorithm.Tuner.Tests.Core
{
    using System;
    using System.Diagnostics;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="ProcessUtils"/> class.
    /// </summary>
    public class ProcessUtilsTest : IDisposable
    {
        #region Constants

        /// <summary>
        /// The path to the test application, which is used in tests.
        /// </summary>
        private const string PathToTestApplication = @"Tools/TestApplication.dll";

        #endregion

        #region Fields

        /// <summary>
        /// A started process. Has to be initialized.
        /// </summary>
        private readonly Process _process;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessUtilsTest"/> class.
        /// </summary>
        public ProcessUtilsTest()
        {
            // Create process that will sleep for 2-3 seconds.
            ProcessStartInfo processInfo = new ProcessStartInfo(
                fileName: "dotnet",
                arguments: $"{ProcessUtilsTest.PathToTestApplication} idle 3");

            // Make sure no additional window will be opened on process start.

            // Start the process.
            this._process = Process.Start(processInfo);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Disposes of <see cref="_process"/>.
        /// </summary>
        public void Dispose()
        {
            this._process.Dispose();
        }

        /// <summary>
        /// Checks that calling <see cref="ProcessUtils.CancelProcess(Process)"/> will cancel the process.
        /// </summary>
        [Fact]
        public void CancelProcessCancelsProcess()
        {
            ProcessUtils.CancelProcess(this._process);
            Assert.True(this._process.HasExited);
        }

        /// <summary>
        /// Checks that calling <see cref="ProcessUtils.CancelProcess(Process)"/> on a process that already
        /// exited does not throw an error.
        /// </summary>
        [Fact]
        public void CancelProcessWorksOnExitedProcess()
        {
            // Cancel once.
            ProcessUtils.CancelProcess(this._process);
            Assert.True(this._process.HasExited);

            // Cancel twice - no exception should be thrown.
            ProcessUtils.CancelProcess(this._process);
        }

        /// <summary>
        /// Checks that calling <see cref="ProcessUtils.CancelProcess(Process)"/> on a disposed process object
        /// will throw an <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void CancelProcessThrowsOnDisposedProcess()
        {
            this._process.Dispose();
            Assert.Throws<InvalidOperationException>(() => ProcessUtils.CancelProcess(this._process));
        }

        #endregion
    }
}