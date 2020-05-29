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

namespace Optano.Algorithm.Tuner.MachineLearning.RandomForest
{
    using System;
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.MachineLearning.Prediction;

    using SharpLearning.DecisionTrees.Nodes;

    /// <summary>
    /// A wrapper that exposes the <see cref="BinaryTree"/>'s <see cref="Root"/> node.
    /// </summary>
    public class GenomePredictionTree : BinaryTree, IWeakPredictor
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomePredictionTree"/> class.
        /// </summary>
        /// <param name="nodes">
        /// The nodes.
        /// </param>
        /// <param name="probabilities">
        /// Not used for regression trees.
        /// Argument is required for generic instanciation of <see cref="GenomePredictionTree"/> within SharpLearning code.
        /// </param>
        /// <param name="targetLabels">
        /// The target labels.
        /// </param>
        /// <param name="variableImportance">
        /// The variable importance.
        /// Not used for regression tree.
        /// </param>
        public GenomePredictionTree(List<Node> nodes, List<double[]> probabilities, double[] targetLabels, double[] variableImportance)
            : base(nodes, probabilities, targetLabels, variableImportance)
        {
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the root node.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown, if tree contains no nodes.
        /// </exception>
        public Node Root
        {
            get
            {
                if (this.Nodes.Count == 0)
                {
                    throw new InvalidOperationException("Tree is empty. Cannot access Root Node!");
                }

                return this.Nodes[0];
            }
        }

        #endregion
    }
}