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
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;

    using SharpLearning.Containers.Matrices;

    /// <summary>
    /// Class that handles the encoding from <see cref="Genome"/>s to <see cref="F64Matrix"/> for a given <see cref="ParameterTree"/>.
    /// </summary>
    /// <typeparam name="TCategoricalEncoding">
    /// The categorical encoder.
    /// </typeparam>
    public class BulkGenomeTransformation<TCategoricalEncoding> : GenomeTransformation<TCategoricalEncoding>, IBulkGenomeTransformation
        where TCategoricalEncoding : CategoricalEncodingBase, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkGenomeTransformation{TCategoricalEncoding}"/> class.
        /// </summary>
        /// <param name="tree">
        /// The parameter tree.
        /// </param>
        public BulkGenomeTransformation(ParameterTree tree)
            : base(tree)
        {
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Converts the given <paramref name="genomes"/> into a single <see cref="F64Matrix"/>.
        /// Each genome will be represented by a single row in the matrix.
        /// </summary>
        /// <param name="genomes">
        /// The genomes to convert.
        /// </param>
        /// <returns>
        /// The converted genome matrix.
        /// </returns>
        public F64Matrix ConvertAllGenomesToMatrix(IEnumerable<Genome> genomes)
        {
            // initialize when number of columns is known
            double[] rawData = null;
            var columnCount = 0;

            // one row per genome
            var genomeArray = genomes.ToArray();
            var rowCount = genomeArray.Length;

            var currentIndex = 0;

            foreach (var genome in genomeArray)
            {
                // no need to Clone, since conversion will be copied into the actual raw data array.
                var conversion = this.ConvertGenome(genome);

                if (rawData == null)
                {
                    // init data array after number of columns is known.
                    columnCount = conversion.Length;
                    rawData = new double[columnCount * rowCount];
                }

                // copy converted row into matrix data array
                for (var genomeColumnIndex = 0; genomeColumnIndex < columnCount; genomeColumnIndex++)
                {
                    rawData[currentIndex++] = conversion[genomeColumnIndex];
                }
            }

            // create matrix
            var matrix = new F64Matrix(rawData, rowCount, columnCount);

            return matrix;
        }

        #endregion
    }
}