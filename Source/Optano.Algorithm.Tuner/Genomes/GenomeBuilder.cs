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

namespace Optano.Algorithm.Tuner.Genomes
{
    using System;
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    /// <summary>
    /// Class responsible for building, modifying and validating genomes.
    /// </summary>
    public class GenomeBuilder
    {
        #region Fields

        /// <summary>
        /// The probability that we switch between parents when doing a crossover and deciding on the value of a
        /// parameter that has different values for both parents and has a parent parameter in the parameter tree which
        /// also has different values for both parents.
        /// </summary>
        private readonly double _crossoverSwitchProbability;

        /// <summary>
        /// The probability that a parameter gets mutated.
        /// </summary>
        private readonly double _mutationRate;

        /// <summary>
        /// Percentage of the variable's domain that is used to determine the variance for Gaussian mutation.
        /// </summary>
        private readonly double _mutationVariancePercentage;

        /// <summary>
        /// All <see cref="IParameterNode" />s in the tree.
        /// </summary>
        private readonly IReadOnlyList<IParameterNode> _parameterNodes;

        /// <summary>
        /// Parameter structure.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeBuilder" /> class.
        /// </summary>
        /// <param name="parameterTree">
        /// The parameters' structure.
        /// All genes created by this builder should comply with it.
        /// </param>
        /// <param name="configuration">Configuration parameters.</param>
        /// <exception cref="ArgumentNullException">Thrown if either of the parameters is null.</exception>
        public GenomeBuilder(ParameterTree parameterTree, AlgorithmTunerConfiguration configuration)
        {
            if (parameterTree == null)
            {
                throw new ArgumentNullException("parameterTree");
            }

            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            this._parameterTree = parameterTree;
            this._parameterNodes = this.FindParameterNodes();

            this.MaximumRepairAttempts = configuration.MaxRepairAttempts;
            this._crossoverSwitchProbability = configuration.CrossoverSwitchProbability;
            this._mutationRate = configuration.MutationRate;
            this._mutationVariancePercentage = configuration.MutationVariancePercentage;
        }

        #endregion

        #region Enums

        /// <summary>
        /// Specifies from where a gene value was inherited in a crossover.
        /// </summary>
        private enum ParameterOrigin
        {
            /// <summary>
            /// The origin does not matter as the gene value is the same for both parents and
            /// we have not yet decided on a preferred parent in the considered tree.
            /// </summary>
            Open,

            /// <summary>
            /// The gene was inherited from the first parent.
            /// </summary>
            FirstParent,

