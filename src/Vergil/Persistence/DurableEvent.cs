#region copyright
// -----------------------------------------------------------------------
// <copyright file="DurableEvent.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Vergil.Persistence
{
    public sealed class DurableEvent<T> : IComparable<DurableEvent<T>>, IEquatable<DurableEvent<T>>
    {
        /// <summary>
        /// A cluster-wide channel identifier for an emitted event.
        /// </summary>
        public ChannelId ChannelId { get; }

        /// <summary>
        /// A sequence number assigned. It's scope is specific to an archive.
        /// </summary>
        public ulong Offset { get; }

        /// <summary>
        /// A timestamp assigned to current <see cref="DurableEvent{T}"/> which allows to track its causality.
        /// </summary>
        public HybridTime Timestamp { get; }

        public T Payload { get; }

        public DurableEvent(
            ChannelId channelId,
            ulong offset,
            HybridTime timestamp, 
            T payload)
        {
            ChannelId = channelId;
            Offset = offset;
            Timestamp = timestamp;
            Payload = payload;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(DurableEvent<T> other) => Timestamp.CompareTo(other.Timestamp);

        public bool Equals(DurableEvent<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return ChannelId.Equals(other.ChannelId) 
                   && Offset == other.Offset 
                   && Timestamp.Equals(other.Timestamp) 
                   && EqualityComparer<T>.Default.Equals(Payload, other.Payload);
        }

        public override bool Equals(object obj) => obj is DurableEvent<T> other && Equals(other);

        public override string ToString() =>
            $"DurableEvent({ChannelId}, {Offset}, {Timestamp}, {Payload})";

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ChannelId.GetHashCode();
                hashCode = (hashCode * 397) ^ Offset.GetHashCode();
                hashCode = (hashCode * 397) ^ Timestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Payload);
                return hashCode;
            }
        }
    }
}