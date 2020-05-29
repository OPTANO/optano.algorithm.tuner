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

namespace Optano.Algorithm.Tuner.Tests
{
    using System;
    using System.IO;
    using System.Linq;

    using Akka.Actor;

    using Optano.Algorithm.Tuner;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest.Configuration;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using Xunit;

    /// <summary>
    /// Provides useful abilities for test classes.
    /// </summary>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public abstract class TestBase : IDisposable
    {
        #region Constants

        /// <summary>
        /// Name for the actor system employed in tests.
        /// </summary>
        public const string ActorSystemName = "test";

        #endregion

        #region Fields

        /// <summary>
        /// Lock to make sure that only one initialization / cleanup runs at a time.
        /// </summary>
        private readonly object _lock = new object();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestBase"/> class.
        /// </summary>
        protected TestBase()
        {
            lock (this._lock)
            {
                Directory.CreateDirectory(AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder.DefaultStatusFileDirectory);

                TestUtils.InitializeLogger();
                Randomizer.Reset();
                Randomizer.Configure(this.Seed());
                this.InitializeDefault();
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the actor system employed in tests.
        /// Might be <c>null</c>.
        /// </summary>
        protected ActorSystem ActorSystem { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Resets the <see cref="Randomizer"/> and <see cref="LoggingHelper"/>.
        /// Calls <see cref="CleanupDefault"/> afterwards.
        /// </summary>
        public void Dispose()
        {
            lock (this._lock)
            {
                DeleteExportDirectory();
                DeleteStatusDirectory();
                DeleteCsvFiles();

                Randomizer.Reset();
                this.CleanupDefault();

                if (this.ActorSystem != null)
                {
                    System.Threading.Tasks.Task.Run(async () => await this.ActorSystem?.Terminate()).Wait();
                    this.ActorSystem?.Dispose();
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a seed.
        /// </summary>
        /// <returns>The seed.</returns>
        protected virtual int Seed()
        {
            return 42;
        }

        /// <summary>
        /// Creates a tree containing two integer parameter nodes.
        /// </summary>
        /// <returns>The created <see cref="ParameterTree"/>.</returns>
        protected virtual ParameterTree GetDefaultParameterTree()
        {
            var root = new ValueNode<int>("a", new IntegerDomain(-5, 5));
            root.SetChild(new ValueNode<int>("b", new IntegerDomain(0, 10)));

            var tree = new ParameterTree(root);
            return tree;
        }

        /// <summary>
        /// Creates a slightly adjusted default config for testing.
        /// </summary>
        /// <returns>The created <see cref="AlgorithmTunerConfiguration"/>.</returns>
        protected virtual AlgorithmTunerConfiguration GetDefaultAlgorithmTunerConfiguration()
        {
            return new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetGenerations(10)
                .SetInstanceNumbers(1, 1)
                .SetEngineeredProportion(1)
                .SetGoalGeneration(0)
                .SetMaximumNumberParallelEvaluations(1)
                .SetEnableRacing(false)
                .AddDetailedConfigurationBuilder(
                    RegressionForestArgumentParser.Identifier,
                    new GenomePredictionRandomForestConfig.GenomePredictionRandomForestConfigBuilder())
                .Build();
        }

        /// <summary>
        /// Called before every test.
        /// </summary>
        protected virtual void InitializeDefault()
        {
        }

        /// <summary>
        /// Optional cleanup-steps that are executed after Resetting the Randomizer.
        /// </summary>
        protected virtual void CleanupDefault()
        {
            lock (this._lock)
            {
                TestBase.DeleteExportDirectory();
                TestBase.DeleteStatusDirectory();
                TestBase.DeleteCsvFiles();

                Randomizer.Reset();

                if (this.ActorSystem != null)
                {
                    System.Threading.Tasks.Task.Run(async () => await this.ActorSystem?.Terminate()).Wait();
                    this.ActorSystem?.Dispose();
                }
            }
        }

        /// <summary>
        /// Deletes the "export" directory that might have been created while testing.
        /// </summary>
        private static void DeleteExportDirectory()
        {
            var exportDirectory = PathUtils.GetAbsolutePathFromExecutableFolderRelative("export");
            if (Directory.Exists(exportDirectory))
            {
                Directory.Delete(exportDirectory, recursive: true);
            }
        }

        /// <summary>
        /// Deletes the "status" directory that might have been created while testing.
        /// </summary>
        private static void DeleteStatusDirectory()
        {
            var statusDirectory = PathUtils.GetAbsolutePathFromExecutableFolderRelative("status");
            if (Directory.Exists(statusDirectory))
            {
                Directory.Delete(statusDirectory, recursive: true);
            }

            var oldStatusFileDirectory = PathUtils.GetAbsolutePathFromExecutableFolderRelative("old_status_files");
            if (Directory.Exists(oldStatusFileDirectory))
            {
                Directory.Delete(oldStatusFileDirectory, recursive: true);
            }
        }

        /// <summary>
        /// Deletes all .csv files that might have been created while testing.
        /// </summary>
        private static void DeleteCsvFiles()
        {
            foreach (var fileName in Directory.EnumerateFiles(PathUtils.GetAbsolutePathFromCurrentDirectory(""), "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".csv", StringComparison.InvariantCultureIgnoreCase)))
            {
                File.Delete(fileName);
            }
        }

        #endregion
    }
}
