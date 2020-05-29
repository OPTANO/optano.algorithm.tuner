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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.Configuration
{
    using System;
    using System.Collections.Generic;

    using NDesk.Options;

    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution;

    /// <summary>
    /// Argument parser for <see cref="DifferentialEvolutionStrategyConfiguration"/>.
    /// </summary>
    public class DifferentialEvolutionStrategyArgumentParser
        : HelpSupportingArgumentParser<DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder>
    {
        #region Fields

        /// <summary>
        /// The argument parser for the differential evolution algorithm.
        /// </summary>
        private readonly DifferentialEvolutionArgumentParser _algorithmParameterParser
            = new DifferentialEvolutionArgumentParser(allowAdditionalArguments: true);

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentialEvolutionStrategyArgumentParser"/> class.
        /// </summary>
        public DifferentialEvolutionStrategyArgumentParser()
            : base(allowAdditionalArguments: true)
        {
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the identifier for the parser class.
        /// </summary>
        public static string Identifier => typeof(DifferentialEvolutionStrategyConfiguration).FullName;

        /// <summary>
        /// Gets the list of arguments that could not be parsed when calling <see cref="ParseArguments(string[])" />.
        /// </summary>
        public override IEnumerable<string> AdditionalArguments => this._algorithmParameterParser.AdditionalArguments;

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
        public override void ParseArguments(string[] args)
        {
            var remainingArguments = this.Options.Parse(args);

            this.FinishedPreProcessing();
            if (this.HelpTextRequested)
            {
                return;
            }

            this._algorithmParameterParser.ParseArguments(remainingArguments.ToArray());
            this.InternalConfigurationBuilder.SetDifferentialEvolutionConfigurationBuilder(
                this._algorithmParameterParser.ConfigurationBuilder);

            this.FinishedParsing();
        }

        /// <summary>
        /// Prints a description on how to configure differential evolution.
        /// </summary>
        /// <param name="printHelpParameter">Indicates whether the help parameter should be printed.</param>
        public override void PrintHelp(bool printHelpParameter)
        {
            Console.WriteLine("Arguments for differential evolution strategy:");
            base.PrintHelp(printHelpParameter);

            this._algorithmParameterParser.PrintHelp(printHelpParameter);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the <see cref="OptionSet"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="OptionSet"/>.
        /// </returns>
        protected override OptionSet CreateOptionSet()
        {
            var optionSet = base.CreateOptionSet();

            optionSet.Add(
                "maxGenerationsPerDePhase=",
                $"The maximum number of generations per differential evolution phase.\nDefault is {DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder.DefaultMaximumNumberGenerations}.\nThis must be a positive integer.",
                (int number) => this.InternalConfigurationBuilder.SetMaximumNumberGenerations(number));
            optionSet.Add(
                "minDomainSize=",
                $"The minimum size an integer domain needs to have to be handled as continuous.\nDefault is {DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder.DefaultMinimumDomainSize}.\nThis must be a positive integer.",
                (int size) => this.InternalConfigurationBuilder.SetMinimumDomainSize(size));
            optionSet.Add(
                "focusOnIncumbent=",
                $"Whether JADE should focus on improving the incumbent. If not, it works on the complete population.\nDefault is {DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder.DefaultFocusOnIncumbent}.",
                (bool incumbentFocus) => this.InternalConfigurationBuilder.SetFocusOnIncumbent(incumbentFocus));
            optionSet.Add(
                "replacementRate=",
                $"The percentage of competitive genomes which get replaced by the best search points found by differential evolution at the end of a phase. A replacement rate of 0 indicates that only the incumbent itself should be replaced.\nDefault is {DifferentialEvolutionStrategyConfiguration.DifferentialEvolutionStrategyConfigurationBuilder.DefaultReplacementRate}.\nThis must be a double in [0, 0.5].",
                (double rate) => this.InternalConfigurationBuilder.SetReplacementRate(rate));
            optionSet.Add(
                "fixInstances",
                "Add if the set of instances to evaluate on should stay the same during a differential evolution phase.",
                y => this.InternalConfigurationBuilder.SetFixInstances(true));

            return optionSet;
        }

        #endregion
    }
}