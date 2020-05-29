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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.DifferentialEvolution.SearchPoint
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Accord.Statistics.Testing;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.SearchPoint;
    using Optano.Algorithm.Tuner.Tests.GenomeBuilders;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GenomeSearchPoint"/> class.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class GenomeSearchPointTest : IDisposable
    {
        #region Fields

        /// <summary>
        /// Minimum size of an integer domain to be handled as continuous.
        /// </summary>
        private readonly int _minimumDomainSize = 4;

        /// <summary>
        /// A <see cref="ParameterTree"/> containing parameters of many different domain types.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// A <see cref="GenomeBuilder"/> which fits <see cref="_parameterTree"/>.
        /// </summary>
        private readonly GenomeBuilder _genomeBuilder;

        /// <summary>
        /// A <see cref="GenomeSearchPoint"/>.
        /// </summary>
        private readonly GenomeSearchPoint _parent;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeSearchPointTest"/> class.
        /// </summary>
        public GenomeSearchPointTest()
        {
            Randomizer.Configure();

            this._parameterTree = this.CreateParameterTree();
            this._genomeBuilder = new GenomeBuilder(
                this._parameterTree,
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().Build(1));
            this._parent = GenomeSearchPoint.CreateFromGenome(
                this._genomeBuilder.CreateRandomGenome(age: 1),
                this._parameterTree,
                this._minimumDomainSize,
                this._genomeBuilder);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Called after every test.
        /// </summary>
        public void Dispose()
        {
            Randomizer.Reset();
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint"/>'s constructor throws a <see cref="ArgumentNullException"/> if
        /// called without any values.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingValues()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenomeSearchPoint(values: null, parent: this._parent, genomeBuilder: this._genomeBuilder));
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint"/>'s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a number of values inconsistent with its parent.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForWrongNumberOfValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GenomeSearchPoint(values: Vector<double>.Build.Dense(1), parent: this._parent, genomeBuilder: this._genomeBuilder));
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint"/>'s constructor throws a <see cref="ArgumentNullException"/> if
        /// called without a <see cref="GenomeBuilder"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingGenomeBuilder()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenomeSearchPoint(Vector<double>.Build.Dense(4), this._parent, genomeBuilder: null));
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.CreateFromGenome"/> throws a <see cref="ArgumentNullException"/>
        /// if called without a <see cref="Genome"/>.
        /// </summary>
        [Fact]
        public void CreateFromGenomeThrowsForMissingGenome()
        {
            Assert.Throws<ArgumentNullException>(
                () => GenomeSearchPoint.CreateFromGenome(
                    genome: null,
                    parameterTree: this._parameterTree,
                    minimumDomainSize: this._minimumDomainSize,
                    genomeBuilder: this._genomeBuilder));
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.CreateFromGenome"/> throws a <see cref="ArgumentNullException"/>
        /// if called without a <see cref="ParameterTree"/>.
        /// </summary>
        [Fact]
        public void CreateFromGenomeThrowsForMissingParameterTree()
        {
            Assert.Throws<ArgumentNullException>(
                () => GenomeSearchPoint.CreateFromGenome(
                    this._genomeBuilder.CreateRandomGenome(age: 1),
                    parameterTree: null,
                    minimumDomainSize: this._minimumDomainSize,
                    genomeBuilder: this._genomeBuilder));
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.CreateFromGenome"/> throws a <see cref="ArgumentNullException"/>
        /// if called without a <see cref="GenomeBuilder"/>.
        /// </summary>
        [Fact]
        public void CreateFromGenomeThrowsForMissingGenomeBuilder()
        {
            Assert.Throws<ArgumentNullException>(
                () => GenomeSearchPoint.CreateFromGenome(
                    this._genomeBuilder.CreateRandomGenome(age: 1),
                    this._parameterTree,
                    this._minimumDomainSize,
                    genomeBuilder: null));
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.CreateFromGenome"/> correctly extracts real-valued parameters from
        /// the <see cref="Genome"/>.
        /// </summary>
        [Fact]
        public void CreateFromGenomeExtractsCorrectValues()
        {
            var genome = this._genomeBuilder.CreateRandomGenome(age: 2);
            var point = GenomeSearchPoint.CreateFromGenome(
                genome,
                this._parameterTree,
                this._minimumDomainSize,
                this._genomeBuilder);

            var continuousIdentifiers = new[] { "a", "c", "e", "f" };
            Assert.True(
                continuousIdentifiers.Length == point.Values.Count,
                $"There should be four continuous parameters: {TestUtils.PrintList(continuousIdentifiers)}.");
            for (int i = 0; i < continuousIdentifiers.Length; i++)
            {
                var identifier = continuousIdentifiers[i];
                Assert.Equal(
                    Convert.ToDouble(genome.GetGeneValue(identifier).GetValue()),
                    point.Values[i]);
            }
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.CreateFromGenome"/> copies only information that stays true for
        /// the created genome.
        /// </summary>
        [Fact]
        public void CreateFromGenomeCopiesCorrectInformation()
        {
            var genome = this._genomeBuilder.CreateRandomGenome(age: 2);
            genome.IsEngineered = true;

            var point = GenomeSearchPoint.CreateFromGenome(
                genome,
                this._parameterTree,
                this._minimumDomainSize,
                this._genomeBuilder);

            Assert.False(
                point.Genome.CreateMutableGenome().IsEngineered,
                "IsEngineered property should not be copied over.");

            // Age is only relevant for GGA. As points are not generated in GGA, the age should be fixed.
            Assert.Equal(
                genome.Age,
                point.Genome.CreateMutableGenome().Age);
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.BaseRandomPointOnGenome"/> throws a <see cref="ArgumentNullException"/>
        /// if called without a <see cref="Genome"/>.
        /// </summary>
        [Fact]
        public void BaseRandomPointOnGenomeThrowsForMissingGenome()
        {
            Assert.Throws<ArgumentNullException>(
                () => GenomeSearchPoint.BaseRandomPointOnGenome(
                    genome: null,
                    parameterTree: this._parameterTree,
                    minimumDomainSize: this._minimumDomainSize,
                    genomeBuilder: this._genomeBuilder));
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.BaseRandomPointOnGenome"/> throws a <see cref="ArgumentNullException"/>
        /// if called without a <see cref="ParameterTree"/>.
        /// </summary>
        [Fact]
        public void BaseRandomPointOnGenomeThrowsForMissingParameterTree()
        {
            Assert.Throws<ArgumentNullException>(
                () => GenomeSearchPoint.BaseRandomPointOnGenome(
                    this._genomeBuilder.CreateRandomGenome(age: 1),
                    parameterTree: null,
                    minimumDomainSize: this._minimumDomainSize,
                    genomeBuilder: this._genomeBuilder));
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.BaseRandomPointOnGenome"/> throws a <see cref="ArgumentNullException"/>
        /// if called without a <see cref="GenomeBuilder"/>.
        /// </summary>
        [Fact]
        public void BaseRandomPointOnGenomeThrowsForMissingGenomeBuilder()
        {
            Assert.Throws<ArgumentNullException>(
                () => GenomeSearchPoint.BaseRandomPointOnGenome(
                    this._genomeBuilder.CreateRandomGenome(age: 1),
                    this._parameterTree,
                    this._minimumDomainSize,
                    genomeBuilder: null));
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.BaseRandomPointOnGenome"/> copies only information that stays true for
        /// the created genome.
        /// </summary>
        [Fact]
        public void BaseRandomPointOnGenomeCopiesCorrectInformation()
        {
            var genome = this._genomeBuilder.CreateRandomGenome(age: 2);
            genome.IsEngineered = true;

            var point = GenomeSearchPoint.BaseRandomPointOnGenome(
                genome,
                this._parameterTree,
                this._minimumDomainSize,
                this._genomeBuilder);

            Assert.False(
                point.Genome.CreateMutableGenome().IsEngineered,
                "IsEngineered property should not be copied over.");
            // Age is only relevant for GGA. As points are not generated in GGA, the age should be fixed.
            Assert.Equal(
                genome.Age,
                point.Genome.CreateMutableGenome().Age);
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.BaseRandomPointOnGenome"/> keeps all real-valued parameters from
        /// the <see cref="Genome"/>.
        /// </summary>
        [Fact]
        public void BaseRandomPointOnGenomeFixesNonContinuousValues()
        {
            var genome = this._genomeBuilder.CreateRandomGenome(age: 2);
            var point = GenomeSearchPoint.BaseRandomPointOnGenome(
                genome,
                this._parameterTree,
                this._minimumDomainSize,
                this._genomeBuilder);

            var nonContinuousIdentifiers = new[] { "b", "d", "aa" };
            foreach (var identifier in nonContinuousIdentifiers)
            {
                Assert.Equal(
                    genome.GetGeneValue(identifier).GetValue(),
                    point.Genome.CreateMutableGenome().GetGeneValue(identifier).GetValue());
            }
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.BaseRandomPointOnGenome"/> sets real-valued parameters uniformly
        /// at random.
        /// </summary>
        [Fact]
        public void BaseRandomPointOnGenomeRandomlyChoosesContinuousValues()
        {
            var genome = this._genomeBuilder.CreateRandomGenome(age: 2);

            // Sample many points.
            int numberPoints = 1000;
            var valuesForLargeIntegerDomain = new Dictionary<int, int>();
            for (int i = 0; i < numberPoints; i++)
            {
                var point = GenomeSearchPoint.BaseRandomPointOnGenome(
                    genome,
                    this._parameterTree,
                    this._minimumDomainSize,
                    this._genomeBuilder);

                // Remember one of their values.
                var value = (int)point.Genome.CreateMutableGenome().GetGeneValue("a").GetValue();
                if (valuesForLargeIntegerDomain.ContainsKey(value))
                {
                    valuesForLargeIntegerDomain[value]++;
                }
                else
                {
                    valuesForLargeIntegerDomain.Add(value, 1);
                }
            }

            // Apply the Chi-Squared test on that gene.
            var numberPossibleGeneValues = (int)this._parameterTree.GetNumericalParameters()
                .Single(parameter => parameter.Identifier.Equals("a"))
                .Domain
                .DomainSize;
            double[] observed =
                valuesForLargeIntegerDomain.Select(keyValuePair => (double)keyValuePair.Value).ToArray();
            double[] expected = Enumerable.Range(0, numberPossibleGeneValues)
                .Select(i => (double)numberPoints / numberPossibleGeneValues)
                .ToArray();
            ChiSquareTest uniformTest = new ChiSquareTest(expected, observed, degreesOfFreedom: numberPossibleGeneValues - 1);
            Assert.False(
                uniformTest.Significant,
                $"BaseRandomPointOnGenome was found to not create uniform distributions by the Chi-Squared test test with significance level of {uniformTest.Size}.");
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.IsValid"/> checks for boundary constraints.
        /// </summary>
        [Fact]
        public void IsValidChecksBoundaries()
        {
            var point = new GenomeSearchPoint(
                Vector<double>.Build.DenseOfArray(new[] { 1.2, 3.5, -24.3, 0.8 }),
                this._parent,
                this._genomeBuilder);
            Assert.False(point.IsValid(), $"{point} should not be valid.");
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.IsValid"/> returns true for valid points.
        /// </summary>
        [Fact]
        public void IsValidReturnsTrueForValidPoint()
        {
            var point = new GenomeSearchPoint(
                Vector<double>.Build.DenseOfArray(new[] { 1.2, 3.5, 0.3, 0.8 }),
                this._parent,
                this._genomeBuilder);
            Assert.True(point.IsValid(), $"{point} should be valid.");
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.IsValid"/> calls <see cref="GenomeBuilder.IsGenomeValid"/>.
        /// </summary>
        [Fact]
        public void IsValidChecksCustomGenomeBuilder()
        {
            var genome = this._genomeBuilder.CreateRandomGenome(age: 2);
            var point = GenomeSearchPoint.CreateFromGenome(
                genome,
                this._parameterTree,
                this._minimumDomainSize,
                this._genomeBuilder);
            Assert.True(point.IsValid(), $"{point} should be valid.");

            var pointAsForbidden = GenomeSearchPoint.CreateFromGenome(
                genome,
                this._parameterTree,
                this._minimumDomainSize,
                new ConfigurableGenomeBuilder(this._parameterTree, isValidFunction: g => false, mutationRate: 0));
            Assert.False(
                pointAsForbidden.IsValid(),
                $"{pointAsForbidden} should not be valid for other genome builder.");
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint"/> rounds values belonging to integer domains.
        /// </summary>
        [Fact]
        public void GenomeSearchPointRoundsIntegerValues()
        {
            var point = new GenomeSearchPoint(
                Vector<double>.Build.DenseOfArray(new[] { 0.51, -3.2, -24.3, 12.8 }),
                this._parent,
                this._genomeBuilder);
            var genome = point.Genome.CreateMutableGenome();

            Assert.Equal(
                1,
                genome.GetGeneValue("a").GetValue());
            Assert.Equal(
                -3,
                genome.GetGeneValue("c").GetValue());
            Assert.Equal(
                -24.3,
                genome.GetGeneValue("e").GetValue());
            Assert.Equal(
                12.8,
                genome.GetGeneValue("f").GetValue());
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.Genome"/> reverses
        /// <see cref="GenomeSearchPoint.CreateFromGenome"/>.
        /// </summary>
        [Fact]
        public void GenomePropertyReversesCreateFromGenome()
        {
            var originalGenome = this._genomeBuilder.CreateRandomGenome(age: 2);
            var point = GenomeSearchPoint.CreateFromGenome(
                originalGenome,
                this._parameterTree,
                this._minimumDomainSize,
                this._genomeBuilder);
            var createdGenome = point.Genome.CreateMutableGenome();

            Assert.True(
                new Genome.GeneValueComparator().Equals(originalGenome, createdGenome),
                $"{createdGenome} should have the same values as the genome it is based on, {originalGenome}.");
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint"/> uses the values fixed by a parent if the 
        /// <see cref="GenomeSearchPoint"/> was constructed via the parent-constructor.
        /// </summary>
        [Fact]
        public void GenomeSearchPointMakesUseOfParent()
        {
            var point = new GenomeSearchPoint(Vector<double>.Build.Dense(4), this._parent, this._genomeBuilder);
            var genome = point.Genome.CreateMutableGenome();
            var parentGenome = this._parent.Genome.CreateMutableGenome();

            var fixedParameters = new[] { "b", "d", "aa" };
            foreach (var identifier in fixedParameters)
            {
                Assert.Equal(
                    parentGenome.GetGeneValue(identifier).GetValue(),
                    genome.GetGeneValue(identifier).GetValue());
            }
        }

        /// <summary>
        /// Checks that the <see cref="Genome"/> produced by <see cref="GenomeSearchPoint.Genome"/> is
        /// independent from the <see cref="Genome"/> the <see cref="GenomeSearchPoint"/> was initialized
        /// with.
        /// </summary>
        [Fact]
        public void GenomePropertyProducesIndependentGenome()
        {
            var genome = this._genomeBuilder.CreateRandomGenome(age: 0);
            var point = GenomeSearchPoint.CreateFromGenome(
                genome,
                this._parameterTree,
                this._minimumDomainSize,
                this._genomeBuilder);
            genome.SetGene("a", new Allele<int>(234));

            var createdGenome = point.Genome.CreateMutableGenome();
            Assert.NotEqual(
                234,
                createdGenome.GetGeneValue("a").GetValue());
        }

        /// <summary>
        /// Checks that <see cref="GenomeSearchPoint.ToString"/> returns the same <see cref="string"/> as its
        /// underlying <see cref="Genome"/> does.
        /// </summary>
        [Fact]
        public void ToStringPrintsGenome()
        {
            var genome = this._genomeBuilder.CreateRandomGenome(age: 0);
            var point = GenomeSearchPoint.CreateFromGenome(
                genome,
                this._parameterTree,
                this._minimumDomainSize,
                this._genomeBuilder);
            Assert.Equal(genome.ToString(), point.ToString());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a <see cref="ParameterTree"/> containing parameters of many different domain types.
        /// </summary>
        /// <returns>The created <see cref="ParameterTree"/>.</returns>
        private ParameterTree CreateParameterTree()
        {
            var root = new AndNode();
            root.AddChild(new ValueNode<int>("a", new IntegerDomain(0, this._minimumDomainSize - 1)));
            root.AddChild(new ValueNode<int>("b", new IntegerDomain(0, this._minimumDomainSize - 2)));
            root.AddChild(new ValueNode<double>("e", new ContinuousDomain(0, 1)));
            root.AddChild(new ValueNode<double>("f", new LogDomain(0.1, 1)));
            root.AddChild(new ValueNode<int>("c", new DiscreteLogDomain(1, this._minimumDomainSize)));
            root.AddChild(new ValueNode<int>("d", new DiscreteLogDomain(1, this._minimumDomainSize - 1)));
            root.AddChild(new ValueNode<double>("aa", new CategoricalDomain<double>(new List<double> { 0d, 1d })));
            return new ParameterTree(root);
        }

        #endregion
    }
}