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
    /// An implementation of <see cref="ITargetAlgorithm{TInstance,TResult}"/> simulating a risky operation
    /// that might fail.
    /// </summary>
    internal class RiskyOperation : ITargetAlgorithm<TestInstance, TestResult>
    {
        #region Fields

        /// <summary>
        /// The number of failing runs this operation needs to produce.
        /// </summary>
        private readonly int _numberOfFails;

        /// <summary>
        /// The number of calls to <see cref="RiskyOperation.Run(TestInstance, CancellationToken)"/> for this instance
        /// so far.
        /// </summary>
        private int _numberOfRuns = 0;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RiskyOperation"/> class.
        /// </summary>
        /// <param name="numberOfFails">The number of times
        /// <see cref="RiskyOperation.Run(TestInstance, CancellationToken)"/> will fail with an exception before only 
        /// producing successful results.
        /// </param>
        public RiskyOperation(int numberOfFails)
        {
            this._numberOfFails = numberOfFails;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates a cancellable task that runs the algorithm on the given instance.
        /// <para>As <see cref="RiskyOperation"/> does not specify any algorithm,
        /// the task will simply either initialize a new instance of the <see cref="TestResult"/> class or throw an
        /// exception if depending on the values of <see cref="_numberOfFails"/> and <see cref="_numberOfRuns"/>.</para>
        /// </summary>
        /// <param name="instance">Instance to run on.</param>
        /// <param name="cancellationToken">Token that should be regurlary checked for cancellation.
        /// If cancellation is detected, the task has to be stopped.</param>
        /// <returns>A task that will initialize a new instance of the <see cref="TestResult"/> or throw an 
        /// <see cref="InvalidOperationException"/> if less than <see cref="_numberOfFails"/> failures have been 
        /// produced so far.</returns>
        public Task<TestResult> Run(TestInstance instance, CancellationToken cancellationToken)
        {
            return Task.Run(
                () =>
                    {
                        this._numberOfRuns++;
                        if (this._numberOfRuns <= this._numberOfFails)
                        {
                            throw new InvalidOperationException("RiskyOperation failed.");
                        }

                        return new TestResult();
                    });
        }

        #endregion
    }
}