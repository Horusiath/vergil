#region copyright
// -----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Vergil
{
    internal static class CollectionExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> entry, out TKey key, out TValue value)
        {
            key = entry.Key;
            value = entry.Value;
        }

        /// <summary>
        /// Merges two immutable dictionaries together, using <paramref name="resolve"/>
        /// function when entries with corresponding keys have different values.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <param name="resolve"></param>
        /// <returns></returns>
        public static ImmutableDictionary<TKey, TValue> Merge<TKey, TValue>(
            this ImmutableDictionary<TKey, TValue> self,
            ImmutableDictionary<TKey, TValue> other,
            Func<TKey, TValue, TValue, TValue> resolve)
        {
            if (other.IsEmpty) return self;
            if (self.IsEmpty) return other;

            var builder = self.ToBuilder();
            foreach (var (key, otherValue) in other)
            {
                if (builder.TryGetValue(key, out var selfValue))
                {
                    builder[key] = resolve(key, selfValue, otherValue);
                }
                else builder.Add(key, otherValue);
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Intersects two immutable dictionaries together, using <paramref name="resolve"/>
        /// function to determine value output of corresponding entries.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <param name="resolve"></param>
        /// <returns></returns>
        public static ImmutableDictionary<TKey, TValue> Intersect<TKey, TValue>(
            this ImmutableDictionary<TKey, TValue> self,
            ImmutableDictionary<TKey, TValue> other,
            Func<TKey, TValue, TValue, TValue> resolve)
        {
            if (other.IsEmpty) return other;
            if (self.IsEmpty) return self;

            var builder = ImmutableDictionary<TKey, TValue>.Empty.ToBuilder();
            foreach (var (key, otherValue) in other)
            {
                if (self.TryGetValue(key, out var selfValue))
                {
                    builder[key] = resolve(key, selfValue, otherValue);
                }
            }

            return builder.ToImmutable();
        }
    }
}