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

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GenomeEvaluation"/> class.
    /// </summary>
    public class GenomeEvaluationTest
    {
        #region Fields

        /// <summary>
        /// A number which can be used as the evaluation identifier.
        /// </summary>
        private readonly int _evaluationId = 16;

        /// <summary>
        /// An <see cref="ImmutableGenome"/> used for message construction, has to be initialized.
        /// </summary>
        private readonly ImmutableGenome _genome;

        /// <summary>
        /// <see cref="GenomeEvaluation"/> to use in tests. Needs to be initialized.
        /// </summary>
        private readonly GenomeEvaluation _genomeEvaluation;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeEvaluationTest"/> class.
        /// </summary>
        public GenomeEvaluationTest()
        {
            this._genome = new ImmutableGenome(new Genome());
            this._genomeEvaluation = new GenomeEvaluation(this._genome, this._evaluationId);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="GenomeEvaluation"/>'s constructor without providing a
        /// genome throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingGenome()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenomeEvaluation(genome: null, evaluationId: this._evaluationId));
        }

        /// <summary>
        /// Checks that <see cref="GenomeEvaluation.Genome"/> returns the same genome it was initialized
        /// with.
        /// </summary>
        [Fact]
        public void GenomeIsSetCorrectly()
        {
            Assert.Equal(this._genome, this._genomeEvaluation.Genome);
        }

        /// <summary>
        /// Checks that <see cref="GenomeEvaluation.EvaluationId"/> returns the same ID it was initialized
        /// with.
        /// </summary>
        [Fact]
        public void EvaluationIdIsSetCorrectly()
        {
            Assert.Equal(
                this._evaluationId,
                this._genomeEvaluation.EvaluationId);
        }

        #endregion
    }
}