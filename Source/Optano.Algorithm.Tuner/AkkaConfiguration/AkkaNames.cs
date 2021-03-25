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

namespace Optano.Algorithm.Tuner.AkkaConfiguration
{
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;

    /// <summary>
    /// Contains names used for Akka.NET objects that get used multiple times throughout the solution.
    /// </summary>
    public static class AkkaNames
    {
        #region Constants

        /// <summary>
        /// The name for the actor system employed by master and worker.
        /// </summary>
        public const string ActorSystemName = "OptanoAlgorithmTunerActorSystem";

        /// <summary>
        /// The name for the <see cref="ResultStorageActor{TInstance,TResult}"/> instance.
        /// </summary>
        public const string ResultStorageActor = "ResultStorageActor";

        /// <summary>
        /// The name for the <see cref="GenerationEvaluationActor{TTargetAlgorithm,TInstance,TResult}"/> instance.
        /// </summary>
        public const string GenerationEvaluationActor = "GenerationEvaluationActor";

        /// <summary>
        /// The name for the router, who is managing the <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}"/> instances.
        /// This router is responsible for starting new evaluation actors, whenever a node joins the cluster.
        /// </summary>
        public const string EvaluationActorRouter = "EvaluationActorRouter";

        /// <summary>
        /// The path to common HOCON configuration used for both master and worker.
        /// </summary>
        public const string CommonAkkaConfigFileName = "Optano.Algorithm.Tuner.AkkaConfiguration.Common.conf";

        /// <summary>
        /// The path to HOCON configuration that activates Akka logging on debug level.
        /// </summary>
        public const string ExtensiveAkkaLoggingFileName = "Optano.Algorithm.Tuner.AkkaConfiguration.ExtensiveAkkaLogging.conf";

        #endregion
    }
}