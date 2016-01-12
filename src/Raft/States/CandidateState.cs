using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.States
{
    public class CandidateState : AbstractState
    {
        private long _electionTimeout = long.MaxValue;

        public CandidateState(Server server) : base(server) { }

        public override void Enter()
        {
            var timeout = _server.PersistedStore.ELECTION_TIMEOUT;
            var randomTimeout = _server.Random.Next(timeout, timeout + timeout) / 2;
            _electionTimeout = _server.Tick + randomTimeout;

            _server.PersistedStore.UpdateState(_server.PersistedStore.Term + 1, _server.ID);

            Console.WriteLine("{0}: Starting new election for term {1}", _server.Name, _server.PersistedStore.Term);

            //only request from peers that are allowed to vote
            foreach (var client in _server.Voters)
                client.Reset();

            foreach (var client in _server.Voters)
                if (client.ReadyToSend)
                    client.VoteRequest();
                    //client.SendVoteRequest();
        }

        public override void Update()
        {
            if (_electionTimeout <= _server.Tick)
            {
                Console.WriteLine("{0}: Election timeout for term {1}", _server.Name, _server.PersistedStore.Term);
                _server.ChangeState(new CandidateState(_server));
            }
            else
            {
                var votes = _server.Voters.Count(x => x.VoteGranted) + 1;
                var votesNeeded = _server.Majority;
                if (votes >= votesNeeded)
                    _server.ChangeState(new LeaderState(_server));
            }
        }

        protected override VoteReply? VoteRequest2(Client client, VoteRequest request)
        {
            if (StepDown(request.Term))
                return null;

            //return new VoteReply()
            //{
            //    From = _server.ID,
            //    Term = _server.PersistedStore.Term,
            //    Granted = false
            //};
            return null;
        }

        //protected override bool VoteRequest(Client client, VoteRequest request)
        //{
        //    if (StepDown(request.Term))
        //        return false;

        //    client.SendVoteReply(false);
        //    return true;
        //}

        protected override bool VoteReply(Client client, VoteReply reply)
        {
            if (StepDown(reply.Term))
                return true;

            client.RpcDue = long.MaxValue;
            client.VoteGranted = reply.Granted;
            Console.WriteLine("{0}: Peer {1} voted {2}", _server.Name, client.ID, reply.Granted);
            return true;
        }

        protected override bool AppendEntriesRequest(Client client, AppendEntriesRequest request)
        {
            var _persistedState = _server.PersistedStore;
            if (_persistedState.Term < request.Term)
                _persistedState.Term = request.Term;

            _server.ChangeState(new FollowerState(_server));
            return false; ;
        }

        protected override bool AppendEntriesReply(Client client, AppendEntriesReply reply)
        {
            if (StepDown(reply.Term))
                return true;

            return true;
        }
    }
}
