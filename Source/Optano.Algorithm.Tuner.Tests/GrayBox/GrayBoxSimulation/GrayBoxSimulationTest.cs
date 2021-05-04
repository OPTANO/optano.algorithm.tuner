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

namespace Optano.Algorithm.Tuner.Tests.GrayBox.GrayBoxSimulation
{
    using System;
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.GrayBox.GrayBoxSimulation;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Contains test for <see cref="GrayBoxSimulation{TTargetAlgorithm,TInstance,TResult}"/>.
    /// </summary>
    public class GrayBoxSimulationTest : IDisposable
    {
        #region Public Methods and Operators

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <summary>
        /// Checks that <see cref="GrayBoxSimulation{TTargetAlgorithm,TInstance,TResult}.GetTournamentStatistics"/> returns correct values.
        /// </summary>
        [Fact]
        public void GetTournamentStatisticsReturnsCorrectValues()
        {
            var firstGenome = new Genome();
            firstGenome.SetGene("id", new Allele<int>(1));
            var secondGenome = new Genome();
            secondGenome.SetGene("id", new Allele<int>(2));
            var thirdGenome = new Genome();
            thirdGenome.SetGene("id", new Allele<int>(3));
            var fourthGenome = new Genome();
            fourthGenome.SetGene("id", new Allele<int>(4));

            var blackBoxRanking = new List<ImmutableGenome>
                                      {
                                          new ImmutableGenome(firstGenome),
                                          new ImmutableGenome(secondGenome),
                                          new ImmutableGenome(thirdGenome),
                                          new ImmutableGenome(fourthGenome),
                                      };

            var grayBoxWinners = new List<ImmutableGenome>
                                     {
                                         new ImmutableGenome(secondGenome),
                                         new ImmutableGenome(thirdGenome),
                                     };

            var (percentageOfTournamentWinnerChanges, adaptedWsCoefficient) =
                GrayBoxSimulation<GrayBoxNoOperation, TestInstance, TestResult>.GetTournamentStatistics(blackBoxRanking, grayBoxWinners);

            percentageOfTournamentWinnerChanges.ShouldBe(0.5);
            Math.Round(adaptedWsCoefficient, 5).ShouldBe(0.41667);
        }

        #endregion
    }
}