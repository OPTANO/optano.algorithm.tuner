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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration
{
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.TargetAlgorithm;

    /// <summary>
    /// An implementation of <see cref="ITargetAlgorithmFactory{TTargetAlgorithm,TInstance,TResult}"/>
    /// that forwards all parameters to the new <see cref="ExtractIntegerValue"/> instance.
    /// </summary>
    public class ExtractIntegerValueCreator : ITargetAlgorithmFactory<ExtractIntegerValue, TestInstance, IntegerResult>
    {
        #region Public Methods and Operators

        /// <summary>
        /// Configures the target algorithm using the given parameters.
        /// </summary>
        /// <param name="parameters">The parameters to configure the target algorithm with.
        /// Should include an integer value with key <see cref="ExtractIntegerValue.ParameterName"/>.</param>
        /// <returns>The configured target algorithm.</returns>
        public ExtractIntegerValue ConfigureTargetAlgorithm(Dictionary<string, IAllele> parameters)
        {
            return new ExtractIntegerValue(parameters);
        }

        #endregion
    }
}