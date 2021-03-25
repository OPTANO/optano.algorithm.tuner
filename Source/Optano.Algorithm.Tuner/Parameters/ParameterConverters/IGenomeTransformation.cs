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

namespace Optano.Algorithm.Tuner.Parameters.ParameterConverters
{
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Genomes;

    /// <summary>
    /// Interface that provides both means for converting a <see cref="Genome"/> into a double representation, and a method for
    /// re-building internal dictionaries that use custom comparers. This is required because the comparers are not
    /// restored when de-serialized.
    /// </summary>
    public interface IGenomeTransformation
    {
        #region Public properties

        /// <summary>
        /// Gets the feature count required to represent the used <see cref="ParameterTree"/> as <see cref="T:double[]"/>.
        /// </summary>
        int FeatureCount { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Convert the genome to double-representation.
        /// </summary>
        /// <param name="genome">
        /// The genome to convert.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/> representation.
        /// </returns>
        double[] ConvertGenomeToArray(Genome genome);

        /// <summary>
        /// Converts a <see cref="T:double[]"/> representation back into a <see cref="Genome"/>.
        /// </summary>
        /// <param name="encodedGenome">
        /// The encoded genome.
        /// </param>
        /// <returns>
        /// The restored <see cref="Genome"/>.
        /// </returns>
        Genome ConvertBack(double[] encodedGenome);

        /// <summary>
        /// Gets the length of each feature.
        /// </summary>
        /// <returns>
        /// The <see cref="IReadOnlyList{Integer}"/>, containing the length of each feature.
        /// </returns>
        IReadOnlyList<int> GetFeatureLengths();

        /// <summary>
        /// Restore the internal dictionaries with correct comparers.
        /// </summary>
        void RestoreInternalDictionariesWithCorrectComparers();

        #endregion
    }
}