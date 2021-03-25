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
    using System.IO;
    using System.Reflection;
    using System.Web;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="PathUtils"/> class.
    /// </summary>
    public class PathUtilsTest
    {
        #region Fields

        /// <summary>
        /// Directory name from which the current executable gets executed.
        /// </summary>
        private readonly DirectoryInfo _callingDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        /// <summary>
        /// Directory where the current executable is stored.
        /// </summary>
        private readonly DirectoryInfo _directoryOfExecutable;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PathUtilsTest"/> class.
        /// </summary>
        public PathUtilsTest()
        {
            // Use different way of finding directory than in methods we are testing.
            var uri = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).AbsolutePath;
            var path = HttpUtility.UrlDecode(uri);
            this._directoryOfExecutable = new FileInfo(path).Directory;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Tests that <see cref="PathUtils.GetAbsolutePathFromExecutableFolderRelative"/> simply returns the
        /// executable's directory if an empty string is provided as path.
        /// </summary>
        [Fact]
        public void GetAbsolutePathFromExecutableFolderReflectsExecutableFolder()
        {
            Assert.Equal(
                this._directoryOfExecutable.FullName,
                PathUtilsTest.TrimTrailingBackslash(PathUtils.GetAbsolutePathFromExecutableFolderRelative(string.Empty)));
        }

        /// <summary>
        /// Tests that <see cref="PathUtils.GetAbsolutePathFromExecutableFolderRelative"/> adds relative paths to the
        /// executable's assembly.
        /// </summary>
        [Fact]
        public void GetAbsolutePathFromExecutableFolderAddsRelativePathToExecutableFolder()
        {
            string relativePath = $@"foo{Path.DirectorySeparatorChar}bar";
            Assert.Equal(
                $@"{this._directoryOfExecutable}{Path.DirectorySeparatorChar}{relativePath}",
                PathUtils.GetAbsolutePathFromExecutableFolderRelative(relativePath));
        }

        /// <summary>
        /// Tests that <see cref="PathUtils.GetAbsolutePathFromExecutableFolderRelative"/> interprets .. in relative
        /// paths as directing to the parent directory.
        /// </summary>
        [Fact]
        public void GetAbsolutePathFromExecutableFolderCanHandleDoubleDots()
        {
            const string Folder = "foo";
            var relativePath = $@"..{Path.DirectorySeparatorChar}{Folder}";
            Assert.Equal(
                Path.Combine(this._directoryOfExecutable.Parent.FullName, Folder),
                PathUtils.GetAbsolutePathFromExecutableFolderRelative(relativePath));
        }

        /// <summary>
        /// Tests that <see cref="PathUtils.GetAbsolutePathFromCurrentDirectory"/> simply returns the
        /// calling directory if an empty string is provided as path.
        /// </summary>
        [Fact]
        public void GetAbsolutePathFromCurrentDirectoryReflectsCallingFolder()
        {
            Assert.Equal(
                PathUtilsTest.TrimTrailingBackslash(this._callingDirectory.FullName),
                PathUtils.GetAbsolutePathFromCurrentDirectory(string.Empty));
        }

        /// <summary>
        /// Tests that <see cref="PathUtils.GetAbsolutePathFromCurrentDirectory"/> adds relative paths to the
        /// calling assembly.
        /// </summary>
        [Fact]
        public void GetAbsolutePathFromCurrentDirectoryAddsRelativePathToCurrentDirectoryFolder()
        {
            string relativePath = $@"foo{Path.DirectorySeparatorChar}bar";
            Assert.Equal(
                Path.Combine(this._callingDirectory.FullName, relativePath),
                PathUtils.GetAbsolutePathFromCurrentDirectory(relativePath));
        }

        /// <summary>
        /// Tests that <see cref="PathUtils.GetAbsolutePathFromCurrentDirectory"/> interprets .. in relative
        /// paths as directing to the parent directory.
        /// </summary>
        [Fact]
        public void GetAbsolutePathFromCurrentDirectoryCanHandleDoubleDots()
        {
            const string Folder = "foo";
            var relativePath = $@"..{Path.DirectorySeparatorChar}{Folder}";
            Assert.Equal(
                Path.Combine(this._callingDirectory?.Parent?.FullName ?? string.Empty, Folder),
                PathUtils.GetAbsolutePathFromCurrentDirectory(relativePath));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Removes the trailing backslash from path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The path without trailing backslash.</returns>
        private static string TrimTrailingBackslash(string path)
        {
            return path?.TrimEnd(Path.DirectorySeparatorChar);
        }

        #endregion
    }
}