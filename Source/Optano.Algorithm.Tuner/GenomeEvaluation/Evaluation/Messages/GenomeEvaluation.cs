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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation.Messages
{
    using System;

    using Optano.Algorithm.Tuner.Genomes;

    /// <summary>
    /// Message describing one genome evaluation that has to be executed.
    /// </summary>
    public class GenomeEvaluation
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeEvaluation" /> class.
        /// </summary>
        /// <param name="genome">The <see cref="ImmutableGenome" /> to evaluate.</param>
        /// <param name="evaluationId">The evaluation identifier to use in responses.</param>
        public GenomeEvaluation(ImmutableGenome genome, int evaluationId)
        {
            this.Genome = genome ?? throw new ArgumentNullException(nameof(genome));
            this.EvaluationId = evaluationId;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome to evaluate.
        /// </summary>
        public ImmutableGenome Genome { get; }

        /// <summary>
        /// Gets the evaluation identifier to use in responses.
        /// </summary>
        public int EvaluationId { get; }

        #endregion
    }
}