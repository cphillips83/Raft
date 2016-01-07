using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.Transports
{
    public class NullTransport : ITransport
    {
        public void ResetConnection(Client client) { }

        public void SendMessage(Client client, IMessage message) { }

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
