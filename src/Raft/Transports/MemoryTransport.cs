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
            private class ClientMessage
            {
                public int Tick;
                public object Message;
            }

            private List<ClientMessage> _clientMessages = new List<ClientMessage>();

            private Random _random;
            private int _minRpc, _maxRpc;
            private float _packetDropRate;

            public ClientTransport(IPEndPoint ip, int minRpc, int maxRpc, float packetDropRate)
            {
                _random = new Random(ip.GetHashCode());
                _minRpc = minRpc;
                _maxRpc = maxRpc;
                _packetDropRate = packetDropRate;
            }

            private void addMessage(object message)
            {
                _clientMessages.Add(new ClientMessage()
                {
                    Tick = _random.Next(_minRpc, _maxRpc),
                    Message = message
                });
            }

            public override void Process(Server server)
            {
                for (var i = 0; i < _clientMessages.Count; i++)
                {
                    var nextMessage = _clientMessages[i];
                    nextMessage.Tick--;

                    if (nextMessage.Tick <= 0 && handleMessage(server, nextMessage.Message))
                        _clientMessages.RemoveAt(i--);
                }
            }

            protected override bool handleMessage(Server server, object msg)
            {
                if ((float)_random.NextDouble() < _packetDropRate)
                    return true;

                return base.handleMessage(server, msg);
            }

            public override void SendMessage(Client client, VoteRequest request)
            {
                addMessage(request);
            }

            public override void SendMessage(Client client, VoteReply reply)
            {
                addMessage(reply);
            }

            public override void SendMessage(Client client, AppendEntriesRequest request)
            {
                addMessage(request);
            }

            public override void SendMessage(Client client, AppendEntriesReply reply)
            {
                addMessage(reply);
            }

            public override void SendMessage(Client client, AddServerRequest request)
            {
                addMessage(request);
            }

            public override void SendMessage(Client client, AddServerReply reply)
            {
                addMessage(reply);
            }

            public override void SendMessage(Client client, RemoveServerRequest request)
            {
                addMessage(request);
            }

            public override void SendMessage(Client client, RemoveServerReply reply)
            {
                addMessage(reply);
            }

            public override void Start(IPEndPoint config)
            {
            }

            public override void Shutdown()
            {
            }

            public void SetPacketDropRate(float packetDropRate)
            {
                _packetDropRate = packetDropRate;
            }
        }

        private int _minRpc, _maxRpc;
        private float _packetDropRate;
        private Dictionary<IPEndPoint, ClientTransport> _clients = new Dictionary<IPEndPoint, ClientTransport>();

        public MemoryTransport()
        {
            _minRpc = 0;
            _maxRpc = 0;
        }

        public MemoryTransport(int minRpc, int maxRpc, float packetDropRate)
        {
            _minRpc = minRpc;
            _maxRpc = maxRpc;
            _packetDropRate = packetDropRate;
        }

        public void SetPacketDropRate(IPEndPoint ip, float packetDropRate)
        {
            var client = GetClient(ip);
            client.SetPacketDropRate(packetDropRate);
        }

        private ClientTransport GetClient(IPEndPoint client)
        {
            ClientTransport transport;
            if (!_clients.TryGetValue(client, out transport))
            {
                transport = new ClientTransport(client, _minRpc, _maxRpc, _packetDropRate);
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

        public void SendMessage(Client client, RemoveServerRequest request)
        {
            var transport = GetClient(client.ID);
            transport.SendMessage(client, request);
        }

        public void SendMessage(Client client, RemoveServerReply reply)
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
