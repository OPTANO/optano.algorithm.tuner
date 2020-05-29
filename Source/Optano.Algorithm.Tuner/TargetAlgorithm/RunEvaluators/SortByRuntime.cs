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
    /// An implementation of <see cref="IRunEvaluator{RuntimeResult}"/> that sorts by average runtime on 
    /// runs, lower runtime first.
    /// </summary>
    public class SortByRuntime : IMetricRunEvaluator<RuntimeResult>
    {
        #region Fields

        /// <summary>
        /// Penalization factor for timed out runs.
        /// </summary>
        private readonly int _factorPar;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortByRuntime"/> class.
        /// </summary>
        /// <param name="factorPar">
        /// Penalization factor for timed out runs' runtime.
        /// </param>
        public SortByRuntime(int factorPar)
        {
            this._factorPar = factorPar;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Sorts the genomes by results, best genome first.
        /// <para>In this case, genomes are sorted by (penalized) average runtime.</para>
        /// </summary>
        /// <param name="runResults">Results from target algorithm runs, grouped by genome.</param>
        /// <returns>The sorted genomes, best genomes first.</returns>
        public IEnumerable<ImmutableGenome> Sort(Dictionary<ImmutableGenome, IEnumerable<RuntimeResult>> runResults)
        {
            return runResults
                .OrderBy(genomeToResults => genomeToResults.Value.Average(result => this.GetMetricRepresentation(result)))
                .Select(genomeToResults => genomeToResults.Key);
        }

        /// <summary>
        /// Gets a metric representation of the provided result.
        /// <para><see cref="IRunEvaluator{TResult}.Sort"/> needs to be based on this.</para>
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The result's (penalized) average runtime.</returns>
        public double GetMetricRepresentation(RuntimeResult result)
        {
            var factor = result.IsCancelled ? this._factorPar : 1;
            return factor * result.Runtime.TotalSeconds;
        }

        #endregion
    }
}