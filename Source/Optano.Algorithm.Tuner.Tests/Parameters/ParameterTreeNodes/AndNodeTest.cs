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

namespace Optano.Algorithm.Tuner.Tests.Parameters.ParameterTreeNodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="AndNode"/>.
    /// </summary>
    public class AndNodeTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="AndNode"/>'s constructor with the children parameter set to null throws an
        /// <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingChildren()
        {
            Assert.Throws<ArgumentNullException>(() => new AndNode(children: null));
        }

        /// <summary>
        /// Checks that added children are returned correctly when using <see cref="AndNode.Children"/>.
        /// </summary>
        [Fact]
        public void ChildrenAreReturnedCorrectly()
        {
            var andNode = new AndNode();

            // Add several children...
            var childIdentifiers = new List<string> { "a", "b", "c", "d" };
            var children = new List<IParameterTreeNode>();
            for (int i = 0; i < childIdentifiers.Count; i++)
            {
                IParameterTreeNode child = AndNodeTest.CreateNode(childIdentifiers[i]);
                children.Add(child);
                andNode.AddChild(child);
            }

            // ..and compare them with the returned set when querying for the node's children.
            Assert.True(
                TestUtils.SetsAreEquivalent(children, andNode.Children),
                $"Added children {TestUtils.PrintList(children)}, but return value was {TestUtils.PrintList(andNode.Children)}");
        }

        /// <summary>
        /// Checks that the children provided on construction are a subset of the ones returned by
        /// <see cref="AndNode.Children"/>.
        /// </summary>
        [Fact]
        public void ChildrenProvidedOnConstructionAreAdded()
        {
            // Initialize and node with several children.
            List<IParameterTreeNode> children =
                Enumerable.Range(0, 3).Select(index => AndNodeTest.CreateNode(index.ToString())).ToList();
            var andNode = new AndNode(children);

            // Add an additional one.
            andNode.AddChild(AndNodeTest.CreateNode("additional"));

            // Make sure the original ones are returned by the GetChildren method.
            var storedChildren = andNode.Children;
            Assert.True(
                children.All(child => storedChildren.Contains(child)),
                $"Not all of {TestUtils.PrintList(children)} have been stored in the node's children: {TestUtils.PrintList(storedChildren)}.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a simple node with the given identifier.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>The created node.</returns>
        private static IParameterTreeNode CreateNode(string identifier)
        {
            return new ValueNode<int>(identifier, new IntegerDomain());
        }

        #endregion
    }
}