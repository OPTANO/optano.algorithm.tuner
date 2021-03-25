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

namespace Optano.Algorithm.Tuner.Serialization
{
    using System.IO;
    using System.IO.Compression;
    using System.Text;

    /// <summary>
    /// (De-)serializes data into / from a file. Can handle multiple serializations of the same, but updated, data.
    /// </summary>
    public abstract class StatusBase
    {
        #region Constants

        /// <summary>
        /// Suffix added to the file name while writing is in progress.
        /// The file will lose the suffix once writing completes.
        /// </summary>
        public const string WorkInProgressSuffix = "_wip";

        #endregion

        #region Static Fields

        /// <summary>
        /// Read/write lock for status files.
        /// </summary>
        protected static readonly object StatusFileLock = new object();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Reads a serialized status object from file.
        /// </summary>
        /// <param name="path">
        /// The file path.
        /// </param>
        /// <typeparam name="T">
        /// The type of the expected deserialized object.
        /// </typeparam>
        /// <returns>
        /// The deserialized status object.
        /// </returns>
        public static T ReadFromFile<T>(string path)
            where T : StatusBase
        {
            T status;

            lock (StatusFileLock)
            {
                using (var file = File.OpenRead(path))
                {
                    status =
                        new Hyperion.Serializer().Deserialize<T>(file);
                }
            }

            return status;
        }

        /// <summary>
        /// Serializes the object and writes it to a file.
        /// </summary>
        /// <param name="path">
        /// File to write the object to.
        /// </param>
        public virtual void WriteToFile(string path)
        {
            // Serialize and write to file.
            var wipFilePath = path + WorkInProgressSuffix;
            using (var file = File.Create(wipFilePath))
            {
                new Hyperion.Serializer().Serialize(this, file);
                file.Flush(true);
            }

            lock (StatusFileLock)
            {
                if (File.Exists(path))
                {
                    this.InspectPreviousStatus(path);
                    File.Delete(path);
                }

                File.Move(wipFilePath, path);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a zip folder and adds a file to it.
        /// </summary>
        /// <param name="filePath">File to zip.</param>
        /// <param name="zipFolderPath">The zip folder to create. Should end with '.zip'.</param>
        /// <param name="fileNameInFolder">The name the added file should have inside the zip folder.</param>
        protected static void ZipFile(string filePath, string zipFolderPath, string fileNameInFolder)
        {
            var zipStream = File.Create(zipFolderPath);
            var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, false, Encoding.UTF8);

            var entry = zipArchive.CreateEntry(fileNameInFolder);
            using (var entryStream = entry.Open())
            {
                var statusFile = File.ReadAllBytes(filePath);
                entryStream.Write(statusFile, 0, statusFile.Length);
            }

            zipArchive.Dispose();
        }

        /// <summary>
        /// Handles an obsolete status before it is deleted.
        /// </summary>
        /// <param name="path">Path to obsolete status file.</param>
        protected virtual void InspectPreviousStatus(string path)
        {
        }

        #endregion
    }
}