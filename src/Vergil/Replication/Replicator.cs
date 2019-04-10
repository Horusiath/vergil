#region copyright
// -----------------------------------------------------------------------
// <copyright file="ReplicatorStage.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Stage;
using Vergil.Persistence;

namespace Vergil.Replication
{
    public sealed class ReplicatorState
    {
        public VectorTime ReplicationProgress { get; }

        public ReplicatorState(VectorTime replicationProgress)
        {
            ReplicationProgress = replicationProgress;
        }
    }

    public static class Replicator
    {
        public static Props Props<T>(string replicaId, Flow<Command<T>, Emission<T>, NotUsed> archive) =>
            Akka.Actor.Props.Create(() => new Replicator<T>(replicaId, archive)).WithDeploy(Deploy.Local);
    }

    public sealed class Replicator<T> : ActorBase
    {
        private readonly string replicaId;
        private readonly IMaterializer materializer;
        private readonly Flow<Command<T>, Emission<T>, NotUsed> archiveFlow;
        
        public Replicator(string replicaId, Flow<Command<T>, Emission<T>, NotUsed> archiveFlow)
        {
            this.replicaId = replicaId;
            this.archiveFlow = archiveFlow;
            this.materializer = Context.System.Materializer();
        }

        protected override void PreStart()
        {
            base.PreStart();
        }

        protected override void PostStop()
        {
            base.PostStop();
        }

        protected override bool Receive(object message)
        {
            switch (message)
            {
                case Connect<T> connect: return true;
                case Subscribe<T> subscribe: return true;
                
                default: return false;
            }
        }
    }
}