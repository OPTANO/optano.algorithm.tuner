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

namespace Optano.Algorithm.Tuner.MachineLearning.TrainingData.SamplingStrategies
{
    using System.Linq;

    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.MachineLearning.Prediction;

    /// <summary>
    /// For each (unique) <see cref="Genome"/>, all observed performances are aggregated using the average over all ranks.
    /// </summary>
    public class AverageRankStrategy : IEnsembleSamplingStrategy<IGenomePredictor>
    {
        #region Public Methods and Operators

        /// <summary>
        /// No params are used.
        /// </summary>
        /// <param name="args">
        /// Arguments are ignored.
        /// </param>
        public void Initialize(params object[] args)
        {
            return;
        }

        /// <summary>
        /// Computes the average <see cref="GenomeTournamentRank.TournamentRank"/> over all <see cref="GenomeTournamentRank"/> for each <see cref="Genome"/> in <see cref="TrainingDataWrapper.Genomes"/>.
        /// </summary>
        /// <param name="data">
        /// The training data.
        /// </param>
        /// <returns>
        /// The aggregated training data.
        /// </returns>
        public AggregatedTrainingDataWrapper AggregateTargets(TrainingDataWrapper data)
        {
            var targets = data.Genomes.Select(g => data.TournamentResults[g].Average(r => r.TournamentRank)).ToArray();

            var result = new AggregatedTrainingDataWrapper() { RelevantConvertedGenomes = data.ConvertedGenomes, RelevantTargets = targets };

            return result;
        }

        /// <summary>
        /// Model is not altered.
        /// </summary>
        /// <param name="newModel">
        /// Parameter is ignored.
        /// </param>
        public void PostProcessModel(IEnsemblePredictor<IGenomePredictor> newModel)
        {
            return;
        }

        #endregion
    }
}