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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Local
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Local;
    using Optano.Algorithm.Tuner.Tests.GenomeBuilders;

    using Shouldly;

    using Xunit;

    // TODO MAYBE #34848: Create superclass for this and ContinuizedGenomeSearchPointTest!
    /// <summary>
    /// Contains tests for the <see cref="PartialGenomeSearchPoint"/> class.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class PartialGenomeSearchPointTest : IDisposable
    {
        #region Fields

        /// <summary>
        /// Minimum size of an integer domain to be handled as continuous.
        /// </summary>
        private readonly int _minimumDomainSize = 4;

        /// <summary>
        /// Lower bounds which fit <see cref="_parameterTree"/>, sorted by identifier.
        /// </summary>
        private readonly double[] _lowerBounds = { 0, 0.1, 0, 1 };

        /// <summary>
        /// Upper bounds which fit <see cref="_parameterTree"/>, sorted by identifier.
        /// </summary>
        private readonly double[] _upperBounds = { 1.4, 1, 3, 4 };

        /// <summary>
        /// A <see cref="ParameterTree"/> containing parameters of different domain types.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// A <see cref="GenomeBuilder"/> which fits <see cref="_parameterTree"/>.
        /// </summary>
        private readonly GenomeBuilder _genomeBuilder;

        /// <summary>
        /// A <see cref="GenomeSearchPointConverter"/> which fits <see cref="_parameterTree"/>.
        /// </summary>
        private readonly GenomeSearchPointConverter _genomeSearchPointConverter;

        /// <summary>
        /// A <see cref="ImmutableGenome"/> fitting <see cref="_parameterTree"/>.
        /// </summary>
        private readonly ImmutableGenome _baseGenome;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PartialGenomeSearchPointTest"/> class.
        /// </summary>
        public PartialGenomeSearchPointTest()
        {
            Randomizer.Configure();

            this._parameterTree = this.CreateParameterTree();
            this._genomeBuilder = new GenomeBuilder(
                this._parameterTree,
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetEnableRacing(true).Build(1));
            this._genomeSearchPointConverter = new GenomeSearchPointConverter(this._parameterTree, this._minimumDomainSize);
            this._baseGenome = new ImmutableGenome(this._genomeBuilder.CreateRandomGenome(age: 0));
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
        /// Checks that <see cref="PartialGenomeSearchPoint"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without any genome.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingGenome()
        {
            Assert.Throws<ArgumentNullException>(
                () => new PartialGenomeSearchPoint(
                    underlyingGenome: null,
                    values: Vector<double>.Build.Random(4),
                    genomeSearchPointConverter: this._genomeSearchPointConverter,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: this._lowerBounds,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without any values.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingValues()
        {
            Assert.Throws<ArgumentNullException>(
                () => new PartialGenomeSearchPoint(
                    underlyingGenome: this._baseGenome,
                    values: null,
                    genomeSearchPointConverter: this._genomeSearchPointConverter,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: this._lowerBounds,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without a <see cref="GenomeSearchPointConverter"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingConverter()
        {
            Assert.Throws<ArgumentNullException>(
                () => new PartialGenomeSearchPoint(
                    underlyingGenome: this._baseGenome,
                    values: Vector<double>.Build.Random(4),
                    genomeSearchPointConverter: null,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: this._lowerBounds,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without a <see cref="GenomeBuilder"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingGenomeBuilder()
        {
            Assert.Throws<ArgumentNullException>(
                () => new PartialGenomeSearchPoint(
                    underlyingGenome: this._baseGenome,
                    values: Vector<double>.Build.Random(4),
                    genomeSearchPointConverter: this._genomeSearchPointConverter,
                    genomeBuilder: null,
                    lowerBounds: this._lowerBounds,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint"/>s constructor throws
        /// a <see cref="ArgumentNullException"/> if called without lower bounds.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingLowerBounds()
        {
            Assert.Throws<ArgumentNullException>(
                () => new PartialGenomeSearchPoint(
                    underlyingGenome: this._baseGenome,
                    values: Vector<double>.Build.Random(4),
                    genomeSearchPointConverter: this._genomeSearchPointConverter,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: null,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint"/>s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without upper bounds.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingUpperBounds()
        {
            Assert.Throws<ArgumentNullException>(
                () => new PartialGenomeSearchPoint(
                    underlyingGenome: this._baseGenome,
                    values: Vector<double>.Build.Random(4),
                    genomeSearchPointConverter: this._genomeSearchPointConverter,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: this._lowerBounds,
                    upperBounds: null));
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint"/>'s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a number of values inconsistent with its
        /// genome converter.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForWrongNumberOfValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PartialGenomeSearchPoint(
                    underlyingGenome: this._baseGenome,
                    values: Vector<double>.Build.Random(3),
                    genomeSearchPointConverter: this._genomeSearchPointConverter,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: this._lowerBounds.Take(3).ToArray(),
                    upperBounds: this._upperBounds.Take(3).ToArray()));
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint"/>s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a number of lower bounds not fitting the
        /// dimension.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForWrongNumberOfLowerBounds()
        {
            var notEnoughLowerBounds = new[] { -203.4, -67, -3 };
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PartialGenomeSearchPoint(
                    underlyingGenome: this._baseGenome,
                    values: Vector<double>.Build.Random(4),
                    genomeSearchPointConverter: this._genomeSearchPointConverter,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: notEnoughLowerBounds,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint"/>s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a number of upper bounds not fitting the
        /// dimension.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForWrongNumberOfUpperBounds()
        {
            var tooManyUpperBounds = new[] { 104.56, 120.02, 60, 0, 46 };
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PartialGenomeSearchPoint(
                    underlyingGenome: this._baseGenome,
                    values: Vector<double>.Build.Random(4),
                    genomeSearchPointConverter: this._genomeSearchPointConverter,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: this._lowerBounds,
                    upperBounds: tooManyUpperBounds));
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint"/>s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a lower bound greater than its respective upper
        /// bound.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForLowerBoundHigherUpperBound()
        {
            this._lowerBounds[1] = this._upperBounds[1] + 0.1;
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PartialGenomeSearchPoint(
                    underlyingGenome: this._baseGenome,
                    values: Vector<double>.Build.Random(4),
                    genomeSearchPointConverter: this._genomeSearchPointConverter,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: this._lowerBounds,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint"/>s constructor sets <see cref="SearchPoint.Values"/>.
        /// </summary>
        [Fact]
        public void ConstructorCorrectlySetsValues()
        {
            var providedValues = Vector<double>.Build.Random(4);
            var searchPoint = new PartialGenomeSearchPoint(
                underlyingGenome: this._baseGenome,
                values: providedValues,
                genomeSearchPointConverter: this._genomeSearchPointConverter,
                genomeBuilder: this._genomeBuilder,
                lowerBounds: this._lowerBounds,
                upperBounds: this._upperBounds);
            Assert.Equal(providedValues, searchPoint.Values);
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint"/>s constructor associates the correct
        /// <see cref="Genome"/> to provided values.
        /// </summary>
        [Fact]
        public void ConstructorAssociatesCorrectGenome()
        {
            var genomeValues = new[] { 1.4, 0.8, 2.7, 1.2 };
            var offset = 20 * 5;
            var searchPoint = new PartialGenomeSearchPoint(
                underlyingGenome: this._baseGenome,
                values: BoundedSearchPoint.StandardizeValues(genomeValues, this._lowerBounds, this._upperBounds) + offset,
                genomeSearchPointConverter: this._genomeSearchPointConverter,
                genomeBuilder: this._genomeBuilder,
                lowerBounds: this._lowerBounds,
                upperBounds: this._upperBounds);

            var genome = searchPoint.Genome.CreateMutableGenome();
            Assert.Equal(
                this._baseGenome.CreateMutableGenome().GetGeneValue("categorical").GetValue(),
                genome.GetGeneValue("categorical").GetValue());
            Assert.Equal(
                1.4,
                genome.GetGeneValue("continuous").GetValue());
            Assert.Equal(
                this._baseGenome.CreateMutableGenome().GetGeneValue("discrete").GetValue(),
                genome.GetGeneValue("discrete").GetValue());
            Assert.Equal(
                this._baseGenome.CreateMutableGenome().GetGeneValue("discrete-log").GetValue(),
                genome.GetGeneValue("discrete-log").GetValue());
            Assert.Equal(
                0.8,
                (double)genome.GetGeneValue("log").GetValue(),
                4);
            Assert.False(searchPoint.IsRepaired, "The genome should not have needed repair.");
            Assert.True(
                3 == (int)genome.GetGeneValue("quasi-continuous").GetValue(),
                "Quasi continuous value should be rounded to nearest integer.");
            Assert.True(
                1 == (int)genome.GetGeneValue("quasi-continuous-log").GetValue(),
                "Quasi continuous value should be rounded to nearest integer.");
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint"/>s constructor associates repaired
        /// <see cref="Genome"/>s if the direct transform is invalid.
        /// </summary>
        [Fact]
        public void InvalidGenomeIsHandledCorrectly()
        {
            var positiveGenomeBuilder = new ConfigurableGenomeBuilder(
                this._parameterTree,
                g => (int)g.GetGeneValue("discrete").GetValue() != 0,
                mutationRate: 1);
            var genome = this._genomeBuilder.CreateRandomGenome(0);
            genome.SetGene("discrete", new Allele<int>(0));
            var genomeValues = new[] { 1.4, 0.3, 0, 1.5 };
            var standardizedValues =
                BoundedSearchPoint.StandardizeValues(genomeValues, this._lowerBounds, this._upperBounds);
            var searchPoint = new PartialGenomeSearchPoint(
                underlyingGenome: new ImmutableGenome(genome),
                values: standardizedValues,
                genomeSearchPointConverter: this._genomeSearchPointConverter,
                genomeBuilder: positiveGenomeBuilder,
                lowerBounds: this._lowerBounds,
                upperBounds: this._upperBounds);

            var associatedGenome = searchPoint.Genome.CreateMutableGenome();
            Assert.True(
                0 != (int)associatedGenome.GetGeneValue("discrete").GetValue(),
                "Genome should have been repaired.");
            Assert.True(searchPoint.IsRepaired, "Repair flag should have been set.");
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint.CreateFromGenome"/> throws a
        /// <see cref="ArgumentNullException"/> if called without a <see cref="Genome"/>.
        /// </summary>
        [Fact]
        public void CreateFromGenomeThrowsForMissingGenome()
        {
            Assert.Throws<ArgumentNullException>(
                () => PartialGenomeSearchPoint.CreateFromGenome(
                    genome: null,
                    parameterTree: this._parameterTree,
                    minimumDomainSize: 5));
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint.CreateFromGenome"/> throws a
        /// <see cref="ArgumentNullException"/> if called without a <see cref="ParameterTree"/>.
        /// </summary>
        [Fact]
        public void CreateFromGenomeThrowsForMissingParameterTree()
        {
            Assert.Throws<ArgumentNullException>(
                () => PartialGenomeSearchPoint.CreateFromGenome(
                    this._genomeBuilder.CreateRandomGenome(age: 1),
                    parameterTree: null,
                    minimumDomainSize: 5));
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint.CreateFromGenome"/> correctly extracts real-valued
        /// parameters from the <see cref="Genome"/>.
        /// </summary>
        [Fact]
        public void CreateFromGenomeExtractsCorrectValues()
        {
            var genome = this._genomeBuilder.CreateRandomGenome(age: 2);
            var point = PartialGenomeSearchPoint.CreateFromGenome(
                genome,
                this._parameterTree,
                this._minimumDomainSize);

            var continuousIdentifiers = new[] { "continuous", "log", "quasi-continuous", "quasi-continuous-log" };
            Assert.True(
                continuousIdentifiers.Length == point.Values.Count,
                $"There should be four continuous parameters: {TestUtils.PrintList(continuousIdentifiers)}.");
            var values = BoundedSearchPoint.StandardizeValues(
                continuousIdentifiers.Select(id => Convert.ToDouble(genome.GetGeneValue(id).GetValue())).ToArray(),
                this._lowerBounds,
                this._upperBounds);
            for (int i = 0; i < continuousIdentifiers.Length; i++)
            {
                var identifier = continuousIdentifiers[i];
                Console.Out.WriteLine($"Checking value of parameter {identifier}...");
                Assert.Equal(
                    values[i],
                    point.Values[i]);
            }
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint"/>'s constructor and
        /// <see cref="PartialGenomeSearchPoint.CreateFromGenome"/> are consistent in (re)transforming
        /// <see cref="Genome"/>s to values.
        /// </summary>
        [Fact]
        public void ConstructionIsConsistent()
        {
            // We do not care about age --> set it to default age.
            var genome = this._genomeBuilder.CreateRandomGenome(age: 0);
            genome.SetGene("quasi-continuous-log", new Allele<int>(3));
            var searchPoint = PartialGenomeSearchPoint.CreateFromGenome(
                genome,
                this._parameterTree,
                this._minimumDomainSize);
            var samePoint = new PartialGenomeSearchPoint(
                underlyingGenome: new ImmutableGenome(genome),
                values: searchPoint.Values,
                genomeSearchPointConverter: this._genomeSearchPointConverter,
                genomeBuilder: this._genomeBuilder,
                lowerBounds: this._lowerBounds,
                upperBounds: this._upperBounds);
            var genomeString = genome.ToCappedDecimalString();
            var samePointString = samePoint.Genome.ToCappedDecimalString();

            genomeString.ShouldBe(samePointString, "Genome should stay the same if transformed into values and back.");

            var values = BoundedSearchPoint.StandardizeValues(new[] { 0, 0.2, 3, 2 }, this._lowerBounds, this._upperBounds);
            searchPoint = new PartialGenomeSearchPoint(
                underlyingGenome: new ImmutableGenome(genome),
                values: values,
                genomeSearchPointConverter: this._genomeSearchPointConverter,
                genomeBuilder: this._genomeBuilder,
                lowerBounds: this._lowerBounds,
                upperBounds: this._upperBounds);
            samePoint = PartialGenomeSearchPoint.CreateFromGenome(
                searchPoint.Genome.CreateMutableGenome(),
                this._parameterTree,
                this._minimumDomainSize);
            for (int i = 0; i < values.Count; i++)
            {
                Assert.Equal(
                    values[i],
                    samePoint.Values[i],
                    4);
            }
        }

        /// <summary>
        /// Checks that the <see cref="Genome"/> produced by <see cref="PartialGenomeSearchPoint.Genome"/> is
        /// independent from the <see cref="Genome"/> the <see cref="PartialGenomeSearchPoint"/> was initialized
        /// with.
        /// </summary>
        [Fact]
        public void GenomePropertyProducesIndependentGenome()
        {
            var genome = this._genomeBuilder.CreateRandomGenome(age: 0);
            var point = PartialGenomeSearchPoint.CreateFromGenome(genome, this._parameterTree, this._minimumDomainSize);
            genome.SetGene("discrete", new Allele<double>(-6));

            var createdGenome = point.Genome.CreateMutableGenome();
            Assert.True(
                -6 != (int)createdGenome.GetGeneValue("discrete").GetValue(),
                "Created genome should be a different object than the one the point was initialized with.");
        }

        /// <summary>
        /// Checks that <see cref="PartialGenomeSearchPoint.ObtainParameterBounds"/> correctly identifies the bounds
        /// of (quasi-)continuous parameters.
        /// </summary>
        [Fact]
        public void ObtainParameterBoundsFindsCorrectBounds()
        {
            PartialGenomeSearchPoint.ObtainParameterBounds(
                this._parameterTree,
                this._minimumDomainSize,
                out var obtainedLowerBounds,
                out var obtainedUpperBounds);
            Assert.Equal(this._lowerBounds, obtainedLowerBounds);
            Assert.Equal(this._upperBounds, obtainedUpperBounds);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a <see cref="ParameterTree"/> containing parameters of different domain types.
        /// </summary>
        /// <returns>The created <see cref="ParameterTree"/>.</returns>
        private ParameterTree CreateParameterTree()
        {
            var root = new AndNode();

            root.AddChild(new ValueNode<double>("categorical", new CategoricalDomain<double>(new List<double> { 123.6, -12.5 })));
            root.AddChild(new ValueNode<double>("continuous", new ContinuousDomain(0, 1.4)));
            root.AddChild(new ValueNode<int>("discrete", new IntegerDomain(0, this._minimumDomainSize - 2)));
            root.AddChild(new ValueNode<int>("discrete-log", new DiscreteLogDomain(1, this._minimumDomainSize - 1)));
            root.AddChild(new ValueNode<double>("log", new LogDomain(0.1, 1)));
            root.AddChild(new ValueNode<int>("quasi-continuous", new IntegerDomain(0, this._minimumDomainSize - 1)));
            root.AddChild(new ValueNode<int>("quasi-continuous-log", new DiscreteLogDomain(1, this._minimumDomainSize)));

            return new ParameterTree(root);
        }

        #endregion
    }
}