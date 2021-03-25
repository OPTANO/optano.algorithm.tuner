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

namespace Optano.Algorithm.Tuner.Tests.ContinuousOptimization
{
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.ContinuousOptimization;

    using Xunit;

    /// <summary>
    /// Defines tests that should be implemented for each <see cref="ISearchPointSorter{TSearchPoint}"/>.
    /// </summary>
    /// <typeparam name="TSearchPoint">
    /// The type of <see cref="SearchPoint"/> handled by the <see cref="ISearchPointSorter{TSearchPoint}"/>.
    /// </typeparam>
    public abstract class SearchPointSorterTestBase<TSearchPoint> : TestBase
        where TSearchPoint : SearchPoint
    {
        #region Properties

        /// <summary>
        /// Gets the <see cref="ISearchPointSorter{TSearchPoint}"/> used in tests.
        /// </summary>
        protected abstract ISearchPointSorter<TSearchPoint> Sorter { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="ISearchPointSorter{TSearchPoint}.Sort"/> and
        /// <see cref="ISearchPointSorter{TSearchPoint}.DetermineRanks"/> are consistent.
        /// </summary>
        [Fact]
        public abstract void SortingIsConsistent();

        /// <summary>
        /// Checks that <see cref="ISearchPointSorter{TSearchPoint}.Sort"/> can handle duplicates.
        /// </summary>
        [Fact]
        public abstract void SortingCanHandleDuplicates();

        #endregion

        #region Methods

        /// <summary>
        /// Checks that <see cref="ISearchPointSorter{TSearchPoint}.Sort"/> and
        /// <see cref="ISearchPointSorter{TSearchPoint}.DetermineRanks"/> are consistent.
        /// </summary>
        /// <param name="points">The <typeparamref name="TSearchPoint"/>s to sort.</param>
        protected void CheckSortingConsistence(IList<TSearchPoint> points)
        {
            var ranking = this.Sorter.Sort(points);
            var ranks = this.Sorter.DetermineRanks(points);

            // Compare sort with independently determined ranks
            for (int rank = 0; rank < ranking.Count; rank++)
            {
                var point = points[ranking[rank]];
                Assert.Equal(ranks[point], rank);
            }
        }

        #endregion
    }
}