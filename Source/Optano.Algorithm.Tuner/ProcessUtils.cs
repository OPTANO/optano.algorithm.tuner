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

namespace Optano.Algorithm.Tuner
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;

    /// <summary>
    /// Utility functions for process handling.
    /// </summary>
    public static class ProcessUtils
    {
        #region Public Methods and Operators

        /// <summary>
        /// Kills the given process. Handles exceptions that might be thrown due to the process currently being
        /// terminated or already having terminated.
        /// </summary>
        /// <param name="process">The process to kill.</param>
        public static void CancelProcess(Process process)
        {
            try
            {
                process.Kill();
                process.WaitForExit();
            }
            catch (Win32Exception ex)
            {
                // The exception might be thrown because the process is already exiting.
                // Check that by waiting for exit for a time.
                process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
                if (!process.HasExited)
                {
                    throw new Win32Exception(
                        "Win32Exception was thrown and process did not exit after 10 seconds.",
                        ex);
                }
            }
            catch (InvalidOperationException)
            {
                // If the process has already exited, the exception is expected and everything is fine.
                // If it's still running, something bad happened.
                if (!process.HasExited)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Returns the current <see cref="Process.Id"/>, or <c>0</c> 
        /// if an exception occurrs during the call.
        /// </summary>
        /// <returns>
        /// The current <see cref="Process.Id"/>.
        /// </returns>
        public static int GetCurrentProcessId()
        {
            try
            {
                return Process.GetCurrentProcess().Id;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Sets the default <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="cultureInfo">The culture info.</param>
        public static void SetDefaultCultureInfo(CultureInfo cultureInfo)
        {
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
        }

        #endregion
    }
}