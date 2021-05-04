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

namespace Optano.Algorithm.Tuner.Tests.DistributedExecution.DummyImplementations
{
    using NDesk.Options;

    using Optano.Algorithm.Tuner.GrayBox.PostTuningRunner;

    /// <summary>
    /// A dummy adapter argument parser with post tuning option, used in tests.
    /// </summary>
    public class DummyPostTuningAdapterArgumentParser : PostTuningAdapterArgumentParser<DummyConfig.DummyConfigBuilder>
    {
        #region Methods

        /// <inheritdoc />
        protected override OptionSet CreateAdapterMasterOptionSet()
        {
            return new OptionSet
                       {
                           {
                               "value=",
                               () =>
                                   "Sets the value.",
                               (int v) => this.InternalConfigurationBuilder.SetValue(v)
                           },
                       };
        }

        /// <inheritdoc />
        protected override OptionSet CreateAdapterPostTuningOptionSet()
        {
            return this.CreateAdapterMasterOptionSet();
        }

        #endregion
    }
}