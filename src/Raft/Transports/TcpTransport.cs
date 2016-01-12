//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading.Tasks;
//using Raft.Logs;
//using Raft.Messages;

//namespace Raft.Transports
//{
//    public class TcpTransport : Transport
//    {
//        public TcpListener _rpc;

//        private class ClientTransport : Transport
//        {

//            private byte[] _spad = new byte[65535];

//            private TcpClient _client;
//            private IPEndPoint _ip;
//            private NetworkStream _stream;
//            private BinaryWriter _writer;
//            private BinaryReader _reader;

//            private bool connected = false;
//            private bool connecting = false;
//            public ClientTransport(IPEndPoint ip)
//            {
//                _ip = ip;
//            }

//            private void setup()
//            {
//                connecting = false;
//                connected = true;
//                _stream = _client.GetStream();
//                _writer = new BinaryWriter(_stream);
//                _reader = new BinaryReader(_stream);
//                Console.WriteLine("Connected to {0}", _ip);
//            }

//            private void tryConnect()
//            {
//                if (connected || connecting)
//                    return;

//                connecting = true;

//                try
//                {
//                    _client = new TcpClient();
//                    _client.NoDelay = true;
                    
//                    _client.BeginConnect(_ip.Address, _ip.Port,
//                        new AsyncCallback(EndConnect), _client);
//                }
//                catch (Exception ex)
//                {
//                    connected = false;
//                    connecting = false;
//                    Console.WriteLine(ex);
//                }
//            }

//            private void EndConnect(IAsyncResult ar)
//            {
//                try
//                {
//                    var s = (TcpClient)ar.AsyncState;
//                    s.EndConnect(ar);
//                    setup();
//                }
//                catch (Exception ex)
//                {
//                    connected = false;
//                    connecting = false;
//                    Console.WriteLine(ex);
//                }
//            }

//            public override void Process(Server server)
//            {
//                if (_client == null)
//                {
//                    tryConnect();
//                    return;
//                }

//                while (_client.Available > 0)
//                {
//                    {
//                        //while (_stream.Position != _stream.Length)
//                        {
//                            //var remoteIP = new IPEndPoint(IPAddress.Any, server.ID.Port);
//                            //using (var msg = new BinaryReader(new MemoryStream(data)))
//                            {
//                                IMessage incomingMessage;
//                                switch ((MessageTypes)_reader.ReadByte())
//                                {
//                                    case MessageTypes.VoteRequest:
//                                        {
//                                            incomingMessage = new VoteRequest();
//                                        }
//                                        break;
//                                    case MessageTypes.VoteReply:
//                                        {
//                                            incomingMessage = new VoteReply();
//                                        }
//                                        break;
//                                    case MessageTypes.AppendEntriesRequest:
//                                        {
//                                            incomingMessage = new AppendEntriesRequest();
//                                        }
//                                        break;
//                                    case MessageTypes.AppendEntriesReply:
//                                        {
//                                            incomingMessage = new AppendEntriesReply();
//                                        }
//                                        break;
//                                    case MessageTypes.EntryRequestReply:
//                                        {
//                                            incomingMessage = new EntryRequestReply();
//                                        }
//                                        break;
//                                    case MessageTypes.EntryRequest:
//                                        {
//                                            incomingMessage = new EntryRequest();
//                                        }
//                                        break;
//                                    case MessageTypes.AddServerRequest:
//                                        {
//                                            incomingMessage = new AddServerRequest();
//                                        }
//                                        break;
//                                    case MessageTypes.AddServerReply:
//                                        {
//                                            incomingMessage = new AddServerReply();
//                                        }
//                                        break;
//                                    case MessageTypes.RemoveServerRequest:
//                                        {
//                                            incomingMessage = new RemoveServerRequest();
//                                        }
//                                        break;
//                                    case MessageTypes.RemoveServerReply:
//                                        {
//                                            incomingMessage = new RemoveServerReply();
//                                        }
//                                        break;
//                                    default:
//                                        Console.WriteLine("Unsupported message");
//                                        return;
//                                }
//                                incomingMessage.Read(_reader);
//                                _incomingMessages.Enqueue(incomingMessage);
//                            }
//                        }
//                    }
//                }
//            }

//            public override void SendMessage(Client client, IMessage message)
//            {
//                if (!connected)
//                {
//                    tryConnect();
//                    return;
//                }

//                try
//                {
//                    message.Write(_writer);

//                    _stream.Flush();
//                }
//                catch (Exception ex)
//                {
//                    try
//                    {
//                        _client.Close();
//                    }
//                    catch { }

//                    _client = null;
//                    _stream = null;
//                    connected = false;
//                    tryConnect();
//                    Console.WriteLine(ex);
//                }
//            }

