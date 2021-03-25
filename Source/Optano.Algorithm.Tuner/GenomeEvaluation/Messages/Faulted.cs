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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Messages
{
    using System;

    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// Represents a faulted evaluation result.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    public class Faulted<TInstance>
        where TInstance : InstanceBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Faulted{TInstance}"/> class.
        /// </summary>
        /// <param name="genomeInstancePair">The genome instance pair.</param>
        /// <param name="reason">The reason.</param>
        public Faulted(GenomeInstancePair<TInstance> genomeInstancePair, Exception reason)
        {
            this.GenomeInstancePair = genomeInstancePair;
            this.Reason = reason;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome instance pair.
        /// </summary>
        public GenomeInstancePair<TInstance> GenomeInstancePair { get; }

        /// <summary>
        /// Gets the reason.
        /// </summary>
        public Exception Reason { get; }

        #endregion
    }
}