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

namespace Optano.Algorithm.Tuner.DistributedExecution
{
    using System.Globalization;
    using System.Reflection;

    using Akka.Actor;
    using Akka.Cluster;
    using Akka.Configuration;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration.ArgumentParsers;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Evaluation;
    using Optano.Algorithm.Tuner.Logging;

    /// <summary>
    /// Responsible for starting an actor system that can support <see cref="EvaluationActor{TTargetAlgorithm,TInstance,TResult}"/>s.
    /// </summary>
    public static class Worker
    {
        #region Public Methods and Operators

        /// <summary>
        /// Executes the worker.
        /// </summary>
        /// <param name="args">
        /// Arguments like the master's hostname.
        /// See <see cref="WorkerArgumentParser" /> for more information.
        /// </param>
        public static void Run(string[] args)
        {
            ProcessUtils.SetDefaultCultureInfo(CultureInfo.InvariantCulture);
            LoggingHelper.Configure($"consoleOutput_Worker_{ProcessUtils.GetCurrentProcessId()}.log");
            Randomizer.Configure();

            // Parse arguments.
            var argsParser = new WorkerArgumentParser();
            if (!ArgumentParserUtils.ParseArguments(argsParser, args))
            {
                return;
            }

            LoggingHelper.ChangeConsoleLoggingLevel(argsParser.VerbosityLevel);

            // Create an actor system for remote nodes to deploy onto.
            var akkaConfig = CustomizeAkkaConfiguration(
                argsParser.OwnHostName,
                argsParser.SeedHostName,
                argsParser.Port,
                argsParser.VerbosityLevel >= VerbosityLevel.Trace);
            var actorSystem = ActorSystem.Create(AkkaNames.ActorSystemName, akkaConfig);

            // Create an actor checking whether the cluster seed is still up.
            actorSystem.ActorOf(Props.Create(() => new SeedObserver()));

            // Do not stop execution before the actor system has been terminated.
            var cluster = Cluster.Get(actorSystem);
            actorSystem.WhenTerminated.Wait();
            cluster.Leave(cluster.SelfAddress);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adapts Akka.NET configuration to use the correct hostname and cluster seed node (actor system name,
        /// hostname and port).
        /// </summary>
        /// <param name="workerHostName">
        /// The worker host name.
        /// </param>
        /// <param name="clusterSeed">
        /// Hostname of cluster seed node.
        /// </param>
        /// <param name="seedPort">
        /// Port of cluster seed node.
        /// </param>
        /// <param name="logOnDebugLevel">
        /// Whether logging should be done on debug level.
        /// </param>
        /// <returns>
        /// The adapted Akka.NET configuration.
        /// </returns>
        private static Config CustomizeAkkaConfiguration(
            string workerHostName,
            string clusterSeed,
            int seedPort,
            bool logOnDebugLevel)
        {
            Config commonBaseConfig =
                ConfigurationFactory.FromResource(AkkaNames.CommonAkkaConfigFileName, Assembly.GetCallingAssembly());

            if (string.IsNullOrWhiteSpace(workerHostName))
            {
                workerHostName = NetworkUtils.GetFullyQualifiedDomainName();
            }

            LoggingHelper.WriteLine(VerbosityLevel.Debug, $"Worker's own HostName: {workerHostName}");

            var logClusterEvents = logOnDebugLevel ? "on" : "off";

            var config = ConfigurationFactory.ParseString(
                $@"
                akka
                {{
                    log-config-on-start = on
                    remote.dot-netty.tcp {{
                        hostname={workerHostName}
                        port = 0 #let os pick random port
                        batching.enabled = false
                    }}
                    cluster.seed-nodes = [""akka.tcp://{AkkaNames.ActorSystemName}@{clusterSeed}:{seedPort}""]
                    cluster.log-info = {logClusterEvents}
               }}").WithFallback(commonBaseConfig);

            return logOnDebugLevel
                       ? config.WithFallback(ConfigurationFactory.FromResource(AkkaNames.ExtensiveAkkaLoggingFileName, Assembly.GetCallingAssembly()))
                       : config;
        }

        #endregion
    }
}