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

namespace Optano.Algorithm.Tuner.GrayBox.DataRecordTypes
{
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Base class for the adapter features class of the target algorithm adapter.
    /// </summary>
    [SuppressMessage(
        "NDepend",
        "ND1205:AStatelessClassOrStructureMightBeTurnedIntoAStaticType",
        Justification = "Checked! We do not want this base class to be static.")]
    public abstract class AdapterFeaturesBase
    {
        #region Public Methods and Operators

        /// <summary>
        /// Returns the header.
        /// </summary>
        /// <param name="prefix">The prefix of each header.</param>
        /// <param name="suffix">The suffix of each header.</param>
        /// <returns>The header.</returns>
        public string[] GetHeader(string prefix = "", string suffix = "")
        {
            var orderedPublicDoubleProperties = this.GetOrderedPublicDoubleProperties();
            return orderedPublicDoubleProperties.Select(p => $"{prefix}{p.Name}{suffix}").ToArray();
        }

        /// <summary>
        /// Returns the adapter features as double array.
        /// </summary>
        /// <returns>The double array.</returns>
        public double[] ToArray()
        {
            var orderedPublicDoubleProperties = this.GetOrderedPublicDoubleProperties();
            return orderedPublicDoubleProperties.Select(p => (double)p.GetValue(this)).ToArray();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns all public double properties of this instance as ordered enumerable.
        /// </summary>
        /// <returns>The ordered enumerable.</returns>
        private IOrderedEnumerable<PropertyInfo> GetOrderedPublicDoubleProperties()
        {
            return this.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(double))
                .OrderBy(p => p.Name);
        }

        #endregion
    }
}