using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.Transports
{
    public class MemoryTransport : ITransport
    {
        private class ClientTransport : AbstractTransport
        {
            public override void SendMessage(Client client, VoteRequest request)
            {
                _incomingMessages.Enqueue(request);       
            }

            public override void SendMessage(Client client, VoteReply reply)
            {
                _incomingMessages.Enqueue(reply);
            }

            public override void SendMessage(Client client, AppendEntriesRequest request)
            {
                _incomingMessages.Enqueue(request);
            }

            public override void SendMessage(Client client, AppendEntriesReply reply)
            {
                _incomingMessages.Enqueue(reply);
            }

            public override void SendMessage(Client client, AddServerRequest request)
            {
                _incomingMessages.Enqueue(request);
            }

            public override void SendMessage(Client client, AddServerReply reply)
            {
                _incomingMessages.Enqueue(reply);
            }

            public override void Start(IPEndPoint config)
            {
            }

            public override void Shutdown()
            {
            }
        }

        private Dictionary<IPEndPoint, ClientTransport> _clients = new Dictionary<IPEndPoint, ClientTransport>();

        private ClientTransport GetClient(IPEndPoint client)
        {
            ClientTransport transport;
            if (!_clients.TryGetValue(client, out transport))
            {
                transport = new ClientTransport();
                _clients.Add(client, transport);
            }

            return transport;
        }

        public void SendMessage(Client client, VoteRequest request)
        {
            var transport = GetClient(client.ID);
            transport.SendMessage(client, request);
        }

        public void SendMessage(Client client, VoteReply reply)
        {
            var transport = GetClient(client.ID);
            transport.SendMessage(client, reply);
        }

        public void SendMessage(Client client, AppendEntriesRequest request)
        {
            var transport = GetClient(client.ID);
            transport.SendMessage(client, request);
        }

        public void SendMessage(Client client, AppendEntriesReply reply)
        {
            var transport = GetClient(client.ID);
            transport.SendMessage(client, reply);
        }

        public void SendMessage(Client client, AddServerRequest request)
        {
            var transport = GetClient(client.ID);
            transport.SendMessage(client, request);
        }

        public void SendMessage(Client client, AddServerReply reply)
        {
            var transport = GetClient(client.ID);
            transport.SendMessage(client, reply);
        }

        public void Process(Server server)
        {
            var client = GetClient(server.ID);
            client.Process(server);
        }

        public void Start(IPEndPoint config)
        {
            
        }

        public void Shutdown()
        {
            
        }
    }
}
