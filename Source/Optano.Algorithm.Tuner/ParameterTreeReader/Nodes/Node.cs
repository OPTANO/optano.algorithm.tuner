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

// ReSharper disable once CheckNamespace
// Namespace is required due to partial class.
namespace Optano.Algorithm.Tuner.ParameterTreeReader.Elements
{
    using System.Diagnostics.CodeAnalysis;

    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    /// <summary>
    /// Represents a node in the parameter tree as defined by an XML document.
    /// </summary>
    /// <remarks>This is the part of the class that was *not* automatically generated and is responsible for converting
    /// the class mirroring the XML element into the behavior implementing class located at Data/Parameters.</remarks>
    [SuppressMessage(
        "NDepend",
        "ND2003:AbstractBaseClassShouldBeSuffixedWithBase",
        Justification = "Naming needs to match rules of code generator for XML tree parser.")]
    public abstract partial class Node
    {
        #region Methods

        /// <summary>
        /// Converts this node to an <see cref="IParameterTreeNode"/>.
        /// </summary>
        /// <returns>The converted <see cref="IParameterTreeNode"/>.</returns>
        /// <exception cref="System.Xml.XmlException">Thrown if the object was read from XML in such a way that it
        /// does not represent a valid <see cref="IParameterTreeNode"/> object.</exception>
        internal abstract IParameterTreeNode ConvertToParameterTreeNode();

        #endregion
    }
}