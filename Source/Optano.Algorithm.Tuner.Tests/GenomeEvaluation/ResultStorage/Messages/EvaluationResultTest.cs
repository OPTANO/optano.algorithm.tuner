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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.ResultStorage.Messages
{
    using System;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for <see cref="EvaluationResult{TInstance,TResult}"/>.
    /// </summary>
    public class EvaluationResultTest
    {
        #region Static Fields

        /// <summary>
        /// A simple <see cref="GenomeInstancePair{TInstance}"/>.
        /// </summary>
        private static readonly GenomeInstancePair<TestInstance> genomeInstancePair =
            new GenomeInstancePair<TestInstance>(new ImmutableGenome(new Genome()), new TestInstance("1"));

        /// <summary>
        /// A simple <see cref="TestResult"/>.
        /// </summary>
        private static readonly TestResult result = new TestResult();

        #endregion

        #region Fields

        /// <summary>
        /// The <see cref="EvaluationResult{TInstance,TResult}"/> to use in tests. Needs to be initialized.
        /// </summary>
        private readonly EvaluationResult<TestInstance, TestResult> _evaluationResult;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EvaluationResultTest"/> class.
        /// </summary>
        public EvaluationResultTest()
        {
            this._evaluationResult = new EvaluationResult<TestInstance, TestResult>(
                EvaluationResultTest.genomeInstancePair,
                EvaluationResultTest.result);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="EvaluationResult{TInstance,TResult}"/>'s constructor without providing a
        /// genome instance pair throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingGenomeInstancePair()
        {
            Assert.Throws<ArgumentNullException>(
                () => new EvaluationResult<TestInstance, TestResult>(null, EvaluationResultTest.result));
        }

        /// <summary>
        /// Verifies that calling <see cref="EvaluationResult{TInstance,TResult}"/>'s constructor without providing a
        /// run result throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingResult()
        {
            Assert.Throws<ArgumentNullException>(
                () => new EvaluationResult<TestInstance, TestResult>(EvaluationResultTest.genomeInstancePair, null));
        }

        /// <summary>
        /// Checks that <see cref="EvaluationResult{TInstance, TResult}.GenomeInstancePair"/> returns the same genome instance pair that was used for initialization.
        /// </summary>
        [Fact]
        public void GenomeInstancePairIsSetCorrectly()
        {
            Assert.Equal(EvaluationResultTest.genomeInstancePair, this._evaluationResult.GenomeInstancePair);
        }

        /// <summary>
        /// Checks that <see cref="EvaluationResult{TInstance,TResult}.RunResult"/> returns the same result that was used for initialization.
        /// </summary>
        [Fact]
        public void ResultIsSetCorrectly()
        {
            Assert.Equal(EvaluationResultTest.result, this._evaluationResult.RunResult);
        }

        #endregion
    }
}