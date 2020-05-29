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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.InstanceValueConsideration
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    /// <summary>
    /// A simple testing algorithm that uses both instance seeds and parameters.
    /// </summary>
    public class MultiplyIntegerWithSeed : ITargetAlgorithm<InstanceSeedFile, IntegerResult>
    {
        #region Fields

        /// <summary>
        /// All parameters this algorithm was configured with.
        /// </summary>
        private readonly Dictionary<string, IAllele> _parameters;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiplyIntegerWithSeed"/> class.
        /// </summary>
        /// <param name="parameters">The parameters to configure with.
        /// Should include an integer parameter with key <see cref="ExtractIntegerValue.ParameterName"/>.</param>
        public MultiplyIntegerWithSeed(Dictionary<string, IAllele> parameters)
        {
            this._parameters = parameters;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates a cancellable task that runs the algorithm on the given instance.
        /// <para>For this class, the task will initialize a new instance of the <see cref="IntegerResult"/> class with
        /// the product of the instance seed and the value of the parameter with key
        /// <see cref="ExtractIntegerValue.ParameterName"/> stored in <see cref="_parameters"/>.
        /// </para>
        /// </summary>
        /// <param name="instance">Instance to run on.</param>
        /// <param name="cancellationToken">Token that should be regurlary checked for cancellation.
        /// If cancellation is detected, the task has to be stopped.</param>
        /// <returns>A task that will initialize a new instance of the <see cref="IntegerResult"/> class with the
        /// product of the instance seed and the value of the parameter with key
        /// <see cref="ExtractIntegerValue.ParameterName"/> stored in <see cref="_parameters"/>.</returns>
        public virtual Task<IntegerResult> Run(InstanceSeedFile instance, CancellationToken cancellationToken)
        {
            return Task.Run(
                () => new IntegerResult(
                    instance.Seed * (int)this._parameters[ExtractIntegerValue.ParameterName].GetValue()));
        }

        #endregion
    }
}