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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration
{
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;

    /// <summary>
    /// An implementation of <see cref="IRunEvaluator{TResult}"/> that sorts genomes by descending average
    /// integer value.
    /// </summary>
    public class SortByValue : IMetricRunEvaluator<IntegerResult>
    {
        #region Public properties

        /// <summary>
        /// Gets a value indicating whether values will be sorted ascending.
        /// In this case, values will always be sorted descending.
        /// </summary>
        public bool SortAscending => false;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Sorts the genomes by results, best genome first.
        /// In this case, genomes are sorted by average integer value of target algorithm runs, higher values first.
        /// </summary>
        /// <param name="runResults">Results from target algorithm runs, grouped by genome.</param>
        /// <returns>The given genomes as a list.</returns>
        public IEnumerable<ImmutableGenome> Sort(Dictionary<ImmutableGenome, IEnumerable<IntegerResult>> runResults)
        {
            return runResults
                .OrderByDescending(genomeAndResults => genomeAndResults.Value.Average(result => result.Value))
                .Select(genomeAndResults => genomeAndResults.Key);
        }

        /// <summary>
        /// Gets a metric representation of the provided result.
        /// <para><see cref="IRunEvaluator{TResult}.Sort"/> needs to be based on this.</para>
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>A metric representation.</returns>
        public double GetMetricRepresentation(IntegerResult result)
        {
            return result.Value;
        }

        #endregion
    }
}