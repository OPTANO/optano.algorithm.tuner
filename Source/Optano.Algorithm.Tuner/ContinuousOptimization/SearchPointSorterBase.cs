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

namespace Optano.Algorithm.Tuner.ContinuousOptimization
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Contains default implementations for <see cref="ISearchPointSorter{TSearchPoint}"/>.
    /// </summary>
    /// <typeparam name="TSearchPoint">Type of <see cref="SearchPoint"/>s to sort.</typeparam>
    public abstract class SearchPointSorterBase<TSearchPoint> : ISearchPointSorter<TSearchPoint>
        where TSearchPoint : SearchPoint
    {
        #region Public Methods and Operators

        /// <summary>
        /// Determines the ranks of a number of <typeparamref name="TSearchPoint"/>s.
        /// </summary>
        /// <param name="points">The <typeparamref name="TSearchPoint"/>s to sort.</param>
        /// <returns><paramref name="points"/> with ranks, starting at 0.</returns>
        public Dictionary<TSearchPoint, int> DetermineRanks(IList<TSearchPoint> points)
        {
            var ranking = this.Sort(points);
            return ranking.Select((identifier, rank) => new { identifier, rank }).ToDictionary(
                identifierWithRank => points[identifierWithRank.identifier],
                identifierWithRank => identifierWithRank.rank);
        }

        /// <summary>
        /// Sorts a number of <typeparamref name="TSearchPoint"/>s.
        /// </summary>
        /// <param name="points">The <typeparamref name="TSearchPoint"/>s to sort.</param>
        /// <returns>Indices of sorted points, best points first.</returns>
        public abstract IList<int> Sort(IList<TSearchPoint> points);

        #endregion
    }
}