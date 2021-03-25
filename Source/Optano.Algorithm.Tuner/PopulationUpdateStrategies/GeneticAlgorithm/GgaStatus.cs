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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.GeneticAlgorithm
{
    using System;
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Results;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Serialization;

    /// <summary>
    /// An object wrapping the current status of <see cref="GgaStrategy{TInstance,TResult}"/>.
    /// Can be serialized to a file and deserialized from one.
    /// </summary>
    public class GgaStatus : StatusBase
    {
        #region Constants

        /// <summary>
        /// File name to use for serialized data.
        /// </summary>
        public const string FileName = "ggaStatus.oatstat";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GgaStatus"/> class.
        /// </summary>
        /// <param name="population">The current population.</param>
        /// <param name="iterationCounter">
        /// The number of times
        /// <see cref="GgaStrategy{TInstance, TResult}.PerformIteration(int, IEnumerable{TInstance})"/> has been
        /// called in this GGA phase.
        /// </param>
        /// <param name="incumbentKeptCounter">
        /// The number of successive generations in which the incumbent has not changed.
        /// </param>
        /// <param name="allKnownRanks">All known ranks.</param>
        public GgaStatus(
            Population population,
            int iterationCounter,
            int incumbentKeptCounter,
            Dictionary<Genome, List<GenomeTournamentRank>> allKnownRanks)
        {
            if (incumbentKeptCounter < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(incumbentKeptCounter),
                    $"Incumbent kept counter should be non-negative, but is {iterationCounter}.");
            }

            if (incumbentKeptCounter > iterationCounter)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(incumbentKeptCounter),
                    $"Incumbent kept counter is {incumbentKeptCounter}, which is greater than the number of iterations ({iterationCounter})!");
            }

            this.AllKnownRanks = allKnownRanks ?? throw new ArgumentNullException(nameof(allKnownRanks));
            this.IterationCounter = iterationCounter;
            this.IncumbentKeptCounter = incumbentKeptCounter;
            this.Population = population;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets all known ranks.
        /// </summary>
        public Dictionary<Genome, List<GenomeTournamentRank>> AllKnownRanks { get; }

        /// <summary>
        /// Gets the current population.
        /// </summary>
        public Population Population { get; }

        /// <summary>
        /// Gets the number of times
        /// <see cref="GgaStrategy{TInstance, TResult}.PerformIteration(int, IEnumerable{TInstance})"/> has been
        /// called in this GGA phase.
        /// </summary>
        public int IterationCounter { get; }

        /// <summary>
        /// Gets the number of successive generations in which the incumbent has not changed.
        /// </summary>
        public int IncumbentKeptCounter { get; }

        #endregion
    }
}