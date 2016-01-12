using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Logs;
using Raft.Messages;

namespace Raft.States
{
    public class FollowerState : AbstractState
    {
        protected Client _leader;
        private long _heatbeatTimeout = long.MaxValue;

        public Client CurrentLeader { get { return _leader; } }

        public FollowerState(Server server) : base(server) { }

        public override void Enter()
        {
            resetHeartbeat();
        }

        public override void Update()
        {
            if (_server.Tick > _heatbeatTimeout)
                HeartbeatTimeout();
        }

        protected virtual void HeartbeatTimeout()
        {
            _server.ChangeState(new CandidateState(_server));
        }

        protected void resetHeartbeat()
        {
            var timeout = _server.PersistedStore.ELECTION_TIMEOUT;
            var randomTimeout = _server.Random.Next(timeout, timeout + timeout);
            _heatbeatTimeout = _server.Tick + randomTimeout;
        }

        protected override VoteReply VoteRequest2(Client client, VoteRequest request)
        {
            var _persistedState = _server.PersistedStore;
            if (_persistedState.Term < request.Term)
            {
                _persistedState.Term = request.Term;
                if (_heatbeatTimeout <= _server.Tick)
                    resetHeartbeat();
            }

            var ourLastLogTerm = _persistedState.GetLastTerm();
            var termCheck = _persistedState.Term == request.Term;
            var canVote = _persistedState.VotedFor == null || _persistedState.VotedFor == request.From;
            var ourLogIsBetter = _persistedState.LogIsBetter(request.LogLength, request.LastTerm);
            //var logTermFurther = request.LastTerm > ourLastLogTerm;
            //var logIndexLonger = request.LastTerm == ourLastLogTerm && request.LogLength >= _persistedState.Length;
            var granted = termCheck && canVote && !ourLogIsBetter;

            if (!termCheck)
                Console.WriteLine("{0}: Can not vote for {1} because term {2}, expected {3}", _server.Name, client.ID, request.Term, _persistedState.Term);

            if (!canVote)
                Console.WriteLine("{0}: Can not vote for {1} because I already voted for {2}", _server.Name, client.ID, _persistedState.VotedFor);

            if (ourLogIsBetter)
                Console.WriteLine("{0}: Can not vote for {1} because my log is more update to date", _server.Name, client.ID);

            if (granted)
            {
                Console.WriteLine("{0}: Voted for {1}", _server.Name, client.ID);
                _persistedState.VotedFor = client.ID;
                _leader = null;
                resetHeartbeat();
            }

            return new VoteReply()
            {
                From = _server.ID,
                Term = _server.PersistedStore.Term,
                Granted = granted
            };
        }

        protected override bool VoteRequest(Client client, VoteRequest request)
        {
            var _persistedState = _server.PersistedStore;
            if (_persistedState.Term < request.Term)
            {
                _persistedState.Term = request.Term;
                if (_heatbeatTimeout <= _server.Tick)
                    resetHeartbeat();
            }

            var ourLastLogTerm = _persistedState.GetLastTerm();
            var termCheck = _persistedState.Term == request.Term;
            var canVote = _persistedState.VotedFor == null || _persistedState.VotedFor == request.From;
            var ourLogIsBetter = _persistedState.LogIsBetter(request.LogLength, request.LastTerm);
            //var logTermFurther = request.LastTerm > ourLastLogTerm;
            //var logIndexLonger = request.LastTerm == ourLastLogTerm && request.LogLength >= _persistedState.Length;
            var granted = termCheck && canVote && !ourLogIsBetter;

            if (!termCheck)
                Console.WriteLine("{0}: Can not vote for {1} because term {2}, expected {3}", _server.Name, client.ID, request.Term, _persistedState.Term);

            if (!canVote)
                Console.WriteLine("{0}: Can not vote for {1} because I already voted for {2}", _server.Name, client.ID, _persistedState.VotedFor);

            if (ourLogIsBetter)
                Console.WriteLine("{0}: Can not vote for {1} because my log is more update to date", _server.Name, client.ID);

            if (granted)
            {
                Console.WriteLine("{0}: Voted for {1}", _server.Name, client.ID);
                _persistedState.VotedFor = client.ID;
                _leader = null;
                resetHeartbeat();
            }

            client.SendVoteReply(granted);
            return true;
        }

