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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Sorting.Messages
{
    using System;
    using System.Collections.Immutable;

    using Optano.Algorithm.Tuner.Genomes;

    /// <summary>
    /// A message containing sorted <see cref="ImmutableGenome"/>s.
    /// </summary>
    public class SortResult
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortResult"/> class.
        /// </summary>
        /// <param name="ranking">The sorted <see cref="ImmutableGenome"/>s, best first.</param>
        public SortResult(ImmutableList<ImmutableGenome> ranking)
        {
            this.Ranking = ranking ?? throw new ArgumentNullException(nameof(ranking));
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets sorted <see cref="ImmutableGenome"/>s, best first.
        /// </summary>
        public ImmutableList<ImmutableGenome> Ranking { get; }

        #endregion
    }
}