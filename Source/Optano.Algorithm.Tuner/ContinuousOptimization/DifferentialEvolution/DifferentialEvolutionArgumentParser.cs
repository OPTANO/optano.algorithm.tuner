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

namespace Optano.Algorithm.Tuner.ContinuousOptimization.DifferentialEvolution
{
    using System;

    using NDesk.Options;

    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;

    /// <summary>
    /// Argument parser for <see cref="DifferentialEvolutionConfiguration"/>.
    /// </summary>
    public class DifferentialEvolutionArgumentParser
        : HelpSupportingArgumentParser<DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder>
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentialEvolutionArgumentParser"/> class.
        /// </summary>
        /// <param name="allowAdditionalArguments">
        /// True, if unknown arguments should be allowed when calling
        /// <see cref="HelpSupportingArgumentParserBase.ParseArguments(string[])" />.
        /// </param>
        public DifferentialEvolutionArgumentParser(bool allowAdditionalArguments = false)
            : base(allowAdditionalArguments)
        {
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Prints a description on how to configure differential evolution.
        /// </summary>
        /// <param name="printHelpParameter">Indicates whether the help parameter should be printed.</param>
        public override void PrintHelp(bool printHelpParameter)
        {
            Console.WriteLine("Arguments for differential evolution:");
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
                "bestPercentage=",
                () =>
                    $"The percentage of population members which may be used as best member in the current-to-pbest mutation strategy.\nDefault is {DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder.DefaultBestPercentage}.\nThis must be a double in the range of (0, 1].",
                (double percentage) => this.InternalConfigurationBuilder.SetBestPercentage(percentage));
            optionSet.Add(
                "meanMutationFactor=",
                () =>
                    $"The initial value of the mean mutation factor.\nDefault is {DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder.DefaultInitialMeanMutationFactor}.\nThis must be a double in the range of [0, 1].",
                (double factor) => this.InternalConfigurationBuilder.SetInitialMeanMutationFactor(factor));
            optionSet.Add(
                "meanCrossoverRate=",
                () =>
                    $"The initial value of the mean crossover rate.\nDefault is {DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder.DefaultInitialMeanCrossoverRate}.\nThis must be a double in the range of [0, 1].",
                (double rate) => this.InternalConfigurationBuilder.SetInitialMeanCrossoverRate(rate));
            optionSet.Add(
                "learningRate=",
                () =>
                    $"The learning rate for the means.\nDefault is {DifferentialEvolutionConfiguration.DifferentialEvolutionConfigurationBuilder.DefaultLearningRate}.\nThis must be a double in the range of [0, 1].",
                (double rate) => this.InternalConfigurationBuilder.SetLearningRate(rate));

            return optionSet;
        }

        #endregion
    }
}