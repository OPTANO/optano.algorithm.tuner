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

namespace Optano.Algorithm.Tuner.TargetAlgorithm.Instances
{
    using System;

    /// <summary>
    /// Represents a problem instance by storing a file name to one.
    /// </summary>
    public class InstanceFile : InstanceBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceFile"/> class.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        public InstanceFile(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            this.Path = path;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the path to the instance file.
        /// </summary>
        public string Path { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns a string representation of the <see cref="InstanceFile"/>.
        /// </summary>
        /// <returns>A string representation of the <see cref="InstanceFile"/>.</returns>
        public override string ToString()
        {
            return this.Path;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current instance by comparing
        /// <see cref="InstanceFile.Path"/>.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True iff the object is equal to this instance.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || !(typeof(InstanceFile) == obj.GetType()))
            {
                return false;
            }

            var otherInstance = obj as InstanceFile;

            // Path is never null anyway.
            return string.Equals(this.Path, otherInstance?.Path, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns a hash code for this <see cref="InstanceFile"/>.
        /// </summary>
        /// <returns>A hash code for this <see cref="InstanceFile"/>.</returns>
        public override int GetHashCode()
        {
            return this.Path.GetHashCode();
        }

        #endregion
    }
}