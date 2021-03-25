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

namespace Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation
{
    using System;
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    /// <summary>
    /// Contains a pair of genome and instance.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    public class GenomeInstancePair<TInstance>
        where TInstance : InstanceBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GenomeInstancePair{TInstance}"/> class.
        /// </summary>
        /// <param name="genome">The genome.</param>
        /// <param name="instance">The instance.</param>
        public GenomeInstancePair(ImmutableGenome genome, TInstance instance)
        {
            this.Genome = genome ?? throw new ArgumentNullException(nameof(genome));
            this.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the genome.
        /// </summary>
        public ImmutableGenome Genome { get; }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public TInstance Instance { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks for equality.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>True, if equal.</returns>
        public static bool operator ==(GenomeInstancePair<TInstance> left, GenomeInstancePair<TInstance> right)
        {
            return object.Equals(left, right);
        }

        /// <summary>
        /// Checks for inequality.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>True, if not equal.</returns>
        public static bool operator !=(GenomeInstancePair<TInstance> left, GenomeInstancePair<TInstance> right)
        {
            return !object.Equals(left, right);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Instance: {this.Instance}{Environment.NewLine}Genome: {this.Genome}";
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(null, obj))
            {
                return false;
            }

            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((GenomeInstancePair<TInstance>)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.Genome != null ? this.Genome.GetHashCode() : 0) * 397) ^ EqualityComparer<TInstance>.Default.GetHashCode(this.Instance);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks for equality.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>True, if equal.</returns>
        protected bool Equals(GenomeInstancePair<TInstance> other)
        {
            return ImmutableGenome.GenomeComparer.Equals(this.Genome, other?.Genome) && object.Equals(this.Instance, other?.Instance);
        }

        #endregion
    }
}