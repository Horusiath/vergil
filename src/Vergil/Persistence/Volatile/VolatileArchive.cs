#region copyright
// -----------------------------------------------------------------------
// <copyright file="VolatileArchive.cs" company="Bartosz Sypytkowski">
//     Copyright (C) 2019-2019 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
// </copyright>
// -----------------------------------------------------------------------
#endregion

using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Stage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Vergil.Persistence.Volatile
{
    public sealed class VolatileArchive<T> : IArchive<T>
    {
        public static readonly VolatileArchive<T> Default = new VolatileArchive<T>(new MemoryStore<T>());

        private readonly MemoryStore<T> store;

        public VolatileArchive(MemoryStore<T> store)
        {
            this.store = store;
        }

        public Flow<Command<T>, Emission<T>, NotUsed> CreateFlow(ulong replayFromOffset = 0) =>
            Flow.FromGraph(new VolatileArchiveStage<T>(store, replayFromOffset));
    }

    internal sealed class VolatileArchiveStage<T> : GraphStage<FlowShape<Command<T>, Emission<T>>>
    {
        public MemoryStore<T> Store { get; }
        public ulong StartOffset { get; }

        public VolatileArchiveStage(MemoryStore<T> store, ulong startOffset)
        {
            Store = store;
            StartOffset = startOffset;
            Shape = new FlowShape<Command<T>, Emission<T>>(Inlet, Outlet);
        }

        public Inlet<Command<T>> Inlet { get; } = new Inlet<Command<T>>("volatile-archive.in");
        public Outlet<Emission<T>> Outlet { get; } = new Outlet<Emission<T>>("volatile-archive.out");
        public override FlowShape<Command<T>, Emission<T>> Shape { get; }

        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        #region logic

        private sealed class Logic : InAndOutGraphStageLogic
        {
            private static readonly Action EmptyAction = () => { throw new Exception("Should never happen"); };
            private const int StatusReplaying = 0;
            private const int StatusReady = 1;
            private const int StatusClosing = 2;

            private readonly MemoryStore<T> store;
            private readonly Inlet<Command<T>> inlet;
            private readonly Outlet<Emission<T>> outlet;
            private readonly Action writingOnPull;

            private int status = StatusReplaying;
            private Exception upstreamFailure = null;
            private Action onPush;
            private Action onPull;

            public Logic(VolatileArchiveStage<T> stage) : base(stage.Shape)
            {
                this.store = stage.Store;
                this.inlet = stage.Inlet;
                this.outlet = stage.Outlet;

                this.onPush = EmptyAction;
                this.onPull = ReplayingOnPull(stage.StartOffset);
                this.writingOnPull = WritingOnPull;

                SetHandler(inlet, this);
                SetHandler(outlet, this);
            }

            private Action ReplayingOnPull(ulong startOffset)
            {
                var enumerator = store.GetEvents(startOffset);
                return () =>
                {
                    if (enumerator.MoveNext())
                    {
                        Push(outlet, new Replaying<T>(enumerator.Current));
                    }
                    else
                    {
                        enumerator.Dispose();
                        Push(outlet, Replayed<T>.Instance);
                        BecomeReady();
                    }
                };
            }

            private void BecomeReady()
            {
                if ((status & StatusClosing) == StatusClosing)
                {
                    if (upstreamFailure is null)
                        base.OnUpstreamFinish();
                    else
                        base.OnUpstreamFailure(upstreamFailure);
                }
                else
                {
                    onPush = WritingOnPush;
                    onPull = writingOnPull;
                    status = StatusReady;
                }
            }

            private void WritingOnPush()
            {
                var req = Grab(inlet);
                switch (req.Type)
                {
                    case CommandType.EmitBatch:
                    {
                        var batch = (EmitBatch<T>) req;
                        var events = batch.Writes.Select(WriteToEvent).ToList();
                        foreach (var e in events)
                        {
                            store.Remember(e);
                        }
                        onPull = WritingBatchOnPull(events.GetEnumerator());
                        onPull();
                        break;
                    }
                    case CommandType.Emit:
                    {
                        var e = WriteToEvent((Emit<T>) req);
                        store.Remember(e);
                        Push(outlet, new Emitted<T>(e));
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private DurableEvent<T> WriteToEvent(Emit<T> emit) =>
                new DurableEvent<T>(emit.ChannelId, store.NextOffset(), emit.Timestamp, emit.Payload);

            private Action WritingBatchOnPull(List<DurableEvent<T>>.Enumerator enumerator)
            {
                return () =>
                {
                    if (enumerator.MoveNext())
                        Push(outlet, new Emitted<T>(enumerator.Current));
                    else
                    {
                        enumerator.Dispose();
                        onPull = writingOnPull;
                        if (!IsClosed(inlet))
                            Pull(inlet);
                    }
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void WritingOnPull() => Pull(inlet);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void OnPush() => onPush();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void OnPull() => onPull();

            public override void OnUpstreamFinish()
            {
                if ((status & StatusReady) == StatusReady)
                {
                    base.OnUpstreamFinish();
                }
                else
                {
                    status |= StatusClosing;
                }
            }

            public override void OnUpstreamFailure(Exception e)
            {
                if ((status & StatusReady) == StatusReady)
                {
                    base.OnUpstreamFailure(e);
                }
                else
                {
                    status |= StatusClosing;
                    upstreamFailure = e;
                }
            }
        }

        #endregion
    }
}