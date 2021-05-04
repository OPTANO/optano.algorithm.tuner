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

namespace Optano.Algorithm.Tuner.Tests.Genomes
{
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="ImmutableGenome"/> class.
    /// </summary>
    public class ImmutableGenomeTest
    {
        #region Constants

        /// <summary>
        /// The identifier of the single gene used for the test genomes.
        /// </summary>
        private const string ParameterId = "a";

        #endregion

        #region Static Fields

        /// <summary>
        /// A simple <see cref="ParameterTree"/> used in tests.
        /// </summary>
        private static readonly ParameterTree parameterTree =
            new ParameterTree(new ValueNode<int>(ImmutableGenomeTest.ParameterId, new IntegerDomain()));

        /// <summary>
        /// A <see cref="ImmutableGenome.GeneValueComparer"/>.
        /// </summary>
        private static readonly IEqualityComparer<ImmutableGenome> geneValueComparer =
            ImmutableGenome.GenomeComparer;

        #endregion

        #region Fields

        /// <summary>
        /// The mutable <see cref="Genome"/> used as a basis for <see cref="ImmutableGenome"/>s in tests.
        /// Needs to be initialized.
        /// </summary>
        private readonly Genome _originalGenome;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableGenomeTest"/> class.
        /// </summary>
        public ImmutableGenomeTest()
        {
            this._originalGenome = new Genome();
            this._originalGenome.SetGene(ImmutableGenomeTest.ParameterId, new Allele<int>(1));
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that the output of <see cref="ImmutableGenome.GetFilteredGenes(ParameterTree)"/> and
        /// <see cref="Genome.GetFilteredGenes(ParameterTree)"/> is the same directly after initialization of
        /// <see cref="ImmutableGenome"/>.
        /// </summary>
        [Fact]
        public void GetFilteredGenesReturnsSameForImmutableAndMutable()
        {
            // Create immutable genome.
            var immutableGenome = new ImmutableGenome(this._originalGenome);

            // Make sure GetFilteredGenes returns the same values as it does for the original genome used at
            // initialization.
            Assert.True(
                immutableGenome.GetFilteredGenes(ImmutableGenomeTest.parameterTree)
                    .SequenceEqual(this._originalGenome.GetFilteredGenes(ImmutableGenomeTest.parameterTree)),
                "GetFilteredGenes returns different values for immutable and mutable genome.");
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome"/> is not modified when the <see cref="Genome"/> that was provided
        /// on initialization is changed.
        /// </summary>
        [Fact]
        public void ImmutableGenomeDoesNotChangeOnOriginalGenomeMutate()
        {
            // Create immutable genome and make sure its values are equal to the original genome used at
            // initialization.
            var immutableGenome = new ImmutableGenome(this._originalGenome);
            Assert.True(
                immutableGenome.GetFilteredGenes(ImmutableGenomeTest.parameterTree)
                    .SequenceEqual(this._originalGenome.GetFilteredGenes(ImmutableGenomeTest.parameterTree)));

            // Mutate original genome.
            this._originalGenome.SetGene(ImmutableGenomeTest.ParameterId, new Allele<int>(2));

            // Make sure the values are now different.
            Assert.False(
                immutableGenome.GetFilteredGenes(ImmutableGenomeTest.parameterTree)
                    .SequenceEqual(this._originalGenome.GetFilteredGenes(ImmutableGenomeTest.parameterTree)),
                "Immutable genome was mutated!");
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome.CreateMutableGenome"/> returns a <see cref="Genome"/> with same
        /// gene values as the one that was provided on construction.
        /// </summary>
        [Fact]
        public void CreateMutableGenomeReturnsGenomeWithOriginalGeneValues()
        {
            // Create immutable genome.
            var immutableGenome = new ImmutableGenome(this._originalGenome);

            // Get mutable genome out of it.
            var mutableGenome = immutableGenome.CreateMutableGenome();

            // Make sure it has the same values as the original genome.
            Assert.True(
                Genome.GenomeComparer.Equals(mutableGenome, this._originalGenome),
                "New mutable genome should have had the same gene values as the original mutable genome.");
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome.CreateMutableGenome"/> returns a <see cref="Genome"/> with same
        /// age as the one that was provided on construction.
        /// </summary>
        [Fact]
        public void CreateMutableGenomeReturnsGenomeWithOriginalAge()
        {
            // Use non-default age for original genome.
            this._originalGenome.AgeOnce();

            // Create immutable genome.
            var immutableGenome = new ImmutableGenome(this._originalGenome);

            // Get mutable genome out of it.
            var mutableGenome = immutableGenome.CreateMutableGenome();

            // Make sure it has the same age as the original genome.
            Assert.Equal(
                this._originalGenome.Age,
                mutableGenome.Age);
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome"/> is not modified when a <see cref="Genome"/> that was provided
        /// by <see cref="ImmutableGenome.CreateMutableGenome"/> is changed.
        /// </summary>
        [Fact]
        public void ImmutableGenomeDoesNotChangeOnChildGenomeMutate()
        {
            // Create immutable genome, get a mutable genome out of it, and make sure they have the same gene values.
            var immutableGenome = new ImmutableGenome(this._originalGenome);
            var mutableGenome = immutableGenome.CreateMutableGenome();
            Assert.True(
                immutableGenome.GetFilteredGenes(ImmutableGenomeTest.parameterTree)
                    .SequenceEqual(mutableGenome.GetFilteredGenes(ImmutableGenomeTest.parameterTree)));

            // Mutate the mutable genome.
            mutableGenome.SetGene(ImmutableGenomeTest.ParameterId, new Allele<int>(3));

            // Check gene values are now different.
            Assert.False(
                immutableGenome.GetFilteredGenes(ImmutableGenomeTest.parameterTree)
                    .SequenceEqual(mutableGenome.GetFilteredGenes(ImmutableGenomeTest.parameterTree)),
                "Immutable genome has been mutated!");
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome.ToString"/> returns the same as <see cref="Genome.ToString"/>
        /// directly after intialization.
        /// </summary>
        [Fact]
        public void ToStringEqualsGenomesToString()
        {
            var immutableGenome = new ImmutableGenome(this._originalGenome);
            Assert.Equal(
                this._originalGenome.ToString(),
                immutableGenome.ToString());
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome.ToFilteredGeneString"/> returns the same as
        /// <see cref="Genome.ToFilteredGeneString"/> directly after intialization.
        /// </summary>
        [Fact]
        public void ToFilteredGeneStringEqualsGenomesToFilteredGeneString()
        {
            var complexGenome = new Genome();
            complexGenome.SetGene("1intDom", new Allele<int>(0));
            complexGenome.SetGene("2intDom", new Allele<int>(1));
            complexGenome.SetGene("3catDom", new Allele<int>(4));

            var tree = this.BuildCategoricalDomainParameterTree();
            tree.AddIgnoredParameter("1intDom");

            var immutableGenome = new ImmutableGenome(complexGenome);
            Assert.Equal(
                complexGenome.ToFilteredGeneString(tree),
                immutableGenome.ToFilteredGeneString(tree));
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome.GeneValueComparer.Equals(ImmutableGenome, ImmutableGenome)"/>
        /// returns true even if the order of genes is different.
        /// </summary>
        [Fact]
        public void GeneValueComparerIgnoresOrder()
        {
            this._originalGenome.SetGene(ImmutableGenomeTest.ParameterId, new Allele<int>(3));
            this._originalGenome.SetGene("b", new Allele<int>(2));
            var genome = new ImmutableGenome(this._originalGenome);

            var otherGenome = new Genome();
            otherGenome.SetGene("b", new Allele<int>(2));
            otherGenome.SetGene(ImmutableGenomeTest.ParameterId, new Allele<int>(3));
            var other = new ImmutableGenome(otherGenome);

            Assert.True(
                ImmutableGenomeTest.geneValueComparer.Equals(genome, other),
                $"Genome {genome} has supposedly different gene values than {other}.");
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome.GeneValueComparer.Equals(ImmutableGenome, ImmutableGenome)"/>
        /// returns false if one of the genomes is missing a gene.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsFalseForMissingGenes()
        {
            this._originalGenome.SetGene(ImmutableGenomeTest.ParameterId, new Allele<int>(3));
            this._originalGenome.SetGene("b", new Allele<int>(2));
            var genome = new ImmutableGenome(this._originalGenome);

            var otherGenome = new Genome();
            otherGenome.SetGene("b", new Allele<int>(2));
            var other = new ImmutableGenome(otherGenome);

            Assert.False(
                ImmutableGenomeTest.geneValueComparer.Equals(genome, other),
                $"Genome {genome} has supposedly the same gene values as {other}.");
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome.GeneValueComparer.Equals(ImmutableGenome, ImmutableGenome)"/> returns
        /// false if one of the values is different.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsFalseForDifferentValue()
        {
            this._originalGenome.SetGene(ImmutableGenomeTest.ParameterId, new Allele<int>(2));
            var genome = new ImmutableGenome(this._originalGenome);

            var otherGenome = new Genome();
            otherGenome.SetGene(ImmutableGenomeTest.ParameterId, new Allele<int>(3));
            var other = new ImmutableGenome(otherGenome);

            Assert.False(
                ImmutableGenomeTest.geneValueComparer.Equals(genome, other),
                $"Genome {genome} has supposedly the same gene values as {other}.");
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome.GeneValueComparer.Equals(ImmutableGenome, ImmutableGenome)"/> returns
        /// false if the first parameter is null and the second one isn't.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsFalseForFirstGenomeNull()
        {
            var genome = new ImmutableGenome(this._originalGenome);
            Assert.False(
                ImmutableGenomeTest.geneValueComparer.Equals(null, genome),
                $"Genome {genome} was identified to be equal to null.");
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome.GeneValueComparer.Equals(ImmutableGenome, ImmutableGenome)"/> returns
        /// false if the second parameter is null and the first one isn't.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsFalseForSecondGenomeNull()
        {
            var genome = new ImmutableGenome(this._originalGenome);
            Assert.False(
                ImmutableGenomeTest.geneValueComparer.Equals(genome, null),
                $"Genome {genome} was identified to be equal to null.");
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome.GeneValueComparer.Equals(ImmutableGenome, ImmutableGenome)"/>
        /// returns true if both parameters are null.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsNullForBothGenomesNull()
        {
            Assert.True(ImmutableGenomeTest.geneValueComparer.Equals(null, null), "null and null were identified as being different.");
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome.GeneValueComparer.GetHashCode(ImmutableGenome)"/> is equal for two
        /// genes if they contain the same gene values, but picked them up in a different order and are of a different
        /// age.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsSameHashCodesForDifferentAgeAndOrder()
        {
            this._originalGenome.SetGene(ImmutableGenomeTest.ParameterId, new Allele<int>(3));
            this._originalGenome.SetGene("b", new Allele<int>(2));
            var genome = new ImmutableGenome(this._originalGenome);

            var otherGenome = new Genome(this._originalGenome.Age + 2);
            otherGenome.SetGene("b", new Allele<int>(2));
            otherGenome.SetGene(ImmutableGenomeTest.ParameterId, new Allele<int>(3));
            var other = new ImmutableGenome(otherGenome);

            var firstGenomeHash = ImmutableGenomeTest.geneValueComparer.GetHashCode(genome);
            var secondGenomeHash = ImmutableGenomeTest.geneValueComparer.GetHashCode(other);
            Assert.True(
                firstGenomeHash == secondGenomeHash,
                $"Genomes {genome} and {other} are equal, but have different hashes {firstGenomeHash} and {secondGenomeHash}.");
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome.GeneValueComparer.GetHashCode(ImmutableGenome)"/> is different for
        /// two genomes with the same genes, but different gene values.
        /// Of course, this does not have to be the case necessarily, but it's still a nice property that should be
        /// true in most cases.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsDifferentHashCodesForDifferentValues()
        {
            this._originalGenome.SetGene(ImmutableGenomeTest.ParameterId, new Allele<int>(2));
            var genome = new ImmutableGenome(this._originalGenome);

            var otherGenome = new Genome();
            otherGenome.SetGene(ImmutableGenomeTest.ParameterId, new Allele<int>(3));
            var other = new ImmutableGenome(otherGenome);

            var firstGenomeHash = ImmutableGenomeTest.geneValueComparer.GetHashCode(genome);
            var secondGenomeHash = ImmutableGenomeTest.geneValueComparer.GetHashCode(other);
            Assert.True(
                firstGenomeHash != secondGenomeHash,
                $"Genomes {genome} and {other} are not equal, but have equal hashes {firstGenomeHash} and {secondGenomeHash}.");
        }

        /// <summary>
        /// Checks that <see cref="ImmutableGenome.GeneValueComparer.GetHashCode(ImmutableGenome)"/> is different for
        /// two genomes with the same gene values where one of the genomes is missing one gene.
        /// Of course, this does not have to be the case necessarily, but it's still a nice property that should be
        /// true in most cases.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsDifferentHashCodesForMissingValues()
        {
            this._originalGenome.SetGene(ImmutableGenomeTest.ParameterId, new Allele<int>(3));
            this._originalGenome.SetGene("b", new Allele<int>(2));
            var genome = new ImmutableGenome(this._originalGenome);

            var otherGenome = new Genome();
            otherGenome.SetGene("b", new Allele<int>(2));
            var other = new ImmutableGenome(otherGenome);

            var firstGenomeHash = ImmutableGenomeTest.geneValueComparer.GetHashCode(genome);
            var secondGenomeHash = ImmutableGenomeTest.geneValueComparer.GetHashCode(other);
            Assert.True(
                firstGenomeHash != secondGenomeHash,
                $"Genomes {genome} and {other} are not equal, but have equal hashes {firstGenomeHash} and {secondGenomeHash}.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Builds a <see cref="ParameterTree"/> which consists of integer value nodes "1intDom" and "2intDom" and a
        /// categorical domain which is dependent on "1intDom".
        /// </summary>
        /// <returns>The created <see cref="ParameterTree"/>.</returns>
        private ParameterTree BuildCategoricalDomainParameterTree()
        {
            // Create the nodes.
            IntegerDomain allIntegers = new IntegerDomain();
            var catDomain = new CategoricalDomain<int>(new List<int> { 1, 2, 3, 4, 5, 6 });
            ValueNode<int> a = new ValueNode<int>("1intDom", allIntegers);
            ValueNode<int> b = new ValueNode<int>("2intDom", allIntegers);
            ValueNode<int> c = new ValueNode<int>("3catDom", catDomain);
            AndNode root = new AndNode();

            // Create connections.
            a.SetChild(c);
            root.AddChild(a);
            root.AddChild(b);

            // Return instantiated tree.
            return new ParameterTree(root);
        }

        #endregion
    }
}