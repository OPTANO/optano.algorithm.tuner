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

namespace Optano.Algorithm.Tuner.TargetAlgorithm.Results
{
    using System;

    /// <summary>
    /// An implementation of <see cref="ResultBase{TResultType}"/> that holds a runtime.
    /// </summary>
    public class RuntimeResult : ResultBase<RuntimeResult>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeResult"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public RuntimeResult(TimeSpan runtime)
            : base(runtime)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeResult"/> class. 
        /// Empty ctor required for <see cref="ResultBase{TResultType}.CreateCancelledResult"/>.
        /// </summary>
        public RuntimeResult()
            : this(TimeSpan.MaxValue)
        {
        }

        #endregion
    }
}