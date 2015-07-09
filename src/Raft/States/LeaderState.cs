using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.States
{
    public class LeaderState : AbstractState
    {
        private class ServerJoin
        {
            public Client Client;
            public int Round;
            public uint RoundIndex;
            public long NextRound;
        }

        private List<ServerJoin> _serversToAdd = new List<ServerJoin>();

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

        public override void Exit()
        {
            foreach (var requests in _serversToAdd)
                requests.Client.SendAddServerReply(AddServerStatus.NotLeader, null);
            
            _serversToAdd.Clear();
        }

        public override void Update()
        {
            _server.AdvanceCommits();

            foreach (var client in _server.Clients)
            {
                if (client.NextHeartBeat <= _server.Tick ||
                    (client.NextIndex <= _server.PersistedStore.Length && client.ReadyToSend))
                {
                    client.SendAppendEntriesRequest();
                }
            }

            foreach (var join in _serversToAdd)
            {
                var client = join.Client;
                if (client.NextHeartBeat <= _server.Tick ||
                    (client.NextIndex <= _server.PersistedStore.Length && client.ReadyToSend))
                {
                    client.SendAppendEntriesRequest();
                }
            }
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

            var joiningServer = _serversToAdd.FirstOrDefault(x => x.Client.ID == client.ID);
            if (joiningServer != null)
                client = joiningServer.Client;

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

            if (joiningServer != null)
            {
                if (joiningServer.NextRound <= _server.Tick)
                {
                    joiningServer.Round--;
                    if (joiningServer.Client.MatchIndex != _server.CommitIndex)
                    {
                        //at the end of the rounds and still not caught up
                        //or we made no progress in a single round
                        if (joiningServer.Round <= 0 || joiningServer.RoundIndex == client.MatchIndex)
                        {
                            client.SendAddServerReply(AddServerStatus.TimedOut, new IPEndPoint(_server.Config.IP, _server.Config.Port));
                            TimeoutServerJoin(client);
                        }
                        else
                        {
                            joiningServer.RoundIndex = client.MatchIndex;
                            joiningServer.NextRound = _server.Tick + _server.PersistedStore.ELECTION_TIMEOUT;
                        }
                    }
                    else
                    {
                        //server is caught up to leader, add via log
                        //here we have to flag that configuration is locked
                        //client.SendAddServerReply(AddServerStatus.Ok, new IPEndPoint(_server.Config.IP, _server.Config.Port));
                    }
                }
            }
            return true;
        }

        protected override bool AddServerRequest(Client client, AddServerRequest request)
        {
            QueueServerJoin(client);
            return true;
        }

        private void QueueServerJoin(Client client)
        {
            foreach (var c in _serversToAdd)
                if (c.Client.ID == client.ID)
                    break;

            _serversToAdd.Add(new ServerJoin() { 
                Client = client, 
                Round = 10,
                NextRound = _server.Tick + _server.PersistedStore.ELECTION_TIMEOUT,
                RoundIndex = 0
            });
        }

        private void TimeoutServerJoin(Client client)
        {
            for (var i = 0; i < _serversToAdd.Count; i++)
            {
                if (_serversToAdd[i].Client.ID == client.ID)
                {
                    _serversToAdd.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
