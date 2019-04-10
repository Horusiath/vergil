#region copyright
// -----------------------------------------------------------------------
// <copyright file="VolatileArchiveTests.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.TestKit;
using FluentAssertions;
using Vergil.Persistence;
using Vergil.Persistence.Volatile;
using Xunit;
using Xunit.Abstractions;

namespace Vergil.Tests.Persistence
{
    public class VolatileArchiveTests : ArchiveTests
    {
        public VolatileArchiveTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Archive_should_start_from_replaying_events_from_given_offset()
        {
            var events = MakeEvents(10).ToArray();
            var store = new MemoryStore<int>(events);
            var archive = new VolatileArchive<int>(store);
            var probe = this.CreateManualSubscriberProbe<Emission<int>>();

            Source.Empty<Command<int>>()
                .Via(archive.CreateFlow(5))
                .ToMaterialized(Sink.FromSubscriber(probe), Keep.Right)
                .Run(Materializer);

            probe.ExpectSubscription().Request(10);
            probe.ExpectNext(new Replaying<int>(new DurableEvent<int>(ChannelId, 6, new HybridTime(new VectorTime((ReplicaId, 6)), DateTimeOffset.MinValue), 6)))
                 .ExpectNext(new Replaying<int>(new DurableEvent<int>(ChannelId, 7, new HybridTime(new VectorTime((ReplicaId, 7)), DateTimeOffset.MinValue), 7)))
                 .ExpectNext(new Replaying<int>(new DurableEvent<int>(ChannelId, 8, new HybridTime(new VectorTime((ReplicaId, 8)), DateTimeOffset.MinValue), 8)))
                 .ExpectNext(new Replaying<int>(new DurableEvent<int>(ChannelId, 9, new HybridTime(new VectorTime((ReplicaId, 9)), DateTimeOffset.MinValue), 9)))
                 .ExpectNext(new Replaying<int>(new DurableEvent<int>(ChannelId, 10, new HybridTime(new VectorTime((ReplicaId, 10)), DateTimeOffset.MinValue), 10)))
                 .ExpectNext(Replayed<int>.Instance)
                 .ExpectComplete();
        }

        [Fact]
        public async Task Archive_should_skip_replaying_for_long_MaxValue()
        {
            var events = MakeEvents(10).ToArray();
            var store = new MemoryStore<int>(events);
            var archive = new VolatileArchive<int>(store);

            var output = await Source.Empty<Command<int>>()
                .Via(archive.CreateFlow(ulong.MaxValue))
                .ToMaterialized(Sink.Seq<Emission<int>>(), Keep.Right)
                .Run(Materializer);

            output.Should().BeEquivalentTo(Replayed<int>.Instance);
        }

        [Fact]
        public async Task Archive_should_replay_events_first_then_accept_new_ones()
        {
            var events = MakeEvents(10).ToArray();
            var store = new MemoryStore<int>(events);
            var archive = new VolatileArchive<int>(store);
            var now = DateTimeOffset.Now;
            var probe = this.CreateManualSubscriberProbe<Emission<int>>();

            Source.From(new Command<int>[]
                {
                    new Emit<int>(ChannelId, new HybridTime(new VectorTime((ReplicaId, 11)), now), 101),
                    new EmitBatch<int>(
                        new Emit<int>(ChannelId, new HybridTime(new VectorTime((ReplicaId, 12)), now), 102),
                        new Emit<int>(ChannelId, new HybridTime(new VectorTime((ReplicaId, 13)), now), 103)),
                    new Emit<int>(ChannelId, new HybridTime(new VectorTime((ReplicaId, 14)), now), 104),
                })
                .Via(archive.CreateFlow(5))
                .ToMaterialized(Sink.FromSubscriber(probe), Keep.Right)
                .Run(Materializer);

            probe.ExpectSubscription().Request(10);

            probe
                .ExpectNext(new Replaying<int>(new DurableEvent<int>(ChannelId, 6, new HybridTime(new VectorTime((ReplicaId, 6)), DateTimeOffset.MinValue), 6)))
                .ExpectNext(new Replaying<int>(new DurableEvent<int>(ChannelId, 7, new HybridTime(new VectorTime((ReplicaId, 7)), DateTimeOffset.MinValue), 7)))
                .ExpectNext(new Replaying<int>(new DurableEvent<int>(ChannelId, 8, new HybridTime(new VectorTime((ReplicaId, 8)), DateTimeOffset.MinValue), 8)))
                .ExpectNext(new Replaying<int>(new DurableEvent<int>(ChannelId, 9, new HybridTime(new VectorTime((ReplicaId, 9)), DateTimeOffset.MinValue), 9)))
                .ExpectNext(new Replaying<int>(new DurableEvent<int>(ChannelId, 10, new HybridTime(new VectorTime((ReplicaId, 10)), DateTimeOffset.MinValue), 10)))
                .ExpectNext(Replayed<int>.Instance)
                .ExpectNext(new Emitted<int>(new DurableEvent<int>(ChannelId, 11, new HybridTime(new VectorTime((ReplicaId, 11)), now), 101)))
                .ExpectNext(new Emitted<int>(new DurableEvent<int>(ChannelId, 12, new HybridTime(new VectorTime((ReplicaId, 12)), now), 102)))
                .ExpectNext(new Emitted<int>(new DurableEvent<int>(ChannelId, 13, new HybridTime(new VectorTime((ReplicaId, 13)), now), 103)))
                .ExpectNext(new Emitted<int>(new DurableEvent<int>(ChannelId, 14, new HybridTime(new VectorTime((ReplicaId, 14)), now), 104)))
                .ExpectComplete();
        }
    }
}