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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    /// <summary>
    /// Responsible for converting between full-fledged <see cref="Genome"/>s and continuous values.
    /// </summary>
    public class GenomeSearchPointConverter
    {
        #region Static Fields

        /// <summary>
        /// Fixes the order of <see cref="IParameterNode"/>s.
        /// </summary>
        public static readonly Comparer<IParameterNode> ParameterNodeComparer = Comparer<IParameterNode>.Create(
            (paramLeft, paramRight) => string.Compare(paramLeft.Identifier, paramRight.Identifier, StringComparison.Ordinal));

        #endregion

        #region Fields

        /// <summary>
        /// The real valued parameters in order of <see cref="ParameterNodeComparer"/>.
        /// </summary>
        private readonly ImmutableList<IParameterNode> _realValuedParameters;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeSearchPointConverter"/> class.
        /// </summary>
        /// <param name="parameterTree">Specification of all parameters.</param>
        /// <param name="minimumDomainSize">Minimum size of an integer domain to be handled as continuous.</param>
        internal GenomeSearchPointConverter(ParameterTree parameterTree, int minimumDomainSize)
        {
            if (parameterTree == null)
            {
                throw new ArgumentNullException(nameof(parameterTree));
            }

            this._realValuedParameters = ExtractContinuousParameters(parameterTree, minimumDomainSize);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Extracts all parameters from the provided <see cref="ParameterTree"/> which can be considered as
        /// continuous.
        /// </summary>
        /// <param name="parameterTree">The <see cref="ParameterTree"/>.</param>
        /// <param name="minimumDomainSize">Minimum size of an integer domain to be considered continuous.</param>
        /// <returns>
        /// All parameters from the provided <see cref="ParameterTree"/> which can be considered as continuous,
        /// orderd by <see cref="ParameterNodeComparer"/>.
        /// </returns>
        internal static ImmutableList<IParameterNode> ExtractContinuousParameters(
            ParameterTree parameterTree,
            int minimumDomainSize)
        {
            return parameterTree
                .GetParameters(ParameterNodeComparer)
                .Where(parameter => ParameterIsConsideredContinuous(parameter, minimumDomainSize))
                .ToImmutableList();
        }

        /// <summary>
        /// Transforms a <see cref="Genome"/> into its real-valued parts.
        /// </summary>
        /// <param name="genome">The <see cref="Genome"/>.</param>
        /// <returns>The created <see cref="T:double[]"/>.</returns>
        internal double[] TransformGenomeIntoValues(Genome genome)
        {
            if (genome == null)
            {
                throw new ArgumentNullException(nameof(genome));
            }

            var values = new double[this._realValuedParameters.Count];
            for (int i = 0; i < this._realValuedParameters.Count; i++)
            {
                var parameter = this._realValuedParameters[i];
                values[i] = Convert.ToDouble(genome.GetGeneValue(parameter.Identifier).GetValue());
            }

            return values;
        }

        /// <summary>
        /// Randomly creates values for all parameters considered as real-valued.
        /// </summary>
        /// <returns>The created <see cref="Vector{T}"/>.</returns>
        internal Vector<double> RandomlyCreateRealValuedParameterValues()
        {
            var values = Vector<double>.Build.Dense(this._realValuedParameters.Count);
            for (int i = 0; i < this._realValuedParameters.Count; i++)
            {
                var parameter = this._realValuedParameters[i];
                values[i] = Convert.ToDouble(parameter.Domain.GenerateRandomGeneValue().GetValue());
            }

            return values;
        }

        /// <summary>
        /// Creates a new <see cref="Genome"/> by merging real-valued components with an existing one.
        /// </summary>
        /// <param name="values">The real-valued components.</param>
        /// <param name="baseGenome">
        /// The <see cref="ImmutableGenome"/> specifying the remaining parameters.
        /// </param>
        /// <returns>The created <see cref="Genome"/>.</returns>
        internal Genome MergeIntoGenome(Vector<double> values, ImmutableGenome baseGenome)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (this._realValuedParameters.Count != values.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(values),
                    $"Real-valued parameters are {string.Join(",", this._realValuedParameters)} (Total: {this._realValuedParameters.Count}), but only {values.Count} values were provided.");
            }

            if (baseGenome == null)
            {
                throw new ArgumentNullException(nameof(baseGenome));
            }

            var genome = baseGenome.CreateMutableGenome();
            for (int i = 0; i < this._realValuedParameters.Count; i++)
            {
                var parameter = this._realValuedParameters[i];
                if (parameter.Domain is NumericalDomain<double>)
                {
                    genome.SetGene(parameter.Identifier, new Allele<double>(values[i]));
                }
                else
                {
                    genome.SetGene(parameter.Identifier, new Allele<int>((int)Math.Round(values[i])));
                }
            }

            genome.IsEngineered = false;

            return genome;
        }

        /// <summary>
        /// Checks whether the provided <paramref name="parameter"/> can be considered as continuous.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="minimumDomainSize">Minimum size of an integer domain to be considered continuous.</param>
        /// <returns>Whether the provided <paramref name="parameter"/> can be considered as continuous.</returns>
        private static bool ParameterIsConsideredContinuous(IParameterNode parameter, int minimumDomainSize)
        {
            return parameter.Domain is NumericalDomain<double>
                   || (parameter.Domain is NumericalDomain<int> && parameter.Domain.DomainSize >= minimumDomainSize);
        }

        #endregion
    }
}