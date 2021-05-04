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

namespace Optano.Algorithm.Tuner.MachineLearning.GenomeRepresentation
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Newtonsoft.Json;

    using Optano.Algorithm.Tuner.Genomes;

    /// <summary>
    /// Utility class that provides better readibility than having to deal with <see cref="T:double[]"/> all the time.
    /// </summary>
    public struct GenomeDoubleRepresentation
    {
        #region Static Fields

        /// <summary>
        /// The equality comparer.
        /// </summary>
        private static readonly DoubleArrayEqualityComparer EqualityComparer = new DoubleArrayEqualityComparer();

        #endregion

        #region Fields

        /// <summary>
        /// double-representation of a <see cref="Genome"/>.
        /// </summary>
        private readonly double[] _convertedGenome;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeDoubleRepresentation"/> struct.
        /// </summary>
        /// <param name="convertedGenome">
        /// The converted genome that should be wrapped.
        /// </param>
        private GenomeDoubleRepresentation(double[] convertedGenome)
        {
            this._convertedGenome = convertedGenome;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the number of columns in the <see cref="_convertedGenome"/>.
        /// </summary>
        public int Length => this._convertedGenome?.Length ?? 0;

        #endregion

        #region Public Indexers

        /// <summary>
        /// Hands the <paramref name="value"/> and <paramref name="index"/> to the internal double array.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public double this[int index]
        {
            get
            {
                return this._convertedGenome[index];
            }

            set
            {
                this._convertedGenome[index] = value;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Implicit conversion from struct to double array.
        /// </summary>
        /// <param name="genomeRepresentation">The genome to convert.</param>
        public static implicit operator double[](GenomeDoubleRepresentation genomeRepresentation)
        {
            return genomeRepresentation._convertedGenome;
        }

        /// <summary>
        /// Implicit conversion from double array to <see cref="GenomeDoubleRepresentation"/>.
        /// </summary>
        /// <param name="featureValues">The feature representation to convert.</param>
        public static implicit operator GenomeDoubleRepresentation(double[] featureValues)
        {
            return new GenomeDoubleRepresentation(featureValues);
        }

        /// <summary>
        /// The equality operator.
        /// </summary>
        /// <param name="a">Parameter a.</param>
        /// <param name="b">Parameter b.</param>
        /// <returns>a==b.</returns>
        [SuppressMessage(
            "NDepend",
            "OPT9001:A.Equals",
            Justification = "structs cannot be null.")]
        public static bool operator ==(GenomeDoubleRepresentation a, GenomeDoubleRepresentation b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// The inequality operator.
        /// </summary>
        /// <param name="a">Parameter a.</param>
        /// <param name="b">Parameter b.</param>
        /// <returns>a!=b.</returns>
        [SuppressMessage(
            "NDepend",
            "OPT9001:A.Equals",
            Justification = "structs cannot be null.")]
        public static bool operator !=(GenomeDoubleRepresentation a, GenomeDoubleRepresentation b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Gets the genome double representation from the given genome identifier string representation. This method is the counterpart to <see cref="GenomeDoubleRepresentation.ToGenomeIdentifierStringRepresentation"/>.
        /// </summary>
        /// <param name="genomeIdentifierStringRepresentation"> The genome identifier string representation.</param>
        /// <returns>The genome double representation.</returns>
        public static GenomeDoubleRepresentation GetGenomeDoubleRepresentationFromGenomeIdentifierStringRepresentation(
            string genomeIdentifierStringRepresentation)
        {
            return JsonConvert.DeserializeObject<double[]>(genomeIdentifierStringRepresentation);
        }

        /// <summary>
        /// Uses a <see cref="DoubleArrayEqualityComparer"/> to check for equality.
        /// </summary>
        /// <param name="other">
        /// The object to compare with.
        /// </param>
        /// <returns>
        /// The true if other is equal.
        /// </returns>
        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }

            if (other is double[])
            {
                return EqualityComparer.Equals(this._convertedGenome, (double[])other);
            }

            if (!(other is GenomeDoubleRepresentation))
            {
                return false;
            }

            return EqualityComparer.Equals(this._convertedGenome, ((GenomeDoubleRepresentation)other)._convertedGenome);
        }

        /// <summary>
        /// Uses the <see cref="DoubleArrayEqualityComparer"/> to compute a hash code.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return EqualityComparer.GetHashCode(this._convertedGenome);
        }

        /// <summary>
        /// Returns the genome identifier string representation.
        /// </summary>
        /// <returns>The genome identifier string representation.</returns>
        public string ToGenomeIdentifierStringRepresentation()
        {
            var roundedValues = this._convertedGenome.Select(g => Math.Round(g, 6)).ToArray();
            return JsonConvert.SerializeObject(roundedValues);
        }

        #endregion
    }
}