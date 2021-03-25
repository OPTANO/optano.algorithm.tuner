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
    using System;
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="OrNode{T}"/>.
    /// </summary>
    public class OrNodeTest : ParameterNodeTest
    {
        #region Static Fields

        /// <summary>
        /// Domain used in tests.
        /// </summary>
        private static readonly CategoricalDomain<string> Domain =
            new CategoricalDomain<string>(new List<string> { "a", "b", "c" });

        #endregion

        #region Fields

        /// <summary>
        /// <see cref="OrNode{T}"/> used in tests. Needs to be initialized. 
        /// </summary>
        private readonly OrNode<string> _decisionNode;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OrNodeTest"/> class.
        /// </summary>
        public OrNodeTest()
        {
            this._decisionNode = new OrNode<string>(ParameterNodeTest.Identifier, OrNodeTest.Domain);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="OrNode{T}"/>'s constructor with a reference type which is not string 
        /// results in an <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForNonStringReferenceType()
        {
            Assert.Throws<ArgumentException>(
                () => new OrNode<object>("test", new CategoricalDomain<object>(new List<object>())));
        }

        /// <summary>
        /// Checks that calling <see cref="OrNode{T}"/>'s constructor with the type 'string' does not throw an error.
        /// </summary>
        [Fact]
        public void ConstructorDoesNotThrowErrorForString()
        {
            var decisionNode = new OrNode<string>("test", new CategoricalDomain<string>(new List<string>()));
        }

        /// <summary>
        /// Checks that calling <see cref="OrNode{T}"/>'s constructor with a type parameter of type struct does not
        /// throw an exception.
        /// </summary>
        [Fact]
        public void ConstructorDoesNotThrowErrorForStruct()
        {
            var decisionNode = new OrNode<VerbosityLevel>(
                "test",
                new CategoricalDomain<VerbosityLevel>(new List<VerbosityLevel>()));
        }

        /// <summary>
        /// Checks that <see cref="OrNode{T}.Children"/> correctly returns all added children.
        /// </summary>
        [Fact]
        public void ChildrenAreReturnedCorrectly()
        {
            // Add several children...
            var children = new List<IParameterTreeNode>();
            var domainValues = OrNodeTest.Domain.PossibleValues;
            for (int i = 0; i < domainValues.Count; i++)
            {
                IParameterTreeNode child = OrNodeTest.CreateNode(domainValues[i] + "_child");
                children.Add(child);
                this._decisionNode.AddChild(domainValues[i], child);
            }

            // ..and compare them with the returned set when querying for the node's children.
            Assert.True(
                TestUtils.SetsAreEquivalent(children, this._decisionNode.Children),
                $"Added children {TestUtils.PrintList(children)}, but return value was {TestUtils.PrintList(this._decisionNode.Children)}");
        }

        /// <summary>
        /// Checks that an exception is thrown when several nodes are added for the same value.
        /// </summary>
        [Fact]
        public void OnlyOneChildPerValue()
        {
            string value = OrNodeTest.Domain.PossibleValues[0];
            var child1 = OrNodeTest.CreateNode("child1");
            var child2 = OrNodeTest.CreateNode("child2");
            this._decisionNode.AddChild(value, child1);
            Assert.Throws<ArgumentException>(() => this._decisionNode.AddChild(value, child2));
        }

        /// <summary>
        /// Checks that an exception is thrown when a node is added for a value not in the domain.
        /// </summary>
        [Fact]
        public void ValueSpecifiedForChildBranchMustExist()
        {
            string value = "notExisting";
            Assert.DoesNotContain(value, OrNodeTest.Domain.PossibleValues);
            Assert.Throws<ArgumentException>(() => this._decisionNode.AddChild(value, OrNodeTest.CreateNode("child")));
        }

        /// <summary>
        /// Checks that <see cref="OrNode{T}.TryGetChild(object, out IParameterTreeNode)"/> throws an exception
        /// when a child is queried using a type different from the domain's type.
        /// </summary>
        [Fact]
        public void GetChildThrowsForWrongType()
        {
            int wrongTypeValue = 5;
            IParameterTreeNode childNode;
            Assert.Throws<ArgumentException>(() => this._decisionNode.TryGetChild(wrongTypeValue, out childNode));
        }

        /// <summary>
        /// Checks that <see cref="OrNode{T}.TryGetChild(object, out IParameterTreeNode)"/> throws an exception
        /// when a child is queried for a value not in the domain.
        /// </summary>
        [Fact]
        public void GetChildThrowsForUnknownValue()
        {
            string value = "notExisting";
            IParameterTreeNode childNode;
            Assert.Throws<ArgumentException>(() => this._decisionNode.TryGetChild(value, out childNode));
        }

        /// <summary>
        /// Checks that <see cref="OrNode{T}.TryGetChild"/> returns false if the child is queried for a value
        /// in the domain that does not have a child assigned. The out parameter should be set to null.
        /// </summary>
        [Fact]
        public void GetChildWorksForLegalValueWithoutChild()
        {
            string value = "a";
            IParameterTreeNode childNode;
            Assert.False(this._decisionNode.TryGetChild(value, out childNode), "False should be returned if no child was found.");
            Assert.Null(childNode);
        }

        /// <summary>
        /// Checks that <see cref="OrNode{T}.TryGetChild"/> sets the correct child if one exists and returns true.
        /// </summary>
        [Fact]
        public void GetChildWorksIfChildExists()
        {
            var decisionNode = new OrNode<string>(
                "test",
                new CategoricalDomain<string>(new List<string> { "a" }));
            var childNode = new ValueNode<int>("value", new IntegerDomain());
            decisionNode.AddChild("a", childNode);

            IParameterTreeNode returnedChildNode;
            Assert.True(decisionNode.TryGetChild("a", out returnedChildNode), "Child should be found.");
            Assert.Equal(childNode, returnedChildNode);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Constructs an <see cref="OrNode{T}"/>.
        /// </summary>
        /// <typeparam name="T">The parameter's type.</typeparam>
        /// <param name="identifier">The identifier.</param>
        /// <param name="domain">The parameter's domain.</param>
        /// <returns>The constructed node.</returns>
        protected override ParameterNodeBase<T> ConstructNode<T>(string identifier, CategoricalDomain<T> domain)
        {
            return new OrNode<T>(identifier, domain);
        }

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