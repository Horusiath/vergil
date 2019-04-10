#region copyright
// -----------------------------------------------------------------------
// <copyright file="VectorTime.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;

namespace Vergil
{
    /// <summary>
    /// Vector clock used to track causality relationship between events occurring
    /// over mutliple nodes, potentially concurrently.
    /// </summary>
    public readonly struct VectorTime :
        IEquatable<VectorTime>,
        IPartiallyComparable<VectorTime>,
        IConvergent<VectorTime>,
        IEnumerable<KeyValuePair<string, ulong>>
    {
        public static readonly VectorTime Zero = new VectorTime(ImmutableDictionary<string, ulong>.Empty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(VectorTime x, VectorTime y) => x.Equals(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(VectorTime x, VectorTime y) => !x.Equals(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(VectorTime x, VectorTime y) => x.PartiallyCompareTo(y) > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(VectorTime x, VectorTime y) => x.PartiallyCompareTo(y) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(VectorTime x, VectorTime y) => x.PartiallyCompareTo(y) < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(VectorTime x, VectorTime y) => x.PartiallyCompareTo(y) <= 0;

        public static bool TryParse(string value, out VectorTime vclock)
        {
            try
            {
                var entries = JsonConvert.DeserializeObject<ImmutableDictionary<string, ulong>>(value);
                vclock = new VectorTime(entries);
                return true;
            }
            catch
            {
                vclock = default;
                return false;
            }
        }

        private readonly ImmutableDictionary<string, ulong> entries;

        public VectorTime(ImmutableDictionary<string, ulong> entries)
        {
            this.entries = entries;
        }

        public VectorTime(params (string, ulong)[] entries)
            : this(entries.ToImmutableDictionary(x => x.Item1, x => x.Item2))
        {
        }

        public int Count
        {
            [Pure]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => entries.Count;
        }

        public ulong this[string replicaId]
        {
            [Pure]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => entries.GetValueOrDefault(replicaId, 0UL);
        }

        [Pure]
        public VectorTime Increment(string replicaId) =>
            entries.TryGetValue(replicaId, out var value)
                ? new VectorTime(entries.SetItem(replicaId, value + 1))
                : new VectorTime(entries.SetItem(replicaId, 1));

        [Pure]
        public VectorTime Prune(string replicaId) => new VectorTime(entries.Remove(replicaId));

        [Pure]
        public VectorTime Merge(VectorTime other)
        {
            var b = this.entries.ToBuilder();
            foreach (var (key, value) in other.entries)
            {
                if (b.TryGetValue(key, out var thisValue))
                {
                    b[key] = Math.Max(value, thisValue);
                }
                else
                {
                    b.Add(key, value);
                }
            }

            return new VectorTime(b.ToImmutable());
        }
        
        [Pure]
        public VectorTime MergeMin(VectorTime other)
        {
            var b = this.entries.ToBuilder();
            foreach (var (key, value) in other.entries)
            {
                if (b.TryGetValue(key, out var thisValue))
                {
                    b[key] = Math.Min(value, thisValue);
                }
                else
                {
                    b.Add(key, value);
                }
            }

            return new VectorTime(b.ToImmutable());
        }

        [Pure]
        public bool Equals(VectorTime other)
        {
            if (ReferenceEquals(entries, other.entries)) return true;
            if (entries.Count != other.entries.Count) return false;

            foreach (var (key, value) in entries)
            {
                if (other.entries.TryGetValue(key, out var ovalue))
                {
                    if (value != ovalue) return false;
                }
                else return false;
            }

            return true;
        }

        [Pure]
        public int? PartiallyCompareTo(VectorTime other)
        {
            int result = 0;
            foreach (var key in entries.Keys.Union(other.entries.Keys))
            {
                var a = this[key];
                var b = other[key];

                if (result == 0)
                {
                    if (a > b) result = 1;
                    else if (a < b) result = -1;
                }
                else if (result == -1 && a > b) return null; // once we reach concurrent case, there can be no other result
                else if (result == 1 && a < b) return null;
            }

            return result;
        }

        public ImmutableDictionary<string, ulong>.Enumerator GetEnumerator() => entries.GetEnumerator();

        IEnumerator<KeyValuePair<string, ulong>> IEnumerable<KeyValuePair<string, ulong>>.GetEnumerator() => entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override bool Equals(object obj) => obj is VectorTime vclock && Equals(vclock);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 0;

                foreach (var (key, value) in entries)
                {
                    hash ^= 397 * hash + (key.GetHashCode() ^ value.GetHashCode());
                }

                return hash;
            }
        }
        internal string ToString(StringBuilder sb)
        {
            sb.Append('{');
            foreach (var (key, value) in entries)
            {
                sb.Append(key).Append(':').Append(value).Append(';');
            }
            sb.Append('}');

            return sb.ToString();
        }

        public override string ToString() => ToString(new StringBuilder());
    }
}