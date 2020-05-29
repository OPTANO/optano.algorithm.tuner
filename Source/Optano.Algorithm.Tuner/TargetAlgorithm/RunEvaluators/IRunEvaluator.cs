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

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Responsible for comparing multiple runs.
    /// <para>
    /// As the class is not modelled as an <see cref="IComparer{T}"/>, the ordering may depend
    /// on global properties.
    /// </para>
    /// </summary>
    /// <typeparam name="TResult">
    /// Type of an individual evaluation's result.
    /// </typeparam>
    public interface IRunEvaluator<TResult>
        where TResult : ResultBase<TResult>, new()
    {
        #region Public Methods and Operators

        /// <summary>
        /// Sorts the provided genomes by performance, best genomes first.
        /// <para>
        /// By default, OPTANO Algorithm Tuner assumes that the most important factors of that sorting are 
        /// runtime and the number of cancellations due to CPU timeouts.
        /// Evaluation schedules are optimized accordingly.
        /// Use parameter --enableRacing=false if you'd like to switch off that optimization.
        /// </para>
        /// </summary>
        /// <param name="runResults">
        /// Results from target algorithm runs, grouped by genome.
        /// <para>
        /// May include results cancelled due to CPU timeouts. <see cref="ResultBase{TResult}.IsCancelled"/> is true for those.
        /// </para>
        /// <para>
        /// If racing is not turned off (i.e. --enableRacing=false is not set), the number of results per genome may
        /// vary:
        /// In racing, evaluations are stopped for a genome once the requested number of winners have successfully
        /// completed all instances without any CPU timeouts in less time than the considered genome has required up to
        /// that point.
        /// </para>
        /// </param>
        /// <returns>The sorted genomes, best genomes first.</returns>
        IEnumerable<ImmutableGenome> Sort(Dictionary<ImmutableGenome, IEnumerable<TResult>> runResults);

        #endregion
    }
}