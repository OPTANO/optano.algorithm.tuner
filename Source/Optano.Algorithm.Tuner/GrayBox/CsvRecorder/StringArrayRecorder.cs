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
    using System.IO;

    /// <summary>
    /// Handles the recording of string arrays.
    /// </summary>
    public class StringArrayRecorder : CsvRecorderBase<string[]>
    {
        #region Fields

        /// <summary>
        /// The header.
        /// </summary>
        private readonly string[] _header;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StringArrayRecorder"/> class.
        /// </summary>
        /// <param name="csvFileInfo"> The file.</param>
        /// <param name="header">The header.</param>
        /// <param name="renameFileIfAlreadyExisting"> Boolean, whether the file should be renamed, if it is already existing.</param>
        public StringArrayRecorder(FileInfo csvFileInfo, string[] header, bool renameFileIfAlreadyExisting)
            : base(csvFileInfo, GrayBoxUtils.DataRecorderDelimiter, renameFileIfAlreadyExisting)
        {
            this._header = header;
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public override string[] GetHeaderFromObject(string[] data)
        {
            return this._header;
        }

        /// <inheritdoc />
        public override string[] GetValuesFromObject(string[] data)
        {
            return data;
        }

        #endregion
    }
}