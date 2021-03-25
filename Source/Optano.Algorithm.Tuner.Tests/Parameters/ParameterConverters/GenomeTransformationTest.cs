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

namespace Optano.Algorithm.Tuner.Tests.Parameters.ParameterConverters
{
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GenomeTransformation{TCategoricalEncoding}"/> class.
    /// </summary>
    public class GenomeTransformationTest : TestBase
    {
        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="IGenomeTransformation"/> to use in tests.
        /// </summary>
        private IGenomeTransformation GenomeTransformation { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ParameterTree"/> to use in tests.
        /// </summary>
        private ParameterTree CurrentTree { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Genome"/>s to use in tests.
        /// </summary>
        private List<Genome> Genomes { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="IGenomeTransformation.GetFeatureLengths"/> returns 1 for a
        /// <see cref="CategoricalOrdinalEncoding"/> with 7 possible values.
        /// </summary>
        [Fact]
        public void OrdinalTestFeatureLength()
        {
            // 7 values in domain
            this.InitializeSingleCategoryOneGenomePerValue<CategoricalOrdinalEncoding>(7);

            var featureLengths = this.GenomeTransformation.GetFeatureLengths();
            Assert.NotNull(featureLengths);
            Assert.Equal(1, featureLengths.Count);
            Assert.Equal(1, featureLengths[0]);
        }

        /// <summary>
        /// Checks that <see cref="IGenomeTransformation.GetFeatureLengths"/> returns 3 for a
        /// <see cref="CategoricalBinaryEncoding"/> with 7 possible values.
        /// </summary>
        [Fact]
        public void BinaryTestFeatureLength()
        {
            // 7 values in domain
            this.InitializeSingleCategoryOneGenomePerValue<CategoricalBinaryEncoding>(7);

            var featureLengths = this.GenomeTransformation.GetFeatureLengths();
            Assert.NotNull(featureLengths);
            Assert.Equal(1, featureLengths.Count);
            Assert.Equal(3, featureLengths[0]);
        }

        /// <summary>
        /// Checks that <see cref="IGenomeTransformation.GetFeatureLengths"/> returns 7 for a
        /// <see cref="CategoricalOneHotEncoding"/> with 7 possible values.
        /// </summary>
        [Fact]
        public void OneHotTestFeatureLength()
        {
            // 7 values in domain
            this.InitializeSingleCategoryOneGenomePerValue<CategoricalOneHotEncoding>(7);

            var featureLengths = this.GenomeTransformation.GetFeatureLengths();
            Assert.NotNull(featureLengths);
            Assert.Equal(1, featureLengths.Count);
            Assert.Equal(7, featureLengths[0]);
        }

        /// <summary>
        /// Validates convert methods for <see cref="CategoricalOrdinalEncoding"/>.
        /// </summary>
        [Fact]
        public void OrdinalConvertAndRestore()
        {
            this.InitializeSingleCategoryOneGenomePerValue<CategoricalOrdinalEncoding>(7);
            this.ValidateGenomeConversion();
        }

        /// <summary>
        /// Validates convert methods for <see cref="CategoricalBinaryEncoding"/>.
        /// </summary>
        [Fact]
        public void BinaryConvertAndRestore()
        {
            this.InitializeSingleCategoryOneGenomePerValue<CategoricalBinaryEncoding>(7);
            this.ValidateGenomeConversion();
        }

        /// <summary>
        /// Validates convert methods for <see cref="CategoricalOneHotEncoding"/>.
        /// </summary>
        [Fact]
        public void OneHotConvertAndRestore()
        {
            this.InitializeSingleCategoryOneGenomePerValue<CategoricalOneHotEncoding>(7);
            this.ValidateGenomeConversion();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a <see cref="GenomeTransformation{TEncoding}"/>.
        /// </summary>
        /// <typeparam name="TEncoding">Type of the <see cref="GenomeTransformation{TEncoding}"/>.</typeparam>
        /// <param name="tree">Specification of target algorithm parameters.</param>
        private void InitializeTypedTransformation<TEncoding>(ParameterTree tree)
            where TEncoding : CategoricalEncodingBase, new()
        {
            this.GenomeTransformation = new GenomeTransformation<TEncoding>(tree);
        }

        /// <summary>
        /// Initializes <see cref="Genomes"/> creating one <see cref="Genome"/> per value of a
        /// <see cref="CategoricalDomain{T}"/>, where the domain consists of <paramref name="categoricalValueCount"/>
        /// strings.
        /// </summary>
        /// <typeparam name="TEncoding">Encoding of domain.</typeparam>
        /// <param name="categoricalValueCount">Number of categorical values to use.</param>
        private void InitializeSingleCategoryOneGenomePerValue<TEncoding>(int categoricalValueCount)
            where TEncoding : CategoricalEncodingBase, new()
        {
            var domain = this.GetTestDomain(categoricalValueCount);
            var tree = this.SingleCategoryTree(domain);

            var genomes = new List<Genome>();
            var singleTreeNode = tree.GetParameters().Single();

            foreach (var possibleValue in domain.PossibleValues)
            {
                var allele = new Allele<string>(possibleValue);
                var genome = new Genome();
                genome.SetGene(singleTreeNode.Identifier, allele);
                genomes.Add(genome);
            }

            this.CurrentTree = tree;
            this.Genomes = genomes;
            this.InitializeTypedTransformation<TEncoding>(this.CurrentTree);
        }

        /// <summary>
        /// Creates a <see cref="ParameterTree"/> consisting of a single node with categorical domain.
        /// </summary>
        /// <typeparam name="T">Type of the categorical domain.</typeparam>
        /// <param name="domain">The categorical domain.</param>
        /// <returns>The created <see cref="ParameterTree"/>.</returns>
        private ParameterTree SingleCategoryTree<T>(CategoricalDomain<T> domain)
        {
            var root = new ValueNode<T>("CategoricalFeature", domain);

            var tree = new ParameterTree(root);
            return tree;
        }

        /// <summary>
        /// Creates a <see cref="CategoricalDomain{T}"/> consisting of <paramref name="members"/> strings of the form
        /// "CategoricalValue_{number}". Iteration starts at 1.
        /// </summary>
        /// <param name="members">Number of categories.</param>
        /// <returns>The created <see cref="CategoricalDomain{T}"/>.</returns>
        private CategoricalDomain<string> GetTestDomain(int members)
        {
            var domainMembers = Enumerable.Range(1, members).Select(m => $"CategoricalValue_{m}").ToList();
            var domain = new CategoricalDomain<string>(domainMembers);

            return domain;
        }

        /// <summary>
        /// Checks that for each <see cref="Genome"/> in <see cref="Genomes"/>, two convertings using
        /// <see cref="GenomeTransformation"/> result in the <see cref="Genome"/> one started with.
        /// </summary>
        private void ValidateGenomeConversion()
        {
            var comparer = Genome.GenomeComparer;

            foreach (var genome in this.Genomes)
            {
                var convertedGenome = this.GenomeTransformation.ConvertGenomeToArray(genome);
                var restoredGenome = this.GenomeTransformation.ConvertBack(convertedGenome);

                Assert.True(comparer.Equals(genome, restoredGenome));
            }
        }

        #endregion
    }
}