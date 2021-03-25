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

namespace Optano.Algorithm.Tuner.TargetAlgorithm.Results
{
    using System;

    /// <summary>
    /// Result of a run of the <see cref="ITargetAlgorithm{TInstance, TResult}"/>.
    /// <para>
    /// Subtypes need to be immutable to guarantee thread-safety.
    /// </para>
    /// </summary>
    /// <typeparam name="TResultType">
    /// Type of results.
    /// </typeparam>
    public abstract class ResultBase<TResultType> : IResult
        where TResultType : ResultBase<TResultType>, new()
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultBase{TResultType}"/> class.
        /// </summary>
        /// <param name="runtime">
        /// The observed runtime.
        /// </param>
        protected ResultBase(TimeSpan runtime)
        {
            this.Runtime = runtime;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets the run's duration.
        /// </summary>
        public TimeSpan Runtime { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether the run was cancelled due to CPU timeout.
        /// </summary>
        public bool IsCancelled { get; protected set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates a <see cref="ResultBase{TResultType}"/> that represents a cancelled run.
        /// </summary>
        /// <param name="runtime">
        /// Run's duration before cancellation. 
        /// </param>
        /// <returns>
        /// A <see cref="ResultBase{TResultType}"/> representing a cancelled run.
        /// </returns>
        public static TResultType CreateCancelledResult(TimeSpan runtime)
        {
            return new TResultType() { IsCancelled = true, Runtime = runtime };
        }

        /// <summary>
        /// Returns the string representation of the object.
        /// <para>By default, the representation contains the <see cref="Runtime"/> and a note that the run was cancelled if
        /// that's the case. If an implementation has additional interesting values, it should overwrite this method to
        /// enable useful logs.</para>
        /// </summary>
        /// <returns>String representation of the object.</returns>
        public override string ToString()
        {
            return this.IsCancelled
                       ? FormattableString.Invariant($"Cancelled after {this.Runtime:G}")
                       : FormattableString.Invariant($"{this.Runtime:G}");
        }

        #endregion
    }
}