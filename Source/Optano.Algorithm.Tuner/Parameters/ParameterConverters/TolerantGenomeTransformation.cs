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

namespace Optano.Algorithm.Tuner.Parameters.ParameterConverters
{
    using System;

    using Optano.Algorithm.Tuner.Parameters.Domains;

    /// <summary>
    /// A <see cref="GenomeTransformation{TCategoricalEncoding}"/> which supports handling of continuous values in
    /// discrete domains.
    /// </summary>
    /// <remarks>Implementation exploits that
    /// <see cref="CategoricalOrdinalEncoding.NumberOfGeneratedColumnsForCategoricalDomain"/> equals 1.</remarks>
    public class TolerantGenomeTransformation : GenomeTransformation<CategoricalOrdinalEncoding>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TolerantGenomeTransformation"/> class.
        /// </summary>
        /// <param name="tree">The <see cref="ParameterTree"/>.</param>
        public TolerantGenomeTransformation(ParameterTree tree)
            : base(tree)
        {
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Rounds values not mapping to continuous <see cref="NumericalDomain{T}"/>s to the nearest integer.
        /// </summary>
        /// <remarks>
        /// Exploits the usage of <see cref="CategoricalOrdinalEncoding"/>: Each domain is expressed by a single value.
        /// </remarks>
        /// <param name="encodedGenome">
        /// <see cref="T:double[]"/> representation of a genome.
        /// </param>
        /// <returns><paramref name="encodedGenome"/> with some indices rounded to the nearest integer.</returns>
        public double[] RoundToValidValues(double[] encodedGenome)
        {
            var rounded = new double[encodedGenome.Length];

            for (int i = 0; i < this.OrderedTreeNodes.Length; i++)
            {
                var featureDomain = this.OrderedTreeNodes[i].Domain;
                if (featureDomain is NumericalDomain<double>)
                {
                    rounded[i] = encodedGenome[i];
                }
                else
                {
                    rounded[i] = Math.Round(encodedGenome[i]);
                }
            }

            return rounded;
        }

        #endregion
    }
}