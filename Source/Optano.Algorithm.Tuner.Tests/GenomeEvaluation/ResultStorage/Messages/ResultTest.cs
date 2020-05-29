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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.ResultStorage.Messages
{
    using System;

    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="ResultMessage{TInstance,TResult}"/>.
    /// </summary>
    public class ResultTest
    {
        #region Static Fields

        /// <summary>
        /// An empty <see cref="ImmutableGenome"/>.
        /// </summary>
        private static readonly ImmutableGenome genome = new ImmutableGenome(new Genome());

        /// <summary>
        /// A simple <see cref="TestInstance"/>.
        /// </summary>
        private static readonly TestInstance testInstance = new TestInstance("1");

        /// <summary>
        /// A <see cref="TestResult"/>.
        /// </summary>
        private static readonly TestResult testResult = new TestResult();

        #endregion

        #region Fields

        /// <summary>
        /// <see cref="ResultMessage{I,R}"/> to use in tests. Needs to be initialized.
        /// </summary>
        private readonly ResultMessage<TestInstance, TestResult> _resultMessage;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultTest"/> class.
        /// </summary>
        public ResultTest()
        {
            this._resultMessage = new ResultMessage<TestInstance, TestResult>(ResultTest.genome, ResultTest.testInstance, ResultTest.testResult);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="ResultMessage{I,R}"/>'s constructor without providing a
        /// genome throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingGenome()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ResultMessage<TestInstance, TestResult>(
                    genome: null,
                    instance: ResultTest.testInstance,
                    runResult: ResultTest.testResult));
        }

        /// <summary>
        /// Verifies that calling <see cref="ResultMessage{TestInstance, TestResult}"/>'s constructor without providing an
        /// instance throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingInstance()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ResultMessage<TestInstance, TestResult>(ResultTest.genome, instance: null, runResult: ResultTest.testResult));
        }

        /// <summary>
        /// Verifies that calling <see cref="ResultMessage{TestInstance, TestResult}"/>'s constructor without providing a
        /// run result throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingResult()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ResultMessage<TestInstance, TestResult>(
                    ResultTest.genome,
                    ResultTest.testInstance,
                    runResult: null));
        }

        /// <summary>
        /// Checks that <see cref="ResultMessage{I,R}.Genome"/> returns the genome that was used for initialization.
        /// </summary>
        [Fact]
        public void GenomeIsSetCorrectly()
        {
            Assert.Equal(ResultTest.genome, this._resultMessage.Genome);
        }

        /// <summary>
        /// Checks that <see cref="ResultMessage{I,R}.Instance"/> returns the same instance it was initialized with.
        /// </summary>
        [Fact]
        public void InstanceIsSetCorrectly()
        {
            Assert.Equal(ResultTest.testInstance, this._resultMessage.Instance);
        }

        /// <summary>
        /// Checks that <see cref="ResultMessage{I,R}.RunResult"/> returns the same result it was initialized with.
        /// </summary>
        [Fact]
        public void ResultIsSetCorrectly()
        {
            Assert.Equal(ResultTest.testResult, this._resultMessage.RunResult);
        }

        #endregion
    }
}
