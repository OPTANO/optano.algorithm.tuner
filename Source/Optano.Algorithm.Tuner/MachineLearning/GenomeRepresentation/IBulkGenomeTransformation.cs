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
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;

    using SharpLearning.Containers.Matrices;

    /// <summary>
    /// Handles the encoding from <see cref="Genome"/>s to <see cref="T:double[]"/> representations.
    /// Especially provides support for SharpLearning's <see cref="F64Matrix"/>.
    /// </summary>
    public interface IBulkGenomeTransformation : IGenomeTransformation
    {
        #region Public Methods and Operators

        /// <summary>
        /// Converts all genomes into <see cref="T:double[]"/> representation
        /// and stores them within a <see cref="F64Matrix"/>.
        /// </summary>
        /// <param name="genomes">
        /// The genomes to convert.
        /// </param>
        /// <returns>
        /// The <see cref="F64Matrix"/>, containing a row for each converted genome.
        /// </returns>
        F64Matrix ConvertAllGenomesToMatrix(IEnumerable<Genome> genomes);

        #endregion
    }
}