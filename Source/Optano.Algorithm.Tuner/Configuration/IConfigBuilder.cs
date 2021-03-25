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

namespace Optano.Algorithm.Tuner.Configuration
{
    /// <summary>
    ///  Interface that is implemented by ConfigBuilder Helpers.
    /// </summary>
    /// <typeparam name="TConfiguration">The configuration type that is built by this <see cref="IConfigBuilder{TConfiguration}"/>.</typeparam>
    public interface IConfigBuilder<out TConfiguration>
        where TConfiguration : ConfigurationBase
    {
        #region Public Methods and Operators

        /// <summary>
        /// Builds a <typeparamref name="TConfiguration"/> using the provided
        /// <see cref="ConfigurationBase"/> as fallback.
        /// </summary>
        /// <param name="fallback">Used if a property is not set for this
        /// <typeparamref name="TConfiguration"/>.
        /// May be null. In that case, defaults are used as fallback.</param>
        /// <returns>The build <typeparamref name="TConfiguration"/>.</returns>
        TConfiguration BuildWithFallback(ConfigurationBase fallback);

        #endregion
    }
}