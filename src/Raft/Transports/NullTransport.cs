using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Transports
{
    public class NullTransport : ITransport
    {
        public void SendMessage(Client client, Messages.VoteRequest request)
        {
        }

        public void SendMessage(Client client, Messages.VoteReply reply)
        {
        }

        public void SendMessage(Client client, Messages.AppendEntriesRequest request)
        {
        }

        public void SendMessage(Client client, Messages.AppendEntriesReply reply)
        {
        }

        public void SendMessage(Client client, Messages.AddServerRequest request)
        {
        }

        public void SendMessage(Client client, Messages.AddServerReply reply)
        {
        }

        public void SendMessage(Client client, Messages.RemoveServerRequest request)
        {
        }

        public void SendMessage(Client client, Messages.RemoveServerReply reply)
        {
        }

        public void Process(Server server)
        {
        }

        public void Start(System.Net.IPEndPoint config)
        {
        }

        public void Shutdown()
        {
        }
    }
}
