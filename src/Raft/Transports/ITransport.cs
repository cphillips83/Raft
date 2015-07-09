using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.Transports
{
    public interface ITransport
    {
        void SendMessage(Client client, VoteRequest request);
        void SendMessage(Client client, VoteReply reply);
        void SendMessage(Client client, AppendEntriesRequest request);
        void SendMessage(Client client, AppendEntriesReply reply);
        void SendMessage(Client client, AddServerRequest request);
        void SendMessage(Client client, AddServerReply reply);

        void Process(Server server);

        void Start(Configuration config);
        void Shutdown();
    }
}
