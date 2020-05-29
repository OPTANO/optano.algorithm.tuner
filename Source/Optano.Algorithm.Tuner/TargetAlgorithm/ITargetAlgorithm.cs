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

namespace Optano.Algorithm.Tuner.TargetAlgorithm
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// An already configured version of the target algorithm.
    /// </summary>
    /// <remarks>
    /// If you use some resources that should be disposed of as soon as the algorithm has completed working,
    /// implement <see cref="IDisposable" /> and OPTANO Algorithm Tuner will dispose it for you.
    /// </remarks>
    /// <typeparam name="TInstance">Type of instances.</typeparam>
    /// <typeparam name="TResult">Type of results.</typeparam>
    public interface ITargetAlgorithm<TInstance, TResult>
        where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Public Methods and Operators

        /// <summary>
        /// Creates a cancellable task that runs the algorithm on the given instance.
        /// </summary>
        /// <param name="instance">Instance to run on.</param>
        /// <param name="cancellationToken">
        /// Token that should regurlarly be checked for cancellation.
        /// If cancellation is detected, the task has to be stopped.
        /// </param>
        /// <returns>A task that has returns everything important about the run on completion.</returns>
        Task<TResult> Run(TInstance instance, CancellationToken cancellationToken);

        #endregion
    }
}