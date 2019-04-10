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

namespace Vergil
{
    public readonly struct MatrixTime : IEquatable<MatrixTime>
    {
        private readonly ImmutableDictionary<string, VectorTime> observations;

        public MatrixTime(IEnumerable<KeyValuePair<string, VectorTime>> observations)
        {
            this.observations = observations.ToImmutableDictionary();
        }
        
        public MatrixTime(ImmutableDictionary<string, VectorTime> observations)
        {
            this.observations = observations;
        }

        public VectorTime this[string replicaId] => observations.GetValueOrDefault(replicaId, VectorTime.Zero);

        public MatrixTime Merge(string replicaId, VectorTime time) => 
            observations.TryGetValue(replicaId, out var observed) 
                ? new MatrixTime(observations.SetItem(replicaId, observed.Merge(time))) 
                : new MatrixTime(observations.SetItem(replicaId, time));

        /// <summary>
        /// Returns a <see cref="VectorTime"/> being the merged max value of all observed vector clocks.
        /// </summary>
        public VectorTime Max()
        {
            var result = VectorTime.Zero;
            foreach (var observation in observations)
            {
                result = result.Merge(observation.Value);
            }

            return result;
        }
        
        /// <summary>
        /// Returns a <see cref="VectorTime"/> being the merged min value of all observed vector clocks.
        /// </summary>
        public VectorTime Min()
        {
            using (var enumerator = observations.GetEnumerator())
            {
                if (!enumerator.MoveNext()) return VectorTime.Zero;

                var result = enumerator.Current.Value;
                while (enumerator.MoveNext())
                {
                    result = result.MergeMin(enumerator.Current.Value);
                }

                return result;
            }
        }

        public bool Equals(MatrixTime other)
        {
            if (ReferenceEquals(observations, other.observations)) return true;
            if (observations.Count != other.observations.Count) return false;

            foreach (var (key, value) in observations)
            {
                if (!other.observations.TryGetValue(key, out var otherValue) || !value.Equals(otherValue))
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MatrixTime other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 0;
                foreach (var (key, value) in observations)
                {
                    hash = (397 * hash) ^ key.GetHashCode() ^ value.GetHashCode();
                }

                return hash;
            }
        }
    }
}