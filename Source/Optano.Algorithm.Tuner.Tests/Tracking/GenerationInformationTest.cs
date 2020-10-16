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

namespace Optano.Algorithm.Tuner.Tests.Tracking
{
    using System;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tracking;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GenerationInformation"/> class.
    /// </summary>
    public class GenerationInformationTest
    {
        #region Constants

        /// <summary>
        /// A valid generation number.
        /// </summary>
        private const int Generation = 12;

        /// <summary>
        /// A valid number of evaluations.
        /// </summary>
        private const int TotalNumberOfEvaluations = 3468;

        #endregion

        #region Fields

        /// <summary>
        /// A strategy type.
        /// </summary>
        private readonly Type _strategy = typeof(DifferentialEvolutionStrategy<TestInstance, TestResult>);

        /// <summary>
        /// An incumbent genome. Initialized in constructor to be 'interesting'.
        /// </summary>
        private readonly ImmutableGenome _incumbent;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerationInformationTest"/> class.
        /// </summary>
        public GenerationInformationTest()
        {
            // Create an interesting genome as incumbent.
            var genome = new Genome(age: 2) { IsEngineered = true };
            genome.SetGene("a", new Allele<int>(-23));
            this._incumbent = new ImmutableGenome(genome);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="GenerationInformation"/>'s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a negative generation number.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForNegativeGenerationNumber()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GenerationInformation(
                    generation: -1,
                    totalElapsedTime: TimeSpan.Zero,
                    totalNumberOfEvaluations: GenerationInformationTest.TotalNumberOfEvaluations,
                    strategy: this._strategy,
                    incumbent: this._incumbent));
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformation"/>'s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a negative total elapsed time.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForNegativeTotalElapsedTime()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GenerationInformation(
                    generation: -1,
                    totalElapsedTime: TimeSpan.Zero - TimeSpan.MaxValue,
                    totalNumberOfEvaluations: GenerationInformationTest.TotalNumberOfEvaluations,
                    strategy: this._strategy,
                    incumbent: this._incumbent));
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformation"/>'s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a negative number of evaluations.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForNegativeNumberEvaluations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GenerationInformation(
                    GenerationInformationTest.Generation,
                    totalElapsedTime: TimeSpan.Zero,
                    totalNumberOfEvaluations: -1,
                    strategy: this._strategy,
                    incumbent: this._incumbent));
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformation"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without a strategy type.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingStrategy()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenerationInformation(
                    GenerationInformationTest.Generation,
                    totalElapsedTime: TimeSpan.Zero,
                    GenerationInformationTest.TotalNumberOfEvaluations,
                    strategy: null,
                    incumbent: this._incumbent));
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformation"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without an incumbent genome.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingIncumbent()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenerationInformation(
                    GenerationInformationTest.Generation,
                    totalElapsedTime: TimeSpan.Zero,
                    GenerationInformationTest.TotalNumberOfEvaluations,
                    this._strategy,
                    incumbent: null));
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformation.ToString"/> contains all relevant information.
        /// </summary>
        [Fact]
        public void ToStringContainsAllInformation()
        {
            var information = new GenerationInformation(
                GenerationInformationTest.Generation,
                totalElapsedTime: TimeSpan.FromSeconds(30),
                GenerationInformationTest.TotalNumberOfEvaluations,
                this._strategy,
                this._incumbent);
            information.IncumbentTrainingScore = -3.4;
            information.IncumbentTestScore = 1234.8;
            Assert.Equal(
                "12;0:00:00:30.0000000;3468;-3.4;1234.8;DifferentialEvolutionStrategy`2;[a: -23](Age: 2)[Engineered: yes]",
                information.ToString());
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformation.ToString"/> works even if no scores are present.
        /// </summary>
        [Fact]
        public void ToStringWorksForMissingInformation()
        {
            var information = new GenerationInformation(
                GenerationInformationTest.Generation,
                totalElapsedTime: TimeSpan.FromSeconds(30),
                GenerationInformationTest.TotalNumberOfEvaluations,
                this._strategy,
                this._incumbent);
            Assert.Equal(
                "12;0:00:00:30.0000000;3468;;;DifferentialEvolutionStrategy`2;[a: -23](Age: 2)[Engineered: yes]",
                information.ToString());
        }

        #endregion
    }
}
