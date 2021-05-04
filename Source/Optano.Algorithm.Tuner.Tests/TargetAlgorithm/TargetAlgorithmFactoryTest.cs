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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/> class.
    /// </summary>
    public class TargetAlgorithmFactoryTest : IDisposable
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetAlgorithmFactoryTest"/> class.
        /// Behaves like the [TestInitialize] of MSTest framework.
        /// </summary>
        public TargetAlgorithmFactoryTest()
        {
            ProcessUtils.SetDefaultCultureInfo(CultureInfo.InvariantCulture);
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <summary>
        /// Checks, that <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}.TryToGetResultFromStringArray"/> works for <see cref="RuntimeResult"/>s.
        /// </summary>
        [Fact]
        public void TryToGetResultFromStringArrayWorksForRuntimeResults()
        {
            var runtimeResult = new RuntimeResult(TimeSpan.FromSeconds(30));
            var targetAlgorithmFactory =
                new DummyTargetAlgorithmFactory<DummyTargetAlgorithm<TestInstance, RuntimeResult>, TestInstance, RuntimeResult>() as
                    ITargetAlgorithmFactory<DummyTargetAlgorithm<TestInstance, RuntimeResult>, TestInstance, RuntimeResult>;
            targetAlgorithmFactory.TryToGetResultFromStringArray(runtimeResult.ToStringArray(), out var result).ShouldBeTrue();
            result.TargetAlgorithmStatus.ShouldBe(runtimeResult.TargetAlgorithmStatus);
            result.IsCancelled.ShouldBe(runtimeResult.IsCancelled);
            result.Runtime.ShouldBe(runtimeResult.Runtime);
        }

        /// <summary>
        /// Checks, that <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}.TryToGetResultFromStringArray"/> works for <see cref="ContinuousResult"/>s.
        /// </summary>
        [Fact]
        public void TryToGetResultFromStringArrayWorksForContinuousResults()
        {
            var continuousResult = new ContinuousResult(0.5, TimeSpan.FromSeconds(30));
            var targetAlgorithmFactory =
                new DummyTargetAlgorithmFactory<DummyTargetAlgorithm<TestInstance, ContinuousResult>, TestInstance, ContinuousResult>() as
                    ITargetAlgorithmFactory<DummyTargetAlgorithm<TestInstance, ContinuousResult>, TestInstance, ContinuousResult>;
            targetAlgorithmFactory.TryToGetResultFromStringArray(continuousResult.ToStringArray(), out var result).ShouldBeTrue();
            result.TargetAlgorithmStatus.ShouldBe(continuousResult.TargetAlgorithmStatus);
            result.IsCancelled.ShouldBe(continuousResult.IsCancelled);
            result.Runtime.ShouldBe(continuousResult.Runtime);
            result.Value.ShouldBe(continuousResult.Value);
        }

        /// <summary>
        /// Checks, that <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}.TryToGetResultFromStringArray"/> throws for unhandled result types.
        /// </summary>
        [Fact]
        public void TryToGetResultFromStringArrayThrowsForUnhandledResultType()
        {
            var testResult = new TestResult(TimeSpan.FromSeconds(30));
            var targetAlgorithmFactory =
                new DummyTargetAlgorithmFactory<DummyTargetAlgorithm<TestInstance, TestResult>, TestInstance, TestResult>() as
                    ITargetAlgorithmFactory<DummyTargetAlgorithm<TestInstance, TestResult>, TestInstance, TestResult>;
            Assert.Throws<NotImplementedException>(
                () => targetAlgorithmFactory.TryToGetResultFromStringArray(testResult.ToStringArray(), out var result));
        }

        /// <summary>
        /// Checks, that <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}.TryToGetInstanceFromInstanceId"/> works for <see cref="InstanceFile"/>s.
        /// </summary>
        [Fact]
        public void TryToGetInstanceFromInstanceIdWorksForInstanceFiles()
        {
            var instanceFile = new InstanceFile("dummy");
            var targetAlgorithmFactory =
                new DummyTargetAlgorithmFactory<DummyTargetAlgorithm<InstanceFile, TestResult>, InstanceFile, TestResult>() as
                    ITargetAlgorithmFactory<DummyTargetAlgorithm<InstanceFile, TestResult>, InstanceFile, TestResult>;
            targetAlgorithmFactory.TryToGetInstanceFromInstanceId(instanceFile.ToId(), out var instance).ShouldBeTrue();
            instance.Equals(instanceFile).ShouldBeTrue();
        }

        /// <summary>
        /// Checks, that <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}.TryToGetInstanceFromInstanceId"/> works for <see cref="InstanceSeedFile"/>s.
        /// </summary>
        [Fact]
        public void TryToGetInstanceFromInstanceIdWorksForInstanceSeedFiles()
        {
            var instanceSeedFile = new InstanceSeedFile("dummy", 42);
            var targetAlgorithmFactory =
                new DummyTargetAlgorithmFactory<DummyTargetAlgorithm<InstanceSeedFile, TestResult>, InstanceSeedFile, TestResult>() as
                    ITargetAlgorithmFactory<DummyTargetAlgorithm<InstanceSeedFile, TestResult>, InstanceSeedFile, TestResult>;
            targetAlgorithmFactory.TryToGetInstanceFromInstanceId(instanceSeedFile.ToId(), out var instance).ShouldBeTrue();
            instance.Equals(instanceSeedFile).ShouldBeTrue();
        }

        /// <summary>
        /// Checks, that <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}.TryToGetInstanceFromInstanceId"/> throws for unhandled instance types.
        /// </summary>
        [Fact]
        public void TryToGetInstanceFromInstanceIdThrowsForUnhandledInstanceType()
        {
            var testInstance = new TestInstance("dummy");
            var targetAlgorithmFactory =
                new DummyTargetAlgorithmFactory<DummyTargetAlgorithm<TestInstance, TestResult>, TestInstance, TestResult>() as
                    ITargetAlgorithmFactory<DummyTargetAlgorithm<TestInstance, TestResult>, TestInstance, TestResult>;
            Assert.Throws<NotImplementedException>(
                () => targetAlgorithmFactory.TryToGetInstanceFromInstanceId(testInstance.ToId(), out var instance));
        }

        #endregion

        /// <summary>
        /// A dummy implementation of <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/>, used in tests.
        /// </summary>
        /// <typeparam name="TTargetAlgorithm">The target algorithm type.</typeparam>
        /// <typeparam name="TInstance">The instance type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        public class DummyTargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> : ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult>
            where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult>
            where TInstance : InstanceBase
            where TResult : ResultBase<TResult>, new()
        {
            #region Public Methods and Operators

            /// <inheritdoc />
            public TTargetAlgorithm ConfigureTargetAlgorithm(Dictionary<string, IAllele> parameters)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        /// <summary>
        /// A dummy implementation of <see cref="ITargetAlgorithm{TInstance,TResult}"/>, used in tests.
        /// </summary>
        /// <typeparam name="TInstance">The instance type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        public class DummyTargetAlgorithm<TInstance, TResult> : ITargetAlgorithm<TInstance, TResult>
            where TInstance : InstanceBase
            where TResult : ResultBase<TResult>, new()
        {
            #region Public Methods and Operators

            /// <inheritdoc />
            public Task<TResult> Run(TInstance instance, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }
}