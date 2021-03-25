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

namespace Optano.Algorithm.Tuner.ContinuousOptimization
{
    using System;
    using System.Globalization;
    using System.Linq;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.Serialization;

    /// <summary>
    /// A point in a multi-dimensional, real-valued search space.
    /// </summary>
    public class SearchPoint : IDeserializationRestorer<SearchPoint>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchPoint"/> class.
        /// </summary>
        /// <param name="values">The real-valued point to base this on.</param>
        public SearchPoint(Vector<double> values)
        {
            this.Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the real-value point this <see cref="SearchPoint"/> is based on.
        /// </summary>
        public Vector<double> Values { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks whether the <see cref="SearchPoint"/> is valid.
        /// </summary>
        /// <returns>Whether the <see cref="SearchPoint"/> is valid.</returns>
        public virtual bool IsValid()
        {
            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "<" + string.Join("; ", this.Values.AsArray().Select(value => value.ToString(CultureInfo.InvariantCulture))) + ">";
        }

        #endregion

        #region Explicit Interface Methods

        /// <summary>
        /// Restores an object after deserialization by <see cref="StatusBase"/>.
        /// </summary>
        /// <remarks>For example, this method might set equality definitions in dictionaries.</remarks>
        /// <returns>The object in restored state.</returns>
        SearchPoint IDeserializationRestorer<SearchPoint>.Restore()
        {
            // Internal state is easy to deserialize --> Nothing to do here.
            return this;
        }

        #endregion
    }
}