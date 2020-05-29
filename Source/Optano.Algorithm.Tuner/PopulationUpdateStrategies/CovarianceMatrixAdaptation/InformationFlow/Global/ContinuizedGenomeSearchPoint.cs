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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Global
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.Serialization;

    /// <summary>
    /// A <see cref="SearchPoint"/> representing a complete <see cref="Genome"/>.
    /// Value transformation is handled by <see cref="TolerantGenomeTransformation"/>.
    /// Invalid <see cref="Genome"/> results are replaced with a repaired version.
    /// 
    /// <para>As parameters are bounded, this is a wrapper around <see cref="BoundedSearchPoint"/>.</para>
    /// </summary>
    public class ContinuizedGenomeSearchPoint : BoundedSearchPoint,
                                                IRepairedGenomeRepresentation,
                                                IDeserializationRestorer<ContinuizedGenomeSearchPoint>
    {
        #region Static Fields

        /// <summary>
        /// Fixes the order of <see cref="IParameterNode"/>s the same way
        /// <see cref="GenomeTransformation{TCategoricalEncoding}"/> does.
        /// </summary>
        private static readonly Comparer<IParameterNode> ParameterNodeComparer = Comparer<IParameterNode>.Create(
            (paramLeft, paramRight) => string.Compare(paramLeft.Identifier, paramRight.Identifier, StringComparison.Ordinal));

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuizedGenomeSearchPoint"/> class.
        /// </summary>
        /// <param name="values">
        /// The real-valued point to base this on.
        /// This is the internal representation, not the one in the parameter space.
        /// <para>Use <see cref="CreateFromGenome"/> if starting from search space.</para>
        /// </param>
        /// <param name="parameterTree">Specifies the parameters.</param>
        /// <param name="genomeBuilder">Responsible for checking validity and repairing.</param>
        /// <param name="lowerBounds">The lower bounds by dimension.</param>
        /// <param name="upperBounds">The upper bounds by dimension.</param>
        public ContinuizedGenomeSearchPoint(
            Vector<double> values,
            ParameterTree parameterTree,
            GenomeBuilder genomeBuilder,
            double[] lowerBounds,
            double[] upperBounds)
            : base(values, lowerBounds, upperBounds)
        {
            if (parameterTree == null)
            {
                throw new ArgumentNullException(nameof(parameterTree));
            }

            if (genomeBuilder == null)
            {
                throw new ArgumentNullException(nameof(genomeBuilder));
            }

            var transformator = new TolerantGenomeTransformation(parameterTree);
            var geneValues = transformator.RoundToValidValues(this.MapIntoBounds().ToArray());
            var genome = transformator.ConvertBack(geneValues);

            // Remember whether there was no direct mapping to a valid genome.
            this.IsRepaired = false;
            if (!genomeBuilder.IsGenomeValid(genome))
            {
                genomeBuilder.MakeGenomeValid(genome);
                this.IsRepaired = true;
            }

            this.Genome = new ImmutableGenome(genome);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuizedGenomeSearchPoint"/> class.
        /// </summary>
        /// <param name="genome">The <see cref="Genome"/> this search point is based on.</param>
        /// <param name="values">
        /// The real-valued point associated with the <see cref="Genome"/>.
        /// This is the internal representation, not the one in the parameter space.
        /// </param>
        /// <param name="lowerBounds">The lower bounds by dimension.</param>
        /// <param name="upperBounds">The upper bounds by dimension.</param>
        private ContinuizedGenomeSearchPoint(ImmutableGenome genome, Vector<double> values, double[] lowerBounds, double[] upperBounds)
            : base(values, lowerBounds, upperBounds)
        {
            this.Genome = genome ?? throw new ArgumentNullException(nameof(genome));
            this.IsRepaired = false;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome described by this <see cref="ContinuizedGenomeSearchPoint"/>.
        /// </summary>
        public ImmutableGenome Genome { get; }

        /// <summary>
        /// Gets a value indicating whether the direct <see cref="Genome"/> mapping was invalid and needed
        /// repairing.
        /// </summary>
        public bool IsRepaired { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates a <see cref="ContinuizedGenomeSearchPoint"/> from <see cref="Genome"/>.
        /// </summary>
        /// <param name="genome">The <see cref="Genome"/> to base the <see cref="ContinuizedGenomeSearchPoint"/> on.</param>
        /// <param name="parameterTree">Specification of all parameters.</param>
        /// <returns>The created <see cref="ContinuizedGenomeSearchPoint"/>.</returns>
        public static ContinuizedGenomeSearchPoint CreateFromGenome(Genome genome, ParameterTree parameterTree)
        {
            if (genome == null)
            {
                throw new ArgumentNullException(nameof(genome));
            }

            if (parameterTree == null)
            {
                throw new ArgumentNullException(nameof(parameterTree));
            }

            var transformator = new TolerantGenomeTransformation(parameterTree);
            var values = transformator.ConvertGenomeToArray(genome);
            ObtainParameterBounds(parameterTree, out var lowerBounds, out var upperBounds);
            var standardizedValues = StandardizeValues(values, lowerBounds, upperBounds);

            return new ContinuizedGenomeSearchPoint(new ImmutableGenome(genome), standardizedValues, lowerBounds, upperBounds);
        }

        /// <summary>
        /// Obtains parameter bounds from parameter specification.
        /// </summary>
        /// <remarks>Categorical parameters with n values are mapped to a domain containing 0 ... n.</remarks>
        /// <seealso cref="CategoricalOrdinalEncoding"/>
        /// <param name="parameterTree">Specifies the parameters.</param>
        /// <param name="lowerBounds">Will contain the lower bounds by dimension.</param>
        /// <param name="upperBounds">Will contain the upper bounds by dimension.</param>
        public static void ObtainParameterBounds(ParameterTree parameterTree, out double[] lowerBounds, out double[] upperBounds)
        {
            // TODO MAYBE #34848: Parts of this method also exist in PartialGenomeSearchPoint. Somehow extract those?

            var orderedParameters = parameterTree.GetParameters(ParameterNodeComparer).ToList();

            lowerBounds = new double[orderedParameters.Count];
            upperBounds = new double[orderedParameters.Count];

            for (int dimension = 0; dimension < orderedParameters.Count; dimension++)
            {
                var parameterNode = orderedParameters[dimension];
                if (parameterNode.Domain.IsCategoricalDomain)
                {
                    lowerBounds[dimension] = 0;
                    upperBounds[dimension] = parameterNode.Domain.DomainSize - 1;
                    continue;
                }

                switch (parameterNode.Domain)
                {
                    case NumericalDomain<int> integerDomain:
                        {
                            lowerBounds[dimension] = integerDomain.Minimum;
                            upperBounds[dimension] = integerDomain.Maximum;
                            break;
                        }
                    case NumericalDomain<double> continuousDomain:
                        lowerBounds[dimension] = continuousDomain.Minimum;
                        upperBounds[dimension] = continuousDomain.Maximum;
                        break;
                    default:
                        throw new NotImplementedException(
                            $"All domains should either be categorical or numerical, but the one of parameter '{parameterNode.Identifier}' is {parameterNode.Domain.GetType()}.");
                }
            }
        }

        /// <summary>
        /// Restores an object after deserialization by <see cref="StatusBase"/>.
        /// </summary>
        /// <remarks>For example, this method might set equality definitions in dictionaries.</remarks>
        /// <returns>The object in restored state.</returns>
        public ContinuizedGenomeSearchPoint Restore()
        {
            // Internal state is easy to deserialize --> Nothing to do here.
            return this;
        }

        #endregion
    }
}