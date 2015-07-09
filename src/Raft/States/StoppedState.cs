using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.States
{
    public class StoppedState : AbstractState
    {
        public StoppedState(Server _server) : base(_server) { }
        protected override bool VoteReply(Client client, VoteReply reply)
        {
            return true;
        }

        protected override bool VoteRequest(Client client, VoteRequest request)
        {
            return true;
        }

        protected override bool AppendEntriesRequest(Client client, AppendEntriesRequest request)
        {
            return true;
        }

        protected override bool AppendEntriesReply(Client client, AppendEntriesReply reply)
        {
            return true;
        }
    }
}
