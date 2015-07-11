using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.States
{
    public class LeaveState : FollowerState
    {
        public LeaveState(Server server)
            : base(server)
        {
        }

        public override void Enter()
        {
            base.Enter();
        }

        protected override void HeartbeatTimeout()
        {
            if (_leader == null)
            {
                Console.WriteLine("{0}: Trying to leave but no leader");
            }
            else
            {
                _leader.SendRemoveServerRequest();
            }
            resetHeartbeat();
        }

        protected override bool RemoveServerReply(Client client, RemoveServerReply reply)
        {
            if (client == null)
                Console.WriteLine("{0}: Server {1} replied with not leader and doesn't know who is the leader", _server.ID, reply.From);
            else
            {
                if (reply.Status == RemoveServerStatus.Ok)
                {
                    client.RpcDue = 0;
                    Console.WriteLine("{0}: Removed from cluster, stopping", _server.ID);
                    _server.ChangeState(new StoppedState(_server));
                }
            }
            return true;
        }

        protected override bool VoteReply(Client client, VoteReply reply)
        {
            return true;
        }
        
        protected override bool AppendEntriesRequest(Client client, AppendEntriesRequest request)
        {
            client.SendRemoveServerRequest();
            return base.AppendEntriesRequest(client, request);
        }

        protected override bool AddServerRequest(Client client, AddServerRequest request)
        {
            return true;
        }

        protected override bool AppendEntriesReply(Client client, AppendEntriesReply reply)
        {
            return true;
        }
    }
}
