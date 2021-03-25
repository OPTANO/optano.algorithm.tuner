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

namespace Optano.Algorithm.Tuner.GenomeEvaluation
{
    using System.Collections.Immutable;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// The incumbent genome wrapper.
    /// </summary>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    public class IncumbentGenomeWrapper<TResult>
        where TResult : ResultBase<TResult>, new()
    {
        #region Public properties

        /// <summary>
        /// Gets or sets the incumbent genome.
        /// </summary>
        public Genome IncumbentGenome { get; set; }

        /// <summary>
        /// Gets or sets the incumbent instance results.
        /// </summary>
        public ImmutableList<TResult> IncumbentInstanceResults { get; set; }

        /// <summary>
        /// Gets or sets the incumbent generation.
        /// </summary>
        public int IncumbentGeneration { get; set; }

        #endregion
    }
}