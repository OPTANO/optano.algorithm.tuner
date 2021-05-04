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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Optano.Algorithm.Tuner.TargetAlgorithm;

    /// <summary>
    /// An implementation of <see cref="ITargetAlgorithm{TInstance,TResult}"/>
    /// returning a new instance of the <see cref="TestResult"/> class after a specified time.
    /// </summary>
    public class NoOperation : ITargetAlgorithm<TestInstance, TestResult>
    {
        #region Fields

        /// <summary>
        /// Time after which to return a <see cref="TestResult"/> when 
        /// <see cref="Run(TestInstance, CancellationToken)"/> is called.
        /// </summary>
        private readonly TimeSpan _runTime;

        /// <summary>
        /// The cancellation token used for the latest evaluation run.
        /// </summary>
        private CancellationToken _evaluationCancellationToken;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NoOperation"/> class.
        /// </summary>
        /// <param name="runTime">Timeout after which to return a <see cref="TestResult"/>
        /// when <see cref="Run(TestInstance, CancellationToken)"/> is called.</param>
        public NoOperation(TimeSpan runTime)
        {
            this._runTime = runTime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoOperation"/> class.
        /// Uses <see cref="TimeSpan.Zero"/> as <see cref="_runTime"/>.
        /// </summary>
        public NoOperation()
            : this(TimeSpan.Zero)
        {
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets a value indicating whether cancellation has been requested so far.
        /// </summary>
        public bool IsCancellationRequested => this._evaluationCancellationToken.IsCancellationRequested;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates a cancellable task that runs the algorithm on the given instance.
        /// <para>As <see cref="NoOperation"/> does not specify any algorithm,
        /// the task will simply initialize a new instance of the <see cref="TestResult"/> class after waiting for the
        /// time span specified on construction.</para>
        /// </summary>
        /// <param name="instance">Instance to run on.</param>
        /// <param name="cancellationToken">Token that should be regurlary checked for cancellation.
        /// If cancellation is detected, the task has to be stopped.</param>
        /// <returns>A task that will initialize a new instance of the <see cref="TestResult"/> class if
        /// cancellation does not get requested.</returns>
        public Task<TestResult> Run(TestInstance instance, CancellationToken cancellationToken)
        {
            // Remember cancellation token.
            this._evaluationCancellationToken = cancellationToken;

            // Don't continue the task if it was cancelled or faulted.
            return Task.Delay(this._runTime, cancellationToken)
                .ContinueWith(
                    continuationFunction: delayTask => new TestResult(this._runTime),
                    continuationOptions: TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        #endregion
    }
}