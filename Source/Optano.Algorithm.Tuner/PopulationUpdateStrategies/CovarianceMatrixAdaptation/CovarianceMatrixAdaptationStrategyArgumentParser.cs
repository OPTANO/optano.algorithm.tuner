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

namespace Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation
{
    using System;

    using NDesk.Options;

    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;

    /// <summary>
    /// Argument parser for <see cref="CovarianceMatrixAdaptationStrategyConfiguration"/>.
    /// </summary>
    public class CovarianceMatrixAdaptationStrategyArgumentParser
        : HelpSupportingArgumentParser<CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CovarianceMatrixAdaptationStrategyArgumentParser"/> class.
        /// </summary>
        public CovarianceMatrixAdaptationStrategyArgumentParser()
            : base(allowAdditionalArguments: true)
        {
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the identifier for the parser class.
        /// </summary>
        public static string Identifier => typeof(CovarianceMatrixAdaptationStrategyConfiguration).FullName;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Prints a description on how to configure differential evolution.
        /// </summary>
        /// <param name="printHelpParameter">Indicates whether the help parameter should be printed.</param>
        public override void PrintHelp(bool printHelpParameter)
        {
            Console.WriteLine("Arguments for CMA-ES strategy:");
            base.PrintHelp(printHelpParameter);
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
                "maxGenerationsPerCmaEsPhase=",
                $"The maximum number of generations per CMA-ES phase.\nDefault is {CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder.DefaultMaximumNumberGenerations}.\nThis must be a positive integer.",
                (int number) => this.InternalConfigurationBuilder.SetMaximumNumberGenerations(number));
            optionSet.Add(
                "focusOnIncumbent=",
                $"Whether CMA-ES should focus on improving the continuous parameters of the incumbent. If not, it modifies all parameters of the complete competitive population.\nDefault is {CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder.DefaultFocusOnIncumbent}.",
                (bool incumbentFocus) => this.InternalConfigurationBuilder.SetFocusOnIncumbent(incumbentFocus));
            optionSet.Add(
                "minDomainSize=",
                $"The minimum size an integer domain needs to have to be handled as continuous.\nDefault is {CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder.DefaultMinimumDomainSize}.\nThis must be a positive integer.",
                (int size) => this.InternalConfigurationBuilder.SetMinimumDomainSize(size));
            optionSet.Add(
                "replacementRate=",
                $"Used if focusOnIncumbent=true. The percentage of competitive genomes which get replaced by the best search points found by CMA-ES at the end of a phase.\nDefault is {CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder.DefaultReplacementRate}.\nThis must be a double in (0, 1].",
                (double rate) => this.InternalConfigurationBuilder.SetReplacementRate(rate));
            optionSet.Add(
                "fixInstances",
                "Add if the set of instances to evaluate on should stay the same during a CMA-ES phase.",
                y => this.InternalConfigurationBuilder.SetFixInstances(true));
            optionSet.Add(
                "initialStepSize=",
                $"The step size with which to start CMA-ES phases.\nDefault is {CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder.DefaultInitialStepSize}.\nThis must be a positive double.",
                (double size) => this.InternalConfigurationBuilder.SetInitialStepSize(size));

            return optionSet;
        }

        #endregion
    }
}