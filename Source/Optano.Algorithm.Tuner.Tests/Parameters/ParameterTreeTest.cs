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

namespace Optano.Algorithm.Tuner.Tests.Parameters
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="ParameterTree"/>.
    /// </summary>
    public class ParameterTreeTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that the root set while constructing the tree is also returned by <see cref="ParameterTree.Root"/>.
        /// </summary>
        [Fact]
        public void RootIsSetCorrectly()
        {
            var root = new AndNode();
            var tree = new ParameterTree(root);
            Assert.Equal(root, tree.Root);
        }

        /// <summary>
        /// Checks that <see cref="ParameterTree.IdentifiersAreUnique"/> returns false if duplicate identifiers exist.
        /// </summary>
        [Fact]
        public void IdentifiesAreUniqueReturnsFalseForDuplicateIdentifiers()
        {
            string duplicateIdentifier = "a";

            // Build tree with two parameters having the same identifier.
            var root = new AndNode();
            root.AddChild(new ValueNode<int>(duplicateIdentifier, new IntegerDomain()));
            root.AddChild(new ValueNode<int>(duplicateIdentifier, new IntegerDomain()));
            var tree = new ParameterTree(root);

            // Check that this is recognized.
            Assert.False(
                tree.IdentifiersAreUnique(),
                $"Parameter tree contained duplicate identifier {duplicateIdentifier}, but that was not detected.");
        }

        /// <summary>
        /// Checks that <see cref="ParameterTree.IdentifiersAreUnique"/> returns true if all identifiers are unique.
        /// </summary>
        [Fact]
        public void IdentifiersAreUniqueReturnsTrueForUniqueIdentifiers()
        {
            // Build tree with two parameters having different identifiers.
            var root = new AndNode();
            root.AddChild(new ValueNode<int>("a", new IntegerDomain()));
            root.AddChild(new ValueNode<int>("b", new IntegerDomain()));
            var tree = new ParameterTree(root);

            // Check that this is recognized.
            Assert.True(
                tree.IdentifiersAreUnique(),
                "Parameter tree was wrongly identified as having duplicate identifiers.");
        }

        /// <summary>
        /// Checks that <see cref="ParameterTree.ContainsParameters"/> returns false
        /// if the tree only consists of AND nodes.
        /// </summary>
        [Fact]
        public void ContainsParametersReturnsFalseForTreeConsistingOfAndNodes()
        {
            // Build tree consisting of two AND nodes.
            var root = new AndNode();
            root.AddChild(new AndNode());
            var tree = new ParameterTree(root);

            // Check that it is recognized as having no parameters.
            Assert.False(
                tree.ContainsParameters(),
                "Parameter tree was wrongly identified as containing parameters.");
        }

        /// <summary>
        /// Checks that <see cref="ParameterTree.ContainsParameters"/> returns true if the tree contains a parameter.
        /// </summary>
        [Fact]
        public void ContainsParametersReturnsTrueForTreeContainingParameters()
        {
            // Build tree containing a node representing a parameter.
            var root = new AndNode();
            root.AddChild(new ValueNode<int>("parameter", new IntegerDomain()));
            var tree = new ParameterTree(root);

            // Check that it is recognized as having parameters.
            Assert.True(
                tree.ContainsParameters(),
                "Parameter tree was wrongly identified as not containing parameters.");
        }

        /// <summary>
        /// Checks that <see cref="ParameterTree.FindActiveIdentifiers"/>
        /// only adds the correct subtree of an OR node to the dictionary.
        /// </summary>
        [Fact]
        public void TestFindActiveIdentifiers()
        {
            // Build up a tree with OR node as root, child a (OR node) for value 0, child b (value node) for value 1.
            // Create the nodes.
            IParameterTreeNode a = new OrNode<int>("a", new CategoricalDomain<int>(new List<int> { 2, 5 }));
            IParameterTreeNode b = new ValueNode<int>("b", new IntegerDomain());
            var rootDecision = new OrNode<int>("or", new CategoricalDomain<int>(new List<int> { 0, 1 }));
            // Create connections.
            rootDecision.AddChild(0, a);
            rootDecision.AddChild(1, b);
            var parameterTree = new ParameterTree(rootDecision);

            // Set value for roort node s.t. only node a should be active.
            var values = new Dictionary<string, IAllele>(3)
                             {
                                 { "a", new Allele<int>(2) },
                                 { "b", new Allele<int>(7) },
                                 { "or", new Allele<int>(0) },
                             };

            var activeIdentifiers = parameterTree.FindActiveIdentifiers(values.ToImmutableDictionary()).ToList();
            var expectedActiveIdentifiers = new List<string> { "or", "a" };
            Assert.True(
                TestUtils.SetsAreEquivalent(activeIdentifiers, expectedActiveIdentifiers),
                $"Active identifiers should be {TestUtils.PrintList(expectedActiveIdentifiers)}, but are {TestUtils.PrintList(activeIdentifiers)}.");
        }

        /// <summary>
        /// Checks that a parameter tree without active filers can be deserialized.
        /// </summary>
        [Fact]
        public void CheckIfTreeCanBeDeserialized()
        {
            var root = new AndNode();
            root.AddChild(new ValueNode<int>("a", new IntegerDomain()));
            root.AddChild(new ValueNode<int>("b", new IntegerDomain()));
            var tree = new ParameterTree(root);

            var serializer = new Hyperion.Serializer();
            var treeStream = new MemoryStream();
            serializer.Serialize(tree, treeStream);
            var streamCopy = new MemoryStream(treeStream.GetBuffer());
            var restoredTree = serializer.Deserialize<ParameterTree>(streamCopy);
            Assert.NotNull(restoredTree);

            var originalNodes = tree.GetParameters().ToList();
            var restoredNodes = restoredTree.GetParameters().ToList();
            Assert.Equal(originalNodes.Count, restoredNodes.Count);
            for (var i = 0; i < originalNodes.Count; i++)
            {
                var expectedNode = originalNodes[i];
                var restoredNode = restoredNodes[i];

                Assert.True(expectedNode.Identifier == restoredNode.Identifier, "Nodes were deserialized in wrong order.");
                Assert.Equal(expectedNode.Domain.DomainSize, restoredNode.Domain.DomainSize);
            }
        }

        /// <summary>
        /// Checks that a parameter tree with active filers can be deserialized.
        /// </summary>
        [Fact]
        public void CheckIfTreeWithFiltersCanBeDeserialized()
        {
            var root = new AndNode();
            root.AddChild(new ValueNode<int>("a", new IntegerDomain()));
            root.AddChild(new ValueNode<int>("b", new IntegerDomain()));
            var tree = new ParameterTree(root);
            tree.AddParameterReplacementDefinition("a", 42, "dummy", 1337);
            tree.AddParameterReplacementDefinition("b", 42, "dummy2", 1337);
            tree.AddParameterReplacementDefinition("a", 43, "dummy", 1338);

            var serializer = new Hyperion.Serializer();
            var treeStream = new MemoryStream();
            serializer.Serialize(tree, treeStream);
            var streamCopy = new MemoryStream(treeStream.GetBuffer());
            var restoredTree = serializer.Deserialize<ParameterTree>(streamCopy);

            Assert.NotNull(restoredTree);
            var originalNodes = tree.GetParameters().ToList();
            var restoredNodes = restoredTree.GetParameters().ToList();

            Assert.Equal(originalNodes.Count, restoredNodes.Count);
            for (var i = 0; i < originalNodes.Count; i++)
            {
                var expectedNode = originalNodes[i];
                var restoredNode = restoredNodes[i];
                // It'd be too tedious to conduct a deeper comparison. Let's just assume that Nodes are correct when Name + DomainSize match.
                Assert.True(expectedNode.Identifier == restoredNode.Identifier, "Nodes were deserialized in wrong order.");
                Assert.Equal(expectedNode.Domain.DomainSize, restoredNode.Domain.DomainSize);
            }

            Assert.True(restoredTree.IsIndicatorParameterAndValueCombinationDefined("a", 42));
            Assert.True(restoredTree.IsIndicatorParameterAndValueCombinationDefined("b", 42));
            Assert.True(restoredTree.IsIndicatorParameterAndValueCombinationDefined("a", 43));
        }

        #endregion
    }
}