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

namespace Optano.Algorithm.Tuner.TargetAlgorithm.Instances
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Optano.Algorithm.Tuner.Logging;

    /// <summary>
    /// An <see cref="InstanceSeedFile"/> treats a combination of actual file (stored in <see cref="InstanceFile.Path"/>) and a <see cref="Seed"/> as a unique Instance.
    /// </summary>
    public class InstanceSeedFile : InstanceFile
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceSeedFile"/> class.
        /// </summary>
        /// <param name="path">
        /// The path to the instance file.
        /// </param>
        /// <param name="seed">
        /// The seed that should be used by the target algorithm.
        /// </param>
        public InstanceSeedFile(string path, int seed)
            : base(path)
        {
            this.Seed = seed;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the seed.
        /// </summary>
        public int Seed { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates a list of <see cref="InstanceSeedFile"/>s on using all valid instance files in the given instance directory.
        /// </summary>
        /// <param name="pathToInstanceDirectory">The path to the instance directory.</param>
        /// <param name="validInstanceExtensions">The list of valid instance file extensions.</param>
        /// <param name="numberOfSeedsToUse">The number of seeds to use.</param>
        /// <param name="rngSeed">The random number generator seed.</param>
        /// <returns>
        /// The created list of <see cref="InstanceSeedFile"/>s.
        /// </returns>
        public static List<InstanceSeedFile> CreateInstanceSeedFilesFromDirectory(
            string pathToInstanceDirectory,
            string[] validInstanceExtensions,
            int numberOfSeedsToUse,
            int rngSeed)
        {
            try
            {
                var instanceDirectory = new DirectoryInfo(pathToInstanceDirectory);
                var instanceSeedCombinations = new List<string>();
                var instanceSeedFiles = new List<InstanceSeedFile>();
                foreach (var instanceFilePath in instanceDirectory.EnumerateFiles()
                    .Where(file => validInstanceExtensions.Any(extension => file.Name.EndsWith(extension))))
                {
                    var fileAndSeedCsv = instanceFilePath.FullName;
                    foreach (var seed in InstanceSeedFile.SeedsToUse(numberOfSeedsToUse, rngSeed))
                    {
                        instanceSeedFiles.Add(new InstanceSeedFile(instanceFilePath.FullName, seed));
                        fileAndSeedCsv += $";{seed}";
                    }

                    instanceSeedCombinations.Add(fileAndSeedCsv);
                }

                InstanceSeedFile.DumpInstanceSeedFileCombinations(instanceDirectory, instanceSeedCombinations);
                return instanceSeedFiles;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
                Console.Out.WriteLine($"Cannot open instance directory {pathToInstanceDirectory}!");
                throw;
            }
        }

        /// <summary>
        /// Generates the seeds to use for the <see cref="InstanceSeedFile"/> combinations.
        /// </summary>
        /// <param name="numberOfSeedsToUse">The number of seeds to use.</param>
        /// <param name="rngSeed">The random number generator seed.</param>
        /// <returns>The seeds.</returns>
        [SuppressMessage(
            "NDepend",
            "ND3101:DontUseSystemRandomForSecurityPurposes",
            Justification = "No security related purpose.")]
        public static IEnumerable<int> SeedsToUse(int numberOfSeedsToUse, int rngSeed)
        {
            var random = new Random(rngSeed);
            for (var i = 0; i < numberOfSeedsToUse; i++)
            {
                yield return random.Next();
            }
        }

        /// <summary>
        /// Checks if the two objects are equal.
        /// </summary>
        /// <param name="obj">
        /// The other object.
        /// </param>
        /// <returns>
        /// True, if <see cref="InstanceFile.Path"/> and <see cref="Seed"/> are equal.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null || !(typeof(InstanceSeedFile) == obj.GetType()))
            {
                return false;
            }

            var other = obj as InstanceSeedFile;
            return this.Path.Equals(other.Path, StringComparison.InvariantCultureIgnoreCase) && this.Seed == other.Seed;
        }

        /// <summary>
        /// Computes the hash code.
        /// Depends on <see cref="InstanceFile.Path"/> and <see cref="Seed"/>.
        /// </summary>
        /// <returns>
        /// The hash code.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 23) + base.GetHashCode();
                hash = (hash * 23) + this.Seed.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns {Path}_{Seed}.
        /// </summary>
        /// <returns>
        /// The string representation.
        /// </returns>
        public override string ToString()
        {
            return $"{base.ToString()}_{this.Seed}";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Dumps the given <see cref="InstanceSeedFile"/> combinations to a file.
        /// </summary>
        /// <param name="instanceDirectory">The instance directory.</param>
        /// <param name="instanceSeedFileCombinations">The <see cref="InstanceSeedFile"/> combinations.</param>
        private static void DumpInstanceSeedFileCombinations(
            FileSystemInfo instanceDirectory,
            IEnumerable<string> instanceSeedFileCombinations)
        {
            var fileName = System.IO.Path.Combine(
                instanceDirectory.FullName,
                $"instanceSeedFileCombinations_{DateTime.Now:MM-dd-hh-mm-ss}.csv");
            try
            {
                File.WriteAllLines(fileName, instanceSeedFileCombinations, Encoding.UTF8);
            }
            catch (Exception e)
            {
                LoggingHelper.WriteLine(VerbosityLevel.Warn, $"Could not write instance seed file combinations to destination {fileName}!");
                LoggingHelper.WriteLine(VerbosityLevel.Warn, e.Message);
            }
        }

        #endregion
    }
}