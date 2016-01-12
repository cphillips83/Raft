using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft.Transports
{
    //public interface IMiddleware
    //{

    //}

    //public interface INodeProxy
    //{
    //    bool Active { get; }
    //    //IAsyncResult S
    //}

    //public class TestMe
    //{
    //    public void Test()
    //    {
    //        //Task<string> asdf;
            
    //    }
    //}

    public interface ITransport
    {
        void ResetConnection(Client client);
        Task<IMessage> SendMessageAsync(Client client, IMessage message);
        void SendMessage(Client client, IMessage message);
        void Process(Server server);

        void Start(IPEndPoint config);
        void Shutdown();

        //bool HandleMessage(Server server, object msg);
    }
}
