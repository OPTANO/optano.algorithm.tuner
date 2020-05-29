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

namespace Optano.Algorithm.Tuner.Configuration.ArgumentParsers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Akka.Util.Internal;

    using NDesk.Options;

    /// <summary>
    /// Utility class for parsing command line arguments. Supports an argument --help / -h for printing useful
    /// information.
    /// </summary>
    public abstract class HelpSupportingArgumentParserBase
    {
        #region Static Fields

        /// <summary>
        /// The help parameter.
        /// </summary>
        protected static readonly string HelpPrototypeText = "h|help";

        #endregion

        #region Fields

        /// <summary>
        /// A value indicating whether the parsed arguments requested the help text to be printed.
        /// </summary>
        private bool _helpTextRequested;

        /// <summary>
        /// A value indicating whether <see cref="ParseArguments(string[])" /> has already been called.
        /// </summary>
        private bool _parsingHappened;

        /// <summary>
        /// A value indicating whether <see cref="ParseArguments(string[])"/> has processed preprocessing options yet.
        /// </summary>
        private bool _preProcessingHappened = false;

        /// <summary>
        /// List of arguments that could not be parsed when calling <see cref="ParseArguments(string[])"/>.
        /// </summary>
        private List<string> _additionalArguments;

        /// <summary>
        /// Backing field for <see cref="Options"/>.
        /// </summary>
        private OptionSet _options;

        /// <summary>
        /// Gets or sets all console arguments.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Reviewed. Property with capital name exists.")]
        protected string[] _allArguments;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpSupportingArgumentParserBase"/> class.
        /// </summary>
        /// <param name="allowAdditionalArguments">
        /// Whether unknown arguments should be allowed when calling <see cref="ParseArguments(string[])"/>.
        /// </param>
        protected HelpSupportingArgumentParserBase(bool allowAdditionalArguments = false)
        {
            this.AllowAdditionalArguments = allowAdditionalArguments;

            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the argument options that can be recognized by this parser.
        /// </summary>
        protected OptionSet Options
        {
            get
            {
                if (this._options == null)
                {
                    this._options = this.CreateOptionSet();
                }

                return this._options;
            }
        }



        /// <summary>
        /// Gets a value indicating whether the parsed arguments required the print of a help text.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called before <see cref="ParseArguments(string[])" />
        /// has been executed.
        /// </exception>
        public bool HelpTextRequested
        {
            get
            {
                this.ThrowExceptionIfNoPreprocessingHasBeenDone();
                return this._helpTextRequested;
            }
        }

        /// <summary>
        /// Gets the list of arguments that could not be parsed when calling <see cref="ParseArguments(string[])"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called before <see cref="ParseArguments(string[])" />
        /// has been executed.
        /// </exception>
        public virtual IEnumerable<string> AdditionalArguments
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return this._additionalArguments;
            }
        }

        /// <summary>
        /// Gets the list of all arguments.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called before <see cref="ParseArguments(string[])" />
        /// has been executed.
        /// </exception>
        public IEnumerable<string> AllArguments
        {
            get
            {
                this.ThrowExceptionIfNoParsingHasBeenDone();
                return this._allArguments;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether <see cref="ParseArguments(string[])"/> should accept unknown arguments or not.
        /// </summary>
        protected bool AllowAdditionalArguments { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Parses the provided arguments.
        /// </summary>
        /// <param name="args">
        /// Arguments to parse.
        /// </param>
        /// <exception cref="OptionException">
        /// Thrown if required parameters have not been set.
        /// </exception>
        /// <exception cref="AggregateException">
        /// Thrown if one or more arguments could not be interpreted.
        /// </exception>
        public virtual void ParseArguments(string[] args)
        {
            this._allArguments = args?.Concat(this._allArguments ?? new string[0]).Distinct(StringComparer.InvariantCulture).ToArray() ?? new string[0];

            // Parse arguments one by one.
            this._additionalArguments = this.Options.Parse(args);

            this.FinishedPreProcessing();
            this.FinishedParsing();

            // Don't do further verification if help was requested.
            if (this.HelpTextRequested)
            {
                return;
            }

            // If some arguments could not be interpreted, throw an error as that was surely unintentional.
            if (!this.AllowAdditionalArguments && this._additionalArguments.Any())
            {
                throw new AggregateException(this._additionalArguments.Select(arg => new OptionException($"Could not resolve '{arg}'.", arg)));
            }
        }

        /// <summary>
        /// Prints a description on how to use the command line arguments.
        /// </summary>
        public virtual void PrintHelp()
        {
            this.PrintHelp(true);
        }

        /// <summary>
        /// Prints a description on how to use the command line arguments.
        /// </summary>
        /// <param name="printHelpParameter">Indicates whether the help options should be printed.</param>
        public virtual void PrintHelp(bool printHelpParameter)
        {
            var options = this.GetOptionsToPrint(printHelpParameter);

            var writer = new StringWriter(CultureInfo.InvariantCulture);
            options.WriteOptionDescriptions(writer);
            writer.Flush();
            Console.WriteLine(writer.ToString());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks if <see cref="ParseArguments(string[])" /> has been called yet and throws an
        /// <see cref="InvalidOperationException" /> if that's not the case.
        /// </summary>
        protected void ThrowExceptionIfNoParsingHasBeenDone()
        {
            if (!this._parsingHappened)
            {
                throw new InvalidOperationException("Tried to access result of parsing before ParseArguments was called!");
            }
        }

        /// <summary>
        /// Checks if <see cref="ParseArguments(string[])" /> has processed preprocessing options yet and throws an
        /// <see cref="InvalidOperationException" /> if that's not the case.
        /// </summary>
        protected void ThrowExceptionIfNoPreprocessingHasBeenDone()
        {
            if (!this._preProcessingHappened)
            {
                throw new InvalidOperationException("Tried to access result of preprocessing before ParseArguments preprocessed!");
            }
        }

        /// <summary>
        /// Indicates that parsing is finished and hence makes it possible to read all properties.
        /// </summary>
        protected void FinishedParsing()
        {
            this._parsingHappened = true;
        }

        /// <summary>
        /// Indicates that the preprocessing part of parsing is finished and hence makes it possible to read some
        /// properties.
        /// </summary>
        protected void FinishedPreProcessing()
        {
            this._preProcessingHappened = true;
        }

        /// <summary>
        /// Creates an <see cref="OptionSet" /> containing all options this parser can handle.
        /// </summary>
        /// <returns>The created <see cref="OptionSet" />.</returns>
        protected virtual OptionSet CreateOptionSet()
        {
            var options = new OptionSet
                              {
                                  {
                                      HelpSupportingArgumentParserBase.HelpPrototypeText, "Information about usage will be printed.",
                                      h => this._helpTextRequested = true
                                  },
                              };

            return options;
        }

        /// <summary>
        /// Returns the options that should be printed.
        /// </summary>
        /// <param name="printHelpParameter">Indicates if help option should be printed.</param>
        /// <returns>The options to print.</returns>
        private OptionSet GetOptionsToPrint(bool printHelpParameter)
        {
            if (printHelpParameter)
            {
                return this.Options;
            }

            var filteredOptions = this.Options.Where(o => !HelpSupportingArgumentParserBase.HelpPrototypeText.Equals(o.Prototype));
            var options = new OptionSet();
            filteredOptions.ForEach(f => options.Add(f));

            return options;
        }

        #endregion
    }
}