        protected override bool VoteReply(Client client, VoteReply reply)
        {
            //we aren't looking for votes, ignore
            return true;
        }

        protected override bool AppendEntriesRequest(Client client, AppendEntriesRequest request)
        {
            var _persistedState = _server.PersistedStore;
            if (_persistedState.Term < request.Term)
                _persistedState.Term = request.Term;

            //Console.WriteLine("heatbeat");
            _leader = client;
            _leader.AgentIP = request.AgentIP;

            var success = false;
            var matchIndex = 0u;

            if (_persistedState.Term == request.Term)
            {
                resetHeartbeat();

                if (request.PrevIndex == 0 ||
                    (request.PrevIndex <= _persistedState.Length && _persistedState.GetTerm(request.PrevIndex) == request.PrevTerm))
                {
                    success = true;

                    var index = request.PrevIndex;
                    for (var i = 0; request.Entries != null && i < request.Entries.Length; i++)
                    {
                        index++;
                        if (_persistedState.GetTerm(index) != request.Entries[i].Index.Term)
                        {
                            while (_persistedState.Length > index - 1)
                            {
                                Console.WriteLine("{0}: Rolling back log {1}", _server.Name, _persistedState.Length - 1);
                                _persistedState.Pop(_server);
                            }

                            //Console.WriteLine("{0}: Writing log value {1}", _id, request.Entries[i].Offset);
                            var pushed = _persistedState.Push(_server, request.Entries[i]);
                            if (pushed)
                            {
                                var lastEntry = _persistedState.GetEntry(index).Value;
                                if (!LogEntry.AreEqual(lastEntry, request.Entries[i]))
                                {
                                    Console.WriteLine("{0}: Log entry push didn't match", _server.Name);
                                    Console.WriteLine("{0}: Expected: {1}", _server.Name, request.Entries[i]);
                                    Console.WriteLine("{0}: Got:      {1}", _server.Name, lastEntry);

                                    pushed = false;
                                }
                                //Console.WriteLine("{0}: Appending index: {1}, data length: {2}", _server.ID, index, request.Entries[i].Index.ChunkSize);                             
                            }

                            if(!pushed)
                            {
                                index--;
                                break;
                            }
                        }
                    }

                    matchIndex = index;
                    _server.AdvanceToCommit(Math.Max(_server.CommitIndex, request.CommitIndex));
                }
                else
                {
                    Console.WriteLine("{0}: Append unsuccessful {{ request.PrevIndex[{1}] == 0 || (request.PrevIndex[{1}] <= _persistedState.Length[{2}] && _persistedState.GetTerm(request.PrevIndex[{1}])[{3}] == request.PrevTerm[{4}])) }}", _server.Name, request.PrevIndex, _persistedState.Length, _persistedState.GetTerm(request.PrevIndex), request.PrevTerm);
                    matchIndex = _server.CommitIndex;
                }
            }
            else
            {
                Console.WriteLine("{0}: Append failed, terms didn't match", _server.Name);
                //matchIndex = _server.CommitIndex;
            }

            client.SendAppendEntriesReply(matchIndex, success);
            return true;
        }

        protected override bool AppendEntriesReply(Client client, AppendEntriesReply reply)
        {
            var _persistedState = _server.PersistedStore;
            if (_persistedState.Term < reply.Term)
                _persistedState.Term = reply.Term;

            return true;
        }

        protected override bool AddServerRequest(Client client, AddServerRequest request)
        {
            if (_leader != null)
            {
                client.SendAddServerReply(AddServerStatus.NotLeader, _leader.ID);
                return true;
            }

            return base.AddServerRequest(client, request);
        }
    }

   
}
