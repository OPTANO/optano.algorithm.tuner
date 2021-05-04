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

namespace Optano.Algorithm.Tuner.TargetAlgorithm.Instances
{
    /// <summary>
    /// Instance to be solved by the <see cref="ITargetAlgorithm{TInstance, TResult}" />.
    /// Needs to be immutable to guarantee thread-safety.
    /// </summary>
    public abstract class InstanceBase
    {
        #region Public Methods and Operators

        /// <summary>
        /// Gets a string representation for the current instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public abstract override string ToString();

        /// <summary>
        /// Gets a hash code for the current instance.
        /// <para>The hash code may not depend on object reference if you plan to use OPTANO Algorithm Tuner in a
        /// distributed fashion.</para>
        /// </summary>
        /// <returns>A hash code for the current instance.</returns>
        public abstract override int GetHashCode();

        /// <summary>
        /// Determines whether the specified object is equal to the current instance.
        /// <para>The function may not depend on object references if you plan to use OPTANO Algorithm Tuner in a
        /// distributed fashion.</para>
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
        public abstract override bool Equals(object obj);

        /// <summary>
        /// Returns the unique ID of the current instance.
        /// </summary>
        /// <returns>The ID.</returns>
        public virtual string ToId()
        {
            return this.ToString();
        }

        #endregion
    }
}