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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages
{
    using System;

    using Optano.Algorithm.Tuner.Genomes;

    /// <summary>
    /// Message signifying a request for target algorithm run results on a specific genome.
    /// </summary>
    public class GenomeResultsRequest
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeResultsRequest" /> class.
        /// </summary>
        /// <param name="genome">The genome the run results are requested for.</param>
        public GenomeResultsRequest(ImmutableGenome genome)
        {
            if (genome == null)
            {
                throw new ArgumentNullException(nameof(genome));
            }

            this.Genome = genome;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome the run results are requested for.
        /// </summary>
        public ImmutableGenome Genome { get; }

        #endregion
    }
}