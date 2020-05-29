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

namespace Optano.Algorithm.Tuner.Tests.ContinuousOptimization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.Serialization;

    /// <summary>
    /// An implementation of <see cref="ISearchPointSorter{TSearchPoint}"/> which minimizes a function.
    /// </summary>
    internal class MinimizeFunction : SearchPointSorterBase<SearchPoint>
    {
        #region Fields

        /// <summary>
        /// The function to minimize.
        /// </summary>
        private readonly Expression<Func<Vector<double>, double>> _objectiveFunction;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MinimizeFunction"/> class.
        /// </summary>
        /// <param name="objectiveFunction">The function to minimize.</param>
        public MinimizeFunction(Expression<Func<Vector<double>, double>> objectiveFunction)
        {
            this._objectiveFunction = objectiveFunction;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Sorts the points by first value in vector representation.
        /// </summary>
        /// <param name="points">The <see cref="SearchPoint"/>s to sort.</param>
        /// <returns>Indices of sorted points, best points first.</returns>
        public override IList<int> Sort(IList<SearchPoint> points)
        {
            return points
                .Select((point, idx) => new { point.Values, idx })
                .OrderBy(vectorAndIdx => this._objectiveFunction.Compile().Invoke(vectorAndIdx.Values))
                .Select(vectorAndIdx => vectorAndIdx.idx)
                .ToList();
        }

        /// <summary>
        /// Restores an object after deserialization by <see cref="StatusBase"/>.
        /// </summary>
        /// <remarks>For example, this method might set equality definitions in dictionaries.</remarks>
        /// <returns>The object in restored state.</returns>
        public ISearchPointSorter<SearchPoint> Restore()
        {
            // No complex internal state --> nothing to do here.
            return this;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return this._objectiveFunction.ToString();
        }

        #endregion
    }
}