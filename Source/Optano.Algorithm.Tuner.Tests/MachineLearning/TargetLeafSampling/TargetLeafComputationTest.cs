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

namespace Optano.Algorithm.Tuner.Tests.MachineLearning.TargetLeafSampling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.TargetLeafSampling;

    using SharpLearning.DecisionTrees.Nodes;

    using Xunit;

    /// <summary>
    /// The target leaf computation test.
    /// </summary>
    public class TargetLeafComputationTest : IDisposable
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetLeafComputationTest"/> class.
        /// </summary>
        public TargetLeafComputationTest()
        {
            this.CreateTree();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the all leaves.
        /// </summary>
        private Node[] AllLeaves { get; set; }

        /// <summary>
        /// Gets or sets the tree.
        /// </summary>
        private GenomePredictionTree Tree { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The cleanup.
        /// </summary>
        public void Dispose()
        {
            this.Tree = null;
            this.AllLeaves = null;
        }

        /// <summary>
        /// Tests that <see cref="TargetLeafComputation"/> returns all 4 reachable leaves.
        /// </summary>
        [Fact]
        public void AllTargetLeavesAreFound()
        {
            // This can produce 4 possible offspring (Feature 2 is not used for splits!)
            var competitive = new double[] { 0, 1, 1 };
            var nonCompetitive = new double[] { 1, 0, 1 };
            var parents = new ParentGenomesConverted(competitive, nonCompetitive);

            var reachedLeaves = TargetLeafComputation.ComputeReachableTargetLeavesForTree(this.Tree, parents, new Dictionary<int, HashSet<int>>(0));
            var orderedResult = reachedLeaves.OrderBy(l => l.CurrentNode.Value).ToArray();
            Assert.Equal(this.AllLeaves.Length, orderedResult.Length);

            for (var reachedLeafIndex = 0; reachedLeafIndex < orderedResult.Length; reachedLeafIndex++)
            {
                var reachedLeafAndFixation = orderedResult[reachedLeafIndex];
                Assert.Equal(
                    reachedLeafIndex,
                    reachedLeafAndFixation.CurrentNode.Value);

                Assert.True(reachedLeafAndFixation.FixedIndicesInDoubleRepresentation.ContainsKey(0), "Feature 0 should always be fixed.");
                Assert.True(reachedLeafAndFixation.FixedIndicesInDoubleRepresentation.ContainsKey(1), "Feature 1 should always be fixed.");
                Assert.False(reachedLeafAndFixation.FixedIndicesInDoubleRepresentation.ContainsKey(2), "Feature 2 should not be fixed.");
            }
        }

        /// <summary>
        /// Tests that <see cref="TargetLeafComputation"/> returns all 4 reachable leaves.
        /// Uses different parents than <see cref="AllTargetLeavesAreFound"/>.
        /// </summary>
        [Fact]
        public void AllTargetLeavesAreFoundVersionB()
        {
            // This can produce 4 possible offspring. (Feature 2 is not used for splits!)
            var competitive = new double[] { 0, 0, 0 };
            var nonCompetitive = new double[] { 1, 1, 1 };
            var parents = new ParentGenomesConverted(competitive, nonCompetitive);

            var reachedLeaves = TargetLeafComputation.ComputeReachableTargetLeavesForTree(this.Tree, parents, new Dictionary<int, HashSet<int>>(0));
            var orderedResult = reachedLeaves.OrderBy(l => l.CurrentNode.Value).ToArray();

            Assert.Equal(this.AllLeaves.Length, orderedResult.Length);

            for (var reachedLeafIndex = 0; reachedLeafIndex < orderedResult.Length; reachedLeafIndex++)
            {
                var reachedLeafAndFixation = orderedResult[reachedLeafIndex];
                Assert.Equal(
                    reachedLeafIndex,
                    reachedLeafAndFixation.CurrentNode.Value);

                Assert.True(reachedLeafAndFixation.FixedIndicesInDoubleRepresentation.ContainsKey(0), "Feature 0 should always be fixed.");
                Assert.True(reachedLeafAndFixation.FixedIndicesInDoubleRepresentation.ContainsKey(1), "Feature 1 should always be fixed.");
                Assert.False(reachedLeafAndFixation.FixedIndicesInDoubleRepresentation.ContainsKey(2), "Feature 2 should not be fixed.");
            }
        }

        /// <summary>
        /// Tests that not all leaves are reachable when a feature is duplicated.
        /// </summary>
        [Fact]
        public void DuplicateFeatureReducesReachableLeaves()
        {
            // This can only produce 2 possible offspring.
            // (Feature 2 is not used for splits!)
            var competitive = new double[] { 0, 0, 0 };
            var nonCompetitive = new double[] { 0, 1, 1 };
            var parents = new ParentGenomesConverted(competitive, nonCompetitive);

            var reachedLeaves = TargetLeafComputation.ComputeReachableTargetLeavesForTree(this.Tree, parents, new Dictionary<int, HashSet<int>>(0));
            var orderedResult = reachedLeaves.OrderBy(l => l.CurrentNode.Value).ToArray();

            Assert.Equal(2, orderedResult.Length);

            Assert.Equal(0, orderedResult[0].CurrentNode.Value);
            Assert.False(orderedResult[0].FixedIndicesInDoubleRepresentation.ContainsKey(0), "Feature 0 should not be fixed.");
            Assert.True(orderedResult[0].FixedIndicesInDoubleRepresentation.ContainsKey(1), "Feature 1 should be fixed.");
            Assert.Equal(TargetLeafGenomeFixation.FixedToCompetitiveParent, orderedResult[0].FixedIndicesInDoubleRepresentation[1]);
            Assert.False(orderedResult[0].FixedIndicesInDoubleRepresentation.ContainsKey(2), "Feature 2 should not be fixed.");

            Assert.Equal(1, orderedResult[1].CurrentNode.Value);
            Assert.False(orderedResult[1].FixedIndicesInDoubleRepresentation.ContainsKey(0), "Feature 0 should not be fixed.");
            Assert.True(orderedResult[1].FixedIndicesInDoubleRepresentation.ContainsKey(1), "Feature 1 should be fixed.");
            Assert.Equal(TargetLeafGenomeFixation.FixedToNonCompetitiveParent, orderedResult[1].FixedIndicesInDoubleRepresentation[1]);
            Assert.False(orderedResult[1].FixedIndicesInDoubleRepresentation.ContainsKey(2), "Feature 2 should not be fixed.");
        }

        /// <summary>
        /// Tests that not all leaves are reachable when a feature is duplicated.
        /// Uses different parents than <see cref="DuplicateFeatureReducesReachableLeaves"/> and <see cref="DuplicateFeatureReducesReachableLeavesVersionC"/>.
        /// </summary>
        [Fact]
        public void DuplicateFeatureReducesReachableLeavesVersionB()
        {
            // This can only produce 2 possible offspring
            // (Feature 2 is not used for splits!)
            var competitive = new double[] { 0, 1, 1 };
            var nonCompetitive = new double[] { 1, 1, 1 };
            var parents = new ParentGenomesConverted(competitive, nonCompetitive);

            var reachedLeaves = TargetLeafComputation.ComputeReachableTargetLeavesForTree(this.Tree, parents, new Dictionary<int, HashSet<int>>(0));
            var orderedResult = reachedLeaves.OrderBy(l => l.CurrentNode.Value).ToArray();

            Assert.Equal(2, orderedResult.Length);

            Assert.Equal(1, orderedResult[0].CurrentNode.Value);
            Assert.True(orderedResult[0].FixedIndicesInDoubleRepresentation.ContainsKey(0), "Feature 0 should be fixed.");
            Assert.Equal(TargetLeafGenomeFixation.FixedToCompetitiveParent, orderedResult[0].FixedIndicesInDoubleRepresentation[0]);
            Assert.False(orderedResult[0].FixedIndicesInDoubleRepresentation.ContainsKey(1), "Feature 1 should not be fixed.");
            Assert.False(orderedResult[0].FixedIndicesInDoubleRepresentation.ContainsKey(2), "Feature 2 should not be fixed.");

            Assert.Equal(3, orderedResult[1].CurrentNode.Value);
            Assert.True(orderedResult[1].FixedIndicesInDoubleRepresentation.ContainsKey(0), "Feature 0 should be fixed.");
            Assert.Equal(TargetLeafGenomeFixation.FixedToNonCompetitiveParent, orderedResult[1].FixedIndicesInDoubleRepresentation[0]);
            Assert.False(orderedResult[1].FixedIndicesInDoubleRepresentation.ContainsKey(1), "Feature 1 should not be fixed.");
            Assert.False(orderedResult[1].FixedIndicesInDoubleRepresentation.ContainsKey(2), "Feature 2 should not be fixed.");
        }

        /// <summary>
        /// Tests that not all leaves are reachable when a feature is duplicated.
        /// Uses different parents than <see cref="DuplicateFeatureReducesReachableLeaves"/> and <see cref="DuplicateFeatureReducesReachableLeavesVersionB"/>.
        /// </summary>
        [Fact]
        public void DuplicateFeatureReducesReachableLeavesVersionC()
        {
            // This can only produce 1 possible offspring
            // (Feature 2 is not used for splits!)
            var competitive = new double[] { 0, 1, 0 };
            var nonCompetitive = new double[] { 0, 1, 1 };
            var parents = new ParentGenomesConverted(competitive, nonCompetitive);

            var reachedLeaves = TargetLeafComputation.ComputeReachableTargetLeavesForTree(this.Tree, parents, new Dictionary<int, HashSet<int>>(0));
            var orderedResult = reachedLeaves.OrderBy(l => l.CurrentNode.Value).ToArray();
            Assert.Single(orderedResult);
            Assert.Equal(1, orderedResult[0].CurrentNode.Value);
            Assert.Empty(orderedResult[0].FixedIndicesInDoubleRepresentation);
        }

        /// <summary>
        /// Tests that fixations and reachable leaves can be computed on unbalanced trees.
        /// Parents can reach all leaves.
        /// </summary>
        [Fact]
        public void UnbalancedTreeIsHandledCorrectly()
        {
            this.CreateUnbalancedTree();
            var competitive = new double[] { 0, 0, 0 };
            var nonCompetitive = new double[] { 1, 1, 1 };
            var parents = new ParentGenomesConverted(competitive, nonCompetitive);

            var reachableLeaves = TargetLeafComputation.ComputeReachableTargetLeavesForTree(this.Tree, parents, new Dictionary<int, HashSet<int>>())
                .OrderBy(l => l.CurrentNode.Value).ToArray();

            Assert.Equal(3, reachableLeaves.Length);

            for (var i = 0; i < reachableLeaves.Length; i++)
            {
                Assert.True(i == reachableLeaves[i].CurrentNode.Value, $"Leaf should have label {i}");
                Assert.True(reachableLeaves[i].FixedIndicesInDoubleRepresentation.ContainsKey(0), "First feature should always be fixed");
                Assert.False(reachableLeaves[i].FixedIndicesInDoubleRepresentation.ContainsKey(2), "Third feature should never be fixed");

                if (i == 0)
                {
                    Assert.False(
                        reachableLeaves[i].FixedIndicesInDoubleRepresentation.ContainsKey(1),
                        "2nd Feature should not be fixed in first leaf.");
                }
                else
                {
                    Assert.True(reachableLeaves[i].FixedIndicesInDoubleRepresentation.ContainsKey(1), "2nd feature should be fixed in other leaves.");
                }
            }
        }

        /// <summary>
        /// Tests that only the first leaf is returned for duplicate feature 0.
        /// </summary>
        [Fact]
        public void UnbalancedTreeWithDuplicateFeature()
        {
            this.CreateUnbalancedTree();
            var competitive = new double[] { 0, 0, 0 };
            var nonCompetitive = new double[] { 0, 1, 1 };
            var parents = new ParentGenomesConverted(competitive, nonCompetitive);

            var reachableLeaves = TargetLeafComputation.ComputeReachableTargetLeavesForTree(this.Tree, parents, new Dictionary<int, HashSet<int>>())
                .OrderBy(l => l.CurrentNode.Value).ToArray();

            Assert.Single(reachableLeaves);
            Assert.Equal(0, reachableLeaves[0].CurrentNode.Value);
            Assert.Empty(reachableLeaves[0].FixedIndicesInDoubleRepresentation);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a binary tree with 4 leaves.
        /// </summary>
        private void CreateTree()
        {
            ////        a
            //      0/      \1
            //      b        b
            //  0/    \1  0/    \1
            //  |      |  |      |
            // (0)    (1) (2)    (3)
            var root = new Node(0, 0.5, 1, 2, 0, 0);
            var node1 = new Node(1, 0.5, 3, 4, 1, 1);
            var node2 = new Node(1, 0.5, 5, 6, 2, 2);
            var leaf1 = new Node(-1, 0, -1, -1, 3, 3);
            var leaf2 = new Node(-1, 1, -1, -1, 4, 4);
            var leaf3 = new Node(-1, 2, -1, -1, 5, 5);
            var leaf4 = new Node(-1, 3, -1, -1, 6, 6);

            var allNodes = new List<Node>() { root, node1, node2, leaf1, leaf2, leaf3, leaf4 };
            var leaves = new[] { leaf1, leaf2, leaf3, leaf4 };

            this.Tree = new GenomePredictionTree(allNodes, new List<double[]>(0), new double[0], new double[0]);
            this.AllLeaves = leaves;
        }

        /// <summary>
        /// Creates an unbalanced tree with 3 leaves.
        /// </summary>
        private void CreateUnbalancedTree()
        {
            ////        a
            //      0/      \1
            //      |        b
            //     (0)    0/    \1
            //            |      |
            //           (1)    (2)
            var root = new Node(0, 0.5, 1, 2, 0, 0);
            var leaf1 = new Node(-1, 0, -1, -1, 1, 1);
            var node2 = new Node(1, 0.5, 3, 4, 2, 2);
            var leaf2 = new Node(-1, 1, -1, -1, 3, 3);
            var leaf3 = new Node(-1, 2, -1, -1, 4, 4);

            var allNodes = new List<Node>() { root, leaf1, node2, leaf2, leaf3 };
            var leaves = new[] { leaf1, leaf2, leaf3 };

            this.Tree = new GenomePredictionTree(allNodes, new List<double[]>(0), new double[0], new double[0]);
            this.AllLeaves = leaves;
        }

        #endregion
    }
}