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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// A <see cref="ISearchPointSorter{TSearchPoint}"/> which evaluates <see cref="IRepairedGenomeRepresentation"/>s
    /// via a <see cref="GenomeSorter{TInstance,TResult}"/>.
    /// </summary>
    /// <typeparam name="TSearchPoint">
    /// The specific type of <see cref="IRepairedGenomeRepresentation"/> which can be evaluated.
    /// </typeparam>
    /// <typeparam name="TInstance">
    /// The type of instance the <see cref="GenomeSorter{TInstance, TResult}"/> expects.
    /// </typeparam>
    public class RepairedGenomeSearchPointSorter<TSearchPoint, TInstance>
        : GenomeAssistedSorterBase<TSearchPoint, TInstance>
        where TSearchPoint : SearchPoint, IRepairedGenomeRepresentation
        where TInstance : InstanceBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="RepairedGenomeSearchPointSorter{TSearchPoint,TInstance}"/> class.
        /// </summary>
        /// <param name="genomeSorter">
        /// An <see cref="IActorRef" /> to a <see cref="GenomeSorter{TInstance, TResult}" />.
        /// </param>
        public RepairedGenomeSearchPointSorter(IActorRef genomeSorter)
            : base(genomeSorter)
        {
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Sorts <typeparamref name="TSearchPoint"/>s by original validity as <see cref="Genome"/> and by the
        /// performance of the associated <see cref="Genome"/> objects.
        /// </summary>
        /// <param name="points">The <typeparamref name="TSearchPoint"/>s to sort.</param>
        /// <returns>Indices of sorted points, best points first.</returns>
        public override IList<int> Sort(IList<TSearchPoint> points)
        {
            var genomesToSort = points.Select(point => point.Genome).ToImmutableList();
            var sortResult = this.SortGenomes(genomesToSort);
            var ranks = AssignRanksToGenomes(sortResult, genomesToSort);

            // Points which have been valid from the start are better than ones that needed repair.
            return points
                .Select((point, idx) => new { point, idx })
                .OrderBy(pointAndIdx => pointAndIdx.point.IsRepaired)
                .ThenBy(pointAndIdx => ranks[pointAndIdx.point.Genome])
                .Select(pointAndIdx => pointAndIdx.idx)
                .ToList();
        }

        #endregion
    }
}