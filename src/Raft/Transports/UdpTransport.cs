using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Raft.Logs;
using Raft.Messages;

namespace Raft.Transports
{
  

    public class UdpTransport : Transport
    {
        public UdpClient _rpc;

        private byte[] _spad = new byte[65535];

        public override void SendMessage(Client client, IMessage message)
        {
            using (var msg = new BinaryWriter(new MemoryStream(_spad, true)))
            {
                message.Write(msg);
                _rpc.Send(_spad, (int)msg.BaseStream.Position, client.ID);                
            }
        }

        public override void Start(IPEndPoint ip)
        {
            _rpc = new UdpClient(ip);
            uint IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
            _rpc.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
            // _rpc.EnableBroadcast = true;
        }

        public override void Shutdown()
        {
            //if (_rpc != null)
            //    _rpc.Close();

            _rpc = null;
        }

        public override void Process(Server server)
        {
            while (_rpc.Available > 0)
            {
                var remoteIP = new IPEndPoint(IPAddress.Any, server.ID.Port);
                var data = _rpc.Receive(ref remoteIP);
                using (var msg = new BinaryReader(new MemoryStream(data)))
                {
                    IMessage incomingMessage;
                    switch ((MessageTypes)msg.ReadByte())
                    {
                        case MessageTypes.VoteRequest:
                            {
                                incomingMessage = new VoteRequest();
                            }
                            break;
                        case MessageTypes.VoteReply:
                            {
                                incomingMessage = new VoteReply();
                            }
                            break;
                        case MessageTypes.AppendEntriesRequest:
                            {
                                incomingMessage = new AppendEntriesRequest();
                            }
                            break;
                        case MessageTypes.AppendEntriesReply:
                            {
                                incomingMessage = new AppendEntriesReply();
                            }
                            break;
                        case MessageTypes.EntryRequestReply:
                            {
                                incomingMessage = new EntryRequestReply();
                            }
                            break;
                        case MessageTypes.EntryRequest:
                            {
                                incomingMessage = new EntryRequest();
                            }
                            break;
                        case MessageTypes.AddServerRequest:
                            {
                                incomingMessage = new AddServerRequest();
                            }
                            break;
                        case MessageTypes.AddServerReply:
                            {
                                incomingMessage = new AddServerReply();
                            }
                            break;
                        case MessageTypes.RemoveServerRequest:
                            {
                                incomingMessage = new RemoveServerRequest();
                            }
                            break;
                        case MessageTypes.RemoveServerReply:
                            {
                                incomingMessage = new RemoveServerReply();
                            }
                            break;
                        default:
                            Console.WriteLine("Unsupported message");
                            return;
                    }
                    incomingMessage.Read(msg);
                    _incomingMessages.Enqueue(incomingMessage);
                }
            }
            base.Process(server);
        }
    }
}
