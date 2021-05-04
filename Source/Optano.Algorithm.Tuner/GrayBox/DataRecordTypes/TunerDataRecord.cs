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

namespace Optano.Algorithm.Tuner.GrayBox.DataRecordTypes
{
    using System.Linq;

    using Optano.Algorithm.Tuner.GrayBox.CsvRecorder;
    using Optano.Algorithm.Tuner.MachineLearning.GenomeRepresentation;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Contains data, that is given to the <see cref="DataRecorder{TResult}"/> from the tuner.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    public class TunerDataRecord<TResult>
        where TResult : ResultBase<TResult>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TunerDataRecord{TResult}"/> class.
        /// </summary>
        /// <param name="nodeId">The node ID.</param>
        /// <param name="generationId">The generation ID.</param>
        /// <param name="tournamentId">The tournament ID.</param>
        /// <param name="instanceId">The instance ID.</param>
        /// <param name="grayBoxConfidence">The gray box confidence.</param>
        /// <param name="genomeHeader">The genome header.</param>
        /// <param name="genome">The genome.</param>
        /// <param name="finalResult">The final result.</param>
        public TunerDataRecord(
            string nodeId,
            int generationId,
            int tournamentId,
            string instanceId,
            double grayBoxConfidence,
            string[] genomeHeader,
            GenomeDoubleRepresentation genome,
            TResult finalResult)
        {
            this.NodeId = nodeId;
            this.GenerationId = generationId;
            this.TournamentId = tournamentId;
            this.InstanceId = instanceId;
            this.GrayBoxConfidence = grayBoxConfidence;
            this.GenomeHeader = genomeHeader;
            this.Genome = genome;
            this.FinalResult = finalResult;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome header prefix.
        /// </summary>
        public static string GenomeHeaderPrefix => "Genome_";

        /// <summary>
        /// Gets the final result header prefix.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        public static string FinalResultHeaderPrefix => "FinalResult_";

        /// <summary>
        /// Gets the other header.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        public static string[] OtherHeader => new[]
                                                  {
                                                      "NodeID",
                                                      "GenerationID",
                                                      "TournamentID",
                                                      "RunID",
                                                      "InstanceID",
                                                      "GenomeID",
                                                      "GrayBoxConfidence",
                                                  };

        /// <summary>
        /// Gets the node ID.
        /// </summary>
        public string NodeId { get; }

        /// <summary>
        /// Gets the generation ID.
        /// </summary>
        public int GenerationId { get; }

        /// <summary>
        /// Gets the tournament ID.
        /// </summary>
        public int TournamentId { get; }

        /// <summary>
        /// Gets the run ID.
        /// </summary>
        public string RunId => $"{this.InstanceId}_{this.GenomeId}";

        /// <summary>
        /// Gets the instance ID.
        /// </summary>
        public string InstanceId { get; }

        /// <summary>
        /// Gets the genome ID.
        /// </summary>
        public string GenomeId => this.Genome.ToGenomeIdentifierStringRepresentation();

        /// <summary>
        /// Gets or sets the gray box confidence.
        /// </summary>
        public double GrayBoxConfidence { get; set; }

        /// <summary>
        /// Gets the Genome specific part of the header.
        /// </summary>
        public string[] GenomeHeader { get; }

        /// <summary>
        /// Gets the <see cref="GenomeDoubleRepresentation"/>.
        /// </summary>
        public GenomeDoubleRepresentation Genome { get; }

        /// <summary>
        /// Gets or sets the final result.
        /// </summary>
        public TResult FinalResult { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns the header.
        /// </summary>
        /// <returns>The header.</returns>
        public string[] GetHeader()
        {
            var genomeHeader = this.GenomeHeader.Select(header => $"{TunerDataRecord<TResult>.GenomeHeaderPrefix}{header}");
            var finalResultHeader = this.FinalResult.GetHeader().Select(header => $"{TunerDataRecord<TResult>.FinalResultHeaderPrefix}{header}");
            return TunerDataRecord<TResult>.OtherHeader.Concat(genomeHeader).Concat(finalResultHeader).ToArray();
        }

        /// <summary>
        /// Returns the string array representation.
        /// </summary>
        /// <returns>The string array representation.</returns>
        public string[] ToStringArray()
        {
            var genome = (double[])this.Genome;
            var genomeValues = genome.Select(g => $"{g:0.######}");
            var finalResultValues = this.FinalResult.ToStringArray();
            var otherValues = new[]
                                  {
                                      this.NodeId,
                                      this.GenerationId.ToString(),
                                      this.TournamentId.ToString(),
                                      this.RunId,
                                      this.InstanceId,
                                      this.GenomeId,
                                      $"{this.GrayBoxConfidence:0.######}",
                                  };
            return otherValues.Concat(genomeValues).Concat(finalResultValues).ToArray();
        }

        /// <summary>
        /// Copies the current tuner data record.
        /// </summary>
        /// <returns>The copied tuner data record.</returns>
        public TunerDataRecord<TResult> Copy()
        {
            return new TunerDataRecord<TResult>(
                this.NodeId,
                this.GenerationId,
                this.TournamentId,
                this.InstanceId,
                this.GrayBoxConfidence,
                this.GenomeHeader,
                this.Genome,
                this.FinalResult);
        }

        #endregion
    }
}