            /// <summary>
            /// The gene was inherited from the second parent.
            /// </summary>
            SecondParent,
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the maximum number of attempts to make a genome valid.
        /// </summary>
        protected int MaximumRepairAttempts { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates a genome with random gene values. A repair operation is applied after the creation
        /// to ensure the genome is valid.
        /// </summary>
        /// <param name="age">The genome's age.</param>
        /// <returns>The created genome.</returns>
        public virtual Genome CreateRandomGenome(int age)
        {
            // Create genome with correct age.
            var genome = new Genome(age);

            // Randomly set each gene value.
            foreach (var parameterNode in this._parameterNodes)
            {
                genome.SetGene(parameterNode.Identifier, parameterNode.Domain.GenerateRandomGeneValue());
            }

            this.MakeGenomeValid(genome);
            return genome;
        }

        /// <summary>
        /// Mutates a given genome. A repair operation is applied after the mutation
        /// to ensure the genome is valid at the end of the method.
        /// </summary>
        /// <param name="genome">Genome to mutate. Will be modified.</param>
        public void Mutate(Genome genome)
        {
            // For each parameter:
            foreach (var parameterNode in this._parameterNodes)
            {
                // Decide if it should be mutated.
                var shouldMutate = Randomizer.Instance.Decide(this._mutationRate);
                if (shouldMutate)
                {
                    this.MutateParameter(genome, parameterNode);
                }
            }

            // Finally, make the new genome valid if it isn't already.
            this.MakeGenomeValid(genome);
        }

        /// <summary>
        /// Computes a single child genome for the given parent genomes.
        /// Does not include a repair step, so always call <see cref="GenomeBuilder.Mutate(Genome)" /> afterwards.
        /// </summary>
        /// <param name="parent1">The first parent.</param>
        /// <param name="parent2">The second parent.</param>
        /// <returns>The computed child. May be invalid.</returns>
        public Genome Crossover(Genome parent1, Genome parent2)
        {
            // Create child without any stored parameter values,
            // then traverse the parameter tree and set the gene values one by one.
            var child = new Genome(0);

            // We will label nodes by which parent's value the child inherits.
            var geneValueOrigin = new Dictionary<IParameterTreeNode, ParameterOrigin>();

            // Start with the root.
            var root = this._parameterTree.Root;
            var rootOrigin = this.SetGeneValue(root, parent1, parent2, child);
            geneValueOrigin[root] = rootOrigin;

            // Then store the nodes that still have to be expanded in an OPEN list.
            var openNodes = new List<IParameterTreeNode> { root };
            var handledNodes = 0;

            // While that list is not empty:
            while (handledNodes < openNodes.Count)
            {
                // Pop a node.
                var currentNode = openNodes[handledNodes++];

                var lastOrigin = geneValueOrigin[currentNode];

                // For every child:
                foreach (var childNode in currentNode.Children)
                {
                    // Add to OPEN list, ...
                    openNodes.Add(childNode);

                    // ..., process it, ...
                    var origin = this.SetGeneValue(
                        childNode,
                        parent1,
                        parent2,
                        child,
                        lastOrigin);

                    // ...and finally add origin to stored origins.
                    geneValueOrigin.Add(childNode, origin);
                }
            }

            // If all nodes have been traversed, return the child.
            return child;
        }

        /// <summary>
        /// Tries to make the given genome valid by using <see cref="MutateParameter(Genome, IParameterNode)" />
        /// at most <see cref="MaximumRepairAttempts" /> times for every invalid parameter.
        /// </summary>
        /// <param name="genome">Genome to make valid. Will be modified.</param>
        /// <exception cref="TimeoutException">Thrown if no valid genome could be found.</exception>
        public virtual void MakeGenomeValid(Genome genome)
        {
            // If the genome is valid, do nothing.
            if (this.IsGenomeValid(genome))
            {
                return;
            }

            LoggingHelper.WriteLine(VerbosityLevel.Debug, $"Repairing genome {genome}.");

            // Else: in a loop:
            for (var i = 0; i < this.MaximumRepairAttempts; i++)
            {
                foreach (var parameterNode in this._parameterNodes)
                {
                    // Mutate the parameter.
                    this.MutateParameter(genome, parameterNode);
                    // If the genome is valid afterwards, return.
                    if (this.IsGenomeValid(genome))
                    {
                        LoggingHelper.WriteLine(VerbosityLevel.Debug, $"Repaired genome, now {genome}.");
                        return;
                    }
                }
            }

            // Throw exception after a number of failed tries.
            throw new TimeoutException(
                $"Tried to make the genome {genome} valid by mutating each parameter {this.MaximumRepairAttempts} times, but failed. " +
                "To solve this issue, either set a higher repair threshold, simplify the rules for valid genomes, " +
                "or overwrite the MakeGenomeValid function with a more intelligent approach.");
        }

        /// <summary>
        /// Decides whether the given genome is valid by calling <see cref="CheckAgainstParameterTree(Genome)" />.
        /// </summary>
        /// <param name="genome">The genome to test.</param>
        /// <returns>False if the genome is invalid.</returns>
        public virtual bool IsGenomeValid(Genome genome)
        {
            return this.CheckAgainstParameterTree(genome);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Changes the given genome by mutating the gene corresponding to the given <see cref="IParameterNode" />.
        /// </summary>
        /// <param name="genome">The genome to mutate. Will be modified.</param>
        /// <param name="parameterNode">Specification of the parameter / gene to mutate.</param>
        protected void MutateParameter(Genome genome, IParameterNode parameterNode)
        {
            var currentValue = genome.GetGeneValue(parameterNode.Identifier);
            var mutatedValue = parameterNode.Domain.MutateGeneValue(
                currentValue,
                this._mutationVariancePercentage);
            genome.SetGene(parameterNode.Identifier, mutatedValue);
        }

        /// <summary>
        /// Finds all <see cref="IParameterNode" />s of the given <see cref="ParameterTree" />.
        /// </summary>
        /// <returns>The found nodes in a list.</returns>
        private IReadOnlyList<IParameterNode> FindParameterNodes()
        {
            // Initialize a list for storing the parameter nodes.
            var parameterNodes = new List<IParameterNode>();

            // Store the nodes that still have to be traversed in an OPEN list.
            var openNodes = new List<IParameterTreeNode> { this._parameterTree.Root };
            var handledNodes = 0;

            // While that list is not empty:
            while (handledNodes < openNodes.Count)
            {
                // Pop a node.
                var currentNode = openNodes[handledNodes++];

                // If the nodes represents a parameter:
                var currentNodeAsParameter = currentNode as IParameterNode;
                if (currentNodeAsParameter != null)
                {
                    parameterNodes.Add(currentNodeAsParameter);
                }

                // Continue traversing.
                openNodes.AddRange(currentNode.Children);
            }

            // Make list readonly.
            return parameterNodes.AsReadOnly();
        }

        /// <summary>
        /// Sets the value for the specified gene on the given child,
        /// respecting its parents and the latest parameter origin.
        /// </summary>
        /// <param name="node">
        /// Specifies the gene to set the value for.
        /// Is allowed to be an <see cref="AndNode" />. In this case, nothing happens.
        /// </param>
        /// <param name="parent1">The first parent.</param>
        /// <param name="parent2">The second parent.</param>
        /// <param name="child">The child. Genome will be modified.</param>
        /// <param name="lastParameterOrigin">
        /// The latest parameter origin.
        /// Influences the probability which parent's gene the child inherits.
        /// </param>
        /// <returns>
        /// The origin of the inherited gene value.
        /// For an <see cref="AndNode" />, returns the given parameter origin.
        /// </returns>
        private ParameterOrigin SetGeneValue(
            IParameterTreeNode node,
            Genome parent1,
            Genome parent2,
            Genome child,
            ParameterOrigin lastParameterOrigin = ParameterOrigin.Open)
        {
            // If the node is not a parameter node, we do not have to set a value.
            // The parameter origin does not change.
            var nodeAsParameter = node as IParameterNode;
            if (nodeAsParameter == null)
            {
                return lastParameterOrigin;
            }

            // If it is a parameter node, find the parents' gene values.
            var parameterName = nodeAsParameter.Identifier;
            var parent1GeneValue = parent1.GetGeneValue(parameterName);
            var parent2GeneValue = parent2.GetGeneValue(parameterName);

            // If they are equal and the last parameter's origin is open, set child's parameter value to it
            // and keep the origin.
            if (lastParameterOrigin == ParameterOrigin.Open && object.Equals(parent1GeneValue, parent2GeneValue))
            {
                child.SetGene(parameterName, parent1GeneValue);
                return lastParameterOrigin;
            }

            // Otherwise, randomly decide on one of the two parents.
            // The probability for the parents is dependent on the last parameter's origin.
            double probabilityForFirstParent;
            switch (lastParameterOrigin)
            {
                case ParameterOrigin.FirstParent:
                    probabilityForFirstParent = 1 - this._crossoverSwitchProbability;
                    break;
                case ParameterOrigin.SecondParent:
                    probabilityForFirstParent = this._crossoverSwitchProbability;
                    break;
                case ParameterOrigin.Open:
                    probabilityForFirstParent = 0.5;
                    break;
                default:
                    throw new ArgumentException($"Parameter origin {lastParameterOrigin} is unknown.");
            }

            // Throw a (biased) coin and decide on one parent to inherit the gene value from.
            var useFirstParent = Randomizer.Instance.Decide(probabilityForFirstParent);
            if (useFirstParent)
            {
                child.SetGene(parameterName, parent1GeneValue);
                return ParameterOrigin.FirstParent;
            }

            child.SetGene(parameterName, parent2GeneValue);
            return ParameterOrigin.SecondParent;
        }

        /// <summary>
        /// Checks whether the given genome matches the <see cref="_parameterTree" />.
        /// </summary>
        /// <param name="genome">The genome to check.</param>
        /// <returns>Whether or not the genome matches the parameter tree.</returns>
        private bool CheckAgainstParameterTree(Genome genome)
        {
            // For each parameter:
            foreach (var parameterNode in this._parameterNodes)
            {
                // Check that fitting gene's value is legal.
                var value = genome.GetGeneValue(parameterNode.Identifier);
                if (!parameterNode.Domain.ContainsGeneValue(value))
                {
                    return false;
                }
            }

            // All parameters exist in genome and their values are legal.
            return true;
        }

        #endregion
    }
}