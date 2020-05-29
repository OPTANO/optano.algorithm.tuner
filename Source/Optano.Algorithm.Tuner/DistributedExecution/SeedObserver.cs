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

namespace Optano.Algorithm.Tuner.DistributedExecution
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Akka.Actor;
    using Akka.Cluster;

    using Optano.Algorithm.Tuner.Logging;

    /// <summary>
    /// Responsible for shutting down the <see cref="ActorSystem" /> if the cluster's seed node cannot be discovered.
    /// </summary>
    public class SeedObserver : ReceiveActor
    {
        #region Fields

        /// <summary>
        /// Provides cluster membership information.
        /// </summary>
        private readonly Cluster _cluster;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SeedObserver" /> class.
        /// </summary>
        public SeedObserver()
        {
            // Fetch cluster information.
            this._cluster = Cluster.Get(Context.System);

            // In a short while, use it to check if the own actor system managed to join a cluster, i. e.
            // the seed node could be found and connected to.
            this.FutureCheckIfNodeJoinedCluster();

            // Also switch to observing state and subscribe to cluster events so seed disconnects can be handled.
            this.Observing();
            this._cluster.Subscribe(this.Self, typeof(ClusterEvent.UnreachableMember));
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Actor observes <see cref="ClusterEvent.UnreachableMember" /> events.
        /// </summary>
        public void Observing()
        {
            // If a member becomes unreachable, check if it is the single seed node.
            this.Receive<ClusterEvent.UnreachableMember>(
                unreachableMessage =>
                    {
                        if (this._cluster.Settings.SeedNodes.Single() == unreachableMessage.Member.Address)
                        {
                            // If it is, system should be terminated because there is no OPTANO Algorithm Tuner master available anymore.
                            LoggingHelper.WriteLine(VerbosityLevel.Warn, "Shutting down worker because master became unreachable.");
                            Context.System.Terminate();
                        }
                    });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts a task that will check whether the own actor system managed to join a cluster.
        /// The check will be executed after some time. If no cluster was joined, the node couldn't find the cluster
        /// seed and the actor system should terminate.
        /// </summary>
        private void FutureCheckIfNodeJoinedCluster()
        {
            var ownActorSystem = Context.System;
            var clusterToJoin = this._cluster;

            Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(
                delayTask =>
                    {
                        if (clusterToJoin.State.Leader == null)
                        {
                            LoggingHelper.WriteLine(VerbosityLevel.Warn, "Shutting down because cluster did not form.");
                            ownActorSystem.Terminate();
                        }
                    });
        }

        #endregion
    }
}