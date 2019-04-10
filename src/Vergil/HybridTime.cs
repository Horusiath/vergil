#region copyright
// -----------------------------------------------------------------------
// <copyright file="HybridTime.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;

namespace Vergil
{
    public readonly struct HybridTime : IComparable<HybridTime>, IEquatable<HybridTime>
    {
        public static readonly HybridTime Zero = new HybridTime(VectorTime.Zero, DateTimeOffset.MinValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(HybridTime x, HybridTime y) => x.Equals(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(HybridTime x, HybridTime y) => !x.Equals(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(HybridTime x, HybridTime y) => x.CompareTo(y) > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(HybridTime x, HybridTime y) => x.CompareTo(y) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(HybridTime x, HybridTime y) => x.CompareTo(y) < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(HybridTime x, HybridTime y) => x.CompareTo(y) <= 0;

        public readonly VectorTime VectorTime;
        public readonly DateTimeOffset SystemTime;

        public HybridTime(VectorTime vectorTime, DateTimeOffset systemTime)
        {
            VectorTime = vectorTime;
            SystemTime = systemTime;
        }

        [Pure]
        public HybridTime Increment(string replicaId, DateTimeOffset systemTime) =>
            new HybridTime(VectorTime.Increment(replicaId), systemTime);

        [Pure]
        public int CompareTo(HybridTime other)
        {
            var cmp = VectorTime.PartiallyCompareTo(other.VectorTime);
            if (cmp.HasValue) return cmp.Value;
            else return SystemTime.CompareTo(other.SystemTime);
        }

        [Pure]
        public bool Equals(HybridTime other)
        {
            if (SystemTime.Equals(other.SystemTime)) return VectorTime.Equals(other.VectorTime);
            else return false;
        }

        [Pure]
        public override bool Equals(object obj) => obj is HybridTime t && Equals(t);

        public override int GetHashCode() => SystemTime.GetHashCode() ^ VectorTime.GetHashCode();

        public override string ToString()
        {
            var sb = new StringBuilder(SystemTime.ToString("O")).Append(':');
            return VectorTime.ToString(sb);
        }
    }
}