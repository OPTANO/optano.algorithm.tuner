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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Global
{
    using System;
    using System.Collections.Generic;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Global;
    using Optano.Algorithm.Tuner.Tests.GenomeBuilders;

    using Shouldly;

    using Xunit;

    // TODO MAYBE #34848: Create superclass for this and PartialGenomeSearchPointTest!
    /// <summary>
    /// Contains tests for the <see cref="ContinuizedGenomeSearchPoint"/> class.
    /// </summary>
    public class ContinuizedGenomeSearchPointTest : IDisposable
    {
        #region Fields

        /// <summary>
        /// Lower bounds which fit <see cref="_parameterTree"/>, sorted by identifier.
        /// </summary>
        private readonly double[] _lowerBounds = { 0, 0, -5, 0 };

        /// <summary>
        /// Upper bounds which fit <see cref="_parameterTree"/>, sorted by identifier.
        /// </summary>
        private readonly double[] _upperBounds = { 1, 1.4, 5, 0 };

        /// <summary>
        /// A <see cref="ParameterTree"/> containing parameters of different domain types.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// A <see cref="GenomeBuilder"/> which fits <see cref="_parameterTree"/>.
        /// </summary>
        private readonly GenomeBuilder _genomeBuilder;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuizedGenomeSearchPointTest"/> class.
        /// </summary>
        public ContinuizedGenomeSearchPointTest()
        {
            Randomizer.Reset();
            Randomizer.Configure();

            this._parameterTree = this.CreateParameterTree();
            this._genomeBuilder = new GenomeBuilder(
                this._parameterTree,
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetEnableRacing(true).Build(1));
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
        /// Checks that <see cref="ContinuizedGenomeSearchPoint"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without any values.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingValues()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ContinuizedGenomeSearchPoint(
                    values: null,
                    parameterTree: this._parameterTree,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: this._lowerBounds,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without a <see cref="ParameterTree"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingParameterTree()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ContinuizedGenomeSearchPoint(
                    values: Vector<double>.Build.Random(4),
                    parameterTree: null,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: this._lowerBounds,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without a <see cref="GenomeBuilder"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingGenomeBuilder()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ContinuizedGenomeSearchPoint(
                    values: Vector<double>.Build.Random(4),
                    parameterTree: this._parameterTree,
                    genomeBuilder: null,
                    lowerBounds: this._lowerBounds,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint"/>s constructor throws
        /// a <see cref="ArgumentNullException"/> if called without lower bounds.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingLowerBounds()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ContinuizedGenomeSearchPoint(
                    values: Vector<double>.Build.Random(4),
                    parameterTree: this._parameterTree,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: null,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint"/>s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without upper bounds.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingUpperBounds()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ContinuizedGenomeSearchPoint(
                    values: Vector<double>.Build.Random(4),
                    parameterTree: this._parameterTree,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: this._lowerBounds,
                    upperBounds: null));
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint"/>s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a number of lower bounds not fitting the
        /// dimension.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForWrongNumberOfLowerBounds()
        {
            var notEnoughLowerBounds = new[] { -203.4, -67, -3 };
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ContinuizedGenomeSearchPoint(
                    values: Vector<double>.Build.Random(4),
                    parameterTree: this._parameterTree,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: notEnoughLowerBounds,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint"/>s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a number of upper bounds not fitting the
        /// dimension.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForWrongNumberOfUpperBounds()
        {
            var tooManyUpperBounds = new[] { 104.56, 120.02, 60, 0, 46 };
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ContinuizedGenomeSearchPoint(
                    values: Vector<double>.Build.Random(4),
                    parameterTree: this._parameterTree,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: this._lowerBounds,
                    upperBounds: tooManyUpperBounds));
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint"/>s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a lower bound greater than its respective upper
        /// bound.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForLowerBoundHigherUpperBound()
        {
            this._lowerBounds[1] = this._upperBounds[1] + 0.1;
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ContinuizedGenomeSearchPoint(
                    values: Vector<double>.Build.Random(4),
                    parameterTree: this._parameterTree,
                    genomeBuilder: this._genomeBuilder,
                    lowerBounds: this._lowerBounds,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint"/>s constructor sets <see cref="SearchPoint.Values"/>.
        /// </summary>
        [Fact]
        public void ConstructorCorrectlySetsValues()
        {
            var providedValues = Vector<double>.Build.Random(4);
            var searchPoint = new ContinuizedGenomeSearchPoint(
                values: providedValues,
                parameterTree: this._parameterTree,
                genomeBuilder: this._genomeBuilder,
                lowerBounds: this._lowerBounds,
                upperBounds: this._upperBounds);
            Assert.Equal(providedValues, searchPoint.Values);
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint"/>s constructor associates the correct
        /// <see cref="Genome"/> to provided values.
        /// </summary>
        [Fact]
        public void ConstructorAssociatesCorrectGenome()
        {
            var genomeValues = new[] { 0.6, 1.4, -4.3, 123.8 };
            var offset = 20 * 5;
            var searchPoint = new ContinuizedGenomeSearchPoint(
                values: BoundedSearchPoint.StandardizeValues(genomeValues, this._lowerBounds, this._upperBounds) + offset,
                parameterTree: this._parameterTree,
                genomeBuilder: this._genomeBuilder,
                lowerBounds: this._lowerBounds,
                upperBounds: this._upperBounds);

            var genome = searchPoint.Genome.CreateMutableGenome();

            // Categorical value should be rounded up to 1 and then be mapped to the second value.
            Assert.Equal(
                -12.5,
                genome.GetGeneValue("categorical").GetValue());

            Assert.Equal(1.4, genome.GetGeneValue("continuous").GetValue());
            Assert.True(-4 == (int)genome.GetGeneValue("discrete").GetValue(), "Integer value should be rounded up.");
            Assert.True(
                "foo" == (string)genome.GetGeneValue("single_categorical").GetValue(),
                "Single categorical value only has one valid value.");
            Assert.False(searchPoint.IsRepaired, "The genome should not have needed repair.");
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint"/>s constructor associates repaired
        /// <see cref="Genome"/>s if the direct transform is invalid.
        /// </summary>
        [Fact]
        public void InvalidGenomeIsHandledCorrectly()
        {
            var positiveGenomeBuilder = new ConfigurableGenomeBuilder(
                this._parameterTree,
                g => (int)g.GetGeneValue("discrete").GetValue() != -4,
                mutationRate: 1);
            var genomeValues = new[] { 0, 1.4, -4, 0 };
            var standardizedValues =
                BoundedSearchPoint.StandardizeValues(genomeValues, this._lowerBounds, this._upperBounds);
            var searchPoint = new ContinuizedGenomeSearchPoint(
                values: standardizedValues,
                parameterTree: this._parameterTree,
                genomeBuilder: positiveGenomeBuilder,
                lowerBounds: this._lowerBounds,
                upperBounds: this._upperBounds);

            var genome = searchPoint.Genome.CreateMutableGenome();
            Assert.True(
                -4 != (int)genome.GetGeneValue("discrete").GetValue(),
                "Genome should have been repaired.");
            Assert.Equal(standardizedValues, searchPoint.Values);
            Assert.True(searchPoint.IsRepaired, "Repair flag should have been set.");
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint.CreateFromGenome"/> throws a
        /// <see cref="ArgumentNullException"/> if called without a <see cref="Genome"/>.
        /// </summary>
        [Fact]
        public void CreateFromGenomeThrowsForMissingGenome()
        {
            Assert.Throws<ArgumentNullException>(
                () => ContinuizedGenomeSearchPoint.CreateFromGenome(
                    genome: null,
                    parameterTree: this._parameterTree));
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint.CreateFromGenome"/> throws a
        /// <see cref="ArgumentNullException"/> if called without a <see cref="ParameterTree"/>.
        /// </summary>
        [Fact]
        public void CreateFromGenomeThrowsForMissingParameterTree()
        {
            Assert.Throws<ArgumentNullException>(
                () => ContinuizedGenomeSearchPoint.CreateFromGenome(
                    this._genomeBuilder.CreateRandomGenome(age: 1),
                    parameterTree: null));
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint.CreateFromGenome"/> sets
        /// <see cref="ContinuizedGenomeSearchPoint.Genome"/> to the provided genome,
        /// <see cref="SearchPoint.Values"/> to the transformed values, and
        /// <see cref="ContinuizedGenomeSearchPoint.IsRepaired"/> to <c>false</c>.
        /// </summary>
        [Fact]
        public void CreateFromGenomeSetsPropertiesCorrectly()
        {
            var genome = new Genome(age: 2);
            genome.SetGene("categorical", new Allele<double>(123.6));
            genome.SetGene("continuous", new Allele<double>(0.2));
            genome.SetGene("discrete", new Allele<int>(-3));
            genome.SetGene("single_categorical", new Allele<string>("foo"));
            var searchPoint = ContinuizedGenomeSearchPoint.CreateFromGenome(genome, this._parameterTree);

            Assert.Equal(
                genome.ToString(),
                searchPoint.Genome.ToString());
            // Values be transformed into [0, 10].
            Assert.Equal(
                BoundedSearchPoint.StandardizeValues(new[] { 0, 0.2, -3, 0 }, this._lowerBounds, this._upperBounds),
                searchPoint.Values);
            Assert.False(
                searchPoint.IsRepaired,
                "Search points directly created from genomes do not have to be repaired.");
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint"/>'s constructor and
        /// <see cref="ContinuizedGenomeSearchPoint.CreateFromGenome"/> are consistent in (re)transforming
        /// <see cref="Genome"/>s to values.
        /// </summary>
        [Fact]
        public void ConstructionIsConsistent()
        {
            // We do not care about age --> set it to default age.
            var genome = this._genomeBuilder.CreateRandomGenome(age: 0);
            var searchPoint = ContinuizedGenomeSearchPoint.CreateFromGenome(genome, this._parameterTree);
            var samePoint = new ContinuizedGenomeSearchPoint(
                values: searchPoint.Values,
                parameterTree: this._parameterTree,
                genomeBuilder: this._genomeBuilder,
                lowerBounds: this._lowerBounds,
                upperBounds: this._upperBounds);

            samePoint.Genome.ToCappedDecimalString().ShouldBe(genome.ToCappedDecimalString());

            var values = BoundedSearchPoint.StandardizeValues(new[] { 0, 0.2, -3, 0 }, this._lowerBounds, this._upperBounds);
            searchPoint = new ContinuizedGenomeSearchPoint(
                values: values,
                parameterTree: this._parameterTree,
                genomeBuilder: this._genomeBuilder,
                lowerBounds: this._lowerBounds,
                upperBounds: this._upperBounds);
            samePoint = ContinuizedGenomeSearchPoint.CreateFromGenome(
                searchPoint.Genome.CreateMutableGenome(),
                this._parameterTree);
            for (int i = 0; i < values.Count; i++)
            {
                Assert.Equal(
                    values[i],
                    samePoint.Values[i],
                    4);
            }
        }

        /// <summary>
        /// Checks that the <see cref="Genome"/> produced by <see cref="ContinuizedGenomeSearchPoint.Genome"/> is
        /// independent from the <see cref="Genome"/> the <see cref="ContinuizedGenomeSearchPoint"/> was initialized
        /// with.
        /// </summary>
        [Fact]
        public void GenomePropertyProducesIndependentGenome()
        {
            var genome = this._genomeBuilder.CreateRandomGenome(age: 0);
            var point = ContinuizedGenomeSearchPoint.CreateFromGenome(genome, this._parameterTree);
            genome.SetGene("discrete", new Allele<double>(-6));

            var createdGenome = point.Genome.CreateMutableGenome();
            Assert.NotEqual(
                -6,
                createdGenome.GetGeneValue("discrete").GetValue());
        }

        /// <summary>
        /// Checks that <see cref="ContinuizedGenomeSearchPoint.ObtainParameterBounds"/> identifies the bounds
        /// correctly.
        /// </summary>
        [Fact]
        public void ObtainParameterBoundsFindsCorrectBounds()
        {
            ContinuizedGenomeSearchPoint.ObtainParameterBounds(
                this._parameterTree,
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
            root.AddChild(new ValueNode<int>("discrete", new IntegerDomain(-5, 5)));
            root.AddChild(new ValueNode<double>("continuous", new ContinuousDomain(0, 1.4)));
            root.AddChild(new ValueNode<double>("categorical", new CategoricalDomain<double>(new List<double> { 123.6, -12.5 })));
            root.AddChild(new ValueNode<string>("single_categorical", new CategoricalDomain<string>(new List<string> { "foo" })));
            return new ParameterTree(root);
        }

        #endregion
    }
}