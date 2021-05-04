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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.InstanceSelection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="InstanceSelector{TInstance}"/>.
    /// </summary>
    public class InstanceSelectorTest : IDisposable
    {
        #region Fields

        /// <summary>
        /// The number of instances to create for <see cref="_instanceList"/>.
        /// </summary>
        private readonly int _numberInstances = 50;

        /// <summary>
        /// An instance list used in multiple tests.
        /// </summary>
        private readonly List<TestInstance> _instanceList;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceSelectorTest"/> class.
        /// </summary>
        public InstanceSelectorTest()
        {
            this._instanceList = new List<TestInstance>(this._numberInstances);
            for (int i = 0; i < this._numberInstances; i++)
            {
                this._instanceList.Add(new TestInstance(i.ToString()));
            }

            Randomizer.Reset();
            Randomizer.Configure();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Resets the <see cref="Randomizer"/>.
        /// </summary>
        public void Dispose()
        {
            Randomizer.Reset();
        }

        /// <summary>
        /// Checks that all instances provided on construction are stored within the <see cref="InstanceSelector{I}"/>.
        /// </summary>
        [Fact]
        public void InstancesAreAllStored()
        {
            // Construct InstanceSelector s.t. it should alway returns all instances.
            var selector = this.BuildSelector(
                numInstancesAtStart: this._numberInstances,
                numInstancesAtEnd: this._numberInstances,
                goalGeneration: 49,
                generations: 50);

            // Check all instances are returned.
            var instances = selector.Select(0);
            Assert.True(
                TestUtils.SetsAreEquivalent(this._instanceList, instances),
                $"{TestUtils.PrintList(instances)} should have all instances defined in {TestUtils.PrintList(this._instanceList)}.");
        }

        /// <summary>
        /// Checks that the number of instances returned in the first generation equals the corresponding value used
        /// for construction.
        /// </summary>
        [Fact]
        public void MinimumNumberReturnedForFirstGeneration()
        {
            int minimumNumber = 3;
            var selector = this.BuildSelector(
                minimumNumber,
                numInstancesAtEnd: 47,
                goalGeneration: 30,
                generations: 50);

            int numberReturnedInstances = selector.Select(0).Count();
            Assert.Equal(
                minimumNumber,
                numberReturnedInstances);
        }

        /// <summary>
        /// Checks that the number of instances returned for the generation that was set as goal generation equals the
        /// maximum number of instances set at construction.
        /// </summary>
        [Fact]
        public void MaximumNumberReturnedForGoalGeneration()
        {
            int maximumNumber = 47;
            int goalGeneration = 8;
            var selector = this.BuildSelector(
                numInstancesAtStart: 3,
                numInstancesAtEnd: maximumNumber,
                goalGeneration: goalGeneration,
                generations: 19);

            int numberReturnedInstances = selector.Select(goalGeneration).Count();
            Assert.Equal(
                maximumNumber,
                numberReturnedInstances);
        }

        /// <summary>
        /// Checks that the number of instances returned for the last generation equals the corresponding value used
        /// for construction.
        /// </summary>
        [Fact]
        public void MaximumNumberReturnedForLastGeneration()
        {
            int maximumNumber = 47;
            int generations = 19;
            var selector = this.BuildSelector(
                numInstancesAtStart: 3,
                numInstancesAtEnd: maximumNumber,
                goalGeneration: 15,
                generations: generations);

            int numberReturnedInstances = selector.Select(generations - 1).Count();
            Assert.Equal(
                maximumNumber,
                numberReturnedInstances);
        }

        /// <summary>
        /// For an example, checks that the number of instances depends linearly on the generation in the first few
        /// generations.
        /// </summary>
        [Fact]
        public void InstancesAreIncreasedLinearlyUntilGoalGeneration()
        {
            // Initialize selector s.t. instance per generation should be 5, 10, 15, ..., 50.
            int minimumNumber = 5;
            int maximumNumber = 50;
            int goalGeneration = 9;
            int generations = 50;
            var selector = this.BuildSelector(minimumNumber, maximumNumber, goalGeneration, generations);

            // Check that.
            for (int i = 0; i <= goalGeneration; i++)
            {
                int numberReturnedInstances = selector.Select(i).Count();
                Assert.True(
                    minimumNumber + (5 * i) == numberReturnedInstances,
                    $"Returned wrong number of instances for generation {i}.");
            }
        }

        /// <summary>
        /// Checks that altering the list used for construction will not change the instances in the
        /// <see cref="InstanceSelector{I}"/> afterwards.
        /// </summary>
        [Fact]
        public void InstancesCannotBeChanged()
        {
            var selector = this.BuildSelector(
                numInstancesAtStart: this._numberInstances,
                numInstancesAtEnd: this._numberInstances,
                goalGeneration: 30,
                generations: 50);

            // Check instances equal the ones used for construction.
            Assert.True(TestUtils.SetsAreEquivalent(this._instanceList, selector.Select(generation: 0)));
            // Change list that was used for construction.
            this._instanceList.RemoveAt(0);

            // Check instances were not changed alongside.
            var instances = selector.Select(generation: 0);
            Assert.False(
                TestUtils.SetsAreEquivalent(this._instanceList, instances),
                $"{TestUtils.PrintList(this._instanceList)} and {TestUtils.PrintList(instances)} should be different after the first list was changed.");
        }

        /// <summary>
        /// Checks that <see cref="InstanceSelector{I}.Select(int)"/> throws an error
        /// if one tries to request instances for a negative generation number.
        /// </summary>
        [Fact]
        public void SelectThrowsErrorForNegativeGenerationNumber()
        {
            var selector = this.BuildSelector(
                numInstancesAtStart: 1,
                numInstancesAtEnd: 10,
                goalGeneration: 2,
                generations: 3);

            // Provoke error.
            Assert.Throws<ArgumentException>(() => selector.Select(generation: -1).ToList());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Builds an <see cref="InstanceSelector{I}"/>.
        /// </summary>
        /// <param name="numInstancesAtStart">Number instances returned for first generation.</param>
        /// <param name="numInstancesAtEnd">Number instances returned all generations between goal generation and last
        /// generation.</param>
        /// <param name="goalGeneration">First generation for which the maximum number of instances should be returned.
        /// </param>
        /// <param name="generations">The number of generations.</param>
        /// <returns>The built <see cref="InstanceSelector{I}"/>.</returns>
        private InstanceSelector<TestInstance> BuildSelector(
            int numInstancesAtStart,
            int numInstancesAtEnd,
            int goalGeneration,
            int generations)
        {
            AlgorithmTunerConfiguration configuration = new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetInstanceNumbers(numInstancesAtStart, numInstancesAtEnd)
                .SetGenerations(generations)
                .SetGoalGeneration(goalGeneration)
                .Build(maximumNumberParallelEvaluations: 4);
            return new InstanceSelector<TestInstance>(this._instanceList, configuration);
        }

        #endregion
    }
}