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

namespace Optano.Algorithm.Tuner.GrayBox.PostTuningRunner
{
    using System;

    /// <summary>
    /// Contains all relevant parameters for the post tuning runner.
    /// </summary>
    public class PostTuningConfiguration
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PostTuningConfiguration" /> class.
        /// </summary>
        /// <param name="pathToPostTuningFile">The path to the post tuning file.</param>
        /// <param name="indexOfFirstPostTuningRun">The index of the first post tuning run.</param>
        /// <param name="numberOfPostTuningRuns">The number of post tuning runs.</param>
        public PostTuningConfiguration(string pathToPostTuningFile, int indexOfFirstPostTuningRun, int numberOfPostTuningRuns)
        {
            if (indexOfFirstPostTuningRun < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(indexOfFirstPostTuningRun),
                    $"{nameof(indexOfFirstPostTuningRun)} needs to be greater or equal to 0.");
            }

            if (numberOfPostTuningRuns <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(numberOfPostTuningRuns),
                    $"{nameof(numberOfPostTuningRuns)} needs to be greater or equal to 1.");
            }

            this.PathToPostTuningFile = pathToPostTuningFile;
            this.IndexOfFirstPostTuningRun = indexOfFirstPostTuningRun;
            this.NumberOfPostTuningRuns = numberOfPostTuningRuns;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the path to the post tuning file.
        /// </summary>
        public string PathToPostTuningFile { get; }

        /// <summary>
        /// Gets the index of the first post tuning run.
        /// </summary>
        public int IndexOfFirstPostTuningRun { get; }

        /// <summary>
        /// Gets the number of desired post tuning runs.
        /// </summary>
        public int NumberOfPostTuningRuns { get; }

        #endregion
    }
}