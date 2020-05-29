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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// Utility methods to update evaluation instances.
    /// </summary>
    public static class UpdateInstances
    {
        #region Constants

        /// <summary>
        /// The maximum number of instances to wrap into a <see cref="AddInstances{TInstance}"/> message.
        /// </summary>
        public const int MaximumInstanceChunkSize = 50;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates all required messages to update evaluation instances.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance.</typeparam>
        /// <param name="instances">The instances to use for evaluation from now on.</param>
        /// <returns>
        /// A <see cref="ClearInstances"/> message followed by <see cref="AddInstances{TInstance}"/> messages
        /// partitioning <paramref name="instances"/>.
        /// </returns>
        public static IEnumerable<object> CreateInstanceUpdateMessages<TInstance>(ImmutableList<TInstance> instances)
            where TInstance : InstanceBase
        {
            yield return new ClearInstances();
            foreach (var addInstancesMessage in CreateAddInstancesMessages(instances))
            {
                yield return addInstancesMessage;
            }

            yield return new InstanceUpdateFinished(instances.Count);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates <see cref="AddInstances{TInstance}"/>s messages that partition <paramref name="instances"/> into
        /// chunks of at most <see cref="MaximumInstanceChunkSize"/> instances each.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance.</typeparam>
        /// <param name="instances">The instances to partition.</param>
        /// <returns>
        /// <see cref="AddInstances{TInstance}"/> messages partitioning <paramref name="instances"/>.
        /// </returns>
        private static IEnumerable<object> CreateAddInstancesMessages<TInstance>(ImmutableList<TInstance> instances)
            where TInstance : InstanceBase
        {
            int nextChunkStartIndex = 0;
            while (nextChunkStartIndex < instances.Count)
            {
                var remainingInstanceNumber = instances.Count - nextChunkStartIndex;
                var chunkSize = Math.Min(MaximumInstanceChunkSize, remainingInstanceNumber);

                yield return new AddInstances<TInstance>(instances.GetRange(nextChunkStartIndex, chunkSize));

                nextChunkStartIndex += chunkSize;
            }
        }

        #endregion
    }
}