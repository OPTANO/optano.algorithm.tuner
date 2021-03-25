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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations
{
    using System;
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// A generic implementation of <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/> that takes a <see cref="Func{TResult}"/>
    /// to generate <see cref="ITargetAlgorithm{I, R}"/> regardless of the given parameters.
    /// </summary>
    /// <typeparam name="TTargetAlgorithm">Type of the <see cref="ITargetAlgorithm{I, R}"/> to generate.</typeparam>
    /// <typeparam name="TInstance">Type of instances the target algorithm accepts.
    /// Must be a subtype of <see cref="InstanceBase"/>.</typeparam>
    /// <typeparam name="TResult">Type of results the <see cref="ITargetAlgorithm{I, R}"/> produces.
    /// Must be a subtype of <see cref="ResultBase{TResultType}"/>.</typeparam>
    internal class TargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> : ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult>
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult> where TInstance : InstanceBase where TResult : ResultBase<TResult>, new()
    {
        #region Fields

        /// <summary>
        /// Function which generates an instance of the class <typeparamref name="TTargetAlgorithm"/>.
        /// </summary>
        private readonly Func<TTargetAlgorithm> _targetAlgorithmCreator;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetAlgorithmFactory{TTargetAlgorithm, TInstance, TResult}"/> class.
        /// </summary>
        /// <param name="targetAlgorithmCreator">Function which generates an instance oft he class
        /// <typeparamref name="TTargetAlgorithm"/>.</param>
        public TargetAlgorithmFactory(Func<TTargetAlgorithm> targetAlgorithmCreator)
        {
            this._targetAlgorithmCreator = targetAlgorithmCreator;
            this.CreatedTargetAlgorithms = new List<TTargetAlgorithm>();
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets all target algorithms that have been created by this factory.
        /// </summary>
        public List<TTargetAlgorithm> CreatedTargetAlgorithms { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Configures the target algorithm using the given parameters.
        /// In this case, the parameters are simply ignored.
        /// </summary>
        /// <param name="parameters">The parameters to configure the target algorithm with.
        /// Will be ignored.</param>
        /// <returns>The configured target algorithm.</returns>
        public TTargetAlgorithm ConfigureTargetAlgorithm(Dictionary<string, IAllele> parameters)
        {
            var targetAlgo = this._targetAlgorithmCreator.Invoke();
            this.CreatedTargetAlgorithms.Add(targetAlgo);

            return targetAlgo;
        }

        #endregion
    }
}