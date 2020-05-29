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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.Instances
{
    using System;

    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="InstanceSeedFile"/> class.
    /// </summary>
    public class InstanceSeedFileTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="InstanceSeedFile"/> throws an <see cref="ArgumentNullException"/> if no path is
        /// provided.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnPathNull()
        {
            Assert.Throws<ArgumentNullException>(() => new InstanceSeedFile(null, 0));
        }

        /// <summary>
        /// Checks that <see cref="InstanceFile.Path"/> returns the name provided on construction.
        /// </summary>
        [Fact]
        public void PathIsSetCorrectly()
        {
            var instance = new InstanceSeedFile("bar/foo", 0);
            Assert.Equal("bar/foo", instance.Path);
        }

        /// <summary>
        /// Checks that <see cref="InstanceSeedFile.Seed"/> returns the seed provided on construction.
        /// </summary>
        [Fact]
        public void SeedIsSetCorrectly()
        {
            var instance = new InstanceSeedFile("foo", 42);
            Assert.Equal(42, instance.Seed);
        }

        /// <summary>
        /// Checks that <see cref="InstanceSeedFile.ToString"/> returns the path provided on construction.
        /// </summary>
        [Fact]
        public void ToStringReturnsPathAndSeed()
        {
            var instance = new InstanceSeedFile("bar/foo", 42);
            Assert.Equal("bar/foo_42", instance.ToString());
        }

        /// <summary>
        /// Checks that <see cref="object.Equals(object, object)"/> returns false if
        /// the instances' paths are different.
        /// </summary>
        [Fact]
        public void EqualsReturnsFalseForDifferentPath()
        {
            var firstInstance = new InstanceSeedFile("1", 42);
            var secondInstance = new InstanceSeedFile("2", 42);
            Assert.False(
                object.Equals(firstInstance, secondInstance),
                $"Instance {firstInstance} should not be equal to instance {secondInstance}.");
        }

        /// <summary>
        /// Checks that <see cref="object.Equals(object, object)"/> returns false if
        /// the instances' seeds are different.
        /// </summary>
        [Fact]
        public void EqualsReturnsFalseForDifferentSeed()
        {
            var firstInstance = new InstanceSeedFile("1", 42);
            var secondInstance = new InstanceSeedFile("1", 43);
            Assert.False(
                object.Equals(firstInstance, secondInstance),
                $"Instance {firstInstance} should not be equal to instance {secondInstance}.");
        }

        /// <summary>
        /// Checks that <see cref="object.Equals(object, object)"/> returns false if
        /// the first parameter is not an <see cref="InstanceSeedFile"/> and the second one is.
        /// </summary>
        [Fact]
        public void EqualsReturnsFalseForOneObjectNotInstanceSeedFile()
        {
            var instance = new InstanceSeedFile("1", 0);
            var wrongType = new InstanceFile("1");
            Assert.False(
                object.Equals(instance, wrongType),
                $"Instance {instance} was identified to be equal to {wrongType}.");
            Assert.False(
                object.Equals(wrongType, instance),
                $"Instance {wrongType} was identified to be equal to {instance}.");
        }

        /// <summary>
        /// Checks that <see cref="object.Equals(object, object)"/> is true for
        /// two <see cref="InstanceSeedFile"/>s with same paths, but pointing to different objects.
        /// </summary>
        [Fact]
        public void EqualsReturnsTrueForEqualPathAndSeed()
        {
            var path = "1";
            var seed = 42;
            var instance1 = new InstanceSeedFile(path, seed);
            var instance2 = new InstanceSeedFile(path, seed);
            Assert.True(
                object.Equals(instance1, instance2),
                $"{instance1} and {instance2} are supposedly different, but both encode instance {path}.");
        }

        /// <summary>
        /// Checks that <see cref="InstanceSeedFile.GetHashCode"/> is equal for two
        /// <see cref="InstanceSeedFile"/>s storing the same path.
        /// </summary>
        [Fact]
        public void GetHashCodeReturnsSameHashCodesForEqualPathAndSeed()
        {
            var path = "1";
            var seed = 42;
            var instance1 = new InstanceSeedFile(path, seed);
            var instance2 = new InstanceSeedFile(path, seed);

            var firstInstanceHash = instance1.GetHashCode();
            var secondInstanceHash = instance2.GetHashCode();
            Assert.Equal(
                firstInstanceHash,
                secondInstanceHash);
        }

        /// <summary>
        /// Checks that <see cref="InstanceSeedFile.GetHashCode"/> is different for two
        /// <see cref="InstanceSeedFile"/>s storing different file names.
        /// Of course, this does not have to be the case necessarily, but it's still a nice property that should be
        /// true in most cases.
        /// </summary>
        [Fact]
        public void GetHashCodeReturnsDifferentHashCodesForDifferentPaths()
        {
            var instance1 = new InstanceSeedFile("1", 42);
            var instance2 = new InstanceSeedFile("2", 42);
            Assert.False(object.Equals(instance1, instance2));

            var firstInstanceHash = instance1.GetHashCode();
            var secondInstanceHash = instance2.GetHashCode();
            Assert.NotEqual(
                firstInstanceHash,
                secondInstanceHash);
        }

        /// <summary>
        /// Checks that <see cref="InstanceSeedFile.GetHashCode"/> is different for two
        /// <see cref="InstanceSeedFile"/>s storing different seeds.
        /// Of course, this does not have to be the case necessarily, but it's still a nice property that should be
        /// true in most cases.
        /// </summary>
        [Fact]
        public void GetHashCodeReturnsDifferentHashCodesForDifferentSeeds()
        {
            var instance1 = new InstanceSeedFile("1", 42);
            var instance2 = new InstanceSeedFile("1", 43);
            Assert.False(object.Equals(instance1, instance2));

            var firstInstanceHash = instance1.GetHashCode();
            var secondInstanceHash = instance2.GetHashCode();
            Assert.NotEqual(
                firstInstanceHash,
                secondInstanceHash);
        }

        #endregion
    }
}