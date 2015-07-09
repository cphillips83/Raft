﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.States
{
    public class FollowerState : AbstractState
    {
        private long _heatbeatTimeout = long.MaxValue;

        public FollowerState(Server server) : base(server) { }

        public override void Enter()
        {
            resetHeartbeat();
        }

        public override void Update()
        {
            if (_server.Tick > _heatbeatTimeout)
                _server.ChangeState(new CandidateState(_server));
        }

        private void resetHeartbeat()
        {
            var timeout = _server.PersistedStore.ELECTION_TIMEOUT;
            var randomTimeout = _server.Random.Next(timeout, timeout + timeout) / 2;
            _heatbeatTimeout = _server.Tick + randomTimeout;
        }

        public override bool VoteRequest(Client client, VoteRequest request)
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
            var logTermFurther = request.LastTerm > ourLastLogTerm;
            var logIndexLonger = request.LastTerm == ourLastLogTerm && request.LogLength >= _persistedState.Length;
            var granted = termCheck && canVote && (logTermFurther || logIndexLonger);

            if (!termCheck)
                Console.WriteLine("{0}: Can not vote for {1} because term {2}, expected {3}", _server.ID, client.ID, request.Term, _persistedState.Term);

            if (!canVote)
                Console.WriteLine("{0}: Can not vote for {1} because I already voted for {2}", _server.ID, client.ID, _persistedState.VotedFor);

            if (!(logTermFurther || logIndexLonger))
                Console.WriteLine("{0}: Can not vote for {1} because my log is more update to date", _server.ID, client.ID);

            if (granted)
            {
                Console.WriteLine("{0}: Voted for {1}", _server.ID, client.ID);
                _persistedState.VotedFor = client.ID;
                resetHeartbeat();
            }

            client.SendVoteReply(granted);
            return true;
        }

        public override bool VoteReply(Client client, VoteReply reply)
        {
            //we aren't looking for votes, ignore
            return true;
        }

        public override bool AppendEntriesRequest(Client client, AppendEntriesRequest request)
        {
            var _persistedState = _server.PersistedStore;
            if (_persistedState.Term < request.Term)
                _persistedState.Term = request.Term;

            //Console.WriteLine("heatbeat");

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
                                Console.WriteLine("{0}: Rolling back log {1}", _server.ID, _persistedState.Length - 1);
                                _persistedState.Pop();
                            }

                            //Console.WriteLine("{0}: Writing log value {1}", _id, request.Entries[i].Offset);
                            _persistedState.Push(request.Entries[i]);
                        }
                    }

                    matchIndex = index;
                    _server.CommitIndex2(Math.Max(_server.CommitIndex, request.CommitIndex));
                }
            }

            client.SendAppendEntriesReply(matchIndex, success);
            return true;
        }

        public override bool AppendEntriesReply(Client client, AppendEntriesReply reply)
        {
            var _persistedState = _server.PersistedStore;
            if (_persistedState.Term < reply.Term)
                _persistedState.Term = reply.Term;

            return true;
        }
    }

   
}
