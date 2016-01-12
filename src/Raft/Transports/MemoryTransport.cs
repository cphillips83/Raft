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
        private class ClientTransport : Transport
        {
            private class ClientMessage
            {
                public int Tick;
                public Task Task;
            }

            private List<ClientMessage> _clientMessages = new List<ClientMessage>();
            private Server _server;
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

            private Task<IMessage> addMessage(object message)
            {
                var task = new Task<IMessage>(() =>
                {
                    handleMessage(_server, message);
                    return null;
                });

                _clientMessages.Add(new ClientMessage()
                {
                    Tick = _random.Next(_minRpc, _maxRpc),
                    Task = task
                });
                return task;
            }

            public override void Process(Server server)
            {
                _server = server;
                for (var i = 0; i < _clientMessages.Count; i++)
                {
                    var nextMessage = _clientMessages[i];
                    nextMessage.Tick--;
                    nextMessage.Task.RunSynchronously();
                    //if (nextMessage.Tick <= 0 && handleMessage(server, nextMessage.Message))
                    _clientMessages.RemoveAt(i--);
                }
            }

            protected override bool handleMessage(Server server, object msg)
            {
                if ((float)_random.NextDouble() < _packetDropRate)
                    return true;

                return base.handleMessage(server, msg);
            }

            public override void SendMessage(Client client, IMessage message)
            {
                addMessage(message);
            }

            public override Task<IMessage> SendMessageAsync(Client client, IMessage message)
            {
                if (message is VoteRequest)
                {
                    var task = new Task<IMessage>(() =>
                    {
                        return _server.CurrentState.VoteRequest2((VoteRequest)message);
                    });

                    _clientMessages.Add(new ClientMessage()
                    {
                        Tick = _random.Next(_minRpc, _maxRpc),
                        Task = task
                    });
                    return task;
                }
                else
                    return addMessage(message);
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

        public void ResetConnection(Client client) { }

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

        public void SendMessage(Client client, IMessage message)
        {
            var transport = GetClient(client.ID);
            transport.SendMessage(client, message);
        }

        public Task<IMessage> SendMessageAsync(Client client, IMessage message)
        {
            var transport = GetClient(client.ID);
            return transport.SendMessageAsync(client, message);
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
