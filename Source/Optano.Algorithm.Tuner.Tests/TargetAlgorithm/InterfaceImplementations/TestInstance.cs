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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations
{
    using System;

    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// An instance used for testing.
    /// </summary>
    public class TestInstance : InstanceBase
    {
        #region Fields

        /// <summary>
        /// The instance's name.
        /// </summary>
        private readonly string _name;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestInstance"/> class.
        /// </summary>
        /// <param name="name">The instance's name.</param>
        public TestInstance(string name)
        {
            this._name = name;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns a <see cref="string"/> representing this instance.
        /// </summary>
        /// <returns>The instance name.</returns>
        public override string ToString()
        {
            return this._name;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current instance by comparing
        /// <see cref="_name"/>.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True iff the object is equal to this instance.</returns>
        public override bool Equals(object obj)
        {
            var otherInstance = obj as TestInstance;
            return string.Equals(this._name, otherInstance?._name, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns a hash code for this <see cref="TestInstance"/>.
        /// </summary>
        /// <returns>A hash code for this <see cref="TestInstance"/>.</returns>
        public override int GetHashCode()
        {
            return this._name.GetHashCode();
        }

        #endregion
    }
}