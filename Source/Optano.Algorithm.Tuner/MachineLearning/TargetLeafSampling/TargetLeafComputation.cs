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

    using Optano.Algorithm.Tuner.MachineLearning.GenomeRepresentation;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;

    using SharpLearning.DecisionTrees.Nodes;

    /// <summary>
    ///     Helper class that handles the traversal of a tree in the search of reachable target leafs.
    /// </summary>
    public static class TargetLeafComputation
    {
        #region Public Methods and Operators

        /// <summary>
        /// Computes the reached target leaves for the current tree.
        /// </summary>
        /// <param name="tree">
        /// The tree.
        /// </param>
        /// <param name="parentGenomes">
        /// The parent genomes.
        /// </param>
        /// <param name="indexSetsForCategoricalFeaturesInDoubleRepresentation">
        /// The index sets for categorical features in double representation.
        ///     (
        ///     <see cref="GeneticEngineering{TLearnerModel,TPredictorModel,TSamplingStrategy}.ComputeIndexSetsForCategoricalFeaturesInDoubleRepresentation"/>
        ///     ).
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{TreeNodeAndFixations}"/> that conatins all leaf nodes that an offspring of the
        ///     <paramref name="parentGenomes"/> can fall into.
        /// </returns>
        public static IEnumerable<TreeNodeAndFixations> ComputeReachableTargetLeavesForTree(
            GenomePredictionTree tree,
            ParentGenomesConverted parentGenomes,
            Dictionary<int, HashSet<int>> indexSetsForCategoricalFeaturesInDoubleRepresentation)
        {
            if (indexSetsForCategoricalFeaturesInDoubleRepresentation == null)
            {
                indexSetsForCategoricalFeaturesInDoubleRepresentation = new Dictionary<int, HashSet<int>>(0);
            }

            var queue = new List<TreeNodeAndFixations> { new TreeNodeAndFixations(tree.Root, null) };
            var head = 0;

            while (head < queue.Count)
            {
                var currentNodeAndFixations = queue[head++];
                var currentNode = currentNodeAndFixations.CurrentNode;

                // check if we reached a leaf:
                if (currentNode.IsLeafNode())
                {
                    yield return currentNodeAndFixations.CreateCopy();
                    continue;
                }

                var competitiveParentGoesLeft = CheckIfParentGoesLeft(parentGenomes.CompetitiveParent, currentNode);
                var nonCompetitiveParentGoesLeft = CheckIfParentGoesLeft(parentGenomes.NonCompetitiveParent, currentNode);
                var parentsFollowSamePath = competitiveParentGoesLeft == nonCompetitiveParentGoesLeft;

                // no need to fix features
                if (parentsFollowSamePath)
                {
                    var childNode = tree.Nodes[competitiveParentGoesLeft ? currentNode.LeftIndex : currentNode.RightIndex];
                    var childNodeAndFixation = new TreeNodeAndFixations(childNode, currentNodeAndFixations.FixedIndicesInDoubleRepresentation);
                    queue.Add(childNodeAndFixation);
                    continue;
                }

                // we know that parents will take separate paths.
                AddTreeNodeAndFixationsIfParentCanContinue(
                    parentGenomes,
                    tree,
                    currentNodeAndFixations,
                    TargetLeafGenomeFixation.FixedToCompetitiveParent,
                    indexSetsForCategoricalFeaturesInDoubleRepresentation,
                    queue);
                AddTreeNodeAndFixationsIfParentCanContinue(
                    parentGenomes,
                    tree,
                    currentNodeAndFixations,
                    TargetLeafGenomeFixation.FixedToNonCompetitiveParent,
                    indexSetsForCategoricalFeaturesInDoubleRepresentation,
                    queue);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks if the given <paramref name="parentToCheck"/> can follow the current tree path.
        /// </summary>
        /// <param name="parents">
        /// The converted parent genomes.
        /// </param>
        /// <param name="tree">
        /// The tree.
        /// </param>
        /// <param name="currentNodeAndFixations">
        /// The current node and feature fixations.
        /// </param>
        /// <param name="parentToCheck">
        /// The parent to check.
        /// </param>
        /// <param name="indexSetsForCategoricalFeaturesInDoubleRepresentation">
        /// The index sets for categorical features in double representation.
        /// </param>
        /// <param name="queue">
        /// The queue of open nodes to traverse.
        ///     If the <paramref name="parentToCheck"/> can continue (i.e. the current node's feature is not fixed to the 'other'
        ///     parent),
        ///     the child node that is visited by <paramref name="parentToCheck"/> will be added (and fixed to the
        ///     <paramref name="parentToCheck"/>).
        /// </param>
        private static void AddTreeNodeAndFixationsIfParentCanContinue(
            ParentGenomesConverted parents,
            GenomePredictionTree tree,
            TreeNodeAndFixations currentNodeAndFixations,
            TargetLeafGenomeFixation parentToCheck,
            Dictionary<int, HashSet<int>> indexSetsForCategoricalFeaturesInDoubleRepresentation,
            List<TreeNodeAndFixations> queue)
        {
            var canParentToCheckContinue = CheckIfGenomeCanContinueAlone(currentNodeAndFixations, parentToCheck);
            if (!canParentToCheckContinue)
            {
                return;
            }

            var currentParent = parents.GetParentToFollow(parentToCheck);
            var currentNode = currentNodeAndFixations.CurrentNode;
            var parentGoesLeft = CheckIfParentGoesLeft(currentParent, currentNode);
            var nextNodeInPath = tree.Nodes[parentGoesLeft ? currentNode.LeftIndex : currentNode.RightIndex];
            var competitiveChildNodeAndFixation = new TreeNodeAndFixations(
                nextNodeInPath,
                currentNodeAndFixations.FixedIndicesInDoubleRepresentation);

            // now add fixation
            if (!indexSetsForCategoricalFeaturesInDoubleRepresentation.ContainsKey(currentNode.FeatureIndex))
            {
                competitiveChildNodeAndFixation.FixedIndicesInDoubleRepresentation[currentNode.FeatureIndex] = parentToCheck;
            }
            else
            {
                // fix all columns of a feature, when one of them is used for a split.
                var allFeatureIndices = indexSetsForCategoricalFeaturesInDoubleRepresentation[currentNode.FeatureIndex];

                foreach (var categoricalFeatureIndex in allFeatureIndices)
                {
                    competitiveChildNodeAndFixation.FixedIndicesInDoubleRepresentation[categoricalFeatureIndex] = parentToCheck;
                }
            }

            queue.Add(competitiveChildNodeAndFixation);
        }

        /// <summary>
        /// Checks if parent can continue alone.
        ///     I.e. the current node's split feature index is not fixed to the 'other' parent.
        /// </summary>
        /// <param name="currentNodeAndFixations">
        /// The current node and fixations.
        /// </param>
        /// <param name="parentToCheck">
        /// The parent to check.
        /// </param>
        /// <returns>
        /// <c>True</c>, iff the current node's split feature index is not fixed to the 'other' parent.
        /// </returns>
        private static bool CheckIfGenomeCanContinueAlone(TreeNodeAndFixations currentNodeAndFixations, TargetLeafGenomeFixation parentToCheck)
        {
            var currentFeatureIndex = currentNodeAndFixations.CurrentNode.FeatureIndex;
            return !IsFixedToDifferentGenome(currentFeatureIndex, parentToCheck, currentNodeAndFixations.FixedIndicesInDoubleRepresentation);
        }

        /// <summary>
        /// Checks if the <paramref name="parentGenome"/> is follows the left or right child node.
        /// </summary>
        /// <param name="parentGenome">
        /// The parent genome.
        /// </param>
        /// <param name="currentNode">
        /// The current node.
        /// </param>
        /// <returns>
        /// <c>True</c>, iff the parent's split feature value is less or equal to the split value stored in the current node.
        /// </returns>
        private static bool CheckIfParentGoesLeft(GenomeDoubleRepresentation parentGenome, Node currentNode)
        {
            return parentGenome[currentNode.FeatureIndex] <= currentNode.Value;
        }

        /// <summary>
        /// Checks if the given <paramref name="featureIndex"/> is fixed to the 'other' genome.
        /// </summary>
        /// <param name="featureIndex">
        /// The feature index.
        /// </param>
        /// <param name="currentGenomeType">
        /// The current genome type.
        /// </param>
        /// <param name="featureFixations">
        /// The feature fixations.
        /// </param>
        /// <returns>
        /// <c>True</c>, iff <paramref name="featureFixations"/> contains a fixation to the 'other' parent (for the current
        ///     <paramref name="featureIndex"/>).
        /// </returns>
        private static bool IsFixedToDifferentGenome(
            int featureIndex,
            TargetLeafGenomeFixation currentGenomeType,
            Dictionary<int, TargetLeafGenomeFixation> featureFixations)
        {
            return featureFixations.ContainsKey(featureIndex) && featureFixations[featureIndex] != currentGenomeType;
        }

        #endregion
    }
}