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

namespace Optano.Algorithm.Tuner.Tests.Parameters.ParameterConverters
{
    using System.Collections.Generic;

    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterConverters;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="TolerantGenomeTransformation"/> class.
    /// </summary>
    public class TolerantGenomeTransformationTest
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks <see cref="TolerantGenomeTransformation.RoundToValidValues"/> acts correctly for different
        /// <see cref="IDomain"/>s.
        /// </summary>
        [Fact]
        public void RoundToValidValuesWorks()
        {
            // Create parameter tree with many different domains.
            var root = new AndNode();
            root.AddChild(new ValueNode<string>("a", new CategoricalDomain<string>(new List<string> { "red", "blue" })));
            root.AddChild(new ValueNode<double>("b", new ContinuousDomain()));
            root.AddChild(new ValueNode<double>("c", new LogDomain(1, 16)));
            root.AddChild(new ValueNode<int>("d", new IntegerDomain()));
            root.AddChild(new ValueNode<int>("e", new DiscreteLogDomain(2, 16)));
            var parameterTree = new ParameterTree(root);

            var transformator = new TolerantGenomeTransformation(parameterTree);
            double[] continuousValues = { 0.2, 0.3, 0.6, 0.8, 1.5 };
            double[] expectedValues = { 0, 0.3, 0.6, 1, 2 };
            Assert.Equal(
                expectedValues,
                transformator.RoundToValidValues(continuousValues));
        }

        #endregion
    }
}