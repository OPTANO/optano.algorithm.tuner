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

    using MathNet.Numerics.LinearAlgebra;

    using Optano.Algorithm.Tuner.ContinuousOptimization;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="SearchPoint"/> class.
    /// </summary>
    public class SearchPointTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="SearchPoint"/>'s constructor throws a <see cref="ArgumentNullException"/> if called
        /// without values.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingValues()
        {
            Assert.Throws<ArgumentNullException>(() => this.CreateSearchPoint(3, values: null));
        }

        /// <summary>
        /// Checks that <see cref="SearchPoint.Values"/> returns the values provided on initialization.
        /// </summary>
        [Fact]
        public void ValuesAreSetCorrectly()
        {
            var values = Vector<double>.Build.Random(3);
            var searchPoint = this.CreateSearchPoint(values.Count, values);
            Assert.Equal(values, searchPoint.Values);
        }

        /// <summary>
        /// Checks that <see cref="SearchPoint.ToString"/> returns the values in format &lt;value1; value2&gt;.
        /// </summary>
        [Fact]
        public void ToStringPrintsValues()
        {
            var values = Vector<double>.Build.DenseOfArray(new[] { 7.2, 4.8, 3 });
            var searchPoint = this.CreateSearchPoint(values.Count, values);
            Assert.Equal("<7.2; 4.8; 3>", searchPoint.ToString());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a <see cref="SearchPoint"/>.
        /// </summary>
        /// <param name="dimension">The dimension the <see cref="SearchPoint"/> should have.</param>
        /// <param name="values">Values to base the <see cref="SearchPoint"/> on.</param>
        /// <returns>The created <see cref="SearchPoint"/>.</returns>
        protected virtual SearchPoint CreateSearchPoint(int dimension, Vector<double> values)
        {
            return new SearchPoint(values);
        }

        #endregion
    }
}