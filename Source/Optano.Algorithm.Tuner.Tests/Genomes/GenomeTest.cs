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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Hyperion;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Contains tests for class <see cref="Genome"/>.
    /// </summary>
    public class GenomeTest
    {
        #region Fields

        /// <summary>
        /// The <see cref="Genome"/> used for testing.
        /// </summary>
        private readonly Genome _genome;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeTest"/> class.
        /// </summary>
        public GenomeTest()
        {
            this._genome = new Genome();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that setting the age by constructor is working.
        /// </summary>
        [Fact]
        public void AgeIsSetCorrectly()
        {
            var genome = new Genome(35);
            Assert.Equal(35, genome.Age);
        }

        /// <summary>
        /// Checks trying to set a negative age results in a <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [Fact]
        public void NegativeAgeThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Genome(-1));
        }

        /// <summary>
        /// Checks that the genome's age is 0 after parameterless construction.
        /// </summary>
        [Fact]
        public void DefaultAgeIsZero()
        {
            var genome = new Genome();
            Assert.Equal(0, genome.Age);
        }

        /// <summary>
        /// Checks that <see cref="Genome"/>'s constructor which takes another <see cref="Genome"/> and an age copies
        /// everything but the age, which is set as specified.
        /// </summary>
        [Fact]
        public void CopyGeneValueConstructorCopiesEverythingButAge()
        {
            this._genome.SetGene("a", new Allele<int>(5));
            this._genome.IsEngineered = true;

            var sameValues = new Genome(this._genome, age: this._genome.Age + 5);
            Assert.Equal(this._genome.IsEngineered, sameValues.IsEngineered);
            Assert.True(
                Genome.GenomeComparer.Equals(this._genome, sameValues),
                "Gene values should have been copied.");
            Assert.Equal(this._genome.Age + 5, sameValues.Age);
        }

        /// <summary>
        /// Checks that <see cref="Genome"/>'s constructor which takes another <see cref="Genome"/> and an age
        /// throws a <see cref="ArgumentOutOfRangeException"/> if called with a negative age.
        /// </summary>
        [Fact]
        public void CopyGeneValueConstructorThrowsForNegativeAge()
        {
            this._genome.SetGene("a", new Allele<int>(5));
            this._genome.IsEngineered = true;

            Assert.Throws<ArgumentOutOfRangeException>(() => new Genome(this._genome, age: -1));
        }

        /// <summary>
        /// Checks that <see cref="Genome"/>'s constructor which takes another <see cref="Genome"/> and an age
        /// creates an object which is independent from the provided one.
        /// </summary>
        [Fact]
        public void CopyGeneValueConstructorCreatesIndependentGenome()
        {
            this._genome.SetGene("a", new Allele<int>(5));
            this._genome.IsEngineered = true;

            var sameValues = new Genome(this._genome, age: this._genome.Age + 5);
            Assert.True(
                Genome.GenomeComparer.Equals(this._genome, sameValues),
                "Gene values should have been copied.");

            this._genome.SetGene("b", new Allele<double>(0.5));
            Assert.False(
                Genome.GenomeComparer.Equals(this._genome, sameValues),
                "Changing the original's values should not affect the copy.");
        }

        /// <summary>
        /// Checks that <see cref="Genome.AgeOnce"/> increases age by 1.
        /// </summary>
        [Fact]
        public void AgingWorks()
        {
            var genome = new Genome(35);
            Assert.Equal(35, genome.Age);
            genome.AgeOnce();

            Assert.Equal(36, genome.Age);
        }

        /// <summary>
        /// Checks setting and getting a gene.
        /// </summary>
        [Fact]
        public void GeneIsSetCorrectly()
        {
            this._genome.SetGene("a", new Allele<int>(5));

            IAllele geneValue = this._genome.GetGeneValue("a");
            Assert.Equal(5, geneValue.GetValue());
        }

        /// <summary>
        /// Checks that setting a gene twice overwrites it.
        /// </summary>
        [Fact]
        public void SetGeneOverwritesOldValue()
        {
            this._genome.SetGene("a", new Allele<int>(5));
            Assert.Equal(5, this._genome.GetGeneValue("a").GetValue());
            this._genome.SetGene("a", new Allele<string>("hello"));

            Assert.Equal("hello", this._genome.GetGeneValue("a").GetValue());
        }

        /// <summary>
        /// Checks that trying to get a nonexistant gene throws a <see cref="KeyNotFoundException"/>.
        /// </summary>
        [Fact]
        public void GetGeneValueThrowsExceptionOnUnknownIdentifier()
        {
            Assert.Throws<KeyNotFoundException>(() => this._genome.GetGeneValue("a"));
        }

        /// <summary>
        /// Checks that <see cref="Genome.GetFilteredGenes(ParameterTree)"/> returns correct values
        /// for the genes.
        /// </summary>
        [Fact]
        public void GetFilteredGenesIgnoreAndNodes()
        {
            var parameterTree = GenomeTest.BuildSimpleTestTreeWithAndRoot();

            this._genome.SetGene("a", new Allele<int>(0));
            this._genome.SetGene("b", new Allele<int>(1));
            this._genome.SetGene("c", new Allele<int>(2));

            var filteredGenes = this._genome.GetFilteredGenes(parameterTree);
            var expectedFilteredGenes = new List<string> { "a", "b", "c" };
            Assert.True(
                TestUtils.SetsAreEquivalent(filteredGenes.Keys, expectedFilteredGenes),
                $"Filtered genes should be {TestUtils.PrintList(expectedFilteredGenes)}, but are {TestUtils.PrintList(filteredGenes.Keys)}.");
        }

        /// <summary>
        /// Checks that <see cref="Genome.GetFilteredGenes(ParameterTree)"/> returns the correct values for all returned identifiers.
        /// </summary>
        [Fact]
        public void GetFilteredGenesUsesCorrectValues()
        {
            var parameterTree = GenomeTest.BuildSimpleTestTreeWithAndRoot();

            this._genome.SetGene("a", new Allele<int>(0));
            this._genome.SetGene("b", new Allele<int>(1));
            this._genome.SetGene("c", new Allele<int>(2));

            var filteredGenes = this._genome.GetFilteredGenes(parameterTree);

            Assert.Equal(0, filteredGenes["a"].GetValue());
            Assert.Equal(1, filteredGenes["b"].GetValue());
            Assert.Equal(2, filteredGenes["c"].GetValue());
        }

        /// <summary>
        /// Checks that <see cref="Genome.GetFilteredGenes(ParameterTree)"/>
        /// only adds the correct subtree of an OR node to the dictionary.
        /// </summary>
        [Fact]
        public void GetFilteredGenesHandlesOrNodesCorrectly()
        {
            // Build up a tree with OR node as root, child a (OR node) for value 0, child b (value node) for value 1.
            // Create the nodes.
            IParameterTreeNode a = new OrNode<int>("a", new CategoricalDomain<int>(new List<int> { 2, 5 }));
            IParameterTreeNode b = new ValueNode<int>("b", new IntegerDomain());
            OrNode<int> rootDecision = new OrNode<int>("or", new CategoricalDomain<int>(new List<int> { 0, 1 }));
            // Create connections.
            rootDecision.AddChild(0, a);
            rootDecision.AddChild(1, b);

            var parameterTree = new ParameterTree(rootDecision);
            this._genome.SetGene("a", new Allele<int>(2));
            this._genome.SetGene("b", new Allele<int>(7));
            this._genome.SetGene("or", new Allele<int>(0));

            var filteredGenes = this._genome.GetFilteredGenes(parameterTree);
            var expectedFilteredGenes = new List<string> { "or", "a" };
            Assert.True(
                TestUtils.SetsAreEquivalent(filteredGenes.Keys, expectedFilteredGenes),
                $"Filtered genes should be {TestUtils.PrintList(expectedFilteredGenes)}, but are {TestUtils.PrintList(filteredGenes.Keys)}.");
        }

        /// <summary>
        /// Tests that dummy parameters are not removed from <see cref="Genome.GetFilteredGenes"/>,
        /// when only a replacement (without ignoring) is defined,
        /// and that replacement parameter/allele is not inserted if indicator value does not match.
        /// </summary>
        [Fact]
        public void ParameterReplacementDoesNotRemoveIfValueDoesNotMatch()
        {
            var tree = GenomeTest.BuildSimpleTestTreeWithAndRoot();
            tree.AddParameterReplacementDefinition("a", 42, "dummy", 1337);
            this._genome.SetGene("a", new Allele<int>(0815));
            this._genome.SetGene("b", new Allele<int>(0));
            this._genome.SetGene("c", new Allele<int>(0));
            var filteredParameters = this._genome.GetFilteredGenes(tree);
            Assert.True(filteredParameters.ContainsKey("a"), "FilterIndicator is not matched, but <a> was removed from parameters.");
            Assert.False(filteredParameters.ContainsKey("dummy"), "<dummy> was inserted even though indicator value is not matched.");
        }

        /// <summary>
        /// Tests that dummy parameters are not removed from <see cref="Genome.GetFilteredGenes"/>,
        /// when only a replacement (without ignoring) is defined,
        /// and that replacement parameter/allele is not inserted if indicator value does not match.
        /// </summary>
        [Fact]
        public void ParameterReplacementRemovesIgnoredParameterIfValueDoesNotMatch()
        {
            var tree = GenomeTest.BuildSimpleTestTreeWithAndRoot();
            tree.AddParameterReplacementDefinition("a", 42, "dummy", 1337, true);
            tree.AddParameterReplacementDefinition("a", 43, "dummy", 1338);
            this._genome.SetGene("a", new Allele<int>(0815));
            this._genome.SetGene("b", new Allele<int>(0));
            this._genome.SetGene("c", new Allele<int>(0));
            var filteredParameters = this._genome.GetFilteredGenes(tree);
            Assert.False(
                filteredParameters.ContainsKey("a"),
                "<a> was not removed, even though it should be added to the set of ignored parameters.");
            Assert.False(filteredParameters.ContainsKey("dummy"), "<dummy> was inserted even though indicator value is not matched.");
        }

        /// <summary>
        /// Tests that dummy parameters are removed from <see cref="Genome.GetFilteredGenes"/>,
        /// and that replacement parameter/allele is inserted if indicator value does match.
        /// </summary>
        [Fact]
        public void ParameterReplacementTriggeredIfValueMatches()
        {
            var tree = GenomeTest.BuildSimpleTestTreeWithAndRoot();
            tree.AddParameterReplacementDefinition("a", 42, "dummy", 1337);
            tree.AddParameterReplacementDefinition("a", 43, "dummy", 1338);
            this._genome.SetGene("a", new Allele<int>(42));
            this._genome.SetGene("b", new Allele<int>(0));
            this._genome.SetGene("c", new Allele<int>(0));
            var filteredParameters = this._genome.GetFilteredGenes(tree);
            Assert.False(filteredParameters.ContainsKey("a"), "<a> was not removed, even though the indicator value was matched.");
            Assert.True(filteredParameters.ContainsKey("dummy"), "<dummy> should have been inserted into the set of parameters.");

            // Replacement <a, 42, dummy, 1337> should have been triggered.
            Assert.Equal(1337, filteredParameters["dummy"].GetValue());
        }

        /// <summary>
        /// Tests that ignored parameters are removed correctly.
        /// </summary>
        [Fact]
        public void IgnoredParametersAreRemoved()
        {
            var tree = GenomeTest.BuildSimpleTestTreeWithAndRoot();
            tree.AddIgnoredParameter("a");
            this._genome.SetGene("a", new Allele<int>(42));
            this._genome.SetGene("b", new Allele<int>(0));
            this._genome.SetGene("c", new Allele<int>(0));
            var filteredParameters = this._genome.GetFilteredGenes(tree);
            Assert.False(filteredParameters.ContainsKey("a"), "<a> was not removed, even though it was ignored.");
            Assert.True(filteredParameters.ContainsKey("b"), "<b> was removed even though it was not filtered");
        }

        /// <summary>
        /// Tests that a single indicator/value-pair can be used for one replacement definition.
        /// </summary>
        [Fact]
        public void TwoMatchesCanNotBeDefined()
        {
            var tree = GenomeTest.BuildSimpleTestTreeWithAndRoot();
            tree.AddParameterReplacementDefinition("a", 42, "dummy", 1337);
            Assert.Throws<InvalidOperationException>(() => tree.AddParameterReplacementDefinition("a", 42, "dummy2", 1338));
        }

        /// <summary>
        /// Checks if an <see cref="ArgumentException"/> is thrown for a parameter replacement
        /// with a <see cref="ParameterReplacementDefinition.IndicatorParameterIdentifier"/> that is not a parameter
        /// of the current <see cref="ParameterTree"/>.
        /// </summary>
        [Fact]
        public void UndefinedIndicatorNameCausesException()
        {
            var tree = GenomeTest.BuildSimpleTestTreeWithAndRoot();
            Assert.Throws<ArgumentException>(() => tree.AddParameterReplacementDefinition("unknown", false, "dummy", 42));
        }

        /// <summary>
        /// Checks that age is correclty printed when using <see cref="Genome.ToString"/>.
        /// </summary>
        [Fact]
        public void AgeIsCorrectlyPrinted()
        {
            string description = new Genome(age: 3).ToString();
            Assert.True(description.Contains("Age: 3"), $"Genome's age is not printed. Print was: {description}.");
        }

        /// <summary>
        /// Checks that genes are correclty printed when using <see cref="Genome.ToString"/>.
        /// </summary>
        [Fact]
        public void GenesAreCorrectlyPrinted()
        {
            this._genome.SetGene("a", new Allele<string>("hello"));
            this._genome.SetGene("b", new Allele<int>(1));

            string expected = "[a: hello, b: 1](Age: 0)[Engineered: no]";
            Assert.True(
                string.Equals(expected, this._genome.ToString()),
                $"Genome was not printed correctly: expected {expected}, but got {this._genome.ToString()}.");
        }

        /// <summary>
        /// Checks that <see cref="Genome.GeneValueComparer.Equals(Genome, Genome)"/> returns true even if the order
        /// of genes is different.
        /// </summary>
        [Fact]
        public void GeneValueComparerIgnoresOrder()
        {
            this._genome.SetGene("a", new Allele<int>(3));
            this._genome.SetGene("b", new Allele<int>(2));
            var other = new Genome();
            other.SetGene("b", new Allele<int>(2));
            other.SetGene("a", new Allele<int>(3));

            Assert.True(
                Genome.GenomeComparer.Equals(this._genome, other),
                $"Genome {this._genome} has supposedly different gene values than {other}.");
        }

        /// <summary>
        /// Checks that <see cref="Genome.GeneValueComparer.Equals(Genome, Genome)"/> returns false if one of the
        /// genomes is missing a gene.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsFalseForMissingGenes()
        {
            this._genome.SetGene("a", new Allele<int>(3));
            this._genome.SetGene("b", new Allele<int>(2));
            var other = new Genome();
            other.SetGene("b", new Allele<int>(2));

            Assert.False(
                Genome.GenomeComparer.Equals(this._genome, other),
                $"Genome {this._genome} has supposedly the same gene values as {other}.");
            Assert.False(
                Genome.GenomeComparer.Equals(this._genome, other),
                $"Genome {this._genome} has supposedly the same gene values as {other}.");
        }

        /// <summary>
        /// Checks that <see cref="Genome.GeneValueComparer.Equals(Genome, Genome)"/> returns false if one of the
        /// values is different.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsFalseForDifferentValue()
        {
            this._genome.SetGene("b", new Allele<int>(2));
            var other = new Genome();
            other.SetGene("b", new Allele<int>(3));

            Assert.False(
                Genome.GenomeComparer.Equals(this._genome, other),
                $"Genome {this._genome} has supposedly the same gene values as {other}.");
        }

        /// <summary>
        /// Checks that <see cref="Genome.GeneValueComparer.Equals(Genome, Genome)"/> returns false if the first
        /// parameter is null and the second one isn't.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsFalseForFirstGenomeNull()
        {
            Assert.False(
                Genome.GenomeComparer.Equals(null, this._genome),
                $"Genome {this._genome} was identified to be equal to null.");
        }

        /// <summary>
        /// Checks that <see cref="Genome.GeneValueComparer.Equals(Genome, Genome)"/> returns false if the second
        /// parameter is null and the first one isn't.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsFalseForSecondGenomeNull()
        {
            Assert.False(
                Genome.GenomeComparer.Equals(this._genome, null),
                $"Genome {this._genome} was identified to be equal to null.");
        }

        /// <summary>
        /// Checks that <see cref="Genome.GeneValueComparer.Equals(Genome, Genome)"/> returns true if both parameters
        /// are null.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsNullForBothGenomesNull()
        {
            Assert.True(Genome.GenomeComparer.Equals(null, null), "null and null were identified as being different.");
        }

        /// <summary>
        /// Checks that <see cref="Genome.GeneValueComparer.GetHashCode(Genome)"/> is equal for two genes if they
        /// contain the same gene values, but picked them up in a different order and are of a different age.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsSameHashCodesForDifferentAgeAndOrder()
        {
            this._genome.SetGene("a", new Allele<int>(3));
            this._genome.SetGene("b", new Allele<int>(2));
            var other = new Genome(this._genome.Age + 2);
            other.SetGene("b", new Allele<int>(2));
            other.SetGene("a", new Allele<int>(3));

            var firstGenomeHash = Genome.GenomeComparer.GetHashCode(this._genome);
            var secondGenomeHash = Genome.GenomeComparer.GetHashCode(other);
            Assert.Equal(
                firstGenomeHash,
                secondGenomeHash);
        }

        /// <summary>
        /// Checks that <see cref="Genome.GeneValueComparer.GetHashCode(Genome)"/> is different for two genomes with
        /// the same genes, but different gene values.
        /// Of course, this does not have to be the case necessarily, but it's still a nice property that should be
        /// true in most cases.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsDifferentHashCodesForDifferentValues()
        {
            this._genome.SetGene("b", new Allele<int>(2));
            var other = new Genome();
            other.SetGene("b", new Allele<int>(3));

            var firstGenomeHash = Genome.GenomeComparer.GetHashCode(this._genome);
            var secondGenomeHash = Genome.GenomeComparer.GetHashCode(other);
            Assert.NotEqual(
                firstGenomeHash,
                secondGenomeHash);
        }

        /// <summary>
        /// Checks that <see cref="Genome.GeneValueComparer.GetHashCode(Genome)"/> is different for two genomes with
        /// the same gene values where one of the genomes is missing one gene.
        /// Of course, this does not have to be the case necessarily, but it's still a nice property that should be
        /// true in most cases.
        /// </summary>
        [Fact]
        public void GeneValueComparerReturnsDifferentHashCodesForMissingValues()
        {
            this._genome.SetGene("a", new Allele<int>(3));
            this._genome.SetGene("b", new Allele<int>(2));
            var other = new Genome();
            other.SetGene("b", new Allele<int>(2));

            var firstGenomeHash = Genome.GenomeComparer.GetHashCode(this._genome);
            var secondGenomeHash = Genome.GenomeComparer.GetHashCode(other);
            Assert.False(
                object.Equals(firstGenomeHash, secondGenomeHash),
                $"Genomes {this._genome} and {other} are not equal, but have equal hashes {firstGenomeHash} and {secondGenomeHash}.");
        }

        /// <summary>
        /// Tests the binary encoding.
        /// </summary>
        [Fact]
        public void TransformGeneToDoubleArrayBinary()
        {
            var tree = this.BuildCategoricalDomainParameterTree();

            // Set genes to values.
            this._genome.SetGene("1intDom", new Allele<int>(0));
            this._genome.SetGene("2intDom", new Allele<int>(1));
            this._genome.SetGene("3catDom", new Allele<int>(4));

            var converter = new GenomeTransformation<CategoricalBinaryEncoding>(tree);

            var result = converter.ConvertGenomeToArray(this._genome);

            var expected = new[] { 0d, 1d, 1d, 1d, 0d };
            Assert.True(expected.SequenceEqual(result), "Expected different double-representation.");
        }

        /// <summary>
        /// Tests the one-hot encoding.
        /// </summary>
        [Fact]
        public void TransformGeneToDoubleArrayOneHot()
        {
            var tree = this.BuildCategoricalDomainParameterTree();

            // Set genes to values.
            this._genome.SetGene("1intDom", new Allele<int>(0));
            this._genome.SetGene("2intDom", new Allele<int>(1));
            this._genome.SetGene("3catDom", new Allele<int>(4));

            var converter = new GenomeTransformation<CategoricalOneHotEncoding>(tree);

            var result = converter.ConvertGenomeToArray(this._genome);

            var expected = new[] { 0d, 1d, 0d, 0d, 0d, 1d, 0d, 0d };
            Assert.True(expected.SequenceEqual(result), "Expected different double-representation.");
        }

        /// <summary>
        /// Tests the ordinal encoding.
        /// </summary>
        [Fact]
        public void TransformGeneToDoubleArrayOrdinal()
        {
            var tree = this.BuildCategoricalDomainParameterTree();

            // Set genes to values.
            this._genome.SetGene("1intDom", new Allele<int>(0));
            this._genome.SetGene("2intDom", new Allele<int>(1));
            this._genome.SetGene("3catDom", new Allele<int>(4));

            var converter = new GenomeTransformation<CategoricalOrdinalEncoding>(tree);

            var result = converter.ConvertGenomeToArray(this._genome);

            var expected = new[] { 0d, 1d, 3d };
            Assert.True(expected.SequenceEqual(result), "Expected different double-representation.");
        }

        /// <summary>
        /// Tests that the <see cref="Genome.ToFilteredGeneString"/> method does not print filtered gene values.
        /// </summary>
        [Fact]
        public void CheckToFilteredGeneString()
        {
            // Set genes to values.
            this._genome.SetGene("1intDom", new Allele<int>(0));
            this._genome.SetGene("2intDom", new Allele<int>(1));
            this._genome.SetGene("3catDom", new Allele<int>(4));

            var tree = this.BuildCategoricalDomainParameterTree();
            tree.AddIgnoredParameter("1intDom");

            var filteredGeneString = this._genome.ToFilteredGeneString(tree);

            Assert.Equal("[2intDom: 1, 3catDom: 4](Age: 0)[Engineered: no]", filteredGeneString);
        }

        /// <summary>
        /// Checks, whether HyperionSerializer handles SortedDictionaries correctly.
        /// </summary>
        [Fact]
        public void HyperionSerializerHandlesSortedDictionariesCorrectly()
        {
            var testDictionary = new SortedDictionary<string, string>();
            testDictionary.Add("c", "entryX");
            testDictionary.Add("a", "entryY");
            testDictionary.Add("b", "entryZ");

            var expectedOrder = new string[] { "a", "b", "c" };
            testDictionary.Keys.ShouldBe(expectedOrder, ignoreOrder: false);

            var serializer = new Serializer();
            using (var file = File.Create("foo.bin"))
            {
                serializer.Serialize(testDictionary, file);
                file.Flush();
            }

            var deserializer = new Serializer();
            using (var file = File.Open("foo.bin", FileMode.Open))
            {
                var restored = deserializer.Deserialize<SortedDictionary<string, string>>(file);
                restored.ShouldBe(testDictionary, ignoreOrder: false);
                restored.Keys.ShouldBe(expectedOrder, ignoreOrder: false);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Builds up a tree with AND node as root, a and b children of AND node, c child of a.
        /// </summary>
        /// <returns>The built tree.</returns>
        private static ParameterTree BuildSimpleTestTreeWithAndRoot()
        {
            // Create the nodes.
            IntegerDomain allIntegers = new IntegerDomain();
            ValueNode<int> a = new ValueNode<int>("a", allIntegers);
            ValueNode<int> b = new ValueNode<int>("b", allIntegers);
            ValueNode<int> c = new ValueNode<int>("c", allIntegers);
            AndNode root = new AndNode();

            // Create connections.
            a.SetChild(c);
            root.AddChild(a);
            root.AddChild(b);

            // Return instantiated tree.
            return new ParameterTree(root);
        }

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