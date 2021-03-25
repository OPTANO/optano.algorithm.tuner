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

namespace Optano.Algorithm.Tuner
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The time span utils.
    /// </summary>
    public static class TimeSpanUtil
    {
        #region Public Methods and Operators

        /// <summary>
        /// Multiplies a timespan by an integer or long value.
        /// </summary>
        /// <param name="multiplicand">
        /// The multiplicand.
        /// </param>
        /// <param name="multiplier">
        /// The multiplier.
        /// </param>
        /// <returns>
        /// The <see cref="TimeSpan"/>.
        /// </returns>
        public static TimeSpan Multiply(this TimeSpan multiplicand, long multiplier)
        {
            return TimeSpan.FromTicks(multiplicand.Ticks * multiplier);
        }

        /// <summary>
        /// Multiplies a timespan by a double value.
        /// </summary>
        /// <param name="multiplicand">
        /// The multiplicand.
        /// </param>
        /// <param name="multiplier">
        /// The multiplier.
        /// </param>
        /// <returns>
        /// The <see cref="TimeSpan"/>.
        /// </returns>
        public static TimeSpan Multiply(this TimeSpan multiplicand, double multiplier)
        {
            return TimeSpan.FromTicks((long)(multiplicand.Ticks * multiplier));
        }

        /// <summary>
        /// Computes the sum over elements of an <see cref="IEnumerable{TSource}"/>, 
        /// that are converted to <see cref="TimeSpan"/> with the given <paramref name="selector"/>.
        /// </summary>
        /// <param name="source">
        /// The source enumerable.
        /// </param>
        /// <param name="selector">
        /// The converter from <typeparamref name="TSource"/> to <see cref="TimeSpan"/>.
        /// </param>
        /// <typeparam name="TSource">
        /// The <see cref="IEnumerable{TSource}"/> element type.
        /// </typeparam>
        /// <returns>
        /// The sum of all converted <see cref="TimeSpan"/>s.
        /// </returns>
        public static TimeSpan Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, TimeSpan> selector)
        {
            return source.Select(selector).Aggregate(TimeSpan.Zero, (t1, t2) => t1 + t2);
        }

        /// <summary>
        /// Computes the average over elements of an <see cref="IEnumerable{TSource}"/>, 
        /// that are converted to <see cref="TimeSpan"/> with the given <paramref name="selector"/>.
        /// </summary>
        /// <param name="source">
        /// The source enumerable.
        /// </param>
        /// <param name="selector">
        /// The converter from <typeparamref name="TSource"/> to <see cref="TimeSpan"/>.
        /// </param>
        /// <typeparam name="TSource">
        /// The <see cref="IEnumerable{TSource}"/> element type.
        /// </typeparam>
        /// <returns>
        /// The average over all converted <see cref="TimeSpan"/>s.
        /// </returns>
        public static TimeSpan Average<TSource>(this IEnumerable<TSource> source, Func<TSource, TimeSpan> selector)
        {
            return TimeSpan.FromTicks((long)source.Select(selector).Average(t => t.Ticks));
        }

        /// <summary>
        /// Returns the smaller of the two given <see cref="TimeSpan"/>s.
        /// </summary>
        /// <param name="timeSpanA">
        /// The time span a.
        /// </param>
        /// <param name="timeSpanB">
        /// The time span b.
        /// </param>
        /// <returns>
        /// The smaller <see cref="TimeSpan"/>.
        /// </returns>
        public static TimeSpan Min(TimeSpan timeSpanA, TimeSpan timeSpanB)
        {
            return timeSpanA <= timeSpanB ? timeSpanA : timeSpanB;
        }

        #endregion
    }
}