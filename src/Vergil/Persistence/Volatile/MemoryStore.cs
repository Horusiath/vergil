#region copyright
// -----------------------------------------------------------------------
// <copyright file="MemoryStore.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Vergil.Persistence.Volatile
{
    public sealed class MemoryStore<T>
    {
        private long offset;
        private ImmutableSortedDictionary<ulong, DurableEvent<T>> events;

        public MemoryStore()
        {
            offset = 0;
            events = ImmutableSortedDictionary<ulong, DurableEvent<T>>.Empty;
        }

        public MemoryStore(params DurableEvent<T>[] events) : this()
        {
            var ordered = events.OrderBy(e => e.Offset);
            foreach (var durableEvent in ordered)
            {
                this.events = this.events.Add(durableEvent.Offset, durableEvent);
                this.offset = (long)durableEvent.Offset;
            }
        }

        public IEnumerator<DurableEvent<T>> GetEvents(ulong afterOffset)
        {
            if (afterOffset != ulong.MaxValue)
                foreach (var e in events)
                {
                    if (e.Key > afterOffset)
                        yield return e.Value;
                }
        }

        public ulong NextOffset()
        {
            return (ulong)Interlocked.Increment(ref offset);
        }

        public void Remember(DurableEvent<T> e)
        {
            while (true)
            {
                var old = events;
                var newEvents = old.Add(e.Offset, e);
                if (ReferenceEquals(Interlocked.CompareExchange(ref events, newEvents, old), old))
                    return;
            }
        }
    }
}