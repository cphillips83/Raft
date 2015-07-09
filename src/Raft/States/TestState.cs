using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.States
{
    public class TestState : AbstractState
    {
        public object LastMessage;
        public Client LastClient;

        public TestState(Server _server) : base(_server) { }

        protected override bool VoteReply(Client client, VoteReply reply)
        {
            LastMessage = reply;
            LastClient = client;
            return true;
        }

        protected override bool VoteRequest(Client client, VoteRequest request)
        {
            LastMessage = request;
            LastClient = client;
            return true;
        }

        protected override bool AppendEntriesRequest(Client client, AppendEntriesRequest request)
        {
            LastMessage = request;
            LastClient = client;
            return true;
        }

        protected override bool AppendEntriesReply(Client client, AppendEntriesReply reply)
        {
            LastMessage = reply;
            LastClient = client;
            return true;
        }

        protected override bool AddServerReply(Client client, AddServerReply reply)
        {
            LastMessage = reply;
            LastClient = client;
            return true;
        }

        protected override bool AddServerRequest(Client client, AddServerRequest request)
        {
            LastMessage = request;
            LastClient = client;
            return true;
        }
    }
}
