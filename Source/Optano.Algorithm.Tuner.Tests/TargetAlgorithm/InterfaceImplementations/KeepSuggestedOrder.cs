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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations
{
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// An implementation of <see cref="IRunEvaluator{TResult}"/> that doesn't reorder the genomes at all.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    internal class KeepSuggestedOrder<TResult> : IRunEvaluator<TResult>
        where TResult : ResultBase<TResult>, new()
    {
        #region Public properties

        /// <summary>
        /// Gets a value indicating whether the sorting is ascending or descending.
        /// Dummy stub to meet interface requirement.
        /// </summary>
        public bool SortAscending => true;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Sorts the genomes by results, best genome first.
        /// In this case, we just keep the order suggested by the successful run result dictionary.
        /// </summary>
        /// <param name="runResults">Results from target algorithm runs, grouped by genome.</param>
        /// <returns>The given genomes as a list.</returns>
        public IEnumerable<ImmutableGenome> Sort(Dictionary<ImmutableGenome, IEnumerable<TResult>> runResults)
        {
            return runResults.Select(keyValuePair => keyValuePair.Key);
        }

        #endregion
    }
}