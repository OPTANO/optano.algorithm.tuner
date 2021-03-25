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

namespace Optano.Algorithm.Tuner.Logging
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Adapter that resolved the implicit cast of interpolated strings from <see cref="FormattableString"/> to <see cref="string"/>, so that a custom <see cref="CultureInfo"/> can be applied before the <see cref="FormattableString"/> is converted.
    /// </summary>
    public class StringIfNotFormattableStringAdapter
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StringIfNotFormattableStringAdapter"/> class.  
        /// </summary>
        /// <param name="s">
        /// The string to wrap.
        /// </param>
        private StringIfNotFormattableStringAdapter(string s)
        {
            this.String = s;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the represented <see cref="string"/>.
        /// </summary>
        private string String { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Implicit conversion from <see cref="string"/> to <see cref="StringIfNotFormattableStringAdapter"/>.
        /// </summary>
        /// <param name="s">
        /// The <see cref="string"/> to implicitly convert.
        /// </param>
        public static implicit operator StringIfNotFormattableStringAdapter(string s)
        {
            return new StringIfNotFormattableStringAdapter(s);
        }

        /// <summary>
        /// Implicit conversion from <see cref="FormattableString"/> to <see cref="StringIfNotFormattableStringAdapter"/>.
        /// If no method override is defined for <see cref="FormattableString"/>, just use the current culture to format the interpolated string.
        /// </summary>
        /// <param name="fs">The interpolated string.</param>
        public static implicit operator StringIfNotFormattableStringAdapter(FormattableString fs)
        {
            // if no override is defined, just use the current culture to format the interpolated string.
            return new StringIfNotFormattableStringAdapter(fs.ToString());
        }

        /// <summary>
        /// Implicit conversion of <see cref="StringIfNotFormattableStringAdapter"/> to <see cref="string"/>.
        /// </summary>
        /// <param name="s">The string to convert.</param>
        public static implicit operator string(StringIfNotFormattableStringAdapter s)
        {
            return s.ToString();
        }

        /// <summary>
        /// Gets the wrapped <see cref="string"/>.
        /// </summary>
        /// <returns>
        /// The wrapped <see cref="string"/>.
        /// </returns>
        public override string ToString()
        {
            return this.String;
        }

        #endregion
    }
}