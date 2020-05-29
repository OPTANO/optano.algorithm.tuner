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

namespace Optano.Algorithm.Tuner.Parameters
{
    using System;

    using Optano.Algorithm.Tuner.Genomes.Values;

    /// <summary>
    /// A wrapper that stores information about a dummy parameter
    /// and how it should be replaced/filtered before the <see cref="ParameterTree"/>
    /// is passed to the target algorithm.
    /// If the current value of <see cref="IndicatorParameterIdentifier"/> matches <see cref="IndicatorParameterValue"/>,
    /// the combination of <see cref="ControlledParameterIdentifier"/>/<see cref="NativeOverrideValue"/> will be
    /// inserted into the dictionary with the ActiveParameters, <c>and</c> the <see cref="IndicatorParameterIdentifier"/> will be removed.
    /// </summary>
    public class ParameterReplacementDefinition
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterReplacementDefinition"/> class.
        /// </summary>
        /// <param name="indicatorParameterIdentifier">
        /// The <see cref="IndicatorParameterIdentifier"/> that is checked for <paramref name="indicatorParameterValue"/>.
        /// </param>
        /// <param name="indicatorParameterValue">
        /// The <see cref="IndicatorParameterValue"/> that triggers the replacement of 
        /// <see cref="IndicatorParameterIdentifier"/> with <see cref="ControlledParameterIdentifier"/>.
        /// </param>
        /// <param name="controlledParameterIdentifier">
        /// The identifier of the parameter that is inserted if indicator is matched.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Values must not be null (or white space). 
        /// </exception>
        internal ParameterReplacementDefinition(
            string indicatorParameterIdentifier,
            object indicatorParameterValue,
            string controlledParameterIdentifier)
        {
            if (string.IsNullOrWhiteSpace(indicatorParameterIdentifier))
            {
                throw new ArgumentNullException(nameof(indicatorParameterIdentifier));
            }

            if (string.IsNullOrWhiteSpace(controlledParameterIdentifier))
            {
                throw new ArgumentNullException(nameof(controlledParameterIdentifier));
            }

            if (indicatorParameterValue == null)
            {
                throw new ArgumentNullException(nameof(indicatorParameterValue));
            }

            this.IndicatorParameterIdentifier = indicatorParameterIdentifier;
            this.IndicatorParameterValue = indicatorParameterValue;
            this.ControlledParameterIdentifier = controlledParameterIdentifier;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the identifier of the artificial parameter that should be replaced.
        /// </summary>
        public string IndicatorParameterIdentifier { get; }

        /// <summary>
        /// Gets the indicator parameter value.
        /// If the current value of <see cref="IndicatorParameterIdentifier"/> matches this value,
        /// it will be replaced with the combination of <see cref="ControlledParameterIdentifier"/>/<see cref="NativeOverrideValue"/>.
        /// </summary>
        public object IndicatorParameterValue { get; }

        /// <summary>
        /// Gets the identifier of the parameter that is inserted, if the current value of 
        /// <see cref="IndicatorParameterIdentifier"/> matches the specified <see cref="IndicatorParameterValue"/>.
        /// </summary>
        public string ControlledParameterIdentifier { get; }

        /// <summary>
        /// Gets the <see cref="IAllele"/> that will be inserted for the current <see cref="IndicatorParameterIdentifier"/>
        /// if the indicator parameter's value matches <see cref="IndicatorParameterValue"/>.
        /// </summary>
        public IAllele NativeOverrideValue { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the value for the <see cref="ControlledParameterIdentifier"/>,
        /// that should be used when the <see cref="IndicatorParameterIdentifier"/> parameter
        /// is set to <see cref="IndicatorParameterValue"/> in the current genome.
        /// </summary>
        /// <param name="value">
        /// The value for the <see cref="IAllele"/>.
        /// </param>
        /// <typeparam name="T">
        /// The value type of the <see cref="Allele{T}"/>.
        /// </typeparam>
        internal void SetNativeOverrideValue<T>(T value)
        {
            this.NativeOverrideValue = new Allele<T>(value);
        }

        #endregion
    }
}