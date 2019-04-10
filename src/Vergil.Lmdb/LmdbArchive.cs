using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Stage;
using Spreads.Buffers;
using Spreads.LMDB;
using Vergil.Persistence;

namespace Vergil.Lmdb
{
    public sealed class LmdbArchive<T> : IArchive<T>
    {
        private readonly string databaseName;
        private readonly Func<LMDBEnvironment> lmdbEnvFactory;

        public LmdbArchive(string databasePath)
        {
            var databaseDir = Path.GetDirectoryName(databasePath);
            this.lmdbEnvFactory = () => LMDBEnvironment.Create(databaseDir);
            this.databaseName = Path.GetFileName(databasePath);
        }
        
        public LmdbArchive(string databaseName, Func<LMDBEnvironment> lmdbEnvFactory)
        {
            databaseName = databaseName;
            lmdbEnvFactory = lmdbEnvFactory;
        }

        public Flow<Command<T>, Emission<T>, NotUsed> CreateFlow(ulong replayFromOffset = 0) =>
            Flow.FromGraph(new LmdbArchiveStage<T>(databaseName, lmdbEnvFactory));
    }

    public sealed class LmdbArchiveStage<T> : GraphStage<FlowShape<Command<T>, Emission<T>>>
    {
        private readonly Func<LMDBEnvironment> lmdbEnvFactory;
        private readonly string databaseName;
        
        public LmdbArchiveStage(string databaseName, Func<LMDBEnvironment> lmdbEnvFactory)
        {
            this.lmdbEnvFactory = lmdbEnvFactory;
            this.databaseName = databaseName;
            Inlet = new Inlet<Command<T>>("lmdb-archive.in");
            Outlet = new Outlet<Emission<T>>("lmdb-archive.out");
            Shape = new FlowShape<Command<T>, Emission<T>>(Inlet, Outlet);
        }

        public Inlet<Command<T>> Inlet { get; }
        public Outlet<Emission<T>> Outlet { get; }
        public override FlowShape<Command<T>, Emission<T>> Shape { get; }

        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        #region logic

        private sealed class Logic : InAndOutGraphStageLogic
        {
            private static readonly DirectBuffer OffsetKey = new DirectBuffer(Encoding.UTF8.GetBytes("$$offset"));
            
            private readonly Inlet<Command<T>> inlet;
            private readonly Outlet<Emission<T>> outlet;
            private readonly LMDBEnvironment env;
            private readonly Database db;

            private ulong offset = 0;
            
            public Logic(LmdbArchiveStage<T> stage) : base(stage.Shape)
            {
                this.env = stage.lmdbEnvFactory();
                this.inlet = stage.Inlet;
                this.outlet = stage.Outlet;
                this.db = env.OpenDatabase(stage.databaseName, new DatabaseConfig(DbFlags.None));
                
                SetHandler(inlet, this);
                SetHandler(outlet, this);
            }

            public override void OnPush()
            {
                var request = Grab(inlet);
            }

            public override void OnPull()
            {
                Pull(inlet);
            }

            public override void OnUpstreamFinish()
            {
                base.OnUpstreamFinish();
            }

            public override void OnUpstreamFailure(Exception e)
            {
                base.OnUpstreamFailure(e);
            }

            public override void OnDownstreamFinish()
            {
                base.OnDownstreamFinish();
            }

            public override void PreStart()
            {
                base.PreStart();
                ReadOffsetPosition();
            }

            private void ReadOffsetPosition()
            {
                using (var txn = env.BeginReadOnlyTransaction())
                using (var cursor = db.OpenReadOnlyCursor(txn))
                {
                    var key = OffsetKey;
                    var buffer = ArrayPool<byte>.Shared.Rent(8);
                    var value = new DirectBuffer(buffer);
                    if (cursor.TryGet(ref key, ref value, CursorGetOption.Last))
                    {
                        this.offset = BinaryPrimitives.ReadUInt64LittleEndian(value.Span);
                    }
                }
            }

            public override void PostStop()
            {
                base.PostStop();
                if (!(this.db is null)) try { this.db.Dispose(); } catch {}
            }
        }

        #endregion
    }
}