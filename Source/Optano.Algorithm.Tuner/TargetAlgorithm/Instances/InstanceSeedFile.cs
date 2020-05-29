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
    /// An <see cref="InstanceSeedFile"/> treats a combination of actual file (stored in <see cref="InstanceFile.Path"/>) and a <see cref="Seed"/> as a unique Instance.
    /// </summary>
    public class InstanceSeedFile : InstanceFile
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceSeedFile"/> class.
        /// </summary>
        /// <param name="path">
        /// The path to the instance file.
        /// </param>
        /// <param name="seed">
        /// The seed that should be used by the target algorithm.
        /// </param>
        public InstanceSeedFile(string path, int seed)
            : base(path)
        {
            this.Seed = seed;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the seed.
        /// </summary>
        public int Seed { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks if the two objects are equal.
        /// </summary>
        /// <param name="obj">
        /// The other object.
        /// </param>
        /// <returns>
        /// True, if <see cref="InstanceFile.Path"/> and <see cref="Seed"/> are equal.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null || !(typeof(InstanceSeedFile) == obj.GetType()))
            {
                return false;
            }

            var other = obj as InstanceSeedFile;
            return this.Path.Equals(other.Path, StringComparison.InvariantCultureIgnoreCase) && this.Seed == other.Seed;
        }

        /// <summary>
        /// Computes the hash code.
        /// Depends on <see cref="InstanceFile.Path"/> and <see cref="Seed"/>.
        /// </summary>
        /// <returns>
        /// The hash code.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 23) + base.GetHashCode();
                hash = (hash * 23) + this.Seed.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns {Path}_{Seed}.
        /// </summary>
        /// <returns>
        /// The string representation.
        /// </returns>
        public override string ToString()
        {
            return $"{base.ToString()}_{this.Seed}";
        }

        #endregion
    }
}