#region copyright
// -----------------------------------------------------------------------
// <copyright file="Protocol.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;
using Vergil.Persistence;

namespace Vergil.Replication
{
    internal sealed class Connect<T>
    {
        public string SenderReplicaId { get; }
        public ulong LastKnownOffset { get; }
        public ISinkRef<Emission<T>> Receiver { get; }

        public Connect(string senderReplicaId, ulong lastKnownOffset, ISinkRef<Emission<T>> receiver)
        {
            SenderReplicaId = senderReplicaId;
            LastKnownOffset = lastKnownOffset;
            Receiver = receiver;
        }
    }

    internal sealed class Disconnect<T>
    {
        public Disconnect(string senderReplicaId)
        {
            SenderReplicaId = senderReplicaId;
        }

        public string SenderReplicaId { get; }
    }

    internal sealed class Subscribe<T>
    {
        public string StreamId { get; }

        public Subscribe(string streamId)
        {
            StreamId = streamId;
        }
    }

    internal sealed class Subscribed<T>
    {
        public ChannelId Channel { get; }
        public Flow<Command<T>, Emission<T>, NotUsed> Flow { get; }

        public Subscribed(ChannelId channel, Flow<Command<T>, Emission<T>, NotUsed> flow)
        {
            Channel = channel;
            Flow = flow;
        }
    }
}