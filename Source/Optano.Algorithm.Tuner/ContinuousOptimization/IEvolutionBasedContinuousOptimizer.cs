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

namespace Optano.Algorithm.Tuner.ContinuousOptimization
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents an evolutionary algorithm for continuous optimization with the ability to (de)serialize its internal
    /// state.
    /// </summary>
    /// <typeparam name="TSearchPoint">The type of <see cref="SearchPoint"/> handled by the algorithm.</typeparam>
    public interface IEvolutionBasedContinuousOptimizer<out TSearchPoint>
        where TSearchPoint : SearchPoint
    {
        #region Public Methods and Operators

        /// <summary>
        /// Executes a single generation.
        /// </summary>
        /// <returns>Current population, best individuals first.</returns>
        IEnumerable<TSearchPoint> NextGeneration();

        /// <summary>
        /// Checks whether any termination criterion is met.
        /// </summary>
        /// <returns>True if and only if at least one termination criterion is met.</returns>
        bool AnyTerminationCriterionMet();

        /// <summary>
        /// Writes all internal data to file.
        /// </summary>
        /// <param name="pathToStatusFile">Path to the file to write.</param>
        void DumpStatus(string pathToStatusFile);

        /// <summary>
        /// Reads all internal data from file.
        /// </summary>
        /// <param name="pathToStatusFile">Path to the file to read.</param>
        void UseStatusDump(string pathToStatusFile);

        #endregion
    }
}