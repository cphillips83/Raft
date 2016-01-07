using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.Transports
{
    public abstract class Transport : ITransport
    {
        public static readonly ITransport NULL = new NullTransport();

        protected Queue<object> _incomingMessages = new Queue<object>();

        public abstract void SendMessage(Client client, IMessage message);

        public abstract void SendMessage(Client client, VoteRequest request);
        public abstract void SendMessage(Client client, VoteReply reply);
        public abstract void SendMessage(Client client, AppendEntriesRequest request);
        public abstract void SendMessage(Client client, AppendEntriesReply reply);
        public abstract void SendMessage(Client client, EntryRequest request);
        public abstract void SendMessage(Client client, EntryRequestReply reply);
        public abstract void SendMessage(Client client, AddServerRequest request);
        public abstract void SendMessage(Client client, AddServerReply relpy);
        public abstract void SendMessage(Client client, RemoveServerRequest request);
        public abstract void SendMessage(Client client, RemoveServerReply relpy);

        public abstract void Start(IPEndPoint ip);
        public abstract void Shutdown();

        public virtual void ResetConnection(Client client) { }

        public virtual void Process(Server server)
        {
            while (_incomingMessages.Count > 0)
            {
                var nextMessage = _incomingMessages.Peek();
                if (handleMessage(server, nextMessage))
                    _incomingMessages.Dequeue();
            }
        }

        protected virtual bool handleMessage(Server server, object msg)
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
            else if (msg is RemoveServerRequest)
                return server.CurrentState.RemoveServerRequest((RemoveServerRequest)msg);
            else if (msg is RemoveServerReply)
                return server.CurrentState.RemoveServerReply((RemoveServerReply)msg);
            else if (msg is EntryRequest)
                return server.CurrentState.EntryRequest((EntryRequest)msg);
            else if (msg is EntryRequestReply)
                return server.CurrentState.EntryRequestReply((EntryRequestReply)msg);

            System.Diagnostics.Debug.Assert(false);
            return true;
        }
    }

}
