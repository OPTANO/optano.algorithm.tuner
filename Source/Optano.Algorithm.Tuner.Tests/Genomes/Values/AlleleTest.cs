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

namespace Optano.Algorithm.Tuner.Tests.Genomes.Values
{
    using Optano.Algorithm.Tuner.Genomes.Values;

    using Xunit;

    /// <summary>
    /// Contains tests for class <see cref="Allele{T}"/>.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public class AlleleTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that the value set in the constructor is also returned by <see cref="Allele{T}.GetValue"/>.
        /// </summary>
        [Fact]
        public void ValueIsStoredCorrectly()
        {
            int a = 4;
            var allele = new Allele<int>(a);
            Assert.Equal(a, allele.GetValue());
        }

        #endregion
    }
}