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

namespace Optano.Algorithm.Tuner.DistributedExecution
{
    using System;

    using NDesk.Options;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.Logging;

    /// <summary>
    /// Utility class for parsing command line arguments.
    /// </summary>
    public class WorkerArgumentParser : HelpSupportingArgumentParser<AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder>
    {
        #region Fields

        /// <summary>
        /// The port to connect to the cluster seed as specified by the parsed arguments.
        /// </summary>
        private int _port = 8081;

        /// <summary>
        /// The cluster seed's host name as specified by the parsed arguments.
        /// </summary>
        private string _seedHostName;

        /// <summary>
        /// Backing field for the <see cref="OwnHostName"/>.
        /// </summary>
        private string _ownHostName;

        /// <summary>
        /// How detailed the console output should be. Default is <see cref="VerbosityLevel.Info" />.
        /// </summary>
        private VerbosityLevel _verbosityLevel = VerbosityLevel.Info;

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the cluster seed's host name.
        /// </summary>
        public string SeedHostName
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return this._seedHostName;
            }
        }

        /// <summary>
        /// Gets the host name that akka listens to on this worker.
        /// </summary>
        public string OwnHostName
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return this._ownHostName;
            }
        }

        /// <summary>
        /// Gets the desired port for the cluster seed.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called before <see cref="ParseArguments(string[])" />
        /// has been executed.
        /// </exception>
        public int Port
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return this._port;
            }
        }

        /// <summary>
        /// Gets how detailed the console output should be.
        /// </summary>
        public VerbosityLevel VerbosityLevel
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return this._verbosityLevel;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Parses the provided arguments.
        /// </summary>
        /// <param name="args">Arguments to parse.</param>
        /// <exception cref="OptionException">Thrown if required parameters have not been set.</exception>
        /// <exception cref="AggregateException">Thrown if one or more arguments could not be interpreted.</exception>
        public override void ParseArguments(string[] args)
        {
            base.ParseArguments(args);

            // Don't do further verification if help was requested.
            if (this.HelpTextRequested)
            {
                return;
            }

            // If no help was requested, check for required arguments.
            if (this.SeedHostName == null)
            {
                throw new OptionException(
                    "Seed host name must be provided. Where is the master located?",
                    "seedHostName");
            }
        }

        /// <summary>
        /// Prints a description on how to use the command line arguments.
        /// </summary>
        /// <param name="printHelpParameter">Indicates whether the help parameter should be printed.</param>
        public override void PrintHelp(bool printHelpParameter)
        {
            Console.WriteLine("Arguments for worker:");
            base.PrintHelp(printHelpParameter);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates an <see cref="OptionSet" /> containing all options important for running Worker.exe.
        /// </summary>
        /// <returns>The created <see cref="OptionSet" />.</returns>
        protected override OptionSet CreateOptionSet()
        {
            var options = base.CreateOptionSet();
            options.Add(
                "s|seedHostName=",
                () => "The seed's host name. Must always be specified.",
                h => this._seedHostName = h);
            options.Add(
                "ownHostName=",
                () =>
                    "The address that the worker uses for incoming Akka messages. Default: FQDN. Note: On some systems the FQDN cannot be resolved on the fly. In that case, please provide the FQDN or an IP address.",
                hostName => this._ownHostName = hostName);
            options.Add(
                "p|port=",
                () =>
                    "The port {NUMBER} on which the seed listens for worker connections. Must be identical for master and respective workers, but different for different parallel runs.\nDefault is 8081.\nThis must be an integer.",
                (int p) => this._port = p);
            options.Add(
                "v|verbose=",
                () =>
                    "The verbosity level. 0 only prints warnings, 1 regurlarly prints some status information, 2 prints more detailed information, e.g. calls to the target mechanism, and 3 is for debugging.\nDefault is 1.\nMust be one of 0, 1, 2, 3.\n",
                (VerbosityLevel level) => this._verbosityLevel = level);

            return options;
        }

        #endregion
    }
}