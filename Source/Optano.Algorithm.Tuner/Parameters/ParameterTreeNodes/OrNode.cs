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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Parameters.Domains;

    /// <summary>
    /// A node of the <see cref="ParameterTree" /> that represents a parameter and whose children consist of the
    /// subtrees that get evaluated if a specific value of the represented categorical parameter is set.
    /// </summary>
    /// <typeparam name="T">The parameter's type. Must either be a value type or of type string.</typeparam>
    public class OrNode<T> : ParameterNodeBase<T>, IOrNode
    {
        #region Fields

        /// <summary>
        /// The node's children. Might be empty.
        /// </summary>
        private readonly Dictionary<T, IParameterTreeNode> _children = new Dictionary<T, IParameterTreeNode>();

        /// <summary>
        /// The node's domain.
        /// </summary>
        private readonly CategoricalDomain<T> _domain;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OrNode{T}" /> class.
        /// </summary>
        /// <param name="identifier">The parameter's identifier. Must be unique.</param>
        /// <param name="domain">The parameter's domain. It must either contain value types or strings.</param>
        /// <exception cref="ArgumentException">Thrown if the type contained in the domain is a reference type
        /// which is not a string.</exception>
        public OrNode(string identifier, CategoricalDomain<T> domain)
            : base(identifier)
        {
            // Check the type of the OR node. We cannot handle arbitrary reference types because the internal
            // dictionary would fail on serialization then. Another option would have been to provide an 
            // IEqualityComparer independent of the reference, but it is assumed that the demand for reference
            // type OR nodes is not that high anyway.
            if (!typeof(T).IsValueType && !typeof(T).Equals(typeof(string)))
            {
                throw new ArgumentException(
                    "OrNodes may only be built for value types and strings.",
                    "domain");
            }

            this._domain = domain;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the node's children.
        /// </summary>
        public override IEnumerable<IParameterTreeNode> Children => this._children.Values;

        /// <summary>
        /// Gets the parameter's domain.
        /// </summary>
        public override IDomain Domain => this._domain;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Looks for the child that gets activated for the given value.
        /// </summary>
        /// <param name="value">The value to find the fitting child for.</param>
        /// <param name="child">Outputs the child if one was found, else null.</param>
        /// <exception cref="ArgumentException">Thrown if the given value is not a legal value for the node.</exception>
        /// <returns>Whether the child was found.</returns>
        public bool TryGetChild(object value, out IParameterTreeNode child)
        {
            // Check given value's type.
            if (!(value is T))
            {
                throw new ArgumentException(
                    $"Tried to get child of {typeof(T)} OR node for a value of type {value.GetType()}");
            }

            // If correct type, check if value is legal and throw an exception if it isn't.
            var typedValue = (T)value;
            this.ThrowIfIllegal(typedValue);

            // Correct type & legal value --> set child and return.
            return this._children.TryGetValue(typedValue, out child);
        }

        /// <summary>
        /// Adds child for the specified value.
        /// </summary>
        /// <param name="value">Value from the domain.</param>
        /// <param name="child">The child to add.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the value is not from the domain or
        /// another child was already added for the value.
        /// </exception>
        public void AddChild(T value, IParameterTreeNode child)
        {
            this.ThrowIfIllegal(value);
            this._children.Add(value, child);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks if the given value is legal.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <exception cref="ArgumentException">Thrown if the given value is illegal.</exception>
        private void ThrowIfIllegal(T value)
        {
            if (!this._domain.PossibleValues.Contains(value))
            {
                throw new ArgumentException($"{value} is not contained in the domain of parameter {this.Identifier}.");
            }
        }

        #endregion
    }
}