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

namespace Optano.Algorithm.Tuner
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The enumerable extensions.
    /// </summary>
    internal static class EnumerableExtensions
    {
        #region Public Methods and Operators

        /// <summary>
        /// Efficiently retrieves the <paramref name="k"/> smallest elements from an enumerable. 
        /// </summary>
        /// <param name="source">
        /// The source to retrieve elements from.
        /// </param>
        /// <param name="k">
        /// The number of elements to retrieve.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{Double}"/>.
        /// </returns>
        public static IEnumerable<double> TakeSmallestSorted(this IEnumerable<double> source, int k)
        {
            var smallestElements = new List<double>(k + 1);
            using (var e = source.GetEnumerator())
            {
                for (var i = 0; i < k; i++)
                {
                    if (e.MoveNext())
                    {
                        smallestElements.Add(e.Current);
                    }
                }

                smallestElements.Sort();
                while (e.MoveNext())
                {
                    var c = e.Current;
                    var index = smallestElements.BinarySearch(c);
                    if (index < 0)
                    {
                        index = ~index;
                    }

                    if (index < k)
                    {
                        smallestElements.Insert(index, c);
                        smallestElements.RemoveAt(k);
                    }
                }
            }

            return smallestElements;
        }

        /// <summary>
        /// Creates a list of size <paramref name="number"/>, consisting of <typeparamref name="T"/>s provided in
        /// <paramref name="items"/>
        /// so that each item occurs the same number of times. If <paramref name="number"/> is not a multiple of
        /// <paramref name="items"/>.Count,
        /// the last spots are filled with a random subset of <paramref name="items"/>.
        /// </summary>
        /// <typeparam name="T">
        /// Element type of the <see cref="List{T}"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to inflate.
        /// </param>
        /// <param name="number">
        /// The required number of items.
        /// </param>
        /// <returns>
        /// The created list in <c>randomized</c> order.
        /// </returns>
        public static List<T> InflateAndShuffle<T>(this List<T> items, int number)
        {
            // take all items timesRepeatAll times, until number would be exceeded.
            var timesRepeatAll = number / items.Count;

            // fill remaining spots with random subset of size numberRemainingRandom.
            var numberRemainingRandom = number % items.Count;

            var chosenItems = Enumerable.Repeat(items, timesRepeatAll).SelectMany(l => l);

            if (numberRemainingRandom > 0)
            {
                chosenItems = chosenItems.Concat(Randomizer.Instance.ChooseRandomSubset(items, numberRemainingRandom));
            }

            // Use Randomizer to shuffle the sequence. Length should be equal to number.
            return Randomizer.Instance.ChooseRandomSubset(chosenItems, number).ToList();
        }

        #endregion
    }
}