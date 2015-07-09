using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.States
{
    public abstract class AbstractState
    {
        protected Server _server;
        protected AbstractState(Server server)
        {
            _server = server;
        }

        public virtual bool StepDown(int term)
        {
            if (_server.PersistedStore.Term < term)
            {
                _server.PersistedStore.UpdateState(term, null);
                _server.ChangeState(new FollowerState(_server));
                return true;
            }

            return false;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void Exit() { }


        public bool VoteRequest(VoteRequest request)
        {
            var client = _server.GetClient(request.From);
            return VoteRequest(client, request);
        }

        public bool VoteReply(VoteReply reply)
        {
            var client = _server.GetClient(reply.From);
            return VoteReply(client, reply);
        }

        public bool AppendEntriesRequest(AppendEntriesRequest request)
        {
            var client = _server.GetClient(request.From);
            return AppendEntriesRequest(client, request);
        }

        public bool AppendEntriesReply(AppendEntriesReply reply)
        {
            var client = _server.GetClient(reply.From);
            return AppendEntriesReply(client, reply);
        }

        protected abstract bool VoteRequest(Client client, VoteRequest request);
        protected abstract bool VoteReply(Client client, VoteReply reply);

        protected abstract bool AppendEntriesRequest(Client client, AppendEntriesRequest request);
        protected abstract bool AppendEntriesReply(Client client, AppendEntriesReply reply);
    }

}
