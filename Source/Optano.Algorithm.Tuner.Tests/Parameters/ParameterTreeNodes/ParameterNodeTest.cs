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
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using Xunit;

    /// <summary>
    /// Contains tests for class <see cref="ParameterNodeBase{T}"/>.
    /// </summary>
    public abstract class ParameterNodeTest
    {
        #region Static Fields

        /// <summary>
        /// Identifier used in tests.
        /// </summary>
        protected static readonly string Identifier = "identifier";

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that identifier is correctly set after construction.
        /// </summary>
        [Fact]
        public void IdentifierIsCorrectlySet()
        {
            var node = this.ConstructNode<int>(ParameterNodeTest.Identifier, new CategoricalDomain<int>(new List<int> { 1 }));
            Assert.Equal(ParameterNodeTest.Identifier, node.Identifier);
        }

        /// <summary>
        /// Checks that <see cref="ParameterNodeBase{T}.ToString"/> returns its identifier.
        /// </summary>
        [Fact]
        public void ToStringReturnsIdentifier()
        {
            ParameterNodeBase<int> node = this.ConstructNode<int>(
                ParameterNodeTest.Identifier,
                new CategoricalDomain<int>(new List<int> { 1 }));
            Assert.Equal(node.Identifier, node.ToString());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Constructs a node of the subclass of <see cref="ParameterNodeBase{T}"/> that one wants to test.
        /// </summary>
        /// <typeparam name="T">The parameter's type.</typeparam>
        /// <param name="identifier">The parameter's identifier.</param>
        /// <param name="domain">The parameter's domain.</param>
        /// <returns>The constructed node.</returns>
        protected abstract ParameterNodeBase<T> ConstructNode<T>(string identifier, CategoricalDomain<T> domain);

        #endregion
    }
}