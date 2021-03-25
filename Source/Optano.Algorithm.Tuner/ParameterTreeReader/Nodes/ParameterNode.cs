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
    using System.Reflection;
    using System.Xml;

    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    /// <summary>
    /// Represents a parameter node (i.e. not an AND node) in the parameter tree as defined by an XML document.
    /// </summary>
    /// <remarks>This is the part of the class that was *not* automatically generated and is responsible for converting
    /// the class mirroring the XML element into the behavior implementing class located at Data/Parameters.</remarks>
    public abstract partial class ParameterNode
    {
        #region Methods

        /// <summary>
        /// Converts this node to an <see cref="IParameterTreeNode"/>.
        /// </summary>
        /// <returns>The converted <see cref="IParameterTreeNode"/>.</returns>
        /// <exception cref="System.Xml.XmlException">Thrown if the object was read from XML in such a way that it
        /// does not represent a valid <see cref="IParameterTreeNode"/> object.</exception>
        internal override IParameterTreeNode ConvertToParameterTreeNode()
        {
            // Find convert method converting to the correct generic type (the domain's value type).
            MethodInfo method = this.GetType().GetMethod(
                "ConvertToParameterTreeNode",
                BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo generic = method.MakeGenericMethod(this.domain.ValueType);

            // Invoke it.
            try
            {
                return generic.Invoke(this, null) as IParameterTreeNode;
            }
            catch (TargetInvocationException e)
            {
                // XmlExceptions are expected and should be returned as such.
                throw (e.InnerException is XmlException) ? e.InnerException : e;
            }
        }

        /// <summary>
        /// Converts this node to an <see cref="IParameterTreeNode"/> containing a domain of a certain type.
        /// </summary>
        /// <typeparam name="T">The type contained in the domain.</typeparam>
        /// <returns>The converted <see cref="IParameterTreeNode"/>.</returns>
        protected abstract IParameterTreeNode ConvertToParameterTreeNode<T>();

        #endregion
    }
}