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

namespace Optano.Algorithm.Tuner.Tests.ParameterTreeReader
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.ParameterTreeReader;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="ParameterTreeConverter"/> class.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class ParameterTreeConverterTest
    {
        #region Constants

        /// <summary>
        /// Prefix for path to test XML files.
        /// </summary>
        private const string PathPrefix = @"ParameterTreeReader/TestData/";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterTreeConverterTest"/> class.
        /// </summary>
        public ParameterTreeConverterTest()
        {
            TestUtils.InitializeLogger();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> returns null if called with a path
        /// that doesn't exist.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreeReturnsNullForMissingFile()
        {
            Assert.Null(
                ParameterTreeConverter.ConvertToParameterTree("foo.xml"));
        }

        /// <summary>
        /// Checks that <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> prints a message to console
        /// that states the document could not be opened if called with a path that doesn't exist.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreePrintsConsoleMessageForMissingFile()
        {
            this.CheckConsoleOutput(
                fileName: "foo.xml",
                check: consoleReader =>
                    {
                        string consoleOutput = consoleReader.ReadToEnd().ToString();
                        Assert.True(
                            consoleOutput.Contains("Error when trying to open document"),
                            $"Console output should have stated that document could not be opened, but was:\r\n\"{consoleOutput}\".");
                    });
        }

        /// <summary>
        /// Checks <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> returns null if called with a
        /// path that points to a file which is not .xml.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreeReturnsNullForNonXml()
        {
            Assert.Null(
                ParameterTreeConverter.ConvertToParameterTree(ParameterTreeConverterTest.PathPrefix + "wrongExtension.txt"));
        }

        /// <summary>
        /// Checks that <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> prints a message to console
        /// that states the document could not be interpreted if called with a path hat points to a file which is not
        /// .xml.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreePrintsConsoleMessageForNonXml()
        {
            this.CheckConsoleOutput(
                fileName: "wrongExtension.txt",
                check: consoleReader =>
                    {
                        string consoleOutput = consoleReader.ReadLine().ToString();
                        Assert.True(
                            consoleOutput.StartsWith("Error when loading XML document"),
                            $"Console output should have stated that document could not be interpreted, but was {consoleOutput}.");
                    });
        }

        /// <summary>
        /// Checks <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> returns null if called with a
        /// path that points to a broken XML document.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreeReturnsNullForBrokenXml()
        {
            Assert.Null(
                ParameterTreeConverter.ConvertToParameterTree(ParameterTreeConverterTest.PathPrefix + "broken.xml"));
        }

        /// <summary>
        /// Checks that <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> prints a message to console
        /// that states the document could not be interpreted if called with a path hat points to a broken XML document.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreePrintsConsoleMessageForBrokenXml()
        {
            this.CheckConsoleOutput(
                fileName: "broken.xml",
                check: consoleReader =>
                    {
                        string consoleOutput = consoleReader.ReadLine().ToString();
                        Assert.True(
                            consoleOutput.StartsWith("Error when loading XML document"),
                            $"Console output should have stated that document could not be interpreted, but was {consoleOutput}.");
                    });
        }

        /// <summary>
        /// Checks <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> returns null if called with a
        /// path that points to an XML document not matching the expected schema.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreeReturnsNullForXmlNotMatchingXsd()
        {
            Assert.Null(
                ParameterTreeConverter.ConvertToParameterTree(ParameterTreeConverterTest.PathPrefix + "illegal.xml"));
        }

        /// <summary>
        /// Checks that <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> prints a message to console
        /// that states the document did not match the schema if called with a path hat points to an XML document not
        /// matching the expected schema.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreePrintsConsoleMessageForXmlNotMatchingXsd()
        {
            this.CheckConsoleOutput(
                fileName: "illegal.xml",
                check: consoleReader =>
                    {
                        string consoleOutput = consoleReader.ReadLine().ToString();
                        Assert.True(
                            consoleOutput.StartsWith("Error when validating XML against schema"),
                            $"Console output should have stated that document did not match the schema, but was {consoleOutput}.");
                    });
        }

        /// <summary>
        /// Checks <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> returns null if called with a
        /// path that points to an XML document defining an categorical domain with elements of different types.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreeReturnsNullForCategoricalDomainWithMixedTypes()
        {
            Assert.Null(
                ParameterTreeConverter.ConvertToParameterTree(ParameterTreeConverterTest.PathPrefix + "mixedCategoricalDomain.xml"));
        }

        /// <summary>
        /// Checks that <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> prints a message to console
        /// complaining about a categorical domain containing more than one type if called with a path that points to
        /// an XML document defining an categorical domain with elements of different types.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreePrintsConsoleMessageForCategoricalDomainWithMixedTypes()
        {
            this.CheckConsoleOutput(
                fileName: "mixedCategoricalDomain.xml",
                check: consoleReader =>
                    {
                        string consoleOutput = consoleReader.ReadLine().ToString();
                        Assert.Equal(
                            "Error when translating into parameter tree: Found categorical domain containing more than one type.",
                            consoleOutput);
                    });
        }

        /// <summary>
        /// Checks <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> returns null if called with a
        /// path that points to an XML document defining an OR node using a domain that is not categorical.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreeReturnsNullForOrNodeWithoutCategoricalDomain()
        {
            Assert.Null(
                ParameterTreeConverter.ConvertToParameterTree(ParameterTreeConverterTest.PathPrefix + "orNodeWithoutCategoricalDomain.xml"));
        }

        /// <summary>
        /// Checks that <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> prints a message to console
        /// complaining about an OR node not having the expected domain if called with a path that points to an XML
        /// document defining an OR node using a domain that is not categorical.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreePrintsConsoleMessageForOrNodeWithoutCategoricalDomain()
        {
            this.CheckConsoleOutput(
                fileName: "orNodeWithoutCategoricalDomain.xml",
                check: consoleReader =>
                    {
                        string consoleOutput = consoleReader.ReadLine().ToString();
                        Assert.Equal(
                            $"Error when translating into parameter tree: Domain of OR node 'a' was not of type {typeof(CategoricalDomain<double>)} as expected.",
                            consoleOutput);
                    });
        }

        /// <summary>
        /// Checks <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> returns null if called with a
        /// path that points to an XML document defining an OR node using a choice with a type not equal to the type
        /// implied by its domain.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreeReturnsNullForChoiceOfWrongType()
        {
            Assert.Null(
                ParameterTreeConverter.ConvertToParameterTree(ParameterTreeConverterTest.PathPrefix + "wrongChoice.xml"));
        }

        /// <summary>
        /// Checks that <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/> prints a message to console
        /// complaining about an OR node's choice not having the expected type if called with a path that points to an
        /// XML document defining an OR node using a choice with a type not equal to the type implied by its domain.
        /// </summary>
        [Fact]
        public void ConvertToParameterTreePrintsConsoleMessageForChoiceOfWrongType()
        {
            this.CheckConsoleOutput(
                fileName: "wrongChoice.xml",
                check: consoleReader =>
                    {
                        string consoleOutput = consoleReader.ReadLine().ToString();
                        Assert.Equal(
                            $"Error when translating into parameter tree: OR node 'a' had a choice of type {typeof(int)} instead of {typeof(bool)}.",
                            consoleOutput);
                    });
        }

        /// <summary>
        /// Checks that using <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/>, the following tree
        /// is read in correctly from an XML file:
        /// * Root is an AND node
        /// * On the next level, there is an OR node (0.1, 0.3 or 0.5) and a value node with values between -0.1 and 0
        /// * The OR node activates another AND node on 0.1 and a discrete value node with values between -2 and 3 on 0.5
        /// * The value whose parent is the root node has another value node with a log domain between 10 and 100 as a child.
        /// </summary>
        [SuppressMessage(
            "StyleCop.CSharp.NamingRules",
            "SA1305:FieldNamesMustNotUseHungarianNotation",
            Justification = "Reviewed. It is an OR NODE. I want to call it orNode.")]
        [Fact]
        public void ConvertToParameterTreeWorksForExampleTree()
        {
            // Convert XML to tree.
            var tree = ParameterTreeConverter.ConvertToParameterTree(ParameterTreeConverterTest.PathPrefix + "exampleTree.xml");

            // Check root node.
            Assert.NotNull(tree.Root);
            Assert.Equal(typeof(AndNode), tree.Root.GetType());

            // Check root node has the desired children.
            Assert.Equal(2, tree.Root.Children.Count());

            // Check type and identifier for OR node (first root child).
            var orNode = tree.Root.Children.First() as OrNode<double>;
            Assert.True(orNode != null, "First root child is not a double OR node as expected.");
            Assert.Equal("or", orNode.Identifier);
            // defaultValue of OR node should be the 2nd value of the categorical list (= 0.3).
            Assert.Equal(0.3, orNode.Domain.GetDefaultValue().GetValue());

            // Check values / children for OR node (first root child).
            IParameterTreeNode secondAndNode;
            Assert.True(orNode.TryGetChild(0.1, out secondAndNode), "OR node did not have a child at 0.1.");
            IParameterTreeNode integerValueNode;
            Assert.True(orNode.TryGetChild(0.5, out integerValueNode), "OR node did not have a child at 0.5");
            Assert.True(
                orNode.Domain.ContainsGeneValue(new Allele<double>(0.3)),
                "0.3 should be a possible value for OR node.");

            // Check type, identifier and values for continuous value node (second root child).
            var firstLevelValueNode = tree.Root.Children.Skip(1).First() as ValueNode<double>;
            Assert.True(firstLevelValueNode != null, "Second root child is not a double value node as expected.");
            Assert.Equal("value2", firstLevelValueNode.Identifier);
            Assert.True(
                firstLevelValueNode.Domain is ContinuousDomain,
                "Continuous value node's domain is not continuous.");
            Assert.True(
                firstLevelValueNode.Domain.ContainsGeneValue(new Allele<double>(-0.1)),
                "Continuous value node's domain does not contain -0.1.");
            Assert.True(
                firstLevelValueNode.Domain.ContainsGeneValue(new Allele<double>(0)),
                "Continuous value node's domain does not contain 0.");

            // Check type for second AND node (child of OR node).
            Assert.True(secondAndNode is AndNode, "Child at 0.1 should have been an AND node.");

            // Check type, identifier and values for integer value node (child of OR node).
            var discreteValueNode = integerValueNode as ValueNode<int>;
            Assert.True(discreteValueNode != null, "Child at 0.5 should have been an integer value node.");
            Assert.Equal("value1", discreteValueNode.Identifier);
            Assert.True(discreteValueNode.Domain is IntegerDomain, "Discrete value node's domain is not integer.");
            Assert.True(
                discreteValueNode.Domain.ContainsGeneValue(new Allele<int>(-2)),
                "Discrete value node's domain does not contain -2.");
            Assert.True(
                discreteValueNode.Domain.ContainsGeneValue(new Allele<int>(3)),
                "Discrete value node's domain does not contain 3.");

            // Check type, identifier and values for child of continuous value node.
            var logNode = firstLevelValueNode.Children.Single() as ValueNode<double>;
            Assert.True(logNode != null, "Child of continuous value node should have been a log value node.");
            Assert.Equal("value3", logNode.Identifier);
            Assert.True(logNode.Domain is LogDomain, "Log value node's domain is not a log domain.");
            Assert.True(
                logNode.Domain.ContainsGeneValue(new Allele<double>(10)),
                "Log value node's domain does not contain 10.");
            Assert.True(
                logNode.Domain.ContainsGeneValue(new Allele<double>(100)),
                "Log value node's domain does not contain 100.");
        }

        /// <summary>
        /// Checks that all default values are parsed from the XML tree.
        /// </summary>
        [Fact]
        public void DomainsReturnXmlDefaultValues()
        {
            Randomizer.Configure(42);
            var tree = ParameterTreeConverter.ConvertToParameterTree(ParameterTreeConverterTest.PathPrefix + "treeWithDefaults.xml");
            var nodes = tree.Root.Children.ToList();

            Assert.Equal(5, nodes.Count);

            var contLog = (ParameterNodeBase<double>)nodes[0];
            Assert.NotNull(contLog);
            Assert.Equal("contLog", contLog.Identifier);
            Assert.Equal(42d, contLog.Domain.GetDefaultValue().GetValue());

            var contLinear = (ParameterNodeBase<double>)nodes[1];
            Assert.NotNull(contLinear);
            Assert.Equal("contLinear", contLinear.Identifier);
            Assert.Equal(123.45, contLinear.Domain.GetDefaultValue().GetValue());

            var discreteLog = (ParameterNodeBase<int>)nodes[2];
            Assert.NotNull(discreteLog);
            Assert.Equal("discreteLog", discreteLog.Identifier);
            Assert.Equal(10, discreteLog.Domain.GetDefaultValue().GetValue());

            var discreteLinear = (ParameterNodeBase<int>)nodes[3];
            Assert.NotNull(discreteLinear);
            Assert.Equal("discreteLinear", discreteLinear.Identifier);
            Assert.Equal(100, discreteLinear.Domain.GetDefaultValue().GetValue());

            var orNode = (OrNode<double>)nodes[4];
            Assert.NotNull(orNode);
            Assert.Equal("or", orNode.Identifier);
            Assert.Equal(2, orNode.Children.Count());
            Assert.Equal(0.3, orNode.Domain.GetDefaultValue().GetValue());

            // make sure that domains without default value return a random value within the domain when calling GetDefaultValue.
            var orValueChild = orNode.Children.Skip(1).Single() as ValueNode<int>;
            Assert.NotNull(orValueChild);
            Assert.Equal("orChild2", orValueChild.Identifier);
            var defaultAllele = orValueChild.Domain.GetDefaultValue();
            Assert.NotNull(defaultAllele);
            var defaultValue = defaultAllele.GetValue();
            Assert.NotNull(defaultValue);
            Assert.True(orValueChild.Domain.ContainsGeneValue(defaultAllele));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks console output on calling <see cref="ParameterTreeConverter.ConvertToParameterTree(string)"/>.
        /// </summary>
        /// <param name="fileName">File name of file lying in <see cref="PathPrefix"/>. Will be tried to be converted.
        /// </param>
        /// <param name="check">Checks to do on the output.</param>
        private void CheckConsoleOutput(string fileName, Action<StringReader> check)
        {
            TestUtils.CheckOutput(
                action: () =>
                    {
                        // Call converter.
                        ParameterTreeConverter.ConvertToParameterTree(ParameterTreeConverterTest.PathPrefix + fileName);
                    },
                check: consoleOutput =>
                    {
                        // Check the console output.
                        using (var reader = new StringReader(consoleOutput.ToString()))
                        {
                            check.Invoke(reader);
                        }
                    });
        }

        #endregion
    }
}