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
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// A message containing all results a <see cref="ResultStorageActor{I,R}"/> has collected for a certain parameter
    /// configuration up to the message send point.
    /// </summary>
    /// <typeparam name="TInstance">Instance type the results have been evaluated on.
    /// Must extend <see cref="InstanceBase"/>.</typeparam>
    /// <typeparam name="TResult">Type of the results.</typeparam>
    public class GenomeResults<TInstance, TResult>
        where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeResults{I,R}"/> class.
        /// </summary>
        /// <param name="runResults">The run results.</param>
        /// <exception cref="ArgumentNullException">Thrown if result parameter is null.</exception>
        public GenomeResults(IDictionary<TInstance, TResult> runResults)
        {
            if (runResults == null)
            {
                throw new ArgumentNullException(nameof(runResults));
            }

            // Translate the dictionary into an immutable one to keep the message immutable.
            this.RunResults = runResults.ToImmutableDictionary();
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the run results.
        /// </summary>
        public ImmutableDictionary<TInstance, TResult> RunResults { get; }

        #endregion
    }
}