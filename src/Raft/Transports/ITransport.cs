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
        void SendMessage(Client client, IMessage message);
        void Process(Server server);

        void Start(IPEndPoint config);
        void Shutdown();
    }
}
