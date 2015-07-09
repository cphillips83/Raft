using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.Transports
{
    public abstract class AbstractTransport : ITransport
    {
        protected Queue<object> _incomingMessages = new Queue<object>();

        public abstract void SendMessage(Client client, VoteRequest request);
        public abstract void SendMessage(Client client, VoteReply reply);
        public abstract void SendMessage(Client client, AppendEntriesRequest request);
        public abstract void SendMessage(Client client, AppendEntriesReply reply);
        public abstract void SendMessage(Client client, AddServerRequest request);
        public abstract void SendMessage(Client client, AddServerReply relpy);

        public abstract void Start(Configuration config);
        public abstract void Shutdown();

        public virtual void Process(Server server)
        {
            while (_incomingMessages.Count > 0)
            {
                var nextMessage = _incomingMessages.Peek();
                if (handleMessage(server, nextMessage))
                    _incomingMessages.Dequeue();
            }
        }

        private bool handleMessage(Server server, object msg)
        {
            if (msg is VoteRequest)
                return server.CurrentState.VoteRequest((VoteRequest)msg);
            else if (msg is VoteReply)
                return server.CurrentState.VoteReply((VoteReply)msg);
            else if (msg is AppendEntriesRequest)
                return server.CurrentState.AppendEntriesRequest((AppendEntriesRequest)msg);
            else if (msg is AppendEntriesReply)
                return server.CurrentState.AppendEntriesReply((AppendEntriesReply)msg);
            else if (msg is AddServerRequest)
                return server.CurrentState.AddServerRequest((AddServerRequest)msg);
            else if (msg is AddServerReply)
                return server.CurrentState.AddServerReply((AddServerReply)msg);

            System.Diagnostics.Debug.Assert(false);
            return true;
        }
    }

}
