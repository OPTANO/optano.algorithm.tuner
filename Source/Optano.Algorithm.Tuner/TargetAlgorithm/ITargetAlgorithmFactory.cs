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

namespace Optano.Algorithm.Tuner.TargetAlgorithm
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;

    /// <summary>
    /// Class responsible for providing the target algorithm in configured form.
    /// </summary>
    /// <typeparam name="TTargetAlgorithm">Type of the target algorithm.</typeparam>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TResult">The result type of a single target algorithm evaluation.</typeparam>
    public interface ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult>
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
    {
        #region Public Methods and Operators

        /// <summary>
        /// Configures the target algorithm using the given parameters.
        /// </summary>
        /// <param name="parameters">The parameters to configure the target algorithm with.</param>
        /// <returns>The configured target algorithm.</returns>
        TTargetAlgorithm ConfigureTargetAlgorithm(Dictionary<string, IAllele> parameters);

        /// <summary>
        /// Tries to get the result from the given string array. This method is the counterpart to <see cref="ResultBase{TResultType}.ToStringArray"/>.
        /// </summary>
        /// <param name="stringArray">The string array.</param>
        /// <param name="result">The result.</param>
        /// <returns>True, if successful.</returns>
        bool TryToGetResultFromStringArray(string[] stringArray, out TResult result)
        {
            result = null;

            if (typeof(TResult) == typeof(RuntimeResult))
            {
                if (stringArray.Length != 2)
                {
                    return false;
                }

                if (!Enum.TryParse(stringArray[0], true, out TargetAlgorithmStatus targetAlgorithmStatus))
                {
                    return false;
                }

                if (!double.TryParse(stringArray[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var runtime))
                {
                    return false;
                }

                result = new RuntimeResult(TimeSpan.FromMilliseconds(runtime), targetAlgorithmStatus) as TResult;
                return true;
            }

            if (typeof(TResult) == typeof(ContinuousResult))
            {
                if (stringArray.Length != 3)
                {
                    return false;
                }

                if (!Enum.TryParse(stringArray[0], true, out TargetAlgorithmStatus targetAlgorithmStatus))
                {
                    return false;
                }

                if (!double.TryParse(stringArray[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var runtime))
                {
                    return false;
                }

                if (!double.TryParse(stringArray[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                {
                    return false;
                }

                result = new ContinuousResult(value, TimeSpan.FromMilliseconds(runtime), targetAlgorithmStatus) as TResult;
                return true;
            }

            throw new NotImplementedException("You cannot use this method without implementing it for your result type.");
        }

        /// <summary>
        /// Tries to get the instance from the given instance id. This method is the counterpart to <see cref="InstanceBase.ToId"/>.
        /// </summary>
        /// <param name="instanceId">The instance id.</param>
        /// <param name="instance">The instance.</param>
        /// <returns>True, if successful.</returns>
        bool TryToGetInstanceFromInstanceId(string instanceId, out TInstance instance)
        {
            instance = null;

            if (typeof(TInstance) == typeof(InstanceFile))
            {
                instance = new InstanceFile(instanceId) as TInstance;
                return true;
            }

            if (typeof(TInstance) == typeof(InstanceSeedFile))
            {
                var indexOfDelimiter = instanceId.LastIndexOf("_", StringComparison.Ordinal);
                if (indexOfDelimiter <= 0)
                {
                    return false;
                }

                var path = instanceId.Substring(0, indexOfDelimiter);

                if (!int.TryParse(instanceId.Substring(indexOfDelimiter + 1), out var seed))
                {
                    return false;
                }

                instance = new InstanceSeedFile(path, seed) as TInstance;
                return true;
            }

            throw new NotImplementedException("You cannot use this method without implementing it for your instance type.");
        }

        #endregion
    }
}