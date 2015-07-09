using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.States
{
    public class LeaderState : AbstractState
    {
        public LeaderState(Server server) : base(server) { }

        public override void Enter()
        {
            var votes = _server.Voters.Count(x => x.VoteGranted) + 1;
            Console.WriteLine("{0}: I am now leader of term {2} with {1} votes", _server.ID, votes, _server.PersistedStore.Term);
            foreach (var client in _server.Clients)
            {
                client.NextIndex = _server.PersistedStore.Length + 1;
                client.NextHeartBeat = 0;
            }
        }

        public override void Update()
        {
            _server.AdvanceCommits(); 
        }

        protected override bool VoteReply(Client client, VoteReply reply)
        {
            StepDown(reply.Term);
            return true;
        }

        protected override bool VoteRequest(Client client, VoteRequest request)
        {
            if (StepDown(request.Term))
                return false;

            client.SendVoteReply(false);
            return true;
        }

        protected override bool AppendEntriesRequest(Client client, AppendEntriesRequest request)
        {
            var _persistedState = _server.PersistedStore;
            if (_persistedState.Term < request.Term)
                _persistedState.Term = request.Term;

            _server.ChangeState(new FollowerState(_server));
            return false;
        }

        protected override bool AppendEntriesReply(Client client, AppendEntriesReply reply)
        {
            if (StepDown(reply.Term))
                return true;

            if (reply.Success)
            {
                client.MatchIndex = Math.Max(client.MatchIndex, reply.MatchIndex);
                client.NextIndex = reply.MatchIndex + 1;
            }
            else
            {
                client.NextIndex = Math.Max(1, client.NextIndex - 1);
            }
            client.RpcDue = 0;
            return true;
        }
    }
}
