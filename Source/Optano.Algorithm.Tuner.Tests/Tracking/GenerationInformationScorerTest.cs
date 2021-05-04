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

namespace Optano.Algorithm.Tuner.Tests.Tracking
{
    using System;
    using System.Collections.Generic;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.InstanceValueConsideration;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;
    using Optano.Algorithm.Tuner.Tracking;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="GenerationInformationScorer{TInstance,TResult}"/> class.
    /// </summary>
    public class GenerationInformationScorerTest : TestBase
    {
        #region Fields

        /// <summary>
        /// Structure representing the tuneable parameters.
        /// </summary>
        private readonly ParameterTree _parameterTree = new ParameterTree(
            new ValueNode<int>(ExtractIntegerValue.ParameterName, new IntegerDomain(-1000, 1000)));

        /// <summary>
        /// The <see cref="Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators.IMetricRunEvaluator{TInstance, TResult}"/>
        /// to score the target algorithm run results.
        /// </summary>
        private readonly SortByDescendingIntegerValue<InstanceSeedFile> _runEvaluator = new SortByDescendingIntegerValue<InstanceSeedFile>();

        /// <summary>
        /// An <see cref="IActorRef" /> to a <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}" />.
        /// </summary>
        private IActorRef _generationEvaluationActor;

        /// <summary>
        /// An <see cref="IActorRef" /> to a <see cref="ResultStorageActor{TInstance,TResult}" />
        /// which knows about all executed target algorithm runs and their results.
        /// </summary>
        private IActorRef _resultStorageActor;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that <see cref="GenerationInformationScorer{TInstance,TResult}"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without a reference to a
        /// <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance, TResult}"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingGenerationEvaluationActor()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenerationInformationScorer<InstanceSeedFile, IntegerResult>(
                    null,
                    this._resultStorageActor,
                    this._runEvaluator));
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformationScorer{TInstance,TResult}"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without a reference to a
        /// <see cref="ResultStorageActor{TInstance, TResult}"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingResultStorageActor()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenerationInformationScorer<InstanceSeedFile, IntegerResult>(
                    this._generationEvaluationActor,
                    null,
                    this._runEvaluator));
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformationScorer{TInstance,TResult}"/>'s constructor throws a
        /// <see cref="ArgumentNullException"/> if called without a reference to a
        /// <see cref="Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators.IMetricRunEvaluator{TInstance, TResult}"/>.
        /// </summary>
        [Fact]
        public void ConstructorThrowsForMissingRunEvaluator()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenerationInformationScorer<InstanceSeedFile, IntegerResult>(
                    this._generationEvaluationActor,
                    this._resultStorageActor,
                    null));
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformationScorer{TInstance,TResult}.ScoreInformationHistory"/> throws a
        /// <see cref="ArgumentNullException"/> if called without any <see cref="GenerationInformation"/> objects.
        /// </summary>
        [Fact]
        public void ScoreInformationHistoryThrowsForMissingHistory()
        {
            var scorer = new GenerationInformationScorer<InstanceSeedFile, IntegerResult>(
                this._generationEvaluationActor,
                this._resultStorageActor,
                this._runEvaluator);
            Assert.Throws<ArgumentNullException>(
                () => scorer.ScoreInformationHistory(
                    informationHistory: null,
                    trainingInstances: GenerationInformationScorerTest.CreateInstances(0, 1),
                    testInstances: GenerationInformationScorerTest.CreateInstances(0, 1)));
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformationScorer{TInstance,TResult}.ScoreInformationHistory"/> throws a
        /// <see cref="ArgumentNullException"/> if called without a set of training instances.
        /// </summary>
        [Fact]
        public void ScoreInformationHistoryThrowsForMissingTrainingInstancesSet()
        {
            var scorer = new GenerationInformationScorer<InstanceSeedFile, IntegerResult>(
                this._generationEvaluationActor,
                this._resultStorageActor,
                this._runEvaluator);

            var dummyInformation = new GenerationInformation(0, TimeSpan.Zero, 0, typeof(int), new ImmutableGenome(new Genome()), "id");
            Assert.Throws<ArgumentNullException>(
                () => scorer.ScoreInformationHistory(
                    informationHistory: new List<GenerationInformation> { dummyInformation },
                    trainingInstances: null,
                    testInstances: GenerationInformationScorerTest.CreateInstances(0, 1)));
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformationScorer{TInstance,TResult}.ScoreInformationHistory"/> throws a
        /// <see cref="ArgumentOutOfRangeException"/> if called with an empty set of training instances.
        /// </summary>
        [Fact]
        public void ScoreInformationHistoryThrowsForEmptySetOfTrainingInstances()
        {
            var scorer = new GenerationInformationScorer<InstanceSeedFile, IntegerResult>(
                this._generationEvaluationActor,
                this._resultStorageActor,
                this._runEvaluator);

            var dummyInformation = new GenerationInformation(0, TimeSpan.Zero, 0, typeof(int), new ImmutableGenome(new Genome()), "id");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => scorer.ScoreInformationHistory(
                    informationHistory: new List<GenerationInformation> { dummyInformation },
                    trainingInstances: new List<InstanceSeedFile>(),
                    testInstances: GenerationInformationScorerTest.CreateInstances(0, 1)));
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformationScorer{TInstance,TResult}.ScoreInformationHistory"/> throws a
        /// <see cref="ArgumentNullException"/> if called without a set of test instances.
        /// </summary>
        [Fact]
        public void ScoreInformationHistoryThrowsForMissingTestInstancesSet()
        {
            var scorer = new GenerationInformationScorer<InstanceSeedFile, IntegerResult>(
                this._generationEvaluationActor,
                this._resultStorageActor,
                this._runEvaluator);

            var dummyInformation = new GenerationInformation(0, TimeSpan.Zero, 0, typeof(int), new ImmutableGenome(new Genome()), "id");
            Assert.Throws<ArgumentNullException>(
                () => scorer.ScoreInformationHistory(
                    informationHistory: new List<GenerationInformation> { dummyInformation },
                    trainingInstances: GenerationInformationScorerTest.CreateInstances(0, 1),
                    testInstances: null));
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformationScorer{TInstance,TResult}.ScoreInformationHistory"/> adds the
        /// correct scores to objects.
        /// </summary>
        [Fact]
        public void ScoreInformationHistoryScoresGenerationInformationObjects()
        {
            var incumbent1 = new Genome();
            incumbent1.SetGene(ExtractIntegerValue.ParameterName, new Allele<int>(-2));
            var generationInformation = new GenerationInformation(0, TimeSpan.Zero, 0, typeof(int), new ImmutableGenome(incumbent1), "id");

            var scorer = new GenerationInformationScorer<InstanceSeedFile, IntegerResult>(
                this._generationEvaluationActor,
                this._resultStorageActor,
                this._runEvaluator);
            scorer.ScoreInformationHistory(
                new List<GenerationInformation> { generationInformation },
                trainingInstances: GenerationInformationScorerTest.CreateInstances(startSeed: 50, number: 4),
                testInstances: GenerationInformationScorerTest.CreateInstances(startSeed: -3, number: 2));

            Assert.Equal(
                -103,
                generationInformation.IncumbentTrainingScore);
            Assert.Equal(
                5,
                generationInformation.IncumbentTestScore);
        }

        /// <summary>
        /// Checks that <see cref="GenerationInformationScorer{TInstance,TResult}.ScoreInformationHistory"/> adds the
        /// correct scores to objects also if no test instances exist.
        /// </summary>
        [Fact]
        public void ScoreInformationHistoryScoresGenerationInformationObjectsWithoutTestInstances()
        {
            var incumbent1 = new Genome();
            incumbent1.SetGene(ExtractIntegerValue.ParameterName, new Allele<int>(-2));
            var generationInformation = new GenerationInformation(0, TimeSpan.Zero, 0, typeof(int), new ImmutableGenome(incumbent1), "id");

            var scorer = new GenerationInformationScorer<InstanceSeedFile, IntegerResult>(
                this._generationEvaluationActor,
                this._resultStorageActor,
                this._runEvaluator);
            scorer.ScoreInformationHistory(
                new List<GenerationInformation> { generationInformation },
                trainingInstances: GenerationInformationScorerTest.CreateInstances(startSeed: 50, number: 4),
                testInstances: new List<InstanceSeedFile>());

            Assert.Equal(
                -103,
                generationInformation.IncumbentTrainingScore);
            Assert.Null(
                generationInformation.IncumbentTestScore);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called before every test.
        /// </summary>
        protected override void InitializeDefault()
        {
            base.InitializeDefault();

            var configuration = this.GetDefaultAlgorithmTunerConfiguration();

            this.ActorSystem = ActorSystem.Create(TestBase.ActorSystemName, configuration.AkkaConfiguration);
            this._resultStorageActor = this.ActorSystem.ActorOf(
                Props.Create(
                    () => new ResultStorageActor<InstanceSeedFile, IntegerResult>()),
                AkkaNames.ResultStorageActor);
            this._generationEvaluationActor = this.ActorSystem.ActorOf(
                Props.Create(
                    () => new GenerationEvaluationActor<MultiplyIntegerWithSeed, InstanceSeedFile, IntegerResult>(
                        new MultiplyIntegerWithSeedCreator(),
                        this._runEvaluator,
                        configuration,
                        this._resultStorageActor,
                        this._parameterTree,
                        null)),
                AkkaNames.GenerationEvaluationActor);
        }

        /// <summary>
        /// Creates <see cref="InstanceSeedFile"/>s with different consecutive seeds.
        /// </summary>
        /// <param name="startSeed">The start seed.</param>
        /// <param name="number">The number of instances to create.</param>
        /// <returns>The created <see cref="InstanceSeedFile"/>.</returns>
        private static List<InstanceSeedFile> CreateInstances(int startSeed, int number)
        {
            var instances = new List<InstanceSeedFile>();
            for (int i = startSeed; i < startSeed + number; i++)
            {
                instances.Add(new InstanceSeedFile(path: i.ToString(), seed: i));
            }

            return instances;
        }

        #endregion
    }
}