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

namespace Optano.Algorithm.Tuner.Tests.GenomeBuilders
{
    using System;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;

    /// <summary>
    /// A <see cref="GenomeBuilder"/> implementation with a configurable
    /// <see cref="GenomeBuilder.IsGenomeValid(Genome)"/> function.
    /// </summary>
    public class ConfigurableGenomeBuilder : GenomeBuilder
    {
        #region Fields

        /// <summary>
        /// Function to call when evaluating <see cref="GenomeBuilder.IsGenomeValid(Genome)"/>.
        /// </summary>
        private readonly Func<Genome, bool> _isValidFunction;

        /// <summary>
        /// Function to call when evaluating <see cref="GenomeBuilder.MakeGenomeValid(Genome)"/>.
        /// </summary>
        private readonly Action<Genome> _makeValidFunction;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurableGenomeBuilder"/> class.
        /// </summary>
        /// <param name="parameterTree">The parameters' structure.
        /// All genes created by this builder should comply with it.</param>
        /// <param name="isValidFunction">The function that checks whether or not a genome is valid.</param>
        /// <param name="mutationRate">Probability that a certain parameter is mutated when mutating a genome.</param>
        public ConfigurableGenomeBuilder(
            ParameterTree parameterTree,
            Func<Genome, bool> isValidFunction,
            double mutationRate)
            : this(parameterTree, isValidFunction, null, mutationRate)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurableGenomeBuilder"/> class.
        /// </summary>
        /// <param name="parameterTree">The parameters' structure.
        /// All genes created by this builder should comply with it.</param>
        /// <param name="isValidFunction">The function that checks whether or not a genome is valid.</param>
        /// <param name="makeValidFunction">The function which repairs a genome.</param>
        /// <param name="mutationRate">Probability that a certain parameter is mutated when mutating a genome.</param>
        public ConfigurableGenomeBuilder(
            ParameterTree parameterTree,
            Func<Genome, bool> isValidFunction,
            Action<Genome> makeValidFunction,
            double mutationRate)
            : base(
                parameterTree,
                new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder().SetMutationRate(mutationRate).Build(1))
        {
            this._isValidFunction = isValidFunction;
            this._makeValidFunction = makeValidFunction;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Decides whether the given genome is valid.
        /// </summary>
        /// <param name="genome">The genome to test.</param>
        /// <returns>False if the genome is invalid.</returns>
        public override bool IsGenomeValid(Genome genome)
        {
            return this._isValidFunction.Invoke(genome);
        }

        /// <summary>
        /// Tries to make the given genome valid.
        /// </summary>
        /// <param name="genome">Genome to make valid. Will be modified.</param>
        public override void MakeGenomeValid(Genome genome)
        {
            if (this._makeValidFunction == null)
            {
                base.MakeGenomeValid(genome);
            }
            else
            {
                this._makeValidFunction.Invoke(genome);
            }
        }

        #endregion
    }
}