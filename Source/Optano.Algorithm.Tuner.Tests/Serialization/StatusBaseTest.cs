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

namespace Optano.Algorithm.Tuner.Tests.Serialization
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Optano.Algorithm.Tuner.Serialization;

    using Xunit;

    /// <summary>
    /// Defines tests that should be implemented for each <see cref="StatusBase"/>.
    /// </summary>
    /// <typeparam name="T">Type of the status class to test.</typeparam>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public abstract class StatusBaseTest<T> : TestBase
        where T : StatusBase
    {
        #region Properties

        /// <summary>
        /// Gets a path to which the status file will get written in tests.
        /// </summary>
        protected abstract string StatusFilePath { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="StatusBase.WriteToFile"/>
        /// throws a <see cref="DirectoryNotFoundException"/> if
        /// called on a path to a non-existing directory.
        /// </summary>
        [Fact]
        public void WriteToFileThrowsForUnknownDirectory()
        {
            var status = this.CreateTestStatus();
            Assert.Throws<DirectoryNotFoundException>(
                () => status.WriteToFile(
                    PathUtils.GetAbsolutePathFromExecutableFolderRelative(
                        $@"non{Path.DirectorySeparatorChar}existing{Path.DirectorySeparatorChar}path")));
        }

        /// <summary>
        /// Checks that <see cref="StatusBase.WriteToFile"/>
        /// only replaces a status file after a new one was
        /// completed.
        /// </summary>
        [Fact]
        public void WriteToFileOnlyDeletesLastStatusAfterWriteFinished()
        {
            /* Create status object. */
            var status = this.CreateTestStatus();

            /* Watch directory to see what happens with status files. */
            DateTime tempFileChanged = DateTime.MaxValue;
            DateTime statusFileDeleted = DateTime.MaxValue;
            DateTime tempFileRenamed = DateTime.MaxValue;
            using (var watcher = new FileSystemWatcher(PathUtils.GetAbsolutePathFromExecutableFolderRelative("status")))
            {
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess;
                watcher.EnableRaisingEvents = true;

                /* Remember when the temporary file got changed last. */
                watcher.Changed += (sender, e) =>
                    {
                        if (e.Name.EndsWith(StatusBase.WorkInProgressSuffix))
                        {
                            tempFileChanged = DateTime.Now;
                        }
                    };
                /* Remember when old status file was deleted. */
                watcher.Deleted += (sender, e) =>
                    {
                        if (e.FullPath.Equals(this.StatusFilePath))
                        {
                            statusFileDeleted = DateTime.Now;
                        }
                        else
                        {
                            Assert.True(false, $"Deleted file {e.FullPath}.");
                        }
                    };
                /* Remember when temporary file was renamed to be the status file. */
                watcher.Renamed += (sender, e) =>
                    {
                        if (e.OldName.EndsWith(StatusBase.WorkInProgressSuffix))
                        {
                            tempFileRenamed = DateTime.Now;
                            Assert.Equal(this.StatusFilePath, e.FullPath);
                        }
                        else
                        {
                            Assert.True(false, $"Renamed file {e.FullPath}.");
                        }
                    };

                /* Write to file. */
                status.WriteToFile(this.StatusFilePath);

                /* Wait a while for all events to be handled. */
                Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();

                /* Make sure status file was replaced at last possible moment. */
                Assert.True(
                    tempFileChanged < statusFileDeleted,
                    "Status file should have been deleted only after the last change to the temporary file.");
                Assert.True(
                    tempFileRenamed - statusFileDeleted <= TimeSpan.FromMilliseconds(50),
                    "Temporary file should have been renamed directly after old status file was deleted.");
            }
        }

        /// <summary>
        /// Checks that <see cref="StatusBase.ReadFromFile{Status}"/> correctly deserializes a
        /// status object written to file by <see cref="StatusBase.WriteToFile"/>.
        /// </summary>
        [Fact]
        public abstract void ReadFromFileDeserializesCorrectly();

        /// <summary>
        /// Checks that <see cref="StatusBase.ReadFromFile{Status}"/>
        /// throws a <see cref="FileNotFoundException"/> if the file
        /// it should read from doesn't exist.
        /// </summary>
        [Fact]
        public void ReadFromFileThrowsForNonExistingFile()
        {
            Assert.Throws<FileNotFoundException>(
                () => StatusBase.ReadFromFile<T>("foo_not_existing_bar"));
        }

        /// <summary>
        /// Checks that <see cref="StatusBase.ReadFromFile{Status}"/>
        /// throws an <see cref="InvalidCastException"/> if it is
        /// called on a file serializing a status object of different type.
        /// </summary>
        [Fact]
        public void ReadFromFileThrowsForWrongGenericType()
        {
            var status = this.CreateTestStatus();
            status.WriteToFile(this.StatusFilePath);
            Assert.Throws<InvalidCastException>(
                () => StatusBase.ReadFromFile<WrongTypeStatus>(this.StatusFilePath));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Deletes status file(s) after a test if any was written.
        /// </summary>
        protected override void CleanupDefault()
        {
            if (File.Exists(this.StatusFilePath))
            {
                File.Delete(this.StatusFilePath);
            }
        }

        /// <summary>
        /// Creates a status object which can be (de)serialized successfully.
        /// </summary>
        /// <returns>The created object.</returns>
        protected abstract T CreateTestStatus();

        #endregion

        /// <summary>
        /// <see cref="StatusBase"/> subtype used in
        /// <see cref="StatusBaseTest{T}.ReadFromFileThrowsForWrongGenericType"/>.
        /// </summary>
        internal class WrongTypeStatus : StatusBase
        {
        }
    }
}