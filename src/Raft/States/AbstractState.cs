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

        public abstract bool VoteRequest(Client client, VoteRequest request);
        public abstract bool VoteReply(Client client, VoteReply reply);

        public abstract bool AppendEntriesRequest(Client client, AppendEntriesRequest request);
        public abstract bool AppendEntriesReply(Client client, AppendEntriesReply reply);
    }

}
