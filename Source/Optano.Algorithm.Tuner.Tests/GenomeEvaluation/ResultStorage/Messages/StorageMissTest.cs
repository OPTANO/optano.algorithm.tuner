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

namespace Optano.Algorithm.Tuner.Tests.GenomeEvaluation.ResultStorage.Messages
{
    using System;

    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="StorageMiss{TInstance}"/> class.
    /// </summary>
    public class StorageMissTest
    {
        #region Static Fields

        /// <summary>
        /// An empty <see cref="ImmutableGenome"/>.
        /// </summary>
        private static readonly ImmutableGenome genome = new ImmutableGenome(new Genome());

        /// <summary>
        /// A simple <see cref="TestInstance"/>.
        /// </summary>
        private static readonly TestInstance testInstance = new TestInstance("1");

        #endregion

        #region Fields

        /// <summary>
        /// <see cref="StorageMiss{TestInstance}"/> to use in tests. Needs to be initialized.
        /// </summary>
        private readonly StorageMiss<TestInstance> _storageMissMessage;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageMissTest"/> class.
        /// </summary>
        public StorageMissTest()
        {
            this._storageMissMessage = new StorageMiss<TestInstance>(StorageMissTest.genome, StorageMissTest.testInstance);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Verifies that calling <see cref="StorageMiss{TestInstance}"/>'s constructor without providing a
        /// genome throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingGenome()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StorageMiss<TestInstance>(genome: null, instance: StorageMissTest.testInstance));
        }

        /// <summary>
        /// Verifies that calling <see cref="StorageMiss{TestInstance}"/>'s constructor without providing an
        /// instance throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsExceptionOnMissingInstance()
        {
            Assert.Throws<ArgumentNullException>(
                () => new StorageMiss<TestInstance>(StorageMissTest.genome, instance: null));
        }

        /// <summary>
        /// Checks that <see cref="StorageMiss{I}.Genome"/> returns the genome that was used for initialization.
        /// </summary>
        [Fact]
        public void GenomeIsSetCorrectly()
        {
            Assert.Equal(StorageMissTest.genome, this._storageMissMessage.Genome);
        }

        /// <summary>
        /// Checks that <see cref="StorageMiss{I}.Instance"/> returns the same instance it was initialized with.
        /// </summary>
        [Fact]
        public void InstanceIsSetCorrectly()
        {
            Assert.Equal(StorageMissTest.testInstance, this._storageMissMessage.Instance);
        }

        #endregion
    }
}