//            public override void Start(IPEndPoint config)
//            {
//                try
//                {
//                    _client = new TcpClient();
//                    _client.NoDelay = true;
//                    _client.ConnectAsync(config.Address, config.Port).Wait(50);
//                    setup();
//                }
//                catch (Exception ex)
//                {
//                    connected = false;
//                    connecting = false;
//                    Console.WriteLine(ex);
//                }
//            }

//            public override void Shutdown()
//            {
//                try
//                {
//                    connecting = false;
//                    connected = false;
//                    _client.Close();
//                    _stream = null;
//                }
//                catch { }
//                _client = null;
//            }
//        }

//        private Dictionary<IPEndPoint, ClientTransport> _clients = new Dictionary<IPEndPoint, ClientTransport>();

//        public override void SendMessage(Client client, IMessage message)
//        {
//            var rc = GetClient(client.ID);
//            rc.SendMessage(client, message);
//        }

//        private ClientTransport GetClient(IPEndPoint client)
//        {
//            ClientTransport transport;
//            if (!_clients.TryGetValue(client, out transport))
//            {
//                transport = new ClientTransport(client);
//                transport.Start(client);
//                _clients.Add(client, transport);
//            }

//            return transport;
//        }

//        public override void Start(IPEndPoint ip)
//        {
//            _rpc = new TcpListener(ip);
//            _rpc.Start();
//        }

//        public override void Shutdown()
//        {
//            try
//            {
//                _rpc.Stop();
//                _rpc = null;

//                foreach (var rm in _remote)
//                    try { rm.Close(); } catch { }

//                _remote.Clear();
//                _streams.Clear();
//            }
//            catch { }
//        }

//        private List<TcpClient> _remote = new List<TcpClient>();
//        private List<BinaryReader> _streams = new List<BinaryReader>();

//        public override void Process(Server server)
//        {
//            while (_rpc.Pending())
//            {
//                var client = _rpc.AcceptTcpClient();
//                _remote.Add(client);
//                _streams.Add(new BinaryReader(client.GetStream()));
//            }

//            for (int i = _remote.Count - 1; i >= 0; i--)
//            {
//                try
//                {
//                    var rm = _remote[i];
//                    while (rm.Available > 0)
//                    {
//                        var _stream = _streams[i];
//                        //while (_stream.BaseStream.Position != _stream.BaseStream.Length)
//                        {
//                            //var remoteIP = new IPEndPoint(IPAddress.Any, server.ID.Port);
//                            //using (var msg = new BinaryReader(new MemoryStream(data)))
//                            {
//                                IMessage incomingMessage;
//                                switch ((MessageTypes)_stream.ReadByte())
//                                {
//                                    case MessageTypes.VoteRequest:
//                                        {
//                                            incomingMessage = new VoteRequest();
//                                        }
//                                        break;
//                                    case MessageTypes.VoteReply:
//                                        {
//                                            incomingMessage = new VoteReply();
//                                        }
//                                        break;
//                                    case MessageTypes.AppendEntriesRequest:
//                                        {
//                                            incomingMessage = new AppendEntriesRequest();
//                                        }
//                                        break;
//                                    case MessageTypes.AppendEntriesReply:
//                                        {
//                                            incomingMessage = new AppendEntriesReply();
//                                        }
//                                        break;
//                                    case MessageTypes.EntryRequestReply:
//                                        {
//                                            incomingMessage = new EntryRequestReply();
//                                        }
//                                        break;
//                                    case MessageTypes.EntryRequest:
//                                        {
//                                            incomingMessage = new EntryRequest();
//                                        }
//                                        break;
//                                    case MessageTypes.AddServerRequest:
//                                        {
//                                            incomingMessage = new AddServerRequest();
//                                        }
//                                        break;
//                                    case MessageTypes.AddServerReply:
//                                        {
//                                            incomingMessage = new AddServerReply();
//                                        }
//                                        break;
//                                    case MessageTypes.RemoveServerRequest:
//                                        {
//                                            incomingMessage = new RemoveServerRequest();
//                                        }
//                                        break;
//                                    case MessageTypes.RemoveServerReply:
//                                        {
//                                            incomingMessage = new RemoveServerReply();
//                                        }
//                                        break;
//                                    default:
//                                        Console.WriteLine("Unsupported message");
//                                        return;
//                                }
//                                incomingMessage.Read(_stream);
//                                _incomingMessages.Enqueue(incomingMessage);
//                            }
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    try { _remote[i].Close(); } catch { }
//                    Console.WriteLine(ex);
//                    _remote.RemoveAt(i);
//                    _streams.RemoveAt(i);
//                }
//            }
//            foreach (var c in _clients.Values)
//            {
//                c.Process(server);
//            }

//            base.Process(server);
//        }
//    }
//}
