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

namespace Optano.Algorithm.Tuner.Genomes
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;

    /// <summary>
    /// A genome, containing values for parameters.
    /// </summary>
    public class Genome
    {
        #region Static Fields

        /// <summary>
        /// The <see cref="GeneValueComparer"/> of <see cref="Genome"/>s.
        /// </summary>
        public static readonly GeneValueComparer GenomeComparer = new GeneValueComparer();

        #endregion

        #region Fields

        /// <summary>
        /// Values for parameters.
        /// </summary>
        private readonly Dictionary<string, IAllele> _genes = new Dictionary<string, IAllele>();

        /// <summary>
        /// Identifiers of parameters, sorted by name. Content must be equivalent to keys of <see cref="_genes" />.
        /// </summary>
        // TODO #34929: Since HyperionSerializer can handle SortedDictionary: Replace this and the dictionary with a SortedDictionary.
        private readonly List<string> _sortedGeneIdentifiers = new List<string>();

        /// <summary>
        /// The backing variable of <see cref="Age"/>.
        /// </summary>
        private int _age;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Genome" /> class.
        /// The creating class is responsible for making sure the created genome is legal.
        /// </summary>
        /// <param name="age">The genome's age.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified age is negative.</exception>
        public Genome(int age = 0)
        {
            this.Age = age;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Genome"/> class.
        /// </summary>
        /// <param name="genome">The genome to copy.</param>
        /// <param name="age">The age of the created genome.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified age is negative.</exception>
        public Genome(Genome genome, int age)
            : this(genome)
        {
            this.Age = age;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Genome" /> class.
        /// <para>Copy constructor.</para>
        /// </summary>
        /// <param name="genome">The genome to copy.</param>
        public Genome(Genome genome)
        {
            if (ReferenceEquals(genome, null))
            {
                throw new ArgumentNullException(nameof(genome));
            }

            this._genes = new Dictionary<string, IAllele>(genome._genes);
            this._sortedGeneIdentifiers = new List<string>(genome._sortedGeneIdentifiers);
            this.Age = genome.Age;
            this.IsEngineered = genome.IsEngineered;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Genome"/> was genetically engineered.
        /// </summary>
        public bool IsEngineered { get; set; }

        /// <summary>
        /// Gets the number of generations this genome has survived so far.
        /// </summary>
        public int Age
        {
            get => this._age;
            private set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException($"Genome's age must be nonnegative, but was set to {value}.");
                }

                this._age = value;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Increment the genome's age by 1.
        /// </summary>
        public void AgeOnce()
        {
            this.Age++;
        }

        /// <summary>
        /// Gets the gene's value for the given identifier.
        /// </summary>
        /// <param name="identifier">The gene's identifier.</param>
        /// <returns>The gene's value.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if gene identifier is not known.</exception>
        public IAllele GetGeneValue(string identifier)
        {
            return this._genes[identifier];
        }

        /// <summary>
        /// Sets a parameter value. Overwrites old value if one existed.
        /// The calling class is responsible for making sure the genome stays legal.
        /// </summary>
        /// <param name="identifier">The identifier of the gene to set.</param>
        /// <param name="allele">The new gene value.</param>
        public void SetGene(string identifier, IAllele allele)
        {
            // Add to dictionary.
            this._genes[identifier] = allele;

            // Update sorted identifier list.
            var identifierIndex = this._sortedGeneIdentifiers.BinarySearch(identifier);
            if (identifierIndex < 0)
            {
                this._sortedGeneIdentifiers.Insert(~identifierIndex, identifier);
            }
        }

        /// <summary>
        /// Computes all <see cref="GetActiveGenes"/> and then applies the
        /// filters that are specified in <see cref="ReplacedParameterFilter"/>.
        /// </summary>
        /// <param name="parameterTree">
        /// The parameter tree.
        /// </param>
        /// <returns>
        /// The <see cref="Dictionary{String, IAllele}"/> with the set of parameters
        /// that can be natively handled by the target algorithm.
        /// </returns>
        public Dictionary<string, IAllele> GetFilteredGenes(ParameterTree parameterTree)
        {
            var activeParameters = this.GetActiveGenes(parameterTree);
            parameterTree.FilterIndicatorParameters(activeParameters);
            return activeParameters;
        }

        /// <summary>
        /// Prints the genome in the form of [identifier1: value1, ..., identifiern: valuen](age: age).
        /// </summary>
        /// <returns>A <see cref="string" /> that represents the genome.</returns>
        public override string ToString()
        {
            return this.BuildStringRepresentation(this._genes);
        }

        /// <summary>
        /// Prints the genome in the form of [identifier1: value1, ..., identifiern: valuen](age: age).
        /// Only uses the currently active parameters. I.e. genes are filtered with <see cref="GetFilteredGenes"/>.
        /// </summary>
        /// <param name="parameterTree">
        /// The parameter tree.
        /// </param>
        /// <returns>
        /// The <see cref="string"/> that represents the genome.
        /// </returns>
        public string ToFilteredGeneString(ParameterTree parameterTree)
        {
            var filteredGenes = this.GetFilteredGenes(parameterTree);
            return this.BuildStringRepresentation(filteredGenes);
        }

        /// <summary>
        /// Returns <see cref="ToString"/> with <see cref="double"/> valued <see cref="IAllele"/> capped to <c>7</c> decimals.
        /// </summary>
        /// <returns>The capped string.</returns>
        public string ToCappedDecimalString()
        {
            return this.BuildStringRepresentation(this._genes, true);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns all gene values, ordered as specified by <paramref name="sortFunc"/>.
        /// </summary>
        /// <param name="sortFunc">Function to order the gene values by.</param>
        /// <returns>The ordered gene values.</returns>
        internal IEnumerable<KeyValuePair<string, IAllele>> GetAllValuesOrdered(IComparer<KeyValuePair<string, IAllele>> sortFunc = null)
        {
            return sortFunc != null ? this._genes.OrderBy(g => g, sortFunc).AsEnumerable() : this._genes;
        }

        /// <summary>
        /// Prints the <paramref name="gene"/> value depending on <paramref name="withCappedDecimalDigits"/>.
        /// </summary>
        /// <param name="gene">The gene to convert to string.</param>
        /// <param name="withCappedDecimalDigits"><c>true</c>, to limit decimal places to 7.</param>
        /// <returns>The string.</returns>
        private static string GetFormattedAlleleString(IAllele gene, bool withCappedDecimalDigits)
        {
            if (!withCappedDecimalDigits || !(gene.GetValue() is double doubleValue))
            {
                return gene.ToString();
            }

            return $"{doubleValue:0.#######}";
        }

        /// <summary>
        /// Identifies the part of the genome that contains active genes,
        /// i.e. OR nodes are evaluated and only correct subtrees are added.
        /// </summary>
        /// <param name="parameterTree">
        /// Structure of parameters.
        /// Caller is responsible for making sure it fits the genome.
        /// </param>
        /// <returns>The part of the genome that contains active genes.</returns>
        private Dictionary<string, IAllele> GetActiveGenes(ParameterTree parameterTree)
        {
            var activeGeneIdentifiers = parameterTree.FindActiveIdentifiers(this._genes.ToImmutableDictionary());
            var activeParameters = activeGeneIdentifiers.ToDictionary(
                identifier => identifier,
                identifier => this._genes[identifier]);
            return activeParameters;
        }

        /// <summary>
        /// Builds a string representation of the current genome.
        /// Only uses the parameters given in <paramref name="genomesToPrint"/>.
        /// </summary>
        /// <param name="genomesToPrint">
        /// Should either be <see cref="_genes"/> or <see cref="GetFilteredGenes"/>.
        /// </param>
        /// <param name="withCappedDecimalDigits">
        /// Default is false.
        /// </param>
        /// <returns>
        /// The <see cref="string"/> representation of this genome.
        /// Parameters will be ordered by Key.
        /// </returns>
        private string BuildStringRepresentation(Dictionary<string, IAllele> genomesToPrint, bool withCappedDecimalDigits = false)
        {
            var builder = new StringBuilder("[");
            builder.Append(
                string.Join(
                    ", ",
                    genomesToPrint
                        .OrderBy(g => g.Key, StringComparer.InvariantCulture)
                        .Select(
                            gene => FormattableString.Invariant($"{gene.Key}: {GetFormattedAlleleString(gene.Value, withCappedDecimalDigits)}"))));
            builder.Append("]");
            builder.Append($"(Age: {this.Age})");
            builder.Append($"[Engineered: {(this.IsEngineered ? "yes" : "no")}]");
            return builder.ToString();
        }

        #endregion

        /// <summary>
        /// An <see cref="IEqualityComparer{Genome}" /> that only checks for gene values
        /// regardless of genome age or gene order.
        /// </summary>
        [SuppressMessage(
            "NDepend",
            "ND1305:NestedTypesShouldNotBeVisible",
            Justification = "The comparer needs to access private fields while being publicly visible.")]
        public class GeneValueComparer : IEqualityComparer<Genome>
        {
            #region Public Methods and Operators

            /// <summary>
            /// Determines whether the specified genomes are equal by comparing their gene values only.
            /// </summary>
            /// <param name="first">The first <see cref="Genome" />.</param>
            /// <param name="second">The second <see cref="Genome" />.</param>
            /// <returns>true if the specified objects are equal; otherwise, false.</returns>
            public bool Equals(Genome first, Genome second)
            {
                // First check for nulls
                var bothNull = (first == null) && (second == null);
                var bothHaveValue = (first != null) && (second != null);
                if (bothNull)
                {
                    return true;
                }

                if (!bothHaveValue)
                {
                    return false;
                }

                // If both genome references have values, check their genes.
                // Add length short cut before expensive gene comparison.
                if (first._genes.Count != second._genes.Count)
                {
                    return false;
                }

                // Then check genes themselves.
                foreach (var identifier in first._sortedGeneIdentifiers)
                {
                    IAllele valueForSecondGenome;
                    var geneContainedInBothGenomes = second._genes.TryGetValue(identifier, out valueForSecondGenome);

                    if (!geneContainedInBothGenomes)
                    {
                        return false;
                    }

                    var firstValue = first._genes[identifier];
                    if (!object.Equals(firstValue, valueForSecondGenome))
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Computes a hash code for the specified genome.
            /// </summary>
            /// <param name="genome">The <see cref="Genome" />.</param>
            /// <returns>The hash code.</returns>
            public int GetHashCode(Genome genome)
            {
                // Ignore overflow / underflow.
                unchecked
                {
                    // Hash value should only be dependent on the stored genes, so combine all their hash values.
                    var hash = 19;
                    foreach (var identifier in genome._sortedGeneIdentifiers)
                    {
                        hash = (hash * 31) + identifier.GetHashCode();
                        hash = (hash * 37) + genome._genes[identifier].GetHashCode();
                    }

                    return hash;
                }
            }

            #endregion
        }
    }
}