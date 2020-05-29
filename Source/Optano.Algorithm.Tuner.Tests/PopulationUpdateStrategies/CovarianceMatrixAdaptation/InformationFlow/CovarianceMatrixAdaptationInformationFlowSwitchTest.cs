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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow
{
    using Akka.Actor;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Global;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow.Local;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="CovarianceMatrixAdaptationInformationFlowSwitch"/> class.
    /// </summary>
    public class CovarianceMatrixAdaptationInformationFlowSwitchTest : TestBase
    {
        #region Properties

        /// <summary>
        /// Gets the <see cref="Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators.IRunEvaluator{TResult}"/> used in
        /// tests.
        /// </summary>
        protected IRunEvaluator<TestResult> RunEvaluator { get; } = new KeepSuggestedOrder<TestResult>();

        /// <summary>
        /// Gets or sets the <see cref="Optano.Algorithm.Tuner.Genomes.GenomeBuilder" /> used in tuning.
        /// </summary>
        protected GenomeBuilder GenomeBuilder { get; set; }

        /// <summary>
        /// Gets an <see cref="IActorRef" /> to a <see cref="ResultStorageActor{TInstance,TResult}" />
        /// which knows about all executed target algorithm runs and their results.
        /// </summary>
        protected IActorRef ResultStorageActor { get; private set; }

        /// <summary>
        /// Gets an <see cref="IActorRef" /> to a <see cref="GenomeSorter{TInstance,TResult}" />.
        /// </summary>
        protected IActorRef GenomeSorter { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that
        /// <see cref="CovarianceMatrixAdaptationInformationFlowSwitch.DetermineCovarianceMatrixAdaptationStrategyType{TInstance, TResult}"/>
        /// returns the correct type depending on <see cref="StrategyConfigurationBase{TConfiguration}.FocusOnIncumbent"/>.
        /// </summary>
        [Fact]
        public void DetermineCovarianceMatrixAdaptationStrategyTypeReturnsCorrectType()
        {
            var localConfiguration = CovarianceMatrixAdaptationInformationFlowSwitchTest.CreateConfiguration(focusOnIncumbent: true);
            Assert.Equal(
                typeof(LocalCovarianceMatrixAdaptationStrategy<TestInstance, TestResult>),
                CovarianceMatrixAdaptationInformationFlowSwitch.DetermineCovarianceMatrixAdaptationStrategyType<TestInstance, TestResult>(
                    localConfiguration));

            var globalConfiguration = CovarianceMatrixAdaptationInformationFlowSwitchTest.CreateConfiguration(focusOnIncumbent: false);
            Assert.Equal(
                typeof(GlobalCovarianceMatrixAdaptationStrategy<TestInstance, TestResult>),
                CovarianceMatrixAdaptationInformationFlowSwitch.DetermineCovarianceMatrixAdaptationStrategyType<TestInstance, TestResult>(
                    globalConfiguration));
        }

        /// <summary>
        /// Checks that
        /// <see cref="CovarianceMatrixAdaptationInformationFlowSwitch.DetermineCovarianceMatrixAdaptationStrategyType{TInstance, TResult}"/>
        /// returns an instance of correct type.
        /// </summary>
        [Fact]
        public void CreateCovarianceMatrixAdaptationStrategyReturnsCorrectStrategyType()
        {
            var localConfiguration = CovarianceMatrixAdaptationInformationFlowSwitchTest.CreateConfiguration(focusOnIncumbent: true);
            var strategy =
                CovarianceMatrixAdaptationInformationFlowSwitch.CreateCovarianceMatrixAdaptationStrategy<TestInstance, TestResult>(
                    localConfiguration,
                    this.GetDefaultParameterTree(),
                    this.GenomeBuilder,
                    this.GenomeSorter,
                    this.ResultStorageActor);
            Assert.Equal(
                typeof(LocalCovarianceMatrixAdaptationStrategy<TestInstance, TestResult>),
                strategy.GetType());

            var globalConfiguration = CovarianceMatrixAdaptationInformationFlowSwitchTest.CreateConfiguration(focusOnIncumbent: false);
            strategy =
                CovarianceMatrixAdaptationInformationFlowSwitch.CreateCovarianceMatrixAdaptationStrategy<TestInstance, TestResult>(
                    globalConfiguration,
                    this.GetDefaultParameterTree(),
                    this.GenomeBuilder,
                    this.GenomeSorter,
                    this.ResultStorageActor);
            Assert.Equal(
                typeof(GlobalCovarianceMatrixAdaptationStrategy<TestInstance, TestResult>),
                strategy.GetType());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called before every test case.
        /// </summary>
        protected override void InitializeDefault()
        {
            var configuration = this.GetDefaultAlgorithmTunerConfiguration();
            this.ActorSystem = ActorSystem.Create(AkkaNames.ActorSystemName, configuration.AkkaConfiguration);

            this.GenomeSorter = this.ActorSystem.ActorOf(
                Props.Create(() => new GenomeSorter<TestInstance, TestResult>(this.RunEvaluator)),
                AkkaNames.GenomeSorter);

            this.ResultStorageActor = this.ActorSystem.ActorOf(
                Props.Create(() => new ResultStorageActor<TestInstance, TestResult>()),
                AkkaNames.ResultStorageActor);

            this.GenomeBuilder = new GenomeBuilder(this.GetDefaultParameterTree(), configuration);
        }

        /// <summary>
        /// Creates a simple <see cref="AlgorithmTunerConfiguration"/> containing the provided focus on incumbent 
        /// option for CMA-ES strategies.
        /// </summary>
        /// <param name="focusOnIncumbent">Specifies the value to set for the focus on incumbent option.</param>
        /// <returns>The newly created <see cref="AlgorithmTunerConfiguration"/>.</returns>
        private static AlgorithmTunerConfiguration CreateConfiguration(bool focusOnIncumbent)
        {
            return new AlgorithmTunerConfiguration.AlgorithmTunerConfigurationBuilder()
                .SetEnableRacing(true)
                .AddDetailedConfigurationBuilder(
                    CovarianceMatrixAdaptationStrategyArgumentParser.Identifier,
                    new CovarianceMatrixAdaptationStrategyConfiguration.CovarianceMatrixAdaptationStrategyConfigurationBuilder()
                        .SetFocusOnIncumbent(focusOnIncumbent))
                .Build(maximumNumberParallelEvaluations: 1);
        }

        #endregion
    }
}