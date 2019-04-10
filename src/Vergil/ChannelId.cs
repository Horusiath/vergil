#region copyright
// -----------------------------------------------------------------------
// <copyright file="ChannelId.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Runtime.CompilerServices;
using Akka.Util;

namespace Vergil
{
    public readonly struct ChannelId : IEquatable<ChannelId>, IComparable<ChannelId>
    {
        public readonly string StreamId;
        public readonly string ReplicaId;

        public ChannelId(string streamId, string replicaId)
        {
            StreamId = streamId;
            ReplicaId = replicaId;
        }

        public bool Equals(ChannelId other) => StreamId == other.StreamId && ReplicaId == other.ReplicaId;

        public int CompareTo(ChannelId other)
        {
            var cmp = String.Compare(StreamId, other.StreamId, StringComparison.Ordinal);
            if (cmp == 0)
            {
                return String.Compare(ReplicaId, other.ReplicaId, StringComparison.Ordinal);
            }

            return cmp;
        }

        public void Deconstruct(out string streamId, out string replicaId)
        {
            streamId = StreamId;
            replicaId = ReplicaId;
        }

        public override bool Equals(object obj) => obj is ChannelId ch && Equals(ch);

        public override int GetHashCode() => MurmurHash.StringHash(StreamId) ^ MurmurHash.StringHash(ReplicaId);

        public override string ToString() => StreamId + "#" + ReplicaId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ChannelId x, ChannelId y) => x.Equals(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ChannelId x, ChannelId y) => !(x == y);
    }
}