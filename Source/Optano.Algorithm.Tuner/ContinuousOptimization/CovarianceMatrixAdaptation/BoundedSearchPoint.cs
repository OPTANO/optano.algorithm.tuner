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

namespace Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation
{
    using System;
    using System.Collections.Generic;

    using MathNet.Numerics.LinearAlgebra;

    /// <summary>
    /// A <see cref="SearchPoint"/> in real vector space which supports the transformations from and to a search space
    /// with box-constraints.
    /// </summary>
    public class BoundedSearchPoint : SearchPoint
    {
        #region Fields

        /// <summary>
        /// Lower bounds by dimension.
        /// </summary>
        private readonly double[] _lowerBounds;

        /// <summary>
        /// Upper bounds by dimension.
        /// </summary>
        private readonly double[] _upperBounds;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundedSearchPoint"/> class.
        /// </summary>
        /// <param name="values">
        /// The real-valued point to base this on.
        /// This is the internal representation, not the one in the boxed search space.
        /// <para>Use <see cref="StandardizeValues"/> to transform points from the actual search space.</para>
        /// </param>
        /// <param name="lowerBounds">The lower bounds by dimension.</param>
        /// <param name="upperBounds">The upper bounds by dimension.</param>
        public BoundedSearchPoint(Vector<double> values, double[] lowerBounds, double[] upperBounds)
            : base(values)
        {
            ValidateBounds(lowerBounds, upperBounds, dimension: values.Count);

            this._lowerBounds = lowerBounds;
            this._upperBounds = upperBounds;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Standardizes box-constrained values from the search space s.t. they are in [0, 10].
        /// <para>Executing <see cref="MapIntoBounds"/> on standardized values returns the original values.</para>
        /// <remarks>
        /// Mapping is executed as specified in https://www.lri.fr/~hansen/cmaes_inmatlab.html#practical.
        /// </remarks>
        /// </summary>
        /// <param name="values">The values to standardize.</param>
        /// <param name="lowerBounds">The lower bounds by dimension.</param>
        /// <param name="upperBounds">The upper bounds by dimension.</param>
        /// <returns>The standardized values.</returns>
        public static Vector<double> StandardizeValues(
            IReadOnlyList<double> values,
            double[] lowerBounds,
            double[] upperBounds)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            ValidateBounds(lowerBounds, upperBounds, values.Count);

            var standardizedValues = Vector<double>.Build.Dense(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                // Prevent division by 0:
                // If there is only one possible value anyway, we can just map to an arbitrary value in [0, 10].
                var domainInterval = upperBounds[i] - lowerBounds[i];
                if (domainInterval == 0)
                {
                    standardizedValues[i] = 0;
                    continue;
                }

                // Else: Map the value into [-1, 1]...
                var scaled = 1 - (2 * ((values[i] - lowerBounds[i]) / domainInterval));
                // ...and into [0, 10] afterwards.
                standardizedValues[i] = 10 * (Math.Acos(scaled) / Math.PI);
            }

            return standardizedValues;
        }

        /// <summary>
        /// Maps the <see cref="SearchPoint"/> into its box-constrained search space.
        /// </summary>
        /// <remarks>
        /// Mapping is executed as specified in https://www.lri.fr/~hansen/cmaes_inmatlab.html#practical.
        /// </remarks>
        /// <returns>The mapped point.</returns>
        public Vector<double> MapIntoBounds()
        {
            var bounded = Vector<double>.Build.Dense(this.Values.Count);
            for (int i = 0; i < this.Values.Count; i++)
            {
                // Map the value into [0, 1]...
                var scaled = (1 - Math.Cos(Math.PI * (this.Values[i] / 10))) / 2;
                // ...and rescale to actual size.
                bounded[i] = this._lowerBounds[i] + ((this._upperBounds[i] - this._lowerBounds[i]) * scaled);
            }

            return bounded;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks that the bounds are valid, i.e.
        /// they are not null,
        /// they have the correct dimension,
        /// they are not infinite,
        /// and the lower bound is not greater than the upper one.
        /// </summary>
        /// <param name="lowerBounds">The lower bounds.</param>
        /// <param name="upperBounds">The upper bounds.</param>
        /// <param name="dimension">The dimension of the <see cref="SearchPoint"/>.</param>
        private static void ValidateBounds(double[] lowerBounds, double[] upperBounds, int dimension)
        {
            if (lowerBounds == null)
            {
                throw new ArgumentNullException(nameof(lowerBounds));
            }

            if (upperBounds == null)
            {
                throw new ArgumentNullException(nameof(lowerBounds));
            }

            if (lowerBounds.Length != dimension)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lowerBounds),
                    $"Expected {dimension} lower bounds, but got {lowerBounds.Length}.");
            }

            if (upperBounds.Length != dimension)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(upperBounds),
                    $"Expected {dimension} upper bounds, but got {upperBounds.Length}.");
            }

            for (int i = 0; i < dimension; i++)
            {
                if (lowerBounds[i] > upperBounds[i])
                {
                    throw new ArgumentOutOfRangeException($"{i}th lower bound is greater than {i}th upper bound.");
                }

                if (double.IsNegativeInfinity(lowerBounds[i]))
                {
                    throw new ArgumentOutOfRangeException(nameof(lowerBounds), $"{i}th lower bound is unbounded.");
                }

                if (double.IsPositiveInfinity(upperBounds[i]))
                {
                    throw new ArgumentOutOfRangeException(nameof(upperBounds), $"{i}th upper bound is unbounded.");
                }
            }
        }

        #endregion
    }
}