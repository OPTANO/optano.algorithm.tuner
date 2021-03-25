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

namespace Optano.Algorithm.Tuner.Tests.Parameters.ParameterTreeNodes
{
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="ValueNode{T}"/>.
    /// </summary>
    public class ValueNodeTest : ParameterNodeTest
    {
        #region Fields

        /// <summary>
        /// <see cref="ValueNode{T}"/> used in tests. 
        /// </summary>
        private readonly ValueNode<int> _valueNode;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueNodeTest"/> class.
        /// </summary>
        public ValueNodeTest()
        {
            this._valueNode = new ValueNode<int>(ParameterNodeTest.Identifier, new IntegerDomain());
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="ValueNode{T}.Children"/> returns an empty collection if no child is set.
        /// </summary>
        [Fact]
        public void NoChildResultsInEmptyCollection()
        {
            Assert.False(this._valueNode.Children.Any(), "No children should be returned if none is set.");
        }

        /// <summary>
        /// Checks that <see cref="ValueNode{T}.Children"/> returns the single child that was set if one was set.
        /// </summary>
        [Fact]
        public void ChildIsReturnedCorrectly()
        {
            IParameterTreeNode child = new AndNode();
            this._valueNode.SetChild(child);
            Assert.True(
                TestUtils.SetsAreEquivalent(new List<IParameterTreeNode> { child }, this._valueNode.Children),
                $"Expected single child, but got {TestUtils.PrintList(this._valueNode.Children)}.");
        }

        /// <summary>
        /// Checks that adding a child for the second time replaces the old child.
        /// </summary>
        [Fact]
        public void AddingChildTwiceDoesOverwrite()
        {
            // Set and replace child.
            IParameterTreeNode child1 = new AndNode();
            this._valueNode.SetChild(child1);
            IParameterTreeNode child2 = new AndNode();
            this._valueNode.SetChild(child2);

            // Check that old child is not included in children anymore, but new child is.
            var children = this._valueNode.Children;
            Assert.False(
                TestUtils.SetsAreEquivalent(new List<IParameterTreeNode> { child1 }, children),
                $"Old child should have been replaced.");
            Assert.True(
                TestUtils.SetsAreEquivalent(new List<IParameterTreeNode> { child2 }, children),
                $"New child should have been returned.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a <see cref="ValueNode{T}"/>.
        /// </summary>
        /// <typeparam name="T">The parameter's type.</typeparam>
        /// <param name="identifier">The identifier.</param>
        /// <param name="domain">The parameter's domain.</param>
        /// <returns>The created node.</returns>
        protected override ParameterNodeBase<T> ConstructNode<T>(string identifier, CategoricalDomain<T> domain)
        {
            return new ValueNode<T>(identifier, domain);
        }

        #endregion
    }
}