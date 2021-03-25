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
    using System.Xml;

    using Optano.Algorithm.Tuner.Parameters.Domains;
    using Optano.Algorithm.Tuner.Parameters.ParameterTreeNodes;

    /// <summary>
    /// Represents an OR node in the parameter tree as defined by an XML document.
    /// </summary>
    /// <remarks>This is the part of the class that was *not* automatically generated and is responsible for converting
    /// the class mirroring the XML element into the behavior implementing class located at Data/Parameters.
    ///
    /// The class definition is marked as generated because it cannot be changed and StyleCop warnings
    /// (e.g. capitalization) can't be fixed here.
    /// </remarks>

    #region Generated Code

#pragma warning disable SA1300 // Element should begin with upper-case letter
    public partial class or
#pragma warning restore SA1300 // Element should begin with upper-case letter

        #endregion

    {
        #region Methods

        /// <summary>
        /// Converts this node to an <see cref="OrNode{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of values the represented parameter can take.</typeparam>
        /// <returns>The converted <see cref="OrNode{T}"/>.</returns>
        /// <exception cref="XmlException">Thrown if the object was read from XML in such a way that it
        /// does not represent a valid <see cref="IParameterTreeNode"/> object.</exception>
        protected override IParameterTreeNode ConvertToParameterTreeNode<T>()
        {
            // Cast domain to correct type.
            CategoricalDomain<T> categoricalDomain =
                this.domain.ConvertToParameterTreeDomain() as CategoricalDomain<T>;
            if (categoricalDomain == null)
            {
                throw new XmlException(
                    $"Domain of OR node '{this.id}' was not of type {typeof(CategoricalDomain<T>)} as expected.");
            }

            // Create OR node.
            var node = new OrNode<T>(this.id, categoricalDomain);

            // Add children:
            foreach (var choice in this.choice)
            {
                // Check activator is of correct type.
                if (!(choice.Item is T))
                {
                    throw new XmlException(
                        $"OR node '{this.id}' had a choice of type {choice.Item.GetType()} instead of {typeof(T)}.");
                }

                // Add child.
                node.AddChild((T)choice.Item, choice.child.ConvertToParameterTreeNode());
            }

            return node;
        }

        #endregion
    }
}