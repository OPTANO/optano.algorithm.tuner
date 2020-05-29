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

namespace Optano.Algorithm.Tuner.TargetAlgorithm.Results
{
    using System;

    /// <summary>
    /// An implementation of <see cref="ResultBase{TResultType}" /> that holds a single continuous value.
    /// </summary>
    public class ContinuousResult : ResultBase<ContinuousResult>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuousResult" /> class.
        /// </summary>
        /// <param name="value">The value it should hold.</param>
        /// <param name="runtime">The runtime.</param>
        public ContinuousResult(double value, TimeSpan runtime)
            : base(runtime)
        {
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuousResult"/> class. 
        /// Empty ctor required for <see cref="ResultBase{TResultType}.CreateCancelledResult"/>.
        /// </summary>
        public ContinuousResult()
            : this(double.NaN, TimeSpan.MaxValue)
        {
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the continuous value.
        /// </summary>
        public double Value { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns the string representation of the object.
        /// </summary>
        /// <returns>String representation of the object.</returns>
        public override string ToString()
        {
            return this.IsCancelled
                       ? FormattableString.Invariant($"Cancelled after {this.Runtime:G} with value set to {this.Value}")
                       : FormattableString.Invariant($"Runtime: {this.Runtime:G}, Value: {this.Value}");
        }

        #endregion
    }
}