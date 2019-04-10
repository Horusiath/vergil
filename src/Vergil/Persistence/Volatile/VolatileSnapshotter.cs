#region copyright
// -----------------------------------------------------------------------
// <copyright file="VolatileSnapshotter.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Vergil.Persistence.Volatile
{
    public sealed class VolatileSnapshotter<T> : ISnapshotter<T>
    {
        private readonly ConcurrentDictionary<string, Snapshot<T>> snapshots;

        public VolatileSnapshotter()
        {
            snapshots = new ConcurrentDictionary<string, Snapshot<T>>();
        }

        public VolatileSnapshotter(IEnumerable<KeyValuePair<string, T>> init)
        {
            snapshots = new ConcurrentDictionary<string, Snapshot<T>>(init
                .Select(kv => new KeyValuePair<string, Snapshot<T>>(kv.Key, new Snapshot<T>(kv.Key, kv.Value))));
        }

        public async ValueTask<Snapshot<T>> GetState(string id)
        {
            if (snapshots.TryGetValue(id, out var snapshot))
                return snapshot;
            else
                return default;
        }

        public async ValueTask SaveSnapshot(Snapshot<T> snapshot)
        {
            snapshots[snapshot.SnapshotId] = snapshot;
        }
    }
}