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
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Parameters.Domains;

    /// <summary>
    /// Typed implementation of <see cref="IParameterNode" />.
    /// </summary>
    /// <typeparam name="T">The parameter's type.</typeparam>
    public abstract class ParameterNodeBase<T> : IParameterNode
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterNodeBase{T}" /> class.
        /// </summary>
        /// <param name="identifier">The parameter's identifier. Must be unique.</param>
        protected ParameterNodeBase(string identifier)
        {
            this.Identifier = identifier;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the parameter's unique identifier.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Gets the parameter's domain.
        /// </summary>
        public abstract IDomain Domain { get; }

        /// <summary>
        /// Gets the node's children.
        /// </summary>
        public abstract IEnumerable<IParameterTreeNode> Children { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns a <see cref="string" /> that represents this node.
        /// </summary>
        /// <returns>The node's identifier.</returns>
        public override string ToString()
        {
            return this.Identifier;
        }

        #endregion
    }
}