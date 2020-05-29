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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages
{
    using System;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// Message signifying that the requested entry was not found in storage.
    /// </summary>
    /// <typeparam name="TInstance">Type of the instances the target algorithm can be run on.</typeparam>
    public class StorageMiss<TInstance>
        where TInstance : InstanceBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageMiss{I}" /> class.
        /// </summary>
        /// <param name="genome">The genome that should have been used to configure the target algorithm.</param>
        /// <param name="instance">The instance the target algorithm should have been run on.</param>
        public StorageMiss(ImmutableGenome genome, TInstance instance)
        {
            // Validate parameters
            if (genome == null)
            {
                throw new ArgumentNullException("genome");
            }

            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            // Set them.
            this.Genome = genome;
            this.Instance = instance;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome that should have been used to configure the target algorithm.
        /// </summary>
        public ImmutableGenome Genome { get; }

        /// <summary>
        /// Gets the instance the target algorithm should have run on.
        /// </summary>
        public TInstance Instance { get; }

        #endregion
    }
}