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
    using System.Collections.Generic;
    using System.Linq;

    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;

    /// <summary>
    /// Helper class that stores and manages the replacement of "dummy" parameters from the
    /// <see cref="ParameterTree"/> before its values are passed to the target algorithm.
    /// </summary>
    public class ReplacedParameterFilter
    {
        #region Fields

        /// <summary>
        /// Controls the access to <see cref="ActiveParameterReplacements"/> and <see cref="IgnoredParameterIdentifiers"/>.
        /// </summary>
        private readonly object _filterLock;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplacedParameterFilter"/> class.
        /// </summary>
        public ReplacedParameterFilter()
        {
            this._filterLock = new object();
            this.ActiveParameterReplacements = new List<ParameterReplacementDefinition>();
            this.IgnoredParameterIdentifiers = new HashSet<string>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the active parameter wrappers.
        /// </summary>
        private List<ParameterReplacementDefinition> ActiveParameterReplacements { get; }

        /// <summary>
        /// Gets the ignored parameter identifiers.
        /// </summary>
        private HashSet<string> IgnoredParameterIdentifiers { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Defines a new replacement for an artifical parameter.
        /// </summary>
        /// <param name="indicatorParameterIdentifier">
        /// The identifier of the indicator (=dummy) parameter that needs to be replaced.
        /// This parameter will always be removed from the genomes set of active parameters.
        /// </param>
        /// <param name="indicatorParameterValue">
        /// If the <paramref name="indicatorParameterIdentifier"/> is set to <paramref name="indicatorParameterValue"/>
        /// in the current genome, the <paramref name="controlledParameterIdentifier"/>/<paramref name="nativeOverrideValue"/>
        /// will be inserted in the set of the <see cref="Genome.GetActiveGenes"/>.
        /// </param>
        /// <param name="controlledParameterIdentifier">
        /// The identifier of the parameter that will be inserted for <paramref name="indicatorParameterIdentifier"/>
        /// when it has the value <paramref name="indicatorParameterValue"/>.
        /// </param>
        /// <param name="nativeOverrideValue">
        /// The value to insert for <paramref name="controlledParameterIdentifier"/>.
        /// </param>
        /// <typeparam name="T">
        /// The value type of the inserted <see cref="Allele{T}"/>.
        /// </typeparam>
        /// <returns>
        /// The added <see cref="ParameterReplacementDefinition"/>.
        /// </returns>
        public ParameterReplacementDefinition DefineParameterReplacement<T>(
            string indicatorParameterIdentifier,
            object indicatorParameterValue,
            string controlledParameterIdentifier,
            T nativeOverrideValue)
        {
            lock (this._filterLock)
            {
                if (this.IsIndicatorParameterAndValueCombinationDefined(indicatorParameterIdentifier, indicatorParameterValue))
                {
                    throw new InvalidOperationException(
                        $"You cannot add more than one active ParameterReplacementDefinition for a given combination of {nameof(indicatorParameterIdentifier)}/{nameof(indicatorParameterValue)}.\r\nA replacement for <{indicatorParameterIdentifier}, {indicatorParameterValue}> is already defined.");
                }

                var wrapper = new ParameterReplacementDefinition(
                    indicatorParameterIdentifier,
                    indicatorParameterValue,
                    controlledParameterIdentifier);
                wrapper.SetNativeOverrideValue(nativeOverrideValue);
                this.ActiveParameterReplacements.Add(wrapper);
                return wrapper;
            }
        }

        /// <summary>
        /// Adds an ignored parameter definition.
        /// Ignored parameters will always be removed from the  set of 
        /// active parameters when <see cref="HandleSpecialCases"/> is called.
        /// </summary>
        /// <param name="ignoredParameterName">
        /// The identifier of the parameter that should be ignored.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="ignoredParameterName"/> must not be null.
        /// </exception>
        public void AddIgnoredParameterDefinition(string ignoredParameterName)
        {
            if (string.IsNullOrWhiteSpace(ignoredParameterName))
            {
                throw new ArgumentNullException(nameof(ignoredParameterName));
            }

            lock (this._filterLock)
            {
                this.IgnoredParameterIdentifiers.Add(ignoredParameterName);
            }
        }

        /// <summary>
        /// Handles the removal (and replacement) of <see cref="ParameterReplacementDefinition.IndicatorParameterIdentifier"/>s
        /// with the specified native parameters.
        /// Also removes all <see cref="IgnoredParameterIdentifiers"/> from <paramref name="parameters"/>.
        /// </summary>
        /// <param name="parameters">
        /// The genome's <see cref="Genome.GetActiveGenes"/>.
        /// </param>
        public void HandleSpecialCases(Dictionary<string, IAllele> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            lock (this._filterLock)
            {
                foreach (var parameterWrapper in this.ActiveParameterReplacements)
                {
                    InsertNativeParameterForIndicator(parameters, parameterWrapper);
                }

                foreach (var ignoredParameter in this.IgnoredParameterIdentifiers)
                {
                    parameters.Remove(ignoredParameter);
                }
            }
        }

        /// <summary>
        /// Checks if a <see cref="ParameterReplacementDefinition"/> for the given
        /// &lt;<paramref name="indicatorParameterIdentifier"/>, <paramref name="indicatorParameterValue"/>&gt;
        /// is defined.
        /// </summary>
        /// <param name="indicatorParameterIdentifier">
        /// The indicator parameter name.
        /// </param>
        /// <param name="indicatorParameterValue">
        /// The indicator parameter value.
        /// </param>
        /// <returns>
        /// <c>True</c>, if a <see cref="ParameterReplacementDefinition"/> is defined.
        /// <c>False</c>, else.
        /// </returns>
        public bool IsIndicatorParameterAndValueCombinationDefined(string indicatorParameterIdentifier, object indicatorParameterValue)
        {
            lock (this._filterLock)
            {
                return this.ActiveParameterReplacements.Any(
                    p => string.Equals(indicatorParameterIdentifier, p.IndicatorParameterIdentifier, StringComparison.InvariantCulture)
                         && object.Equals(indicatorParameterValue, p.IndicatorParameterValue));
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Inserts the specified replacement value for the indicator parameter,
        /// if <see cref="CheckIfOverrideIsRequired"/>.
        /// Note: 
        /// <see cref="ParameterReplacementDefinition.IndicatorParameterIdentifier"/> is removed from
        /// <paramref name="parameters"/>, if it is contained <c>and</c> the current value 
        /// matches <see cref="ParameterReplacementDefinition.IndicatorParameterValue"/>.
        /// </summary>
        /// <param name="parameters">
        /// The genome's <see cref="Genome.GetActiveGenes"/>.
        /// </param>
        /// <param name="parameterReplacementDefinition">
        /// The parameter wrapper to handle.
        /// </param>
        private static void InsertNativeParameterForIndicator(
            Dictionary<string, IAllele> parameters,
            ParameterReplacementDefinition parameterReplacementDefinition)
        {
            IAllele dummyIndicatorParameter;
            if (!parameters.TryGetValue(parameterReplacementDefinition.IndicatorParameterIdentifier, out dummyIndicatorParameter))
            {
                return;
            }

            var overrideOrSetNativeValue = CheckIfOverrideIsRequired(dummyIndicatorParameter, parameterReplacementDefinition);
            if (overrideOrSetNativeValue)
            {
                parameters[parameterReplacementDefinition.ControlledParameterIdentifier] = parameterReplacementDefinition.NativeOverrideValue;
                parameters.Remove(parameterReplacementDefinition.IndicatorParameterIdentifier);
            }
        }

        /// <summary>
        /// Check if override of <see cref="ParameterReplacementDefinition.IndicatorParameterIdentifier"/> is required.
        /// </summary>
        /// <param name="indicatorInCurrentTree">
        /// The indicator's value in the current tree.
        /// </param>
        /// <param name="replacementDefinitionWithOverride">
        /// The wrapper with override.
        /// </param>
        /// <returns>
        /// True, if <paramref name="indicatorInCurrentTree"/>.GetValue().Equals(<see cref="ParameterReplacementDefinition.IndicatorParameterValue"/>).
        /// </returns>
        private static bool CheckIfOverrideIsRequired(
            IAllele indicatorInCurrentTree,
            ParameterReplacementDefinition replacementDefinitionWithOverride)
        {
            return object.Equals(indicatorInCurrentTree.GetValue(), replacementDefinitionWithOverride.IndicatorParameterValue);
        }

        #endregion
    }
}