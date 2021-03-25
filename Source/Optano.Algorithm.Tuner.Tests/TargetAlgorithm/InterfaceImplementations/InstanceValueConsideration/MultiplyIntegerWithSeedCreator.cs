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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.InstanceValueConsideration
{
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    /// <summary>
    /// A minimal implementation of <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/>.
    /// </summary>
    public class MultiplyIntegerWithSeedCreator
        : ITargetAlgorithmFactory<MultiplyIntegerWithSeed, InstanceSeedFile, IntegerResult>
    {
        #region Public Methods and Operators

        /// <summary>
        /// Configures the target algorithm using the given parameters.
        /// </summary>
        /// <param name="parameters">The parameters to configure the target algorithm with.</param>
        /// <returns>The configured target algorithm.</returns>
        public MultiplyIntegerWithSeed ConfigureTargetAlgorithm(Dictionary<string, IAllele> parameters)
        {
            return new MultiplyIntegerWithSeed(parameters);
        }

        #endregion
    }
}