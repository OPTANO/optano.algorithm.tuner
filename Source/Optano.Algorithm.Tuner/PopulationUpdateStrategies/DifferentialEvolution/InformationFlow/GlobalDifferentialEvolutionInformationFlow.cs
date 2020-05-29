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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.InformationFlow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.SearchPoint;

    /// <summary>
    /// Defines the information flow between the overall tuning process and a
    /// <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/> instance which acts on the complete population,
    /// i.e. fixes different discrete parameters for each individual.
    /// </summary>
    /// <remarks>
    /// This information flow strategy has the advantage that it exchanges lots of information.
    /// On the negative side, evaluating the same continuous parameter set in combination with different discrete sets
    /// may exhibit very different performance, violating some assumptions of DE.
    /// Note that, if no discrete parameters are used, this disadvantage does not matter.
    /// </remarks>
    /// <seealso cref="LocalDifferentialEvolutionInformationFlow"/>
    public class GlobalDifferentialEvolutionInformationFlow : IInformationFlowStrategy<GenomeSearchPoint>
    {
        #region Fields

        /// <summary>
        /// Detailed options about this strategy.
        /// </summary>
        private readonly DifferentialEvolutionStrategyConfiguration _strategyConfiguration;

        /// <summary>
        /// The structure representing the tunable parameters.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// The <see cref="GenomeBuilder" /> used in tuning.
        /// </summary>
        private readonly GenomeBuilder _genomeBuilder;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalDifferentialEvolutionInformationFlow"/> class.
        /// </summary>
        /// <param name="strategyConfiguration">
        /// Options used for the <see cref="DifferentialEvolutionStrategy{TInstance,TResult}"/>.
        /// </param>
        /// <param name="parameterTree">Provides the tunable parameters.</param>
        /// <param name="genomeBuilder">Responsible for creation, modification and crossover of genomes.</param>
        public GlobalDifferentialEvolutionInformationFlow(
            DifferentialEvolutionStrategyConfiguration strategyConfiguration,
            ParameterTree parameterTree,
            GenomeBuilder genomeBuilder)
        {
            this._strategyConfiguration = strategyConfiguration ?? throw new ArgumentNullException(nameof(strategyConfiguration));
            this._parameterTree = parameterTree ?? throw new ArgumentNullException(nameof(parameterTree));
            this._genomeBuilder = genomeBuilder ?? throw new ArgumentNullException(nameof(genomeBuilder));
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public IEnumerable<GenomeSearchPoint> DetermineInitialPoints(Population basePopulation, Genome currentIncumbent)
        {
            if (basePopulation.CompetitiveCount < 3)
            {
                throw new ArgumentException(
                    $"JADE needs at least 3 individuals to work, but the competitive population only has {basePopulation.CompetitiveCount}.");
            }

            return basePopulation
                .GetCompetitiveIndividuals()
                .Select(
                    genome => GenomeSearchPoint.CreateFromGenome(
                        genome,
                        this._parameterTree,
                        this._strategyConfiguration.MinimumDomainSize,
                        this._genomeBuilder));
        }

        /// <inheritdoc />
        public IEnumerable<Genome> DefineCompetitivePopulation(
            IReadOnlyList<Genome> originalCompetitives,
            Genome originalIncumbent,
            IList<GenomeSearchPoint> mostRecentSorting)
        {
            return mostRecentSorting.Select(point => point.Genome.CreateMutableGenome());
        }

        #endregion
    }
}