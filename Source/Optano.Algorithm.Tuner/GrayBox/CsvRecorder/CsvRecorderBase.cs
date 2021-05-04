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

namespace Optano.Algorithm.Tuner.GrayBox.CsvRecorder
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Optano.Algorithm.Tuner.Logging;

    /// <summary>
    /// Abstract class to record a csv file.
    /// </summary>
    /// <typeparam name="TData">The data type.</typeparam>
    public abstract class CsvRecorderBase<TData>
    {
        #region Fields

        /// <summary>
        /// The file, that stores the given information.
        /// </summary>
        private readonly FileInfo _csvFileInfo;

        /// <summary>
        /// An object to lock the current file.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The delimiter.
        /// </summary>
        private readonly char _delimiter;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvRecorderBase{TData}"/> class.
        /// </summary>
        /// <param name="csvFileInfo">The csv file.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <param name="renameFileIfAlreadyExisting"> Boolean, whether the file should be renamed, if it is already existing.</param>
        protected CsvRecorderBase(FileInfo csvFileInfo, char delimiter, bool renameFileIfAlreadyExisting)
        {
            this._csvFileInfo = csvFileInfo;
            this._delimiter = delimiter;

            Directory.CreateDirectory(csvFileInfo.DirectoryName ?? throw new InvalidOperationException("The directory name cannot be null!"));

            if (renameFileIfAlreadyExisting && File.Exists(csvFileInfo.FullName))
            {
                lock (this._lock)
                {
                    var targetFileInfo = new FileInfo(
                        Path.Combine(
                            csvFileInfo.Directory!.FullName,
                            $"{Path.GetFileNameWithoutExtension(csvFileInfo.Name)}_{DateTime.Now.Ticks}{csvFileInfo.Extension}"));

                    GrayBoxUtils.TryToMoveFile(csvFileInfo, targetFileInfo);
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Writes a row to the csv file. Moreover creates the file with header, if not already existing.
        /// </summary>
        /// <param name="data"> The data.</param>
        public void WriteRow(TData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var dataList = new List<TData>() { data };
            this.WriteRows(dataList);
        }

        /// <summary>
        /// Writes multiple rows to the csv file. Moreover creates the file with header, if not already existing.
        /// </summary>
        /// <param name="multipleData"> The data.</param>
        public void WriteRows(List<TData> multipleData)
        {
            if (multipleData == null)
            {
                throw new ArgumentNullException(nameof(multipleData));
            }

            if (!multipleData.Any())
            {
                LoggingHelper.WriteLine(VerbosityLevel.Warn, $"Got empty list to write to {this._csvFileInfo.FullName}!");
                return;
            }

            lock (this._lock)
            {
                StreamWriter writer = null;
                try
                {
                    if (!File.Exists(this._csvFileInfo.FullName))
                    {
                        writer = this._csvFileInfo.CreateText();

                        // Get header from first element of list.
                        var headerElements = this.GetHeaderFromObject(multipleData.First());
                        var header = GrayBoxUtils.GetAndCheckLine(headerElements, this._delimiter);
                        writer.WriteLine(header);
                    }
                    else
                    {
                        writer = this._csvFileInfo.AppendText();
                    }

                    foreach (var data in multipleData)
                    {
                        var lineElements = this.GetValuesFromObject(data);
                        var line = GrayBoxUtils.GetAndCheckLine(lineElements, this._delimiter);
                        writer.WriteLine(line);
                    }
                }
                catch (CsvDelimiterException)
                {
                    throw;
                }
                catch
                {
                    LoggingHelper.WriteLine(VerbosityLevel.Warn, $"Cannot write to {this._csvFileInfo.FullName}!");
                }
                finally
                {
                    writer?.Flush();
                    writer?.Close();
                }
            }
        }

        /// <summary>
        /// Returns the header from the given object.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The header.</returns>
        public abstract string[] GetHeaderFromObject(TData data);

        /// <summary>
        /// Returns the values from the given object.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The values.</returns>
        public abstract string[] GetValuesFromObject(TData data);

        #endregion
    }
}