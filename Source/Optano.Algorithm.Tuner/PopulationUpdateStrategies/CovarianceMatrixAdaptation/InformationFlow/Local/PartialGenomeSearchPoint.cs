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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Local
{
    using System;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Serialization;

    /// <summary>
    /// A <see cref="SearchPoint"/> based on a <see cref="Genome"/> s.t. categorical and small integer domains are
    /// fixed in search.
    /// Invalid <see cref="Genome"/> results are replaced with a repaired version.
    /// 
    /// <para>As parameters are bounded, this is a wrapper around <see cref="BoundedSearchPoint"/>.</para>
    /// </summary>
    public class PartialGenomeSearchPoint : BoundedSearchPoint, IRepairedGenomeRepresentation, IDeserializationRestorer<PartialGenomeSearchPoint>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PartialGenomeSearchPoint"/> class.
        /// </summary>
        /// <param name="underlyingGenome">The underlying <see cref="ImmutableGenome"/>.</param>
        /// <param name="values">
        /// The real-valued point to base continuous parameters on.
        /// This is the internal representation, not the one in the parameter space.
        /// <para>Use <see cref="CreateFromGenome"/> if starting from search space.</para>
        /// </param>
        /// <param name="genomeSearchPointConverter">
        /// Responsible for converting between full-fledged <see cref="Genome"/>s and continuous values in
        /// parameter space.
        /// </param>
        /// <param name="genomeBuilder">Responsible for checking validity and repairing.</param>
        /// <param name="lowerBounds">The lower bounds by dimension.</param>
        /// <param name="upperBounds">The upper bounds by dimension.</param>
        public PartialGenomeSearchPoint(
            ImmutableGenome underlyingGenome,
            Vector<double> values,
            GenomeSearchPointConverter genomeSearchPointConverter,
            GenomeBuilder genomeBuilder,
            double[] lowerBounds,
            double[] upperBounds)
            : base(values, lowerBounds, upperBounds)
        {
            if (genomeSearchPointConverter == null)
            {
                throw new ArgumentNullException(nameof(genomeSearchPointConverter));
            }

            if (genomeBuilder == null)
            {
                throw new ArgumentNullException(nameof(genomeBuilder));
            }

            // To create the complete genome, map the continuous values into parameter search space again.
            var genome = genomeSearchPointConverter.MergeIntoGenome(this.MapIntoBounds(), underlyingGenome);

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
        /// Initializes a new instance of the <see cref="PartialGenomeSearchPoint"/> class.
        /// </summary>
        /// <param name="genome">
        /// The <see cref="Genome"/> this search point is based on.
        /// Must be valid and match <paramref name="values"/>, else use different constructor.
        /// </param>
        /// <param name="values">
        /// The real-valued point to base continuous parameters on.
        /// This is the internal representation, not the one in the parameter space.
        /// </param>
        /// <param name="lowerBounds">The lower bounds by dimension.</param>
        /// <param name="upperBounds">The upper bounds by dimension.</param>
        private PartialGenomeSearchPoint(
            ImmutableGenome genome,
            Vector<double> values,
            double[] lowerBounds,
            double[] upperBounds)
            : base(values, lowerBounds, upperBounds)
        {
            this.Genome = genome ?? throw new ArgumentNullException(nameof(genome));
            this.IsRepaired = false;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome described by this <see cref="PartialGenomeSearchPoint"/>.
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
        /// Creates a <see cref="PartialGenomeSearchPoint"/> from <see cref="Genome"/>.
        /// </summary>
        /// <param name="genome">The <see cref="Genome"/> to base the <see cref="PartialGenomeSearchPoint"/> on.</param>
        /// <param name="parameterTree">Specification of all parameters.</param>
        /// <param name="minimumDomainSize">Minimum size of an integer domain to be handled as continuous.</param>
        /// <returns>The created <see cref="PartialGenomeSearchPoint"/>.</returns>
        public static PartialGenomeSearchPoint CreateFromGenome(
            Genome genome,
            ParameterTree parameterTree,
            int minimumDomainSize)
        {
            if (genome == null)
            {
                throw new ArgumentNullException(nameof(genome));
            }

            if (parameterTree == null)
            {
                throw new ArgumentNullException(nameof(parameterTree));
            }

            ObtainParameterBounds(parameterTree, minimumDomainSize, out var lowerBounds, out var upperBounds);
            var converter = new GenomeSearchPointConverter(parameterTree, minimumDomainSize);
            var values = converter.TransformGenomeIntoValues(genome);

            // Map search space values into internal space before calling constructor.
            var standardizedValues = StandardizeValues(values, lowerBounds, upperBounds);

            return new PartialGenomeSearchPoint(new ImmutableGenome(genome), standardizedValues, lowerBounds, upperBounds);
        }

        /// <summary>
        /// Obtains parameter bounds from parameter specification.
        /// </summary>
        /// <param name="parameterTree">Specifies the parameters. Should all be numerical.</param>
        /// <param name="minimumDomainSize">
        /// The minimum size an integer domain needs to have to be handled as continuous.
        /// </param>
        /// <param name="lowerBounds">Will contain the lower bounds by dimension.</param>
        /// <param name="upperBounds">Will contain the upper bounds by dimension.</param>
        public static void ObtainParameterBounds(
            ParameterTree parameterTree,
            int minimumDomainSize,
            out double[] lowerBounds,
            out double[] upperBounds)
        {
            // TODO MAYBE #34848: Parts of this method also exist in ContinuizedGenomeSearchPoint. Somehow extract those?

            var parameters = GenomeSearchPointConverter.ExtractContinuousParameters(parameterTree, minimumDomainSize);

            lowerBounds = new double[parameters.Count];
            upperBounds = new double[parameters.Count];

            for (int dimension = 0; dimension < parameters.Count; dimension++)
            {
                var parameterNode = parameters[dimension];

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
                            $"All domains should be numerical, but the one of parameter '{parameterNode.Identifier}' is {parameterNode.Domain.GetType()}.");
                }
            }
        }

        /// <summary>
        /// Restores an object after deserialization by <see cref="StatusBase"/>.
        /// </summary>
        /// <remarks>For example, this method might set equality definitions in dictionaries.</remarks>
        /// <returns>The object in restored state.</returns>
        public PartialGenomeSearchPoint Restore()
        {
            // Internal state is easy to deserialize --> Nothing to do here.
            return this;
        }

        #endregion
    }
}