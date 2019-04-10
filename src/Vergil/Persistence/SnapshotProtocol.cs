#region copyright
// -----------------------------------------------------------------------
// <copyright file="SnapshotProtocol.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vergil.Persistence
{
    public interface ISnapshotter<TState>
    {
        ValueTask<Snapshot<TState>> GetState(string id);
        ValueTask SaveSnapshot(Snapshot<TState> snapshot);
    }

    public enum SnapshotRequestType
    {
        GetSnapshot = 1,
        SetSnapshot = 2,
        DropSnapshot = 3,
    }

    public abstract class SnapshotRequest<T>
    {
        public readonly SnapshotRequestType Type;

        protected SnapshotRequest(SnapshotRequestType type)
        {
            Type = type;
        }
    }

    public sealed class GetSnapshot<T> : SnapshotRequest<T>
    {
        public GetSnapshot() : base(SnapshotRequestType.GetSnapshot)
        {
        }
    }

    public sealed class SetSnapshot<T> : SnapshotRequest<T>
    {
        public SetSnapshot() : base(SnapshotRequestType.SetSnapshot)
        {
        }
    }

    public sealed class Snapshot<T> : IEquatable<Snapshot<T>>
    {
        public string SnapshotId { get; }
        public T State { get; }

        public Snapshot(string snapshotId, T state)
        {
            SnapshotId = snapshotId;
            State = state;
        }

        public bool Equals(Snapshot<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(SnapshotId, other.SnapshotId) && EqualityComparer<T>.Default.Equals(State, other.State);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Snapshot<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((SnapshotId != null ? SnapshotId.GetHashCode() : 0) * 397) ^ EqualityComparer<T>.Default.GetHashCode(State);
            }
        }
    }
}