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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm.Instances
{
    using System;

    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="InstanceFile"/> class.
    /// </summary>
    public class InstanceFileTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="InstanceFile"/> throws an <see cref="ArgumentNullException"/> if no path is
        /// provided.
        /// </summary>
        [Fact]
        public void ConstructorThrowsOnPathNull()
        {
            Assert.Throws<ArgumentNullException>(() => new InstanceFile(null));
        }

        /// <summary>
        /// Checks that <see cref="InstanceFile.Path"/> returns the name provided on construction.
        /// </summary>
        [Fact]
        public void PathIsSetCorrectly()
        {
            var instance = new InstanceFile("bar/foo");
            Assert.Equal("bar/foo", instance.Path);
        }

        /// <summary>
        /// Checks that <see cref="InstanceFile.ToString"/> returns the path provided on construction.
        /// </summary>
        [Fact]
        public void ToStringReturnsPath()
        {
            var instance = new InstanceFile("bar/foo");
            Assert.Equal("bar/foo", instance.ToString());
        }

        /// <summary>
        /// Checks that <see cref="object.Equals(object, object)"/> returns false if
        /// the instances' paths are different.
        /// </summary>
        [Fact]
        public void EqualsReturnsFalseForDifferentValue()
        {
            var firstInstance = new InstanceFile("1");
            var secondInstance = new InstanceFile("2");
            Assert.False(
                object.Equals(firstInstance, secondInstance),
                $"Instance {firstInstance} is supposedly the same as instance {secondInstance}.");
        }

        /// <summary>
        /// Checks that <see cref="object.Equals(object, object)"/> returns false if
        /// the first parameter is not an <see cref="InstanceFile"/> and the second one is.
        /// </summary>
        [Fact]
        public void EqualsReturnsFalseForOneObjectNotInstanceFile()
        {
            var instance = new InstanceFile("1");
            var wrongType = new TestInstance("1");
            Assert.False(
                object.Equals(wrongType, instance),
                $"Instance {instance} was identified to be equal to {wrongType}.");
        }

        /// <summary>
        /// Checks that <see cref="object.Equals(object, object)"/> is true for
        /// two <see cref="InstanceFile"/>s with same paths, but pointing to different objects.
        /// </summary>
        [Fact]
        public void EqualsReturnsTrueEqualFileNames()
        {
            string path = "1";
            var instance1 = new InstanceFile(path);
            var instance2 = new InstanceFile(path);
            Assert.True(
                object.Equals(instance1, instance2),
                $"{instance1} and {instance2} are supposedly different, but both encode instance {path}.");
        }

        /// <summary>
        /// Checks that <see cref="InstanceFile.GetHashCode"/> is equal for two
        /// <see cref="InstanceFile"/>s storing the same path.
        /// </summary>
        [Fact]
        public void GetHashCodeReturnsSameHashCodesForEqualPaths()
        {
            string path = "test";
            var instance1 = new InstanceFile(path);
            var instance2 = new InstanceFile(path);
            Assert.True(object.Equals(instance1, instance2));

            var firstInstanceHash = instance1.GetHashCode();
            var secondInstanceHash = instance2.GetHashCode();
            Assert.True(
                firstInstanceHash == secondInstanceHash,
                $"Instances {instance1} and {instance2} are equal, but have different hashes {firstInstanceHash} and {secondInstanceHash}.");
        }

        /// <summary>
        /// Checks that <see cref="InstanceFile.GetHashCode"/> is different for two
        /// <see cref="InstanceFile"/>s storing different file names.
        /// Of course, this does not have to be the case necessarily, but it's still a nice property that should be
        /// true in most cases.
        /// </summary>
        [Fact]
        public void GetHashCodeReturnsDifferentHashCodesForDifferentPaths()
        {
            var instance1 = new InstanceFile("1");
            var instance2 = new InstanceFile("2");
            Assert.False(object.Equals(instance1, instance2));

            var firstInstanceHash = instance1.GetHashCode();
            var secondInstanceHash = instance2.GetHashCode();
            Assert.True(
                firstInstanceHash != secondInstanceHash,
                $"Instances {instance1} and {instance2} are not equal, but have equal hashes {firstInstanceHash} and {secondInstanceHash}.");
        }

        #endregion
    }
}