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
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="UpdateInstances"/> class.
    /// </summary>
    public class UpdateInstancesTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="UpdateInstances.CreateInstanceUpdateMessages{TInstance}"/> creates two messages if
        /// called with few instances: One to clear old instances and one to add the new ones.
        /// </summary>
        [Fact]
        public void CreateInstanceUpdateMessagesWorksForFewInstances()
        {
            var messages = UpdateInstances.CreateInstanceUpdateMessages(UpdateInstancesTest.CreateInstances(number: 2)).ToList();

            Assert.True(
                3 == messages.Count,
                "There should be two messages: One to clear old instances, one to add new ones, and one to complete the instance update.");
            Assert.True(messages[0] is ClearInstances, "The first message should clear old instances.");
            Assert.True(messages[1] is AddInstances<TestInstance>, "The second message should add new instances.");
            Assert.True(messages[2] is InstanceUpdateFinished, "The third message should complete the update process.");

            UpdateInstancesTest.CheckAddMessageContainsCorrectInstances(
                (AddInstances<TestInstance>)messages[1],
                firstInstanceIdentifier: 0,
                numberInstances: 2);
        }

        /// <summary>
        /// Checks that <see cref="UpdateInstances.CreateInstanceUpdateMessages{TInstance}"/> creates several messages
        /// if called with many instances: One to clear old instances and multiple ones to add all new instances.
        /// </summary>
        [Fact]
        public void CreateInstanceUpdateMessagesWorksForManyInstances()
        {
            int numberInstances = (4 * UpdateInstances.MaximumInstanceChunkSize) + 7;
            var messages = UpdateInstances.CreateInstanceUpdateMessages(UpdateInstancesTest.CreateInstances(numberInstances)).ToList();

            Assert.True(
                7 == messages.Count,
                "There should be seven messages: One to clear old instances, five to add new ones, and one to complete the instance update.");
            Assert.True(messages[0] is ClearInstances, "The first message should clear old instances.");

            for (int i = 1; i <= 4; i++)
            {
                Assert.True(messages[i] is AddInstances<TestInstance>, $"The {i}th message should add new instances.");
                UpdateInstancesTest.CheckAddMessageContainsCorrectInstances(
                    (AddInstances<TestInstance>)messages[i],
                    firstInstanceIdentifier: (i - 1) * UpdateInstances.MaximumInstanceChunkSize,
                    numberInstances: UpdateInstances.MaximumInstanceChunkSize);
            }

            Assert.True(
                messages[5] is AddInstances<TestInstance>,
                "The fifth message should add the remaining instances.");
            UpdateInstancesTest.CheckAddMessageContainsCorrectInstances(
                (AddInstances<TestInstance>)messages[5],
                firstInstanceIdentifier: 4 * UpdateInstances.MaximumInstanceChunkSize,
                numberInstances: 7);

            Assert.True(messages[6] is InstanceUpdateFinished, "The seventh message should complete the update process.");
        }

        /// <summary>
        /// Checks that <see cref="UpdateInstances.CreateInstanceUpdateMessages{TInstance}"/> creates several messages
        /// if called with instances divisible by chunk size: One to clear old instances and multiple maximum size ones
        /// to add all new instances.
        /// </summary>
        [Fact]
        public void CreateInstanceUpdateMessagesWorksForEdgeCases()
        {
            int numberInstances = 2 * UpdateInstances.MaximumInstanceChunkSize;
            var messages = UpdateInstances.CreateInstanceUpdateMessages(UpdateInstancesTest.CreateInstances(numberInstances)).ToList();

            Assert.True(
                4 == messages.Count,
                "There should be four messages: One to clear old instances, two to add new ones, and one to complete the instance update.");
            Assert.True(messages[0] is ClearInstances, "The first message should clear old instances.");

            for (int i = 1; i <= 2; i++)
            {
                Assert.True(messages[i] is AddInstances<TestInstance>, $"The {i}th message should add new instances.");
                UpdateInstancesTest.CheckAddMessageContainsCorrectInstances(
                    (AddInstances<TestInstance>)messages[i],
                    firstInstanceIdentifier: (i - 1) * UpdateInstances.MaximumInstanceChunkSize,
                    numberInstances: UpdateInstances.MaximumInstanceChunkSize);
            }

            Assert.True(messages[3] is InstanceUpdateFinished, "The fourth message should complete the update process.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a number of unique <see cref="TestInstance"/>s.
        /// </summary>
        /// <param name="number">The number of <see cref="TestInstance"/>s to create.</param>
        /// <returns>The created <see cref="TestInstance"/>s.</returns>
        private static ImmutableList<TestInstance> CreateInstances(int number)
        {
            var instances = new List<TestInstance>(number);
            for (int i = 0; i < number; i++)
            {
                instances.Add(new TestInstance(i.ToString()));
            }

            return instances.ToImmutableList();
        }

        /// <summary>
        /// Checks that a <see cref="AddInstances{TInstance}"/> message contains the expected
        /// <see cref="TestInstance"/>s.
        /// </summary>
        /// <param name="message">The <see cref="AddInstances{TInstance}"/> message.</param>
        /// <param name="firstInstanceIdentifier">
        /// The first expected instance identifier. Remaining identifiers are successive integers.
        /// </param>
        /// <param name="numberInstances">The expected number of <see cref="TestInstance"/>s.</param>
        private static void CheckAddMessageContainsCorrectInstances(
            AddInstances<TestInstance> message,
            int firstInstanceIdentifier,
            int numberInstances)
        {
            Assert.Equal(
                Enumerable.Range(firstInstanceIdentifier, numberInstances).Select(id => id.ToString()).ToArray(),
                message.Instances.Select(instance => instance.ToString()).ToArray());
        }

        #endregion
    }
}
