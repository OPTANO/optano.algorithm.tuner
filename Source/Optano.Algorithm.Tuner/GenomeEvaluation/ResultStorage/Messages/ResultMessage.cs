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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages
{
    using System;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    ///     Message containing a target algorithm run result.
    /// </summary>
    /// <typeparam name="TInstance">Type of the instances the target algorithm can be run on.</typeparam>
    /// <typeparam name="TResult">Type of the target algorithm run result.</typeparam>
    public class ResultMessage<TInstance, TResult>
        where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ResultMessage{TInstance, TResult}" /> class.
        /// </summary>
        /// <param name="genome">The genome the target algorithm was configured with.</param>
        /// <param name="instance">The instance the target algorithm was run on.</param>
        /// <param name="runResult">The run result.</param>
        public ResultMessage(ImmutableGenome genome, TInstance instance, TResult runResult)
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

            if (runResult == null)
            {
                throw new ArgumentNullException("runResult");
            }

            // Set them.
            this.Genome = genome;
            this.Instance = instance;
            this.RunResult = runResult;
        }

        #endregion

        #region Public properties

        /// <summary>
        ///     Gets the genome the target algorithm was configured with.
        /// </summary>
        public ImmutableGenome Genome { get; }

        /// <summary>
        ///     Gets the instance the target algorithm was run on.
        /// </summary>
        public TInstance Instance { get; }

        /// <summary>
        ///     Gets the run result.
        /// </summary>
        public TResult RunResult { get; }

        #endregion
    }
}