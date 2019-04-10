using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Vergil.Persistence.Volatile;
using Vergil.Replication;
using Xunit;
using Xunit.Abstractions;

namespace Vergil.Tests.Replication
{
    public class ReplicatorTests : TestKit
    {
        public ReplicatorTests(ITestOutputHelper output) : base(output: output)
        {
        }

        [Fact]
        public async Task Replicator_should_start_from_replaying_its_own_state()
        {
        }
        
        [Fact]
        public async Task Replicator_should_be_able_to_connect_to_another_one_and_receive_pending_updates()
        {
            
        }
        
        [Fact]
        public async Task Replicators_should_be_able_to_exchange_data_after_stabilizing()
        {
            
        }
        
        [Fact]
        public async Task Replicator_should_still_work_when_another_one_died()
        {
            
        }
        
        [Fact]
        public async Task Replicators_should_be_able_to_reconnect_after_failure_and_keep_on_working()
        {
            
        }
    }
}