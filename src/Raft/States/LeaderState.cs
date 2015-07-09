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
            var _clients = _server._clients;
            var _persistedStore = _server.PersistedStore;

            var matchIndexes = new uint[_clients.Count + 1];
            matchIndexes[matchIndexes.Length - 1] = _persistedStore.Length;
            for (var i = 0; i < _clients.Count; i++)
                matchIndexes[i] = _clients[i].MatchIndex;

            Array.Sort(matchIndexes);

            var n = matchIndexes[_clients.Count / 2];
            if (_persistedStore.GetTerm(n) == _persistedStore.Term)
                _server.CommitIndex2(Math.Max(_server.CommitIndex, n));

            foreach (var client in _server.Clients)
            {
                if (client.NextHeartBeat <= _server.Tick ||
                    (client.NextIndex <= _server.PersistedStore.Length && client.ReadyToSend))
                {
                    client.SendAppendEntriesRequest();
                }
            }
        }

        public override bool VoteReply(Client client, VoteReply reply)
        {
            StepDown(reply.Term);
            return true;
        }

        public override bool VoteRequest(Client client, VoteRequest request)
        {
            if (StepDown(request.Term))
                return false;

            client.SendVoteReply(false);
            return true;
        }

        public override bool AppendEntriesRequest(Client client, AppendEntriesRequest request)
        {
            var _persistedState = _server.PersistedStore;
            if (_persistedState.Term < request.Term)
                _persistedState.Term = request.Term;

            _server.ChangeState(new FollowerState(_server));
            return false;
        }

        public override bool AppendEntriesReply(Client client, AppendEntriesReply reply)
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
