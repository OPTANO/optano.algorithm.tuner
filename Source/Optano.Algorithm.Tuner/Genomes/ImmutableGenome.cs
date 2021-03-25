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
    using System.Diagnostics.CodeAnalysis;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;

    /// <summary>
    /// A wrapper around a <see cref="Genome"/> making the class immutable.
    /// </summary>
    public sealed class ImmutableGenome
    {
        #region Static Fields

        /// <summary>
        /// The <see cref="GeneValueComparer"/> of <see cref="ImmutableGenome"/>s.
        /// </summary>
        public static readonly GeneValueComparer GenomeComparer = new GeneValueComparer();

        #endregion

        #region Fields

        /// <summary>
        /// The wrapped <see cref="Genome"/>.
        /// </summary>
        private readonly Genome _genome;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableGenome"/> class.
        /// </summary>
        /// <param name="genome">The <see cref="Genome"/> to copy and wrap.</param>
        public ImmutableGenome(Genome genome)
        {
            if (genome == null)
            {
                throw new ArgumentNullException(nameof(genome));
            }

            // Copy the genome s.t. noone can modify it anymore.
            this._genome = new Genome(genome);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the age of the genome.
        /// </summary>
        public int Age => this._genome.Age;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates a mutable <see cref="Genome"/> with the same age and genes as this instance.
        /// </summary>
        /// <returns>The newly created <see cref="Genome"/>.</returns>
        public Genome CreateMutableGenome()
        {
            return new Genome(this._genome);
        }

        /// <summary>
        /// Identifies the part of the genome that contains active genes,
        /// i.e. OR nodes are evaluated and only correct subtrees are added.
        /// Applies the <see cref="ReplacedParameterFilter"/>
        /// that are defined in the <paramref name="parameterTree"/>.
        /// </summary>
        /// <param name="parameterTree">
        /// Structure of parameters.
        /// Caller is responsible for making sure it fits the genome.
        /// </param>
        /// <returns>The part of the genome that contains active genes, filtered with <see cref="ReplacedParameterFilter"/>.</returns>
        public Dictionary<string, IAllele> GetFilteredGenes(ParameterTree parameterTree)
        {
            return this._genome.GetFilteredGenes(parameterTree);
        }

        /// <summary>
        /// Returns a string representation of the genome.
        /// </summary>
        /// <returns>The string representation of the genome.</returns>
        public override string ToString()
        {
            return this._genome.ToString();
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
            return this._genome.ToFilteredGeneString(parameterTree);
        }

        /// <summary>
        /// Returns <see cref="ToString"/> with <see cref="double"/> valued <see cref="IAllele"/> capped to <c>7</c> decimals.
        /// </summary>
        /// <returns>The capped string.</returns>
        public string ToCappedDecimalString()
        {
            return this._genome.ToCappedDecimalString();
        }

        #endregion

        /// <summary>
        /// An <see cref="IEqualityComparer{ImmutableGenome}" /> that only checks for gene values
        /// egardless of genome age or gene order.
        /// </summary>
        [SuppressMessage(
            "NDepend",
            "ND1305:NestedTypesShouldNotBeVisible",
            Justification = "The comparer needs to access private fields while being publicly visible.")]
        public class GeneValueComparer : IEqualityComparer<ImmutableGenome>
        {
            #region Public Methods and Operators

            /// <summary>
            /// Determines whether the specified genomes are equal by comparing their gene values only.
            /// </summary>
            /// <param name="x">The first <see cref="ImmutableGenome" />.</param>
            /// <param name="y">The second <see cref="ImmutableGenome" />.</param>
            /// <returns>true if the specified objects are equal; otherwise, false.</returns>
            public bool Equals(ImmutableGenome x, ImmutableGenome y)
            {
                // since there cannot exist an IG(null), we do not need to distinguish between x == null and x.genome == null (or y respectively).
                return Genome.GenomeComparer.Equals(x?._genome, y?._genome);
            }

            /// <summary>
            /// Computes a hash code for the specified genome.
            /// </summary>
            /// <param name="genome">The <see cref="ImmutableGenome" />.</param>
            /// <returns>The hash code.</returns>
            public int GetHashCode(ImmutableGenome genome)
            {
                return Genome.GenomeComparer.GetHashCode(genome._genome);
            }

            #endregion
        }
    }
}