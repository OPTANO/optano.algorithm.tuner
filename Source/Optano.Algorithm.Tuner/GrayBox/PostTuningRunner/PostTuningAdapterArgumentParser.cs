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

namespace Optano.Algorithm.Tuner.GrayBox.PostTuningRunner
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using NDesk.Options;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.DistributedExecution;

    /// <summary>
    /// The adapter argument parser with post tuning options.
    /// </summary>
    /// <typeparam name="TConfigBuilder">
    /// The config builder used to build the current <see cref="ConfigurationBase"/>.
    /// </typeparam>
    [SuppressMessage(
        "NDepend",
        "ND1201:ClassShouldntBeTooDeepInInheritanceTree",
        Justification = "This type is part of the parser pattern.")]
    public abstract class PostTuningAdapterArgumentParser<TConfigBuilder> : AdapterArgumentParser<TConfigBuilder>
        where TConfigBuilder : IConfigBuilder<ConfigurationBase>, new()
    {
        #region Fields

        /// <summary>
        /// Backing field for <see cref="IsPostTuningRunner"/>.
        /// </summary>
        private bool _isPostTuningRunner = false;

        /// <summary>
        /// The path to the post tuning file.
        /// </summary>
        private string _pathToPostTuningFile = PathUtils.GetAbsolutePathFromCurrentDirectory("postTuningRuns.csv");

        /// <summary>
        /// The index of the first post tuning run.
        /// </summary>
        private int _indexOfFirstPostTuningRun = 0;

        /// <summary>
        /// The number of post tuning runs.
        /// </summary>
        private int _numberOfPostTuningRuns = 1;

        #endregion

        #region Public properties

        /// <summary>
        /// Gets a value indicating whether this instance of the application should act as post tuning runner.
        /// </summary>
        public bool IsPostTuningRunner
        {
            get
            {
                this.ThrowExceptionIfNoPreprocessingHasBeenDone();
                return this._isPostTuningRunner;
            }
        }

        /// <summary>
        /// Gets the post tuning configuration.
        /// </summary>
        public PostTuningConfiguration PostTuningConfiguration
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return new PostTuningConfiguration(this._pathToPostTuningFile, this._indexOfFirstPostTuningRun, this._numberOfPostTuningRuns);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public override void PrintHelp()
        {
            // Print master and worker arguments.
            base.PrintHelp();

            // Print post tuning arguments.
            Console.WriteLine(
                "\nAdditional arguments if this instance acts as post tuning runner (i.e. --postTuning provided):");

            if (this.CreateAdapterPostTuningOptionSet().Count >= 1)
            {
                Console.WriteLine(
                    "\nAdapter arguments for post tuning runner:");
                this.CreateAdapterPostTuningOptionSet().WriteOptionDescriptions(Console.Out);
            }

            Console.WriteLine("\nOAT arguments for post tuning runner:");
            this.CreateOatPostTuningOptionSet().WriteOptionDescriptions(Console.Out);
            Console.WriteLine(
                "\nIn addition, the post tuning runner makes use of several OAT master arguments. In particular, you might want to set the following parameters.");
            Console.WriteLine();
            Console.WriteLine($"\t--{MasterArgumentParser.MaxParallelEvaluationsOptionName}=VALUE");
            Console.WriteLine($"\t--{MasterArgumentParser.CpuTimeoutOptionName}=VALUE");
            Console.WriteLine($"\t--{MasterArgumentParser.EnableDataRecordingOptionName}=VALUE");
            Console.WriteLine($"\t--{MasterArgumentParser.DataRecordDirectoryOptionName}=VALUE");
            Console.WriteLine($"\t--{MasterArgumentParser.DataRecordUpdateIntervalOptionName}=VALUE");
            Console.WriteLine(
                $"\nPlease see '{MasterArgumentParser.MasterArgumentsHeadline}' for details on these parameters.");
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override OptionSet CreatePreprocessingOptionSet()
        {
            var options = base.CreatePreprocessingOptionSet();
            options.Add(
                "postTuning",
                () => "Indicates that this instance of the application should act as post tuning runner.",
                (string b) => this._isPostTuningRunner = true);
            return options;
        }

        /// <inheritdoc />
        protected override void ParseRemainingArguments()
        {
            base.ParseRemainingArguments();

            if (this.IsPostTuningRunner && this.IsMaster)
            {
                throw new InvalidOperationException(
                    "You cannot start OPTANO Algorithm Tuner as master (i.e. --master provided) and as post tuning runner (i.e. --postTuning provided).");
            }

            if (this.IsPostTuningRunner)
            {
                this._additionalArguments = this.CreateOatPostTuningOptionSet().Parse(this._additionalArguments);
                this._additionalArguments = this.CreateAdapterPostTuningOptionSet().Parse(this._additionalArguments);
            }
        }

        /// <summary>
        /// Creates an <see cref="OptionSet"/> containing all adapter options that can be set if a post tuning runner is requested.
        /// </summary>
        /// <returns>The created <see cref="OptionSet"/>.</returns>
        protected abstract OptionSet CreateAdapterPostTuningOptionSet();

        /// <summary>
        /// Creates an <see cref="OptionSet"/> containing all additional OAT options that can be set if a post tuning runner is requested.
        /// </summary>
        /// <returns>The created <see cref="OptionSet"/>.</returns>
        private OptionSet CreateOatPostTuningOptionSet()
        {
            var options = new OptionSet
                              {
                                  {
                                      "pathToPostTuningFile=",
                                      () =>
                                          "Sets the path to the post tuning file, containing the desired genome instance pairs.\r\nDefault is 'currentDirectory/postTuningRuns.csv'.",
                                      (string p) => this._pathToPostTuningFile = p
                                  },
                                  {
                                      "indexOfFirstPostTuningRun=",
                                      () =>
                                          "Sets the index of the first post tuning genome instance pair to evaluate.\r\nDefault is 0.\r\nNeeds to be greater or equal to 0.",
                                      (int i) => this.SetIndexOfFirstPostTuningRun(i)
                                  },
                                  {
                                      "numberOfPostTuningRuns=",
                                      () =>
                                          "Sets the number of post tuning runs to start in total.\r\nDefault is 1.\r\nNeeds to be greater or equal to 1.",
                                      (int n) => this.SetNumberOfPostTuningRuns(n)
                                  },
                              };
            return options;
        }

        /// <summary>
        /// Sets the index of the first post tuning run.
        /// </summary>
        /// <param name="index">The index of the first post tuning run.</param>
        private void SetIndexOfFirstPostTuningRun(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"{nameof(index)} needs to be greater or equal to 0.");
            }

            this._indexOfFirstPostTuningRun = index;
        }

        /// <summary>
        /// Sets number of post tuning runs.
        /// </summary>
        /// <param name="number">The number of post tuning runs.</param>
        private void SetNumberOfPostTuningRuns(int number)
        {
            if (number < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number),
                    $"{nameof(number)} needs to be greater or equal to 1.");
            }

            this._numberOfPostTuningRuns = number;
        }

        #endregion
    }
}