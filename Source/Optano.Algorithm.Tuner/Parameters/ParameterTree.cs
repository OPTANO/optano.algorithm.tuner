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

namespace Optano.Algorithm.Tuner.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    /// <summary>
    /// Specifies the parameters' names, domains and dependencies in form of an AND-OR tree:
    /// OR nodes contain categorical parameters. Every child consists of the parameter tree that emerges
    /// when the categorical parameter is set to a certain value.
    /// Children of AND nodes can be optimized separately.
    /// </summary>
    public class ParameterTree
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterTree" /> class.
        /// </summary>
        /// <param name="root">The tree's root.</param>
        public ParameterTree(IParameterTreeNode root)
        {
            this.Root = root;
            this.ReplacedParameterFilter = new ReplacedParameterFilter();
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the parameter tree's root.
        /// </summary>
        public IParameterTreeNode Root { get; }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the filter for replacement of helper parameters.
        /// </summary>
        private ReplacedParameterFilter ReplacedParameterFilter { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Finds out whether the tree contains any nodes that represent parameters.
        /// </summary>
        /// <returns>True if the tree contains parameters, false otherwise.</returns>
        public bool ContainsParameters()
        {
            // Store nodes that still have to be traversed, starting from the root.
            var openNodes = new List<IParameterTreeNode> { this.Root };

            var handledNodes = 0;
            // While there are still such nodes:
            while (handledNodes < openNodes.Count)
            {
                // Pop one.
                var node = openNodes[handledNodes++];

                // Return true if it represents a parameter.
                if (node is IParameterNode)
                {
                    return true;
                }

                openNodes.AddRange(node.Children);
            }

            // If we have not returned at this point, we have looked at all nodes and none represented a parameter.
            return false;
        }

        /// <summary>
        /// Checks if all identifiers stored in the tree are unique.
        /// </summary>
        /// <returns>Whether or not the identifiers are unique.</returns>
        public bool IdentifiersAreUnique()
        {
            // Store nodes that still have to be traversed, starting from the root.
            var openNodes = new List<IParameterTreeNode> { this.Root };
            var handledNodes = 0;

            var usedIdentifiers = new HashSet<string>();
            // While there are still such nodes:
            while (handledNodes < openNodes.Count)
            {
                // Pop a node.
                var node = openNodes[handledNodes++];

                // If it is a parameter:
                // Store all identifiers found so far.
                var nodeAsParameter = node as IParameterNode;
                if (nodeAsParameter != null)
                {
                    if (usedIdentifiers.Contains(nodeAsParameter.Identifier))
                    {
                        return false;
                    }

                    usedIdentifiers.Add(nodeAsParameter.Identifier);
                }

                // Add children to open nodes list.
                openNodes.AddRange(node.Children);
            }

            // All nodes have been traversed and no identifier has been found twice.
            return true;
        }

        /// <summary>
        /// Gets all parameter nodes in this tree.
        /// </summary>
        /// <param name="paramComparer">Optional: specific return order.</param>
        /// <returns>The (ordered) enumerable of all <see cref="IParameterNode"/>.</returns>
        public IEnumerable<IParameterNode> GetParameters(Comparer<IParameterNode> paramComparer = null)
        {
            var allParameters = new List<IParameterNode>();

            var openNodes = new List<IParameterTreeNode>() { this.Root };
            var currentIndex = 0;

            // collect all IParameterNodes
            while (currentIndex < openNodes.Count)
            {
                var currentNode = openNodes[currentIndex++];

                var paramNode = currentNode as IParameterNode;
                if (paramNode != null)
                {
                    allParameters.Add(paramNode);
                }

                openNodes.AddRange(currentNode.Children);
            }

            // return ordered result
            var result = paramComparer != null ? allParameters.OrderBy(p => p, paramComparer) : allParameters.AsEnumerable();

            return result;
        }

        /// <summary>
        /// Retrieves all numerical parameters.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerable{IParameterNode}"/> containing all numberical parameters.
        /// </returns>
        public IEnumerable<IParameterNode> GetNumericalParameters()
        {
            foreach (var parameter in this.GetParameters())
            {
                if (!parameter.Domain.IsCategoricalDomain)
                {
                    Debug.Assert(
                        parameter.Domain is NumericalDomain<int> || parameter.Domain is NumericalDomain<double>,
                        "We only expect numerical domains of type int and double at this point.");
                    yield return parameter;
                }
            }
        }

        /// <summary>
        /// Identifies the identifiers which address active genes if the parameter values equal <paramref name="values"/>,
        /// i.e. OR nodes are evaluated and only correct subtrees are added.
        /// </summary>
        /// <param name="values">
        /// Values of parameters.
        /// Caller is responsible for making sure the dictionary fits the parameter tree.
        /// </param>
        /// <returns>The identifiers which address active genes.</returns>
        public IEnumerable<string> FindActiveIdentifiers(ImmutableDictionary<string, IAllele> values)
        {
            var activeIdentifiers = new List<string>();

            // Store the nodes that still have to be traversed. While the list is not empty:
            var openNodes = new List<IParameterTreeNode> { this.Root };
            while (openNodes.Any())
            {
                // Pop a node.
                var currentNode = openNodes[0];
                openNodes.RemoveAt(0);

                // If it represents a parameter, add the parameter's identifier as active gene identifier.
                var currentNodeAsParameter = currentNode as IParameterNode;
                if (currentNodeAsParameter != null)
                {
                    activeIdentifiers.Add(currentNodeAsParameter.Identifier);
                }

                // Then add active children to openNodes:
                if (currentNode is IOrNode currentNodeAsOrNode)
                {
                    // If current node is an OR node, only add correct child.
                    if (currentNodeAsOrNode.TryGetChild(values[currentNodeAsParameter.Identifier].GetValue(), out var childNode))
                    {
                        openNodes.Add(childNode);
                    }
                }
                else
                {
                    // Else add all children.
                    openNodes.AddRange(currentNode.Children);
                }
            }

            return activeIdentifiers;
        }

        /// <summary>
        /// Removes the indicator parameters from <paramref name="activeParameters"/>.
        /// If their current value matches the specified <see cref="ParameterReplacementDefinition.IndicatorParameterValue"/>,
        /// the defined native parameter/value will be inserted into <paramref name="activeParameters"/>.
        /// </summary>
        /// <param name="activeParameters">
        /// The active parameters to filter on.
        /// <c>Values will be reomved from the passed dictionary</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="activeParameters"/> must not be null.
        /// </exception>
        public void FilterIndicatorParameters(Dictionary<string, IAllele> activeParameters)
        {
            if (activeParameters == null)
            {
                throw new ArgumentNullException(nameof(activeParameters));
            }

            this.ReplacedParameterFilter.HandleSpecialCases(activeParameters);
        }

        /// <summary>
        /// Defines a new replacement for an (artifical) parameter.
        /// Example:
        /// It can be used for modeling special cases, such as "x = 0" disables "heuristic x", 
        /// when x is a numerical parameter with a large domain.
        /// In that case call:
        /// <see cref="AddParameterReplacementDefinition{T}"/>("xActive", false, "x", 0).
        /// Afterwards, <see cref="Genome.GetFilteredGenes(ParameterTree)"/> will never contain
        /// a value for "xActive", but if "xActive == false", the defined replacement (i.e. "x = 0")
        /// will be contained in the returned dictionary.
        /// Important:
        /// The <paramref name="indicatorParameterIdentifier"/> needs to be a member of the current <see cref="ParameterTree"/>.
        /// </summary>
        /// <param name="indicatorParameterIdentifier">
        /// The name of the indicator (=dummy) parameter that needs to be replaced.
        /// This parameter will always be removed from the genomes set of active parameters.
        /// </param>
        /// <param name="indicatorParameterValue">
        /// If the <paramref name="indicatorParameterIdentifier"/> is set to <paramref name="indicatorParameterValue"/>
        /// in the current genome, the <paramref name="controlledParameterIdentifier"/>/<paramref name="nativeOverrideValue"/>
        /// will be inserted in the set of the <see cref="Genome.GetActiveGenes"/>.
        /// </param>
        /// <param name="controlledParameterIdentifier">
        /// The name of the parameter that will be inserted for <paramref name="indicatorParameterIdentifier"/>
        /// when it has the value <paramref name="indicatorParameterValue"/>.
        /// </param>
        /// <param name="nativeOverrideValue">
        /// The value to insert for <paramref name="controlledParameterIdentifier"/>.
        /// </param>
        /// <param name="alwaysRemoveIndicatorParameter">
        /// If <c>true</c>, the <paramref name="indicatorParameterIdentifier"/> is added to <see cref="AddIgnoredParameter"/>.
        /// </param>
        /// <typeparam name="T">
        /// The value type of the inserted <see cref="Allele{T}"/>.
        /// </typeparam>
        /// <returns>
        /// The added <see cref="ParameterReplacementDefinition"/>.
        /// </returns>
        public ParameterReplacementDefinition AddParameterReplacementDefinition<T>(
            string indicatorParameterIdentifier,
            object indicatorParameterValue,
            string controlledParameterIdentifier,
            T nativeOverrideValue,
            bool alwaysRemoveIndicatorParameter = false)
        {
            if (this.GetParameters().All(p => p.Identifier != indicatorParameterIdentifier))
            {
                throw new ArgumentException($"{indicatorParameterIdentifier} is not a member of the current parameter tree.");
            }

            if (this.IsIndicatorParameterAndValueCombinationDefined(indicatorParameterIdentifier, indicatorParameterValue))
            {
                throw new InvalidOperationException(
                    $"You cannot add more than one active ParameterReplacementDefinition for a given combination of {nameof(indicatorParameterIdentifier)}/{nameof(indicatorParameterValue)}.\r\nA replacement for <{indicatorParameterIdentifier}, {indicatorParameterValue}> is already defined.");
            }

            var replacement = this.ReplacedParameterFilter.DefineParameterReplacement(
                indicatorParameterIdentifier,
                indicatorParameterValue,
                controlledParameterIdentifier,
                nativeOverrideValue);
            if (alwaysRemoveIndicatorParameter)
            {
                this.AddIgnoredParameter(indicatorParameterIdentifier);
            }

            return replacement;
        }

        /// <summary>
        /// Checks if a <see cref="ParameterReplacementDefinition"/> for the given
        /// &lt;<paramref name="indicatorParameterIdentifier"/>, <paramref name="indicatorParameterValue"/>&gt;
        /// is defined.
        /// </summary>
        /// <param name="indicatorParameterIdentifier">
        /// The indicator parameter name.
        /// </param>
        /// <param name="indicatorParameterValue">
        /// The indicator parameter value.
        /// </param>
        /// <returns>
        /// <c>True</c>, if a <see cref="ParameterReplacementDefinition"/> is defined.
        /// <c>False</c>, else.
        /// </returns>
        public bool IsIndicatorParameterAndValueCombinationDefined(string indicatorParameterIdentifier, object indicatorParameterValue)
        {
            return this.ReplacedParameterFilter.IsIndicatorParameterAndValueCombinationDefined(indicatorParameterIdentifier, indicatorParameterValue);
        }

        /// <summary>
        /// Adds an ignored parameter definition.
        /// Ignored parameters will always be removed from the  set of 
        /// active parameters when <see cref="FilterIndicatorParameters"/> is called.
        /// </summary>
        /// <param name="ignoredParameterName">
        /// The name of the parameter that should be ignored.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="ignoredParameterName"/> must not be null.
        /// </exception>
        public void AddIgnoredParameter(string ignoredParameterName)
        {
            if (this.GetParameters().All(p => p.Identifier != ignoredParameterName))
            {
                throw new ArgumentException($"{ignoredParameterName} is not a member of the current parameter tree.");
            }

            this.ReplacedParameterFilter.AddIgnoredParameterDefinition(ignoredParameterName);
        }

        #endregion
    }
}