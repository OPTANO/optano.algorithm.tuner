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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration
{
    using System;

    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// An implementation of <see cref="ResultBase{TResultType}"/> with a runtime of 0 and the possibility to store an
    /// integer value.
    /// </summary>
    public class IntegerResult : ResultBase<IntegerResult>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegerResult"/> class.
        /// </summary>
        /// <param name="value">The value to store.</param>
        public IntegerResult(int value)
            : base(TimeSpan.Zero)
        {
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegerResult"/> class.
        /// </summary>
        public IntegerResult()
            : this(0)
        {
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the stored value.
        /// </summary>
        public int Value { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns the string representation of the object.
        /// </summary>
        /// <returns>The stored value as string.</returns>
        public override string ToString()
        {
            return this.Value.ToString();
        }

        #endregion
    }
}