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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// Messages signifying that the set of instances used for evaluation should be extended.
    /// </summary>
    /// <typeparam name="TInstance">
    /// Type of instance the target algorithm is able to process.
    /// </typeparam>
    public class AddInstances<TInstance>
        where TInstance : InstanceBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AddInstances{I}" /> class.
        /// </summary>
        /// <remarks>
        /// Internal because this should be used in combination with <see cref="ClearInstances"/> only.
        /// </remarks>
        /// <param name="instances">The instances to additionally use for evaluation from now on.</param>
        internal AddInstances(IEnumerable<TInstance> instances)
        {
            // Verify parameter.
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            // Set field. Make sure to create a new list to make the message immutable.
            this.Instances = instances.ToImmutableList();
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the additional instances to use from now on.
        /// </summary>
        public ImmutableList<TInstance> Instances { get; }

        #endregion
    }
}