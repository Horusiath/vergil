#region copyright
// -----------------------------------------------------------------------
// <copyright file="ArchiveTests.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using Akka.Streams;
using Akka.TestKit.Xunit2;
using Vergil.Persistence;
using Xunit.Abstractions;

namespace Vergil.Tests.Persistence
{
    public class ArchiveTests : TestKit
    {
        protected const string StreamId = "SREF";
        protected const string ReplicaId = "A";
        protected const string ArchiveId = "ARCH";

        protected static readonly ChannelId ChannelId = new ChannelId(StreamId, ReplicaId);
        protected readonly IMaterializer Materializer;

        public ArchiveTests(ITestOutputHelper output) : base(output: output)
        {
            Materializer = Sys.Materializer();
        }

        public IEnumerable<DurableEvent<int>> MakeEvents(int count)
        {
            var timestamp = HybridTime.Zero;
            for (int i = 1; i <= count; i++)
            {
                timestamp = timestamp.Increment(ReplicaId, DateTimeOffset.MinValue);
                yield return new DurableEvent<int>(ChannelId, (ulong)i, timestamp, i);
            }
        }
    }
}