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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations
{
    using System;

    /// <summary>
    /// A variant of <see cref="NoOperation"/> that mock-ingly implements <see cref="IDisposable"/>-.
    /// </summary>
    internal class DisposableNoOp : NoOperation, IDisposable
    {
        #region Public properties

        /// <summary>
        /// Gets a value indicating whether the object has been disposed of already.
        /// </summary>
        public bool HasBeenDisposed { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            this.HasBeenDisposed = true;
        }

        #endregion
    }
}