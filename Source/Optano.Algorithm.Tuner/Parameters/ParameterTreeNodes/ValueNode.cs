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
    /// A node of <see cref="ParameterTree" /> that represents a parameter and has at most one child.
    /// </summary>
    /// <typeparam name="T">The parameter's type.</typeparam>
    public class ValueNode<T> : ParameterNodeBase<T>
    {
        #region Fields

        /// <summary>
        /// The node's domain.
        /// </summary>
        private readonly DomainBase<T> _domain;

        /// <summary>
        /// The node's child. Might be null.
        /// </summary>
        private IParameterTreeNode _child;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueNode{T}" /> class.
        /// </summary>
        /// <param name="identifier">The parameter's identifier. Must be unique.</param>
        /// <param name="domain">The parameter's domain.</param>
        public ValueNode(string identifier, DomainBase<T> domain)
            : base(identifier)
        {
            this._domain = domain;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the parameter's domain.
        /// </summary>
        public override IDomain Domain => this._domain;

        /// <summary>
        /// Gets the node's children: A set that is either empty or contains a single child.
        /// </summary>
        public override IEnumerable<IParameterTreeNode> Children
        {
            get
            {
                var childList = new List<IParameterTreeNode>(1);
                if (this._child != null)
                {
                    childList.Add(this._child);
                }

                return childList.AsReadOnly();
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Sets the node's child. Overwrites it if it was already set.
        /// </summary>
        /// <param name="child">The child.</param>
        public void SetChild(IParameterTreeNode child)
        {
            this._child = child;
        }

        #endregion
    }
}