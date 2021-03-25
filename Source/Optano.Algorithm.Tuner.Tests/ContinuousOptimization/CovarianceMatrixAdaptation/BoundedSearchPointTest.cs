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

namespace Optano.Algorithm.Tuner.Tests.ContinuousOptimization.CovarianceMatrixAdaptation
{
    using System;
    using System.Linq;

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.ContinuousOptimization.CovarianceMatrixAdaptation;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="BoundedSearchPoint"/> class.
    /// </summary>
    public class BoundedSearchPointTest : SearchPointTest
    {
        #region Fields

        /// <summary>
        /// Values used in tests.
        /// </summary>
        private readonly Vector<double> _values = Vector<double>.Build.Random(4);

        /// <summary>
        /// Lower bounds used in tests.
        /// </summary>
        private readonly double[] _lowerBounds = { 0.1, 0, -12.4, 7 };

        /// <summary>
        /// Upper bounds used in tests.
        /// </summary>
        private readonly double[] _upperBounds = { 104.56, 0.02, 0, 7 };

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="BoundedSearchPoint"/>s constructor throws a <see cref="ArgumentNullException"/>
        /// if called without lower bounds.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingLowerBounds()
        {
            Assert.Throws<ArgumentNullException>(() => new BoundedSearchPoint(this._values, lowerBounds: null, upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="BoundedSearchPoint"/>s constructor throws a <see cref="ArgumentNullException"/>
        /// if called without upper bounds.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingUpperBounds()
        {
            Assert.Throws<ArgumentNullException>(() => new BoundedSearchPoint(this._values, this._lowerBounds, upperBounds: null));
        }

        /// <summary>
        /// Checks that <see cref="BoundedSearchPoint"/>s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a number of lower bounds not fitting the
        /// dimension.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForWrongNumberOfLowerBounds()
        {
            var notEnoughLowerBounds = new[] { 0.1, 0, -12.4 };
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedSearchPoint(this._values, notEnoughLowerBounds, this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="BoundedSearchPoint"/>s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a number of upper bounds not fitting the
        /// dimension.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForWrongNumberOfUpperBounds()
        {
            var tooManyUpperBounds = new[] { 104.56, 0.02, 0, 7, 46 };
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedSearchPoint(this._values, this._lowerBounds, tooManyUpperBounds));
        }

        /// <summary>
        /// Checks that <see cref="BoundedSearchPoint"/>s constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a lower bound greater than its respective upper
        /// bound.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForLowerBoundHigherUpperBound()
        {
            this._lowerBounds[1] = this._upperBounds[1] + 0.1;
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedSearchPoint(this._values, this._lowerBounds, this._upperBounds));
        }

        /// <summary>
        /// Checks that the mapping works correctly.
        /// </summary>
        /// <remarks>Expected values were computed independently from this program.</remarks>
        [Fact]
        public void MapIntoBoundsWorksCorrectly()
        {
            var testValues = Vector<double>.Build.DenseOfArray(new[] { 105.56, -23, -6.2, 28 });
            var mapped = new BoundedSearchPoint(testValues, this._lowerBounds, this._upperBounds).MapIntoBounds();

            var tolerance = 1e-4;
            TestUtils.Equals(mapped[0], 61.4714, tolerance);
            TestUtils.Equals(mapped[1], 0.0041, tolerance);
            TestUtils.Equals(mapped[2], -3.9176, tolerance);
            TestUtils.Equals(mapped[3], 7, tolerance);
        }

        /// <summary>
        /// Checks that <see cref="BoundedSearchPoint.StandardizeValues"/> throws a <see cref="ArgumentNullException"/>
        /// if called without a set of values.
        /// </summary>
        [Fact]
        public void StandardizeValuesThrowsForMissingValueSet()
        {
            Assert.Throws<ArgumentNullException>(
                () => BoundedSearchPoint.StandardizeValues(
                    values: null,
                    lowerBounds: this._lowerBounds,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="BoundedSearchPoint.StandardizeValues"/> throws a <see cref="ArgumentNullException"/>
        /// if called without lower bounds.
        /// </summary>
        [Fact]
        public void StandardizeValuesThrowsForMissingLowerBounds()
        {
            Assert.Throws<ArgumentNullException>(
                () => BoundedSearchPoint.StandardizeValues(
                    this._values.ToArray(),
                    lowerBounds: null,
                    upperBounds: this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="BoundedSearchPoint.StandardizeValues"/> throws a <see cref="ArgumentNullException"/>
        /// if called without upper bounds.
        /// </summary>
        [Fact]
        public void StandardizeValuesThrowsForMissingUpperBounds()
        {
            Assert.Throws<ArgumentNullException>(
                () => BoundedSearchPoint.StandardizeValues(
                    this._values.ToArray(),
                    this._lowerBounds,
                    upperBounds: null));
        }

        /// <summary>
        /// Checks that <see cref="BoundedSearchPoint.StandardizeValues"/> throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a number of lower bounds not fitting the
        /// dimension.
        /// </summary>
        [Fact]
        public void StandardizeValuesThrowsForWrongNumberOfLowerBounds()
        {
            var notEnoughLowerBounds = new[] { 0.1, 0, -12.4 };
            Assert.Throws<ArgumentOutOfRangeException>(
                () => BoundedSearchPoint.StandardizeValues(
                    this._values.ToArray(),
                    notEnoughLowerBounds,
                    this._upperBounds));
        }

        /// <summary>
        /// Checks that <see cref="BoundedSearchPoint.StandardizeValues"/> constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a number of upper bounds not fitting the
        /// dimension.
        /// </summary>
        [Fact]
        public void StandardizeValuesThrowsForWrongNumberOfUpperBounds()
        {
            var tooManyUpperBounds = new[] { 104.56, 0.02, 0, 7, 46 };
            Assert.Throws<ArgumentOutOfRangeException>(
                () => BoundedSearchPoint.StandardizeValues(
                    this._values.ToArray(),
                    this._lowerBounds,
                    tooManyUpperBounds));
        }

        /// <summary>
        /// Checks that <see cref="BoundedSearchPoint.StandardizeValues"/> constructor throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with a lower bound greater than its respective upper
        /// bound.
        /// </summary>
        [Fact]
        public void StandardizeValuesThrowsForLowerBoundHigherUpperBound()
        {
            this._lowerBounds[1] = this._upperBounds[1] + 0.1;
            Assert.Throws<ArgumentOutOfRangeException>(
                () => BoundedSearchPoint.StandardizeValues(
                    this._values.ToArray(),
                    this._lowerBounds,
                    this._upperBounds));
        }

        /// <summary>
        /// Checks that executing <see cref="BoundedSearchPoint.MapIntoBounds"/> on values standardized via
        /// <see cref="BoundedSearchPoint.StandardizeValues"/> returns the original values.
        /// </summary>
        [Fact]
        public void MapIntoValuesUndoesStandardizeValues()
        {
            var boundedValues = new[] { 23.67, 0.01, -12.4, 7 };
            var standardized = BoundedSearchPoint.StandardizeValues(boundedValues, this._lowerBounds, this._upperBounds);
            var backIntoBounds =
                new BoundedSearchPoint(standardized, this._lowerBounds, this._upperBounds).MapIntoBounds();

            for (int i = 0; i < boundedValues.Length; i++)
            {
                Assert.Equal(boundedValues[i], backIntoBounds[i], 4);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a <see cref="SearchPoint"/>.
        /// </summary>
        /// <param name="dimension">The dimension the <see cref="SearchPoint"/> should have.</param>
        /// <param name="values">Values to base the <see cref="SearchPoint"/> on.</param>
        /// <returns>The created <see cref="SearchPoint"/>.</returns>
        protected override SearchPoint CreateSearchPoint(int dimension, Vector<double> values)
        {
            var lower = Enumerable.Range(0, dimension).Select(i => (double)i).ToArray();
            var upper = lower.Select(i => i * i).ToArray();
            return new BoundedSearchPoint(values, lower, upper);
        }

        #endregion
    }
}