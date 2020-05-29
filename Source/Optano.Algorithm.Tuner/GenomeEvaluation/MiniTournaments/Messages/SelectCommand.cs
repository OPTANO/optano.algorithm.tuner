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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// Message indicating that the given instances should be used to evaluate the given genomes and discover the best
    /// ones.
    /// </summary>
    /// <typeparam name="TInstance">The type of instance that can be run by the target algorithm.</typeparam>
    public class SelectCommand<TInstance>
        where TInstance : InstanceBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectCommand{TInstance}"/> class.
        /// </summary>
        /// <param name="participants">
        /// Genomes to evaluate.
        /// </param>
        /// <param name="instances">
        /// Instances for evaluation.
        /// </param>
        /// <param name="currentGeneration">
        /// The current Generation.
        /// </param>
        public SelectCommand(IEnumerable<ImmutableGenome> participants, IEnumerable<TInstance> instances, int currentGeneration)
        {
            // Verify parameters.
            if (participants == null)
            {
                throw new ArgumentNullException("participants");
            }

            if (instances == null)
            {
                throw new ArgumentNullException("instances");
            }

            // Copy the parameters over into fields to make the message immutable.
            this.Participants = participants.ToImmutableList();
            this.Instances = instances.ToImmutableList();
            this.CurrentGeneration = currentGeneration;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genomes to evaluate.
        /// </summary>
        public ImmutableList<ImmutableGenome> Participants { get; }

        /// <summary>
        /// Gets the instances to evaluate the genomes with.
        /// </summary>
        public ImmutableList<TInstance> Instances { get; }

        /// <summary>
        /// Gets the current generation.
        /// </summary>
        public int CurrentGeneration { get; }

        #endregion
    }
}