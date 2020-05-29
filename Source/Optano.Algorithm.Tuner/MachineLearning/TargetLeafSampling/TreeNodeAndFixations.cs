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

namespace Optano.Algorithm.Tuner.MachineLearning.TargetLeafSampling
{
    using System.Collections.Generic;

    using SharpLearning.DecisionTrees.Nodes;

    /// <summary>
    /// Wrapper that stores the current <see cref="TargetLeafGenomeFixation"/>s (by feature index) within the current <see cref="Node"/>.
    /// </summary>
    public class TreeNodeAndFixations
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNodeAndFixations"/> class.
        /// </summary>
        /// <param name="currentNode">
        /// The current node.
        /// </param>
        /// <param name="fixedIndicesInDoubleRepresentation">
        /// The fixations (from earlier splits) by index in the double representation. 
        /// Dictionary will be copied. Parameter can be null.
        /// </param>
        public TreeNodeAndFixations(Node currentNode, Dictionary<int, TargetLeafGenomeFixation> fixedIndicesInDoubleRepresentation)
        {
            this.CurrentNode = currentNode;
            this.FixedIndicesInDoubleRepresentation = fixedIndicesInDoubleRepresentation == null
                                                          ? new Dictionary<int, TargetLeafGenomeFixation>()
                                                          : new Dictionary<int, TargetLeafGenomeFixation>(fixedIndicesInDoubleRepresentation);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNodeAndFixations"/> class.
        /// Simple copy constructor.
        /// </summary>
        /// <param name="source">
        /// The source to copy from.
        /// Must not be null.
        /// </param>
        private TreeNodeAndFixations(TreeNodeAndFixations source)
            : this(source.CurrentNode, source.FixedIndicesInDoubleRepresentation)
        {
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the current node.
        /// </summary>
        public Node CurrentNode { get; }

        /// <summary>
        /// Gets the fixations (from earlier splits) by index in the double representation.
        /// </summary>
        public Dictionary<int, TargetLeafGenomeFixation> FixedIndicesInDoubleRepresentation { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates a copy of the current <see cref="TreeNodeAndFixations"/>.
        /// The <see cref="FixedIndicesInDoubleRepresentation"/> will be re-created (i.e. deeply copied).
        /// </summary>
        /// <returns>
        /// The copied <see cref="TreeNodeAndFixations"/>.
        /// </returns>
        public TreeNodeAndFixations CreateCopy()
        {
            return new TreeNodeAndFixations(this);
        }

        #endregion
    }
}