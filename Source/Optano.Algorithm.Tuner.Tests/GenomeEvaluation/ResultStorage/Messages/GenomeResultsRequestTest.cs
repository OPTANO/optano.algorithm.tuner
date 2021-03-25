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

    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GenomeResultsRequest"/> class.
    /// </summary>
    public class GenomeResultsRequestTest
    {
        #region Static Fields

        /// <summary>
        /// An empty <see cref="ImmutableGenome"/>.
        /// </summary>
        private static readonly ImmutableGenome Genome = new ImmutableGenome(new Genome());

        #endregion

        #region Fields

        /// <summary>
        /// <see cref="GenomeResultsRequest"/> to use in tests. Needs to be initialized.
        /// </summary>
        private readonly GenomeResultsRequest _genomeResultsRequestMessage;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeResultsRequestTest"/> class.
        /// </summary>
        public GenomeResultsRequestTest()
        {
            this._genomeResultsRequestMessage = new GenomeResultsRequest(GenomeResultsRequestTest.Genome);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="GenomeResultsRequest"/>'s constructor without providing a
        /// genome throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingGenome()
        {
            Assert.Throws<ArgumentNullException>(() => new GenomeResultsRequest(genome: null));
        }

        /// <summary>
        /// Checks that <see cref="GenomeResultsRequest.Genome"/> returns the genome that was used for initialization.
        /// </summary>
        [Fact]
        public void GenomeIsSetCorrectly()
        {
            Assert.Equal(GenomeResultsRequestTest.Genome, this._genomeResultsRequestMessage.Genome);
        }

        #endregion
    }
}