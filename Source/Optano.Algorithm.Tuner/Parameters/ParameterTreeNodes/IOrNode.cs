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

namespace Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes
{
    using System;

    /// <summary>
    /// A node of <see cref="ParameterTree" /> that splits the parameters into groups
    /// that only become active for a certain parameter value each.
    /// </summary>
    internal interface IOrNode
    {
        #region Public Methods and Operators

        /// <summary>
        /// Looks for the child that gets activated for the given value.
        /// </summary>
        /// <param name="value">The value to find the fitting child for.</param>
        /// <param name="child">Outputs the child if one was found, else null.</param>
        /// <exception cref="ArgumentException">Thrown if the given value is not a legal value for the node.</exception>
        /// <returns>Whether a child was found.</returns>
        bool TryGetChild(object value, out IParameterTreeNode child);

        #endregion
    }
}