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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.SearchPoint
{
    using System;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Serialization;

    /// <summary>
    /// A <see cref="SearchPoint"/> based on a <see cref="Genome"/> s.t. categorical and small integer domains are
    /// fixed in search.
    /// </summary>
    public class GenomeSearchPoint : SearchPoint, IGenomeRepresentation, IDeserializationRestorer<GenomeSearchPoint>
    {
        #region Fields

        /// <summary>
        /// Responsible for converting between full-fledged <see cref="Genome"/>s and continuous values.
        /// </summary>
        private readonly GenomeSearchPointConverter _genomeSearchPointConverter;

        /// <summary>
        /// The genome described by this <see cref="GenomeSearchPoint"/>.
        /// </summary>
        private readonly Genome _genome;

        /// <summary>
        /// Whether the <see cref="_genome"/> is valid.
        /// </summary>
        private readonly bool _isValid;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeSearchPoint"/> class.
        /// </summary>
        /// <param name="values">The real-valued components of this point.</param>
        /// <param name="parent">
        /// The <see cref="GenomeSearchPoint"/> this point is based on. Will be used to set remaining components.
        /// </param>
        /// <param name="genomeBuilder">
        /// Responsible for validity checking of <see cref="Genome"/>s.
        /// </param>
        public GenomeSearchPoint(Vector<double> values, GenomeSearchPoint parent, GenomeBuilder genomeBuilder)
            : this(values, parent._genomeSearchPointConverter, parent.Genome, genomeBuilder)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeSearchPoint"/> class.
        /// </summary>
        /// <param name="values">The real-valued components of this point.</param>
        /// <param name="genomeSearchPointConverter">
        /// Responsible for converting between full-fledged <see cref="Genome"/>s and continuous values.
        /// </param>
        /// <param name="underlyingGenome">The underlying <see cref="ImmutableGenome"/>.</param>
        /// <param name="genomeBuilder">
        /// Responsible for validity checking of <see cref="Genome"/>s.
        /// </param>
        private GenomeSearchPoint(
            Vector<double> values,
            GenomeSearchPointConverter genomeSearchPointConverter,
            ImmutableGenome underlyingGenome,
            GenomeBuilder genomeBuilder)
            : base(values)
        {
            if (underlyingGenome == null)
            {
                throw new ArgumentNullException(nameof(underlyingGenome));
            }

            if (genomeBuilder == null)
            {
                throw new ArgumentNullException(nameof(genomeBuilder));
            }

            this._genomeSearchPointConverter = genomeSearchPointConverter ?? throw new ArgumentNullException(nameof(genomeSearchPointConverter));

            this._genome = this._genomeSearchPointConverter.MergeIntoGenome(this.Values, underlyingGenome);
            this._isValid = genomeBuilder.IsGenomeValid(this._genome);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome described by this <see cref="GenomeSearchPoint"/>.
        /// </summary>
        public ImmutableGenome Genome => new ImmutableGenome(this._genome);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates a <see cref="GenomeSearchPoint"/> from <see cref="Genome"/>.
        /// </summary>
        /// <param name="genome">The <see cref="Genome"/> to base the <see cref="GenomeSearchPoint"/> on.</param>
        /// <param name="parameterTree">Specification of all parameters.</param>
        /// <param name="minimumDomainSize">Minimum size of an integer domain to be handled as continuous.</param>
        /// <param name="genomeBuilder">
        /// Responsible for validity checking of <see cref="Genome"/>s.
        /// Needs to be compatible with <paramref name="parameterTree"/>.
        /// </param>
        /// <returns>The created <see cref="GenomeSearchPoint"/>.</returns>
        public static GenomeSearchPoint CreateFromGenome(
            Genome genome,
            ParameterTree parameterTree,
            int minimumDomainSize,
            GenomeBuilder genomeBuilder)
        {
            if (genome == null)
            {
                throw new ArgumentNullException(nameof(genome));
            }

            var converter = new GenomeSearchPointConverter(parameterTree, minimumDomainSize);
            var values = converter.TransformGenomeIntoValues(genome);

            return new GenomeSearchPoint(
                Vector<double>.Build.DenseOfArray(values),
                converter,
                new ImmutableGenome(genome),
                genomeBuilder);
        }

        /// <summary>
        /// Creates a <see cref="GenomeSearchPoint"/> which is based on a <see cref="Genome"/>, but uses
        /// random values for the real-valued components.
        /// </summary>
        /// <param name="genome">
        /// The <see cref="Genome"/> to base the <see cref="GenomeSearchPoint"/> on.
        /// Components considered as real-valued are ignored.
        /// </param>
        /// <param name="parameterTree">Specification of all parameters.</param>
        /// <param name="minimumDomainSize">Minimum size of an integer domain to be handled as continuous.</param>
        /// <param name="genomeBuilder">
        /// Responsible for validity checking of <see cref="Genome"/>s.
        /// Needs to be compatible with <paramref name="parameterTree"/>.
        /// </param>
        /// <returns>The created <see cref="GenomeSearchPoint"/>.</returns>
        public static GenomeSearchPoint BaseRandomPointOnGenome(
            Genome genome,
            ParameterTree parameterTree,
            int minimumDomainSize,
            GenomeBuilder genomeBuilder)
        {
            var converter = new GenomeSearchPointConverter(parameterTree, minimumDomainSize);
            var values = converter.RandomlyCreateRealValuedParameterValues();

            return new GenomeSearchPoint(values, converter, new ImmutableGenome(genome), genomeBuilder);
        }

        /// <summary>
        /// Checks whether the <see cref="SearchPoint"/> is valid.
        /// </summary>
        /// <returns>Whether the <see cref="SearchPoint"/> is valid.</returns>
        public override bool IsValid()
        {
            return this._isValid && base.IsValid();
        }

        /// <summary>
        /// Restores an object after deserialization by <see cref="StatusBase"/>.
        /// </summary>
        /// <remarks>For example, this method might set equality definitions in dictionaries.</remarks>
        /// <returns>The object in restored state.</returns>
        public GenomeSearchPoint Restore()
        {
            // Internal state is easy to deserialize --> Nothing to do here.
            return this;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Genome.ToString();
        }

        #endregion
    }
}