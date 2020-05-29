#region Copyright (c) OPTANO GmbH

// ////////////////////////////////////////////////////////////////////////////////
// 
//        OPTANO GmbH Source Code
//        Copyright (c) 2010-2020 OPTANO GmbH
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

namespace Optano.Algorithm.Tuner
{
    using System;
    using System.IO;

    /// <summary>
    /// Utility methods for dealing with paths.
    /// </summary>
    public static class PathUtils
    {
        #region Public Methods and Operators

        /// <summary>
        /// Translates a path relative to the folder where the .exe is located into an absolute one.
        /// </summary>
        /// <param name="relativePath">Path relative to executable folder.</param>
        /// <returns>The absolute path.</returns>
        public static string GetAbsolutePathFromExecutableFolderRelative(string relativePath)
        {
            return GetAbsolutePathFromDirectory(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        }

        /// <summary>
        /// Translates a path relative to the folder where program was started into an absolute one.
        /// </summary>
        /// <param name="relativePath">Path relative to folder the program was started from.</param>
        /// <returns>The absolute path.</returns>
        public static string GetAbsolutePathFromCurrentDirectory(string relativePath)
        {
            return GetAbsolutePathFromDirectory(Environment.CurrentDirectory, relativePath);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Translates a relative path into an absolute one.
        /// </summary>
        /// <param name="directory">Directory the path is relative to.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <returns>The absolute path.</returns>
        private static string GetAbsolutePathFromDirectory(string directory, string relativePath)
        {
            return new DirectoryInfo(Path.Combine(directory, relativePath)).FullName;
        }

        #endregion
    }
}