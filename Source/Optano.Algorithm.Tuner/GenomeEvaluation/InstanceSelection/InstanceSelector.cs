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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection
{
    using System;
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// Responsible for choosing instances to evaluate genomes.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    public class InstanceSelector<TInstance>
        where TInstance : InstanceBase
    {
        #region Fields

        /// <summary>
        /// The maximum number of instances. It is used from generation <see cref="_goalGeneration" /> onwards.
        /// </summary>
        private readonly int _endNumInstances;

        /// <summary>
        /// The generation index after which <see cref="_endNumInstances" /> should always be returned.
        /// </summary>
        private readonly int _goalGeneration;

        /// <summary>
        /// All available instances.
        /// </summary>
        private readonly IReadOnlyList<TInstance> _instances;

        /// <summary>
        /// The factor used for linear increase of the number of instances depending on the generation.
        /// </summary>
        private readonly double _linearIncrease;

        /// <summary>
        /// The minimum number of instances. It is used at generation 0.
        /// </summary>
        private readonly int _startNumInstances;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceSelector{TInstance}"/> class.
        /// </summary>
        /// <param name="instances">
        /// All available instances.
        /// </param>
        /// <param name="configuration">
        /// Algorithm tuner configuration containing parameters needed by the selector,
        /// e.g. goal generation.
        /// </param>
        public InstanceSelector(IEnumerable<TInstance> instances, AlgorithmTunerConfiguration configuration)
        {
            this._instances = new List<TInstance>(instances).AsReadOnly();
            this._startNumInstances = configuration.StartNumInstances;
            this._endNumInstances = configuration.EndNumInstances;
            this._goalGeneration = configuration.GoalGeneration;
            this._linearIncrease =
                (double)(configuration.EndNumInstances - configuration.StartNumInstances) / configuration.GoalGeneration;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Randomly selects a subset of instances for the given generation.
        /// </summary>
        /// <param name="generation">
        /// Current generation, starting from 0.
        /// The higher this is, the more instances will be selected.
        /// </param>
        /// <exception cref="ArgumentException">Thrown if the given generation is negative.</exception>
        /// <returns>Instances to use for evaluation.</returns>
        public IEnumerable<TInstance> Select(int generation)
        {
            if (generation < 0)
            {
                throw new ArgumentException($"Generation index must be at least 0, but was {generation}.");
            }

            // Compute number instances depending on whether goal generation has been reached yet or not.
            var number = generation >= this._goalGeneration
                             ? this._endNumInstances
                             : (int)Math.Round((this._linearIncrease * generation) + this._startNumInstances);

            // Return random subset of the computed size.
            return Randomizer.Instance.ChooseRandomSubset(this._instances, number);
        }

        #endregion
    }
}