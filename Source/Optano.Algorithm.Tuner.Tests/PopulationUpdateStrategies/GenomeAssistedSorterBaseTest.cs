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

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies
{
    using System;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.ContinuousOptimization;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Actors;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;
    using Optano.Algorithm.Tuner.Tests.ContinuousOptimization;
    using Optano.Algorithm.Tuner.Tests.GenomeBuilders;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    using Xunit;

    using SortByValue = Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration.SortByValue;

    /// <summary>
    /// Contains tests for the <see cref="GenomeAssistedSorterBase{TSearchPoint,TInstance}"/> class.
    /// </summary>
    /// <typeparam name="TSearchPoint">
    /// The type of <see cref="SearchPoint"/> handled by the
    /// <see cref="GenomeAssistedSorterBase{TSearchPoint,TInstance}"/>.
    /// </typeparam>
    [Collection(TestUtils.NonParallelCollectionGroupOneName)]
    public abstract class GenomeAssistedSorterBaseTest<TSearchPoint> : SearchPointSorterTestBase<TSearchPoint>
        where TSearchPoint : SearchPoint
    {
        #region Constants

        /// <summary>
        /// The name of a parameter which does not matter for the <see cref="_runEvaluator"/>.
        /// </summary>
        /// <remarks>Starts with 'aaa' to be first in alphabet.</remarks>
        private const string FreeParameterName = "aaa_free";

        #endregion

        #region Fields

        /// <summary>
        /// <see cref="IRunEvaluator{TResult}"/> used in test.
        /// </summary>
        private readonly IRunEvaluator<IntegerResult> _runEvaluator = new SortByValue();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="Optano.Algorithm.Tuner.Parameters.ParameterTree"/> used in tests.
        /// </summary>
        protected ParameterTree ParameterTree { get; private set; }

        /// <summary>
        /// Gets a <see cref="Optano.Algorithm.Tuner.Genomes.GenomeBuilder"/> used in tests.
        /// </summary>
        protected GenomeBuilder GenomeBuilder { get; private set; }

        /// <summary>
        /// Gets a reference to the <see cref="GenomeSorter{TInstance,TResult}"/> used in tests.
        /// </summary>
        protected IActorRef GenomeSorter { get; private set; }

        /// <summary>
        /// Gets the <see cref="ISearchPointSorter{TSearchPoint}"/> used in tests.
        /// </summary>
        protected override ISearchPointSorter<TSearchPoint> Sorter => this.GenomeAssistedSorter;

        /// <summary>
        /// Gets the <see cref="GenomeAssistedSorterBase{TSearchPoint,TInstance}"/> used in tests.
        /// </summary>
        protected abstract GenomeAssistedSorterBase<TSearchPoint, TestInstance> GenomeAssistedSorter { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="GenomeAssistedSorterBase{TSearchPoint, TInstance}"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> iif called without a <see cref="GenomeSorter{TInstance, TResult}"/>.
        /// </summary>
        [Fact]
        public abstract void ConstructorThrowsForMissingGenomeSorter();

        #endregion

        #region Methods

        /// <summary>
        /// Called before every test.
        /// </summary>
        protected override void InitializeDefault()
        {
            var configuration = this.GetDefaultAlgorithmTunerConfiguration();
            this.ParameterTree = GenomeAssistedSorterBaseTest<TSearchPoint>.CreateParameterTree();
            this.ActorSystem = ActorSystem.Create(TestBase.ActorSystemName, configuration.AkkaConfiguration);

            this.GenomeSorter = this.ActorSystem.ActorOf(
                Props.Create(() => new GenomeSorter<TestInstance, IntegerResult>(this._runEvaluator)),
                AkkaNames.GenomeSorter);
            this.BuildUpRelatedActors(this.ActorSystem, configuration);

            this.GenomeBuilder = this.CreateGenomeBuilderWithForbiddenValue(configuration);

            this.InitializeSorter(this.GenomeSorter);
            // Ensure sorting data exists.
            this.GenomeAssistedSorter.UpdateInstances(new[] { new TestInstance("test") });
        }

        /// <summary>
        /// Initializes <see cref="GenomeAssistedSorter"/>.
        /// </summary>
        /// <param name="genomeSorter">
        /// An <see cref="IActorRef" /> to a <see cref="GenomeSorter{TInstance, TResult}" />.
        /// </param>
        protected abstract void InitializeSorter(IActorRef genomeSorter);

        /// <summary>
        /// Creates a <see cref="Optano.Algorithm.Tuner.Parameters.ParameterTree"/> which consists of two independent parameters:
        /// <see cref="FreeParameterName"/> and <see cref="ExtractIntegerValue.ParameterName"/>, both integers between
        /// -5 and 5.
        /// </summary>
        /// <returns>The created <see cref="Optano.Algorithm.Tuner.Parameters.ParameterTree"/>.</returns>
        private static ParameterTree CreateParameterTree()
        {
            var root = new AndNode();
            root.AddChild(new ValueNode<int>(GenomeAssistedSorterBaseTest<TSearchPoint>.FreeParameterName, new IntegerDomain(-5, 5)));
            root.AddChild(new ValueNode<int>(ExtractIntegerValue.ParameterName, new IntegerDomain(-5, 5)));
            return new ParameterTree(root);
        }

        /// <summary>
        /// Builds up a <see cref="TournamentSelector{TTargetAlgorithm,TInstance,TResult}"/>.
        /// This is necessary to have <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>s.
        /// </summary>
        /// <param name="actorSystem">The <see cref="ActorSystem"/> to add the actors to.</param>
        /// <param name="configuration">The <see cref="AlgorithmTunerConfiguration"/> for the actors.</param>
        private void BuildUpRelatedActors(ActorSystem actorSystem, AlgorithmTunerConfiguration configuration)
        {
            var resultStorageActor = actorSystem.ActorOf(
                Props.Create(() => new ResultStorageActor<TestInstance, IntegerResult>()),
                AkkaNames.ResultStorageActor);
            var targetAlgorithmFactory = new ExtractIntegerValueCreator();
            var tournamentSelector = actorSystem.ActorOf(
                Props.Create(
                    () => new TournamentSelector<ExtractIntegerValue, TestInstance, IntegerResult>(
                        targetAlgorithmFactory,
                        this._runEvaluator,
                        configuration,
                        resultStorageActor,
                        this.ParameterTree)),
                AkkaNames.TournamentSelector);
        }

        /// <summary>
        /// Creates a <see cref="Optano.Algorithm.Tuner.Genomes.GenomeBuilder"/> which forbids using "3" for <see cref="FreeParameterName"/>.
        /// </summary>
        /// <param name="configuration">
        /// <see cref="AlgorithmTunerConfiguration"/> for the <see cref="Optano.Algorithm.Tuner.Genomes.GenomeBuilder"/>.
        /// </param>
        /// <returns>The created <see cref="Optano.Algorithm.Tuner.Genomes.GenomeBuilder"/>.</returns>
        private ConfigurableGenomeBuilder CreateGenomeBuilderWithForbiddenValue(AlgorithmTunerConfiguration configuration)
        {
            Func<Genome, bool> invalidityFunction = candidate =>
                !object.Equals(
                    candidate.GetGeneValue(GenomeAssistedSorterBaseTest<TSearchPoint>.FreeParameterName).GetValue(),
                    3);
            return new ConfigurableGenomeBuilder(
                this.ParameterTree,
                invalidityFunction,
                configuration.MutationRate);
        }

        #endregion
    }
}