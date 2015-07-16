using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.Transports
{
    public interface ITransport
    {
        void ResetConnection(Client client);
        void SendMessage(Client client, VoteRequest request);
        void SendMessage(Client client, VoteReply reply);
        void SendMessage(Client client, AppendEntriesRequest request);
        void SendMessage(Client client, AppendEntriesReply reply);
        void SendMessage(Client client, AddServerRequest request);
        void SendMessage(Client client, AddServerReply reply);
        void SendMessage(Client client, RemoveServerRequest request);
        void SendMessage(Client client, RemoveServerReply reply);

        void Process(Server server);

        void Start(IPEndPoint config);
        void Shutdown();
    }
}
