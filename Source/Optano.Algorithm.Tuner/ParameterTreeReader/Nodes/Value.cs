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

// ReSharper disable once CheckNamespace
// Namespace is required due to partial class.
namespace Optano.Algorithm.Tuner.ParameterTreeReader.Elements
{
    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    /// <summary>
    /// Represents a simple node in the parameter tree that's neither an AND nor an OR node.
    /// This class mirrors an XML element.
    /// </summary>
    /// <remarks>This is the part of the class that was *not* automatically generated and is responsible for converting
    /// the class mirroring the XML element into the behavior implementing class located at Data/Parameters.
    ///
    /// The class definition is marked as generated because it cannot be changed and StyleCop warnings
    /// (e.g. capitalization) can't be fixed here.
    /// </remarks>

    #region Generated Code

#pragma warning disable SA1300 // Element should begin with upper-case letter
    public partial class value
#pragma warning restore SA1300 // Element should begin with upper-case letter

        #endregion

    {
        #region Methods

        /// <summary>
        /// Converts this node to a <see cref="ValueNode{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of values the represented parameter can take.</typeparam>
        /// <returns>The converted <see cref="ValueNode{T}"/>.</returns>
        protected override IParameterTreeNode ConvertToParameterTreeNode<T>()
        {
            var node = new ValueNode<T>(this.id, this.domain.ConvertToParameterTreeDomain() as DomainBase<T>);
            node.SetChild(this.node?.ConvertToParameterTreeNode());
            return node;
        }

        #endregion
    }
}