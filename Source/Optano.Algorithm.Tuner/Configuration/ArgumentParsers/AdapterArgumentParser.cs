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

namespace Optano.Algorithm.Tuner.Configuration.ArgumentParsers
{
    using System;

    using NDesk.Options;

    using Optano.Algorithm.Tuner.DistributedExecution;

    /// <summary>
    /// The adapter argument parser.
    /// </summary>
    /// <typeparam name="TConfigBuilder">
    /// The config builder used to build the current <see cref="ConfigurationBase"/>.
    /// </typeparam>
    public abstract class AdapterArgumentParser<TConfigBuilder> : HelpSupportingArgumentParser<TConfigBuilder>
        where TConfigBuilder : IConfigBuilder<ConfigurationBase>, new()
    {
        #region Fields

        /// <summary>
        /// Backing field for <see cref="IsMaster"/>.
        /// </summary>
        private bool _isMaster = false;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterArgumentParser{TConfigBuilder}"/> class.
        /// </summary>
        protected AdapterArgumentParser()
            : base(true)
        {
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets a value indicating whether this instance of the application should act as master.
        /// </summary>
        public bool IsMaster
        {
            get
            {
                this.ThrowExceptionIfNoPreprocessingHasBeenDone();
                return this._isMaster;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc />
        public override void ParseArguments(string[] args)
        {
            // First check for options influencing which other options are possible.
            this._additionalArguments = this.CreatePreprocessingOptionSet().Parse(args);
            this.FinishedPreProcessing();

            // Don't verify further if help was requested.
            if (this.HelpTextRequested)
            {
                return;
            }

            this.ParseRemainingArguments();

            this.FinishedParsing();

            this.CheckForRequiredArgumentsAndThrowException();
        }

        /// <inheritdoc />
        public override void PrintHelp()
        {
            // Print general arguments.
            Console.Out.WriteLine("General arguments for the application:");
            this.CreatePreprocessingOptionSet().WriteOptionDescriptions(Console.Out);

            // Print master arguments.
            Console.Out.WriteLine(
                "\nAdditional arguments if this instance acts as master (i.e. --master provided):");

            var masterOptions = this.CreateAdapterMasterOptionSet();
            if (masterOptions.Count >= 1)
            {
                Console.Out.WriteLine(
                    "\nAdapter arguments for master:");
                masterOptions.WriteOptionDescriptions(Console.Out);
            }

            Console.Out.WriteLine();
            new MasterArgumentParser().PrintHelp(false);

            // Print worker arguments.
            Console.Out.WriteLine(
                "\nAdditional arguments if this instance acts as worker (i.e. nothing provided):");
            Console.Out.WriteLine();
            new WorkerArgumentParser().PrintHelp(false);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates an <see cref="OptionSet"/> containing all adapter options that somehow influence which other options can be set.
        /// </summary>
        /// <returns>The created <see cref="OptionSet"/>.</returns>
        protected virtual OptionSet CreatePreprocessingOptionSet()
        {
            var options = this.CreateOptionSet();
            options.Add(
                "master",
                () => "Indicates that this instance of the application should act as master.",
                m => this._isMaster = true);
            return options;
        }

        /// <summary>
        /// Parses the remaining arguments.
        /// </summary>
        protected virtual void ParseRemainingArguments()
        {
            if (this.IsMaster)
            {
                this._additionalArguments = this.CreateAdapterMasterOptionSet().Parse(this._additionalArguments);
            }
        }

        /// <summary>
        /// Checks for required arguments and throws an exception.
        /// </summary>
        protected virtual void CheckForRequiredArgumentsAndThrowException()
        {
        }

        /// <summary>
        /// Creates an <see cref="OptionSet"/> containing all adapter options that can be set if the master is requested.
        /// </summary>
        /// <returns>The created <see cref="OptionSet"/>.</returns>
        protected abstract OptionSet CreateAdapterMasterOptionSet();

        #endregion
    }
}