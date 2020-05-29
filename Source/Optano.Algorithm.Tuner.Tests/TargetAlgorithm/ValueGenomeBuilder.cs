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

namespace Optano.Algorithm.Tuner.Tests.TargetAlgorithm
{
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations.ValueConsideration;

    /// <summary>
    /// A <see cref="GenomeBuilder"/> which uses predefined values to set for 
    /// <see cref="ExtractIntegerValue.ParameterName"/> instead of creating totally random genomes in
    /// <see cref="CreateRandomGenome"/>.
    /// </summary>
    public class ValueGenomeBuilder : GenomeBuilder
    {
        #region Fields

        /// <summary>
        /// The values to use for the next calls of <see cref="CreateRandomGenome"/>.
        /// </summary>
        private readonly IEnumerator<int> _values;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueGenomeBuilder"/> class.
        /// </summary>
        /// <param name="parameterTree">The parameter tree.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="values">Values to use for calls of <see cref="CreateRandomGenome"/>.</param>
        public ValueGenomeBuilder(ParameterTree parameterTree, AlgorithmTunerConfiguration configuration, IEnumerable<int> values)
            : base(parameterTree, configuration)
        {
            this._values = values.GetEnumerator();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates a random genome with the specified age, then changes the gene
        /// <see cref="ExtractIntegerValue.ParameterName"/> to one specified by <see cref="_values"/>.
        /// </summary>
        /// <param name="age">The new genome's age.</param>
        /// <returns>The created genome.</returns>
        public override Genome CreateRandomGenome(int age)
        {
            var genome = base.CreateRandomGenome(age);
            this._values.MoveNext();
            genome.SetGene(ExtractIntegerValue.ParameterName, new Allele<int>(this._values.Current));

            return genome;
        }

        #endregion
    }
}