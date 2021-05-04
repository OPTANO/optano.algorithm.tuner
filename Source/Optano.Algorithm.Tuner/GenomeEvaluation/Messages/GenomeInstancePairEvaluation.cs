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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Messages
{
    using System;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// A message containing all necessary information about a genome instance pair evaluation.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    public class GenomeInstancePairEvaluation<TInstance>
        where TInstance : InstanceBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeInstancePairEvaluation{TInstance}"/> class.
        /// </summary>
        /// <param name="genomeInstancePair">The genome instance pair.</param>
        /// <param name="generationId">The generation id.</param>
        /// <param name="tournamentId">The tournament id.</param>
        /// <param name="useGrayBoxInEvaluation">Boolean indicating whether to use gray box tuning in current evaluation.</param>
        public GenomeInstancePairEvaluation(
            GenomeInstancePair<TInstance> genomeInstancePair,
            int generationId,
            int tournamentId,
            bool useGrayBoxInEvaluation)
        {
            this.GenomeInstancePair = genomeInstancePair ?? throw new ArgumentNullException(nameof(genomeInstancePair));
            this.GenerationId = generationId;
            this.TournamentId = tournamentId;
            this.UseGrayBoxInEvaluation = useGrayBoxInEvaluation;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome instance pair.
        /// </summary>
        public GenomeInstancePair<TInstance> GenomeInstancePair { get; }

        /// <summary>
        /// Gets the generation id.
        /// </summary>
        public int GenerationId { get; }

        /// <summary>
        /// Gets the tournament id.
        /// </summary>
        public int TournamentId { get; }

        /// <summary>
        /// Gets a value indicating whether to use gray box tuning in current evaluation.
        /// </summary>
        public bool UseGrayBoxInEvaluation { get; }

        #endregion
    }
}