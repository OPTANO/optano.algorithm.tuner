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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Sorting.Messages
{
    using System;
    using System.Collections.Immutable;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// A message signifying that a number of <see cref="ImmutableGenome"/>s should be sorted.
    /// </summary>
    /// <typeparam name="TInstance">The type of instance that can be run by the target algorithm.</typeparam>
    public class SortCommand<TInstance>
        where TInstance : InstanceBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortCommand{TInstance}"/> class.
        /// </summary>
        /// <param name="items"><see cref="ImmutableGenome"/>s to sort.</param>
        /// <param name="instances"><typeparamref name="TInstance"/>s to base the sorting on.</param>
        public SortCommand(ImmutableList<ImmutableGenome> items, ImmutableList<TInstance> instances)
        {
            this.Items = items ?? throw new ArgumentNullException(nameof(items));
            this.Instances = instances ?? throw new ArgumentNullException(nameof(instances));
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the <see cref="ImmutableGenome"/>s to sort.
        /// </summary>
        public ImmutableList<ImmutableGenome> Items { get; }

        /// <summary>
        /// Gets the <typeparamref name="TInstance"/>s to base the sorting on.
        /// </summary>
        public ImmutableList<TInstance> Instances { get; }

        #endregion
    }
}