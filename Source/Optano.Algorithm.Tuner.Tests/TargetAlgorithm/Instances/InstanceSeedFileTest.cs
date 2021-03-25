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
    using System.IO;
    using System.Linq;

    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="InstanceSeedFile"/> class.
    /// </summary>
    public class InstanceSeedFileTest : IDisposable
    {
        #region Static Fields

        /// <summary>
        /// The file names that should be translated into <see cref="InstanceSeedFile"/>s on <see cref="InstanceSeedFile.CreateInstanceSeedFilesFromDirectory"/>.
        /// </summary>
        private static readonly string[] ValidFileNames = { "useful1.valid", "useful2.valid", "useful3.valid.zip", "useful4.valid.zip" };

        /// <summary>
        /// The file names that should not be translated into <see cref="InstanceSeedFile"/>s on <see cref="InstanceSeedFile.CreateInstanceSeedFilesFromDirectory"/>.
        /// </summary>
        private static readonly string[] NonValidFileNames = { "useless.txt", "useless.txt.zip" };

        /// <summary>
        /// The list of valid instance extensions.
        /// </summary>
        private static readonly string[] ValidInstanceExtensions = { ".valid", ".valid.zip" };

        #endregion

        #region Fields

        /// <summary>
        /// The path to the test instance folder. Has to be initialized.
        /// </summary>
        private readonly string _pathToTestInstanceFolder;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceSeedFileTest"/> class.
        /// Behaves like the [TestInitialize] of MSTest framework.
        /// </summary>
        public InstanceSeedFileTest()
        {
            this._pathToTestInstanceFolder = PathUtils.GetAbsolutePathFromExecutableFolderRelative("testInstanceFolder");
            Directory.CreateDirectory(this._pathToTestInstanceFolder);
            foreach (var fileName in InstanceSeedFileTest.ValidFileNames.Union(InstanceSeedFileTest.NonValidFileNames))
            {
                var handle = File.Create(Path.Combine(this._pathToTestInstanceFolder, fileName));
                handle.Close();
            }
        }

        #endregion

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

        /// <summary>
        /// Verifies that calling <see cref="InstanceSeedFile.CreateInstanceSeedFilesFromDirectory"/> with a non existant directory throws
        /// a <see cref="DirectoryNotFoundException"/>.
        /// </summary>
        [Fact]
        public void CreateInstancesThrowsExceptionIfItCannotOpenFolder()
        {
            Exception exception =
                Assert.Throws<DirectoryNotFoundException>(
                    () =>
                        {
                            InstanceSeedFile.CreateInstanceSeedFilesFromDirectory(
                                "foobarFolder",
                                InstanceSeedFileTest.ValidInstanceExtensions,
                                1,
                                42);
                        });
        }

        /// <summary>
        /// Verifies that calling <see cref="InstanceSeedFile.CreateInstanceSeedFilesFromDirectory"/> with a non existant directory prints
        /// out a message to the console telling the user the directory doesn't exist.
        /// </summary>
        [Fact]
        public void CreateInstancesPrintsMessageIfItCannotOpenFolder()
        {
            TestUtils.CheckOutput(
                action: () =>
                    {
                        // Call CreateInstanceSeedFilesFromDirectory with a non existant directory path.
                        try
                        {
                            InstanceSeedFile.CreateInstanceSeedFilesFromDirectory(
                                "foobarFolder",
                                InstanceSeedFileTest.ValidInstanceExtensions,
                                1,
                                42);
                        }
                        catch (DirectoryNotFoundException)
                        {
                            // This is expected.
                        }
                    },
                check: consoleOutput =>
                    {
                        // Check that information about it is written to console.
                        var reader = new StringReader(consoleOutput.ToString());
                        reader.ReadLine().ShouldContain("foobarFolder");
                        reader.ReadLine().ShouldBe("Cannot open instance directory foobarFolder!");
                    });
        }

        /// <summary>
        /// Checks that <see cref="InstanceSeedFile.CreateInstanceSeedFilesFromDirectory"/> creates an instance out of each valid file and
        /// the instance's file name matches the complete path to that file.
        /// </summary>
        [Fact]
        public void CreateInstancesCorrectlyExtractsPathsToValidFiles()
        {
            // Call method.
            var instances = InstanceSeedFile.CreateInstanceSeedFilesFromDirectory(
                this._pathToTestInstanceFolder,
                InstanceSeedFileTest.ValidInstanceExtensions,
                1,
                42);

            // Check that file names of instances match the complete paths of all valid files.
            var expectedPaths = InstanceSeedFileTest.ValidFileNames
                .Select(name => this._pathToTestInstanceFolder + Path.DirectorySeparatorChar + name).ToList();
            var instancePaths = instances.Select(instance => instance.Path).ToList();
            expectedPaths.ShouldBe(
                instancePaths,
                true,
                $"{TestUtils.PrintList(instancePaths)} should have been equal to {TestUtils.PrintList(expectedPaths)}.");
        }

        /// <summary>
        /// Checks that <see cref="InstanceSeedFile.CreateInstanceSeedFilesFromDirectory"/> ignores all non valid files.
        /// </summary>
        [Fact]
        public void CreateInstancesIgnoresNonValidFiles()
        {
            // Call method.
            var instances = InstanceSeedFile.CreateInstanceSeedFilesFromDirectory(
                this._pathToTestInstanceFolder,
                InstanceSeedFileTest.ValidInstanceExtensions,
                1,
                42);

            // Check that no non valid file has been translated into an instance.
            var instancePaths = instances.Select(instance => instance.Path);
            instancePaths.Any(path => InstanceSeedFileTest.NonValidFileNames.Any(path.Contains))
                .ShouldBeFalse("Not all non valid files have been ignored.");
        }

        /// <summary>
        /// Checks that <see cref="InstanceSeedFile.SeedsToUse"/> returns the correct number of seeds.
        /// </summary>
        [Fact]
        public void SeedsToUseReturnsCorrectNumberOfSeeds()
        {
            const int NumberOfSeeds = 6;
            var seedsToUse = InstanceSeedFile.SeedsToUse(NumberOfSeeds, 42);
            seedsToUse.Count().ShouldBe(NumberOfSeeds);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Directory.Delete(this._pathToTestInstanceFolder, recursive: true);
        }

        #endregion
    }
}