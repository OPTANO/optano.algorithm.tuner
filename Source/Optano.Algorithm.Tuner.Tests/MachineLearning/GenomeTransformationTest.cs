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

namespace Optano.Algorithm.Tuner.Tests.MachineLearning
{
    using System;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Tests for <see cref="GenomeTransformation{TCategoricalEncoding}"/>.
    /// </summary>
    public class GenomeTransformationTest : IDisposable
    {
        #region Fields

        /// <summary>
        /// A categorical domain with 10 discrete values.
        /// </summary>
        private CategoricalDomain<int> _categoricalDomain;

        /// <summary>
        /// A parameter tree with a continuous domain (name: "continuous") and a categorical domain (name: "test").
        /// </summary>
        private ParameterTree _tree;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeTransformationTest"/> class.
        /// Serves as test initialize method.
        /// </summary>
        public GenomeTransformationTest()
        {
            this._categoricalDomain = new CategoricalDomain<int>(Enumerable.Range(0, 10).ToList());

            var root = new AndNode();
            var contNode = new ValueNode<double>("continuous", new ContinuousDomain());
            var categoricalNode = new ValueNode<int>("test", this._categoricalDomain);
            root.AddChild(contNode);
            root.AddChild(categoricalNode);
            this._tree = new ParameterTree(root);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Serves as test teardown method.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Tests that binary encoding reports the correct number of generated columns.
        /// </summary>
        [Fact]
        public void BinaryEncodingComputesCorrectNumberOfGeneratedColumns()
        {
            var encoding = new CategoricalBinaryEncoding();
            encoding.NumberOfGeneratedColumns(this._categoricalDomain).ShouldBe((int)Math.Ceiling(Math.Log2(this._categoricalDomain.DomainSize)));
        }

        /// <summary>
        /// Tests that one hot encoding reports the correct number of generated columns.
        /// </summary>
        [Fact]
        public void OneHotEncodingReturnsCorrectNumberOfGeneratedColumns()
        {
            var encoding = new CategoricalOneHotEncoding();
            encoding.NumberOfGeneratedColumns(this._categoricalDomain).ShouldBe((int)this._categoricalDomain.DomainSize);
        }

        /// <summary>
        /// Tests that ordinal encoding reports the correct number of generated columns.
        /// </summary>
        [Fact]
        public void OrdinalEncodingReturnsCorrectNumberOfGeneratedColumns()
        {
            var encoding = new CategoricalOrdinalEncoding();
            encoding.NumberOfGeneratedColumns(this._categoricalDomain).ShouldBe(1);
        }

        /// <summary>
        /// Tests that encodings are decoded correctly.
        /// </summary>
        /// <param name="encodingType">The categorical encoding to test.</param>
        [Theory]
        [InlineData(typeof(CategoricalBinaryEncoding))]
        [InlineData(typeof(CategoricalOrdinalEncoding))]
        [InlineData(typeof(CategoricalOneHotEncoding))]
        public void EncodedValuesAreDecodedCorrectly(Type encodingType)
        {
            var encoding = (CategoricalEncodingBase)Activator.CreateInstance(encodingType);
            encoding.ShouldNotBeNull();
            var encodedCategory = encoding.Encode("test", this._categoricalDomain);

            foreach (var value in this._categoricalDomain.PossibleValues)
            {
                var encodedValue = encodedCategory.GetColumnRepresentation(value);
                var decodedValue = encodedCategory.GetDomainValueAsAllele(encodedValue);
                decodedValue.GetValue().ShouldBe(value);
            }
        }

        /// <summary>
        /// Tests that the genome transformation reports the correct feature lengths.
        /// </summary>
        /// <param name="encodingType">The typed GenomeTransformation.</param>
        /// <param name="expectedLengths">The expected feature lengths.</param>
        [Theory]
        [InlineData(typeof(GenomeTransformation<CategoricalBinaryEncoding>), new[] { 1, 4 })]
        [InlineData(typeof(GenomeTransformation<CategoricalOrdinalEncoding>), new[] { 1, 1 })]
        [InlineData(typeof(GenomeTransformation<CategoricalOneHotEncoding>), new[] { 1, 10 })]
        public void GenomeTransformationComputesCorrectFeatureLengths(Type encodingType, int[] expectedLengths)
        {
            var transformation = (IGenomeTransformation)Activator.CreateInstance(encodingType, this._tree);
            transformation.ShouldNotBeNull();

            transformation.FeatureCount.ShouldBe(expectedLengths.Sum());
            transformation.GetFeatureLengths().ShouldBe(expectedLengths);
        }

        /// <summary>
        /// Tests that GenomeTransformation encodes and decodes genomes correctly.
        /// </summary>
        /// <param name="genomeTransformationType">The typed GenomeTransformation.</param>
        /// <param name="expectedConversion">The expected double representation.</param>
        [Theory]
        [InlineData(typeof(GenomeTransformation<CategoricalBinaryEncoding>), new[] { 42d, 1d, 0d, 0d, 0d })]
        [InlineData(typeof(GenomeTransformation<CategoricalOrdinalEncoding>), new[] { 42d, 1d })]
        [InlineData(typeof(GenomeTransformation<CategoricalOneHotEncoding>), new[] { 42d, 0d, 1d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d })]
        public void GenomeTransformationEncodesAndDecodesCorrectly(Type genomeTransformationType, double[] expectedConversion)
        {
            var transformation = (IGenomeTransformation)Activator.CreateInstance(genomeTransformationType, this._tree);
            transformation.ShouldNotBeNull();

            var genome = new Genome(1);
            genome.SetGene("continuous", new Allele<double>(42));
            genome.SetGene("test", new Allele<int>(1));

            var convertedGenome = transformation.ConvertGenomeToArray(genome);
            convertedGenome.ShouldBe(expectedConversion);

            var decodedGenome = transformation.ConvertBack(convertedGenome);
            Genome.GenomeComparer.Equals(decodedGenome, genome).ShouldBeTrue();
        }

        #endregion
    }
}