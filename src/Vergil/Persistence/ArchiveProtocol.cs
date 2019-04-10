#region copyright
// -----------------------------------------------------------------------
// <copyright file="Protocol.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace Vergil.Persistence
{
    public interface IArchive<T>
    {
        Flow<Command<T>, Emission<T>, NotUsed> CreateFlow(ulong replayFromOffset = 0);
    }

    public enum CommandType : byte
    {
        Emit = 1,
        EmitBatch = 2,
    }

    public abstract class Command<T>
    {
        public readonly CommandType Type;

        protected Command(CommandType type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Request for archive flow to emit a single value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Emit<T> : Command<T>, IEquatable<Emit<T>>
    {
        public ChannelId ChannelId { get; }
        public HybridTime Timestamp { get; }
        public T Payload { get; }

        public Emit(ChannelId channelId, HybridTime timestamp, T payload)
            : base(CommandType.Emit)
        {
            ChannelId = channelId;
            Timestamp = timestamp;
            Payload = payload;
        }

        public bool Equals(Emit<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ChannelId.Equals(other.ChannelId) && Timestamp.Equals(other.Timestamp) && EqualityComparer<T>.Default.Equals(Payload, other.Payload);
        }

        public override bool Equals(object obj)
        {
            return obj is Emit<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ChannelId.GetHashCode();
                hashCode = (hashCode * 397) ^ Timestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Payload);
                return hashCode;
            }
        }
    }

    public sealed class EmitBatch<T> : Command<T>, IEquatable<EmitBatch<T>>
    {
        public EmitBatch(ImmutableArray<Emit<T>> writes) : base(CommandType.EmitBatch)
        {
            Writes = writes;
        }

        public EmitBatch(params Emit<T>[] emits)
            : this(ImmutableArray.CreateRange(emits))
        {
        }

        public ImmutableArray<Emit<T>> Writes { get; }

        public bool Equals(EmitBatch<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Writes.Length != other.Writes.Length) return false;

            for (int i = 0; i < Writes.Length; i++)
            {
                if (!Writes[i].Equals(other.Writes[0])) return false;
            }

            return true;
        }

        public override bool Equals(object obj) => obj is EmitBatch<T> other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 0;
                foreach (var write in Writes)
                {
                    hash ^= (397) * write.GetHashCode();
                }
                return hash;
            }
        }
    }
    
    public enum EmissionType
    {
        Replaying = 1,
        Replayed = 2,
        Emitted = 3
    }

    public abstract class Emission<T>
    {
        public readonly EmissionType Type;

        protected Emission(EmissionType type)
        {
            Type = type;
        }
    }

    public sealed class Replaying<T> : Emission<T>, IEquatable<Replaying<T>>
    {
        public DurableEvent<T> Event { get; }

        public Replaying(DurableEvent<T> @event) : base(EmissionType.Replaying)
        {
            Event = @event;
        }

        public bool Equals(Replaying<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Event, other.Event);
        }

        public override bool Equals(object obj) => obj is Replaying<T> other && Equals(other);

        public override int GetHashCode() => Event.GetHashCode();

        public override string ToString() => $"Replaying({Event})";
    }

    public sealed class Replayed<T> : Emission<T>
    {
        public static Replayed<T> Instance { get; } = new Replayed<T>();
        private Replayed() : base(EmissionType.Replayed) {}
        public override string ToString() => $"Replayed()";
    }

    public sealed class Emitted<T> : Emission<T>, IEquatable<Emitted<T>>
    {
        public DurableEvent<T> Event { get; }

        public Emitted(DurableEvent<T> @event) : base(EmissionType.Emitted)
        {
            Event = @event;
        }

        public bool Equals(Emitted<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Event, other.Event);
        }

        public override bool Equals(object obj) => obj is Emitted<T> other && Equals(other);

        public override int GetHashCode() => Event.GetHashCode();

        public override string ToString() => $"Emitted({Event})";
    }
}