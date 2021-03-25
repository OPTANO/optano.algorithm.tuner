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

namespace Optano.Algorithm.Tuner.Tests.Parameters.Domains
{
    using System;
    using System.Globalization;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters.Domains;

    using Xunit;

    /// <summary>
    /// Defines tests that should be implemented for each <see cref="IDomain"/>.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public abstract class DomainBaseTest : IDisposable
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainBaseTest"/> class.
        /// </summary>
        protected DomainBaseTest()
        {
            ProcessUtils.SetDefaultCultureInfo(CultureInfo.InvariantCulture);
            Randomizer.Reset();
            Randomizer.Configure(new Random().Next());
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Resets the <see cref="Randomizer"/>.
        /// </summary>
        public void Dispose()
        {
            Randomizer.Reset();
        }

        /// <summary>
        /// Checks that <see cref="IDomain.DomainSize"/> correctly returns the domain's magnitude.
        /// </summary>
        public abstract void DomainSizeIsCorrect();

        /// <summary>
        /// Checks that <see cref="IDomain.MutateGeneValue(IAllele, double)"/> throws an
        /// <see cref="ArgumentException"/> if called with a type that is not a subtype of the domain objects' type.
        /// </summary>
        public abstract void MutateGeneValueThrowsExceptionForWrongType();

        /// <summary>
        /// Checks that <see cref="IDomain.MutateGeneValue(IAllele, double)"/> throws an
        /// <see cref="ArgumentException"/> if called with a gene value that is not contained in the domain.
        /// </summary>
        public abstract void MutateGeneValueThrowsExceptionForInvalidValue();

        /// <summary>
        /// Checks that <see cref="IDomain.ContainsGeneValue(IAllele)"/> returns false if called with a type
        /// that is not a subtype of the domain objects' type.
        /// </summary>
        public abstract void ContainsGeneValueReturnsFalseForWrongType();

        #endregion
    }
}