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

    /// <summary>
    /// A node of the <see cref="ParameterTree" /> that partitions parameters into groups that can be optimized
    /// independently.
    /// </summary>
    public class AndNode : IParameterTreeNode
    {
        #region Fields

        /// <summary>
        /// The node's children. Might be empty.
        /// </summary>
        private readonly List<IParameterTreeNode> _children = new List<IParameterTreeNode>();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AndNode" /> class.
        /// </summary>
        public AndNode()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AndNode" /> class.
        /// </summary>
        /// <param name="children">The children to add to the node.</param>
        public AndNode(IEnumerable<IParameterTreeNode> children)
        {
            this._children.AddRange(children);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the node's children.
        /// </summary>
        public IEnumerable<IParameterTreeNode> Children => this._children.AsReadOnly();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Adds a child.
        /// </summary>
        /// <param name="node">The child to add.</param>
        public void AddChild(IParameterTreeNode node)
        {
            this._children.Add(node);
        }

        #endregion
    }
}