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

namespace Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators
{
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// An implementation of <see cref="IRunEvaluator{R}" /> that sorts genomes by average
    /// value in target algorithm runs.
    /// </summary>
    public class SortByValue : IMetricRunEvaluator<ContinuousResult>
    {
        #region Fields

        /// <summary>
        /// A value indicating whether values should be sorted ascendingly.
        /// </summary>
        private readonly bool _ascending;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortByValue"/> class.
        /// </summary>
        /// <param name="ascending">Whether values should be sorting ascendingly.</param>
        public SortByValue(bool ascending = true)
        {
            this._ascending = ascending;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Sorts the genomes by results, best genome first.
        /// We decided to sort genomes by the number of valid results first and by the average value of these results afterwards. Typically the number of runs should be the same for every genome.
        /// </summary>
        /// <param name="runResults">Results from target algorithm runs, grouped by genome.</param>
        /// <returns>The sorted genomes.</returns>
        public IEnumerable<ImmutableGenome> Sort(Dictionary<ImmutableGenome, IEnumerable<ContinuousResult>> runResults)
        {
            var orderByNumberOfValidResults = this.OrderByNumberOfValidResults(runResults);
            if (this._ascending)
            {
                return orderByNumberOfValidResults
                    .ThenBy(
                        genomeToResult => genomeToResult.Value.Where(this.HasValidResultValue).Select(this.GetMetricRepresentation)
                            .DefaultIfEmpty().Average())
                    .Select(genomeToResult => genomeToResult.Key);
            }
            else
            {
                return orderByNumberOfValidResults
                    .ThenByDescending(
                        genomeToResult => genomeToResult.Value.Where(this.HasValidResultValue).Select(this.GetMetricRepresentation)
                            .DefaultIfEmpty().Average())
                    .Select(genomeToResult => genomeToResult.Key);
            }
        }

        /// <summary>
        /// Gets a metric representation of the provided result.
        /// <para><see cref="IRunEvaluator{TResult}.Sort"/> needs to be based on this.</para>
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns><see cref="ContinuousResult.Value"/>.</returns>
        public double GetMetricRepresentation(ContinuousResult result)
        {
            return result.Value;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sorts the genomes descending by the number of valid results, genomes with most valid results first.
        /// </summary>
        /// <param name="runResults">Results from target algorithm runs, grouped by genome.</param>
        /// <returns>The sorted genomes.</returns>
        private IOrderedEnumerable<KeyValuePair<ImmutableGenome, IEnumerable<ContinuousResult>>> OrderByNumberOfValidResults(
            Dictionary<ImmutableGenome, IEnumerable<ContinuousResult>> runResults)
        {
            return runResults.OrderByDescending(kvp => kvp.Value.Count(this.HasValidResultValue));
        }

        /// <summary>
        /// Checks, if the result has a valid result value.
        /// </summary>
        /// <param name="runResult">Result from target algorithm run.</param>
        /// <returns>True, if the result has a valid result value.</returns>
        private bool HasValidResultValue(ContinuousResult runResult)
        {
            return !runResult.IsCancelled && !double.IsNaN(runResult.Value) && !double.IsInfinity(runResult.Value);
        }

        #endregion
    }
}