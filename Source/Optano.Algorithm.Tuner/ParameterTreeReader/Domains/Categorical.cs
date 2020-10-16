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
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Parameters.Domains;

    /// <summary>
    /// Represents a categorical domain as defined by an XML document.
    /// </summary>
    /// <remarks>This is the part of the class that was *not* automatically generated and is responsible for converting
    /// the class mirroring the XML element into the behavior implementing class located at Data/Parameters.
    ///
    /// The class definition is marked as generated because it cannot be changed and StyleCop warnings
    /// (e.g. capitalization) can't be fixed here.
    /// </remarks>

    #region Generated Code

#pragma warning disable SA1300 // Element should begin with upper-case letter
    public partial class categorical
#pragma warning restore SA1300 // Element should begin with upper-case letter

        #endregion

    {
        #region Properties

        /// <summary>
        /// Gets the <see cref="Type"/> of values in the domain, i. e. <see cref="string"/>.
        /// </summary>
        internal override Type ValueType
        {
            get
            {
                var enumerator = this.Categories.GetEnumerator();
                enumerator.MoveNext();
                return enumerator.Current.GetType();
            }
        }

        /// <summary>
        /// Gets all possible categories.
        /// </summary>
        private IEnumerable Categories
        {
            get
            {
                // Find all category field which are not null.
                var categoryGroups = new List<IEnumerable>
                                         {
                                             this.doubles.AsEnumerable(),
                                             this.ints.AsEnumerable(),
                                             this.booleans.AsEnumerable(),
                                             this.strings.AsEnumerable(),
                                         }
                    .Where(element => element != null);

                // There should only be one of those.
                if (categoryGroups.Count() > 1)
                {
                    throw new System.Xml.XmlException("Found categorical domain containing more than one type.");
                }

                // Return it.
                return categoryGroups.Single();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Converts this domain to an <see cref="IDomain"/>.
        /// </summary>
        /// <returns>The converted <see cref="IDomain"/>.</returns>
        /// <exception cref="System.Xml.XmlException">Thrown if the object was read from XML in such a way that it
        /// does not represent a valid <see cref="IDomain"/> object.</exception>
        internal override IDomain ConvertToParameterTreeDomain()
        {
            // Find convert method using the correct generic parameter (this.ValueType).
            MethodInfo method = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(m => m.IsGenericMethod && m.Name.Equals("ConvertToParameterTreeDomain"));
            MethodInfo generic = method.MakeGenericMethod(this.ValueType);

            // Invoke it.
            return generic.Invoke(this, null) as IDomain;
        }

        /// <summary>
        /// Converts this domain to a <see cref="CategoricalDomain{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of values in the domain.</typeparam>
        /// <returns>The converted <see cref="CategoricalDomain{T}"/>.</returns>
        private IDomain ConvertToParameterTreeDomain<T>()
        {
            // Cast the categories to the correct type.
            var typedCategories = new List<T>();
            var enumerator = this.Categories.GetEnumerator();
            while (enumerator.MoveNext())
            {
                typedCategories.Add((T)enumerator.Current);
            }

            if (this.defaultIndexOrValueSpecified)
            {
                var defaultIndex = (int)this.defaultIndexOrValue;
                var defaultValue = typedCategories[defaultIndex];

                return new CategoricalDomain<T>(typedCategories, new Allele<T>(defaultValue));
            }
            else
            {
                // Create categorical domain.
                return new CategoricalDomain<T>(typedCategories);
            }
        }

        #endregion
    }
}