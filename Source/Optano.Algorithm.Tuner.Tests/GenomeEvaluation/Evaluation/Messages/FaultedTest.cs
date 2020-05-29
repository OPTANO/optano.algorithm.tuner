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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.Evaluation.Messages
{
    using System;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="Faulted{TInstance}"/> class.
    /// </summary>
    public class FaultedTest
    {
        #region Static Fields

        /// <summary>
        /// A <see cref="TestInstance"/> used for message construction.
        /// </summary>
        private static readonly TestInstance instance = new TestInstance("test");

        #endregion

        #region Fields

        /// <summary>
        /// An <see cref="AggregateException"/> used for message construction.
        /// </summary>
        private readonly AggregateException _exception = new AggregateException();

        /// <summary>
        /// An <see cref="ImmutableGenome"/> used for message construction, has to be initialized.
        /// </summary>
        private readonly ImmutableGenome _genome;

        /// <summary>
        /// <see cref="Faulted{I}"/> to use in tests. Needs to be initialized.
        /// </summary>
        private readonly Faulted<TestInstance> _faultedMessage;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FaultedTest"/> class.
        /// </summary>
        public FaultedTest()
        {
            this._genome = new ImmutableGenome(new Genome());
            this._faultedMessage = new Faulted<TestInstance>(this._genome, FaultedTest.instance, this._exception);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="Faulted{I}"/>'s constructor without providing a
        /// genome throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingGenome()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Faulted<TestInstance>(genome: null, instance: FaultedTest.instance, exception: this._exception));
        }

        /// <summary>
        /// Verifies that calling <see cref="Faulted{I}"/>'s constructor without providing an
        /// instance throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingInstance()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Faulted<TestInstance>(this._genome, instance: null, exception: this._exception));
        }

        /// <summary>
        /// Verifies that calling <see cref="Faulted{I}"/>'s constructor without providing an
        /// exception throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Faulted<TestInstance>(this._genome, FaultedTest.instance, exception: null));
        }

        /// <summary>
        /// Checks that <see cref="Faulted{I}.Genome"/> returns the same genome it was initialized
        /// with.
        /// </summary>
        [Fact]
        public void GenomeIsSetCorrectly()
        {
            Assert.Equal(this._genome, this._faultedMessage.Genome);
        }

        /// <summary>
        /// Checks that <see cref="Faulted{I}.Instance"/> returns the instance that was used for initialization.
        /// </summary>
        [Fact]
        public void InstanceIsSetCorrectly()
        {
            Assert.Equal(FaultedTest.instance, this._faultedMessage.Instance);
        }

        /// <summary>
        /// Checks that <see cref="Faulted{I}.Exception"/> returns the exception that was used for initialization.
        /// </summary>
        [Fact]
        public void ExceptionIsSetCorrectly()
        {
            Assert.Equal(this._exception, this._faultedMessage.Exception);
        }

        #endregion
    }
}