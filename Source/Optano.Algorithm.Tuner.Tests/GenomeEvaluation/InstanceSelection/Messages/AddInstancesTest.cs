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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.InstanceSelection.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="AddInstances{TInstance}"/> class.
    /// </summary>
    public class AddInstancesTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="AddInstances{TInstance}"/>'s constructor without providing 
        /// instances throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnMissingInstances()
        {
            Assert.Throws<ArgumentNullException>(() => new AddInstances<TestInstance>(instances: null));
        }

        /// <summary>
        /// Checks that <see cref="AddInstances{TInstance}.Instances"/> returns instances equal to the ones
        /// provided on initialization.
        /// </summary>
        [Fact]
        public void InstancesAreSetCorrectly()
        {
            // Set instances in constructor.
            List<TestInstance> testInstances = this.CreateSingleTestInstanceList();
            var message = new AddInstances<TestInstance>(testInstances);

            // Test they are set correctly.
            Assert.True(
                testInstances.SequenceEqual(message.Instances),
                "Instances were not set correctly.");
        }

        /// <summary>
        /// Checks that modifying the instances list <see cref="AddInstances{TInstance}"/> was initialized with does not 
        /// modify the message.
        /// </summary>
        [Fact]
        public void InstancesAreImmutable()
        {
            // Set instances in constructor.
            List<TestInstance> testInstances = this.CreateSingleTestInstanceList();
            var message = new AddInstances<TestInstance>(testInstances);

            // Precondition: Instances are the same.
            Assert.True(testInstances.SequenceEqual(message.Instances));

            // Modify the external list.
            testInstances.Add(new TestInstance("other"));

            // Check that didn't modify the interal list.
            Assert.False(
                testInstances.SequenceEqual(message.Instances),
                "Instances have been modified even though they are supposed to be immutable.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a list containing a single <see cref="TestInstance"/>.
        /// </summary>
        /// <returns>The list.</returns>
        private List<TestInstance> CreateSingleTestInstanceList()
        {
            return new List<TestInstance> { new TestInstance("test") };
        }

        #endregion
    }
}