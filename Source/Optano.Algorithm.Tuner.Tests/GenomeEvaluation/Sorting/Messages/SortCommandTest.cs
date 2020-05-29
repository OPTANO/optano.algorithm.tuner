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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.Sorting.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="SortCommand{TInstance}"/> class.
    /// </summary>
    public class SortCommandTest
    {
        #region Fields

        /// <summary>
        /// The <see cref="ImmutableGenome"/>s to sort.
        /// </summary>
        private readonly ImmutableList<ImmutableGenome> _items;

        /// <summary>
        /// The <see cref="TestInstance"/>s to base the sorting on.
        /// </summary>
        private readonly ImmutableList<TestInstance> _instances;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortCommandTest"/> class.
        /// </summary>
        public SortCommandTest()
        {
            this._instances =
                new List<TestInstance> { new TestInstance("test") }
                    .ToImmutableList();
            this._items =
                new List<ImmutableGenome> { new ImmutableGenome(new Genome(1)), new ImmutableGenome(new Genome(2)) }
                    .ToImmutableList();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="SortCommand{TInstance}"/>'s constructor throws a <see cref="ArgumentNullException"/>
        /// if called without any items.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingItems()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SortCommand<TestInstance>(items: null, instances: this._instances));
        }

        /// <summary>
        /// Checks that <see cref="SortCommand{TInstance}"/>'s constructor throws a <see cref="ArgumentNullException"/>
        /// if called without any instances.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingInstances()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SortCommand<TestInstance>(this._items, instances: null));
        }

        /// <summary>
        /// Checks that <see cref="SortCommand{TInstance}.Items"/> returns the items provided on initialization.
        /// </summary>
        [Fact]
        public void ItemsAreSetCorrectly()
        {
            var result = new SortCommand<TestInstance>(this._items, this._instances);
            TestUtils.SetsAreEquivalent(this._items, result.Items);
        }

        /// <summary>
        /// Checks that <see cref="SortCommand{TInstance}.Instances"/> returns the instances provided on
        /// initialization.
        /// </summary>
        [Fact]
        public void InstancesAreSetCorrectly()
        {
            var result = new SortCommand<TestInstance>(this._items, this._instances);
            TestUtils.SetsAreEquivalent(this._instances, result.Instances);
        }

        #endregion
    }
}