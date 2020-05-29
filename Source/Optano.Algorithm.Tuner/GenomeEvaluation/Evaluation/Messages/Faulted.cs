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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation.Messages
{
    using System;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// Message indicating that a target algorithm run using a certain genome - instance combination resulted in an
    /// exception.
    /// </summary>
    /// <typeparam name="TInstance">The type of instance that was used. Must extend <see cref="InstanceBase"/>.</typeparam>
    public class Faulted<TInstance>
        where TInstance : InstanceBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Faulted{I}"/> class.
        /// </summary>
        /// <param name="genome">The genome used for the problematic evaluation.</param>
        /// <param name="instance">The instance used for the problematic evaluation.</param>
        /// <param name="exception">The resulting <see cref="AggregateException"/>.</param>
        public Faulted(ImmutableGenome genome, TInstance instance, AggregateException exception)
        {
            // Validate parameters.
            if (genome == null)
            {
                throw new ArgumentNullException("genome");
            }

            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            // Set them.
            this.Genome = genome;
            this.Instance = instance;
            this.Exception = exception;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome the target algorithm was configured with.
        /// </summary>
        public ImmutableGenome Genome { get; }

        /// <summary>
        /// Gets the instance the target algorithm was run on.
        /// </summary>
        public TInstance Instance { get; }

        /// <summary>
        /// Gets the resulting exception.
        /// </summary>
        public AggregateException Exception { get; }

        #endregion
    }
}