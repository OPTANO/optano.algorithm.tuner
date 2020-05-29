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

namespace Optano.Algorithm.Tuner.MachineLearning.GenomeRepresentation
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The genome double representation with predicted rank.
    /// </summary>
    public struct GenomeDoubleRepresentationWithPredictedRank
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeDoubleRepresentationWithPredictedRank"/> struct.
        /// </summary>
        /// <param name="genomeRepresentation">
        /// The genome representation.
        /// </param>
        /// <param name="predictedRank">
        /// The predicted rank.
        /// </param>
        public GenomeDoubleRepresentationWithPredictedRank(GenomeDoubleRepresentation genomeRepresentation, double predictedRank)
        {
            this.ConvertedGenomeRepresentation = genomeRepresentation;
            this.PredictedRank = predictedRank;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the converted genome representation.
        /// </summary>
        public GenomeDoubleRepresentation ConvertedGenomeRepresentation { get; }

        /// <summary>
        /// Gets the predicted rank.
        /// </summary>
        public double PredictedRank { get; }

        #endregion

        #region Public Methods and Operators

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
        public static bool operator ==(GenomeDoubleRepresentationWithPredictedRank a, GenomeDoubleRepresentationWithPredictedRank b)
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
        public static bool operator !=(GenomeDoubleRepresentationWithPredictedRank a, GenomeDoubleRepresentationWithPredictedRank b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Checks if <paramref name="other"/> is equal to this <see cref="GenomeDoubleRepresentationWithPredictedRank"/>.
        /// </summary>
        /// <param name="other">
        /// The object to compare.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool Equals(object other)
        {
            if (!(other is GenomeDoubleRepresentationWithPredictedRank))
            {
                return false;
            }

            var cast = (GenomeDoubleRepresentationWithPredictedRank)other;
            return this.ConvertedGenomeRepresentation.Equals(cast.ConvertedGenomeRepresentation) && this.PredictedRank.Equals(cast.PredictedRank);
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/> hash code.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (31 * this.ConvertedGenomeRepresentation.GetHashCode()) + (53 * this.PredictedRank.GetHashCode());
            }
        }

        #endregion
    }
}