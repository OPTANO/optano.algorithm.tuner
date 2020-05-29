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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.Sorting.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting.Messages;
    using Optano.Algorithm.Tuner.Genomes;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="SortResult"/> class.
    /// </summary>
    public class SortResultTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="SortResult"/>'s constructor throws a <see cref="ArgumentNullException"/> if called
        /// without a ranking.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingRanking()
        {
            Assert.Throws<ArgumentNullException>(() => new SortResult(ranking: null));
        }

        /// <summary>
        /// Checks that <see cref="SortResult.Ranking"/> returns the ranking provided on initialization.
        /// </summary>
        [Fact]
        public void RankingIsSetCorrectly()
        {
            var ranking = new List<ImmutableGenome>
                              { new ImmutableGenome(new Genome(1)), new ImmutableGenome(new Genome(2)) };
            var result = new SortResult(ranking.ToImmutableList());
            Assert.Equal(ranking, result.Ranking);
        }

        #endregion
    }
}