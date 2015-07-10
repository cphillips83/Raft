using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.States
{
    public class JoinState : FollowerState
    {
        private Client _bootstrap;

        public JoinState(Server server, Client bootstrap)
            : base(server)
        {
            _bootstrap = bootstrap;
        }

        public override void Enter()
        {
            base.Enter();
            HeartbeatTimeout();
        }

        protected override void HeartbeatTimeout()
        {
            _bootstrap.SendAddServerRequest();
        }

        protected override bool AddServerReply(Client client, AddServerReply reply)
        {
            if (client == null)
                Console.WriteLine("{0}: Server {1} replied with not leader and doesn't know who is the leader", _server.ID, reply.From);
            else
            {
                //try again, playing catch up or not leader
                //client should be derived from leader hint
                if (reply.Status == AddServerStatus.TimedOut || reply.Status == AddServerStatus.NotLeader)
                {
                    _bootstrap = client;
                    resetHeartbeat();
                    Console.WriteLine("{0}: Server {1} replied with not leader or timedout and suggest {2}", _server.ID, reply.From, reply.From);
                }
                else
                {
                    //ok, we were added! switch to follower
                    //client list should be up to date via the log entries
                    Console.WriteLine("{0}: Server {1} replied with OK", _server.ID, reply.From);
                    _server.ChangeState(new FollowerState(_server));
                }
            }
            return true;
        }

        //protected override bool AppendEntriesRequest(Client client, AppendEntriesRequest request)
        //{
        //    if (client == null)
        //        client = new Client(_server, request.From);

        //    return base.AppendEntriesRequest(client, request);
        //}

        protected override bool VoteReply(Client client, VoteReply reply)
        {
            return true;
        }

        protected override bool AddServerRequest(Client client, AddServerRequest request)
        {
            return true;
        }

        //protected override bool VoteRequest(Client client, VoteRequest request)
        //{
        //    if (client == null)
        //        client = new Client(_server, request.From);

        //    return base.VoteRequest(client, request);
        //}

        // should give its vote
        //protected override bool VoteRequest(Client client, VoteRequest request)
        //{
        //    return true;
        //}

        protected override bool AppendEntriesReply(Client client, AppendEntriesReply reply)
        {
            return true;
        }
    }
}
