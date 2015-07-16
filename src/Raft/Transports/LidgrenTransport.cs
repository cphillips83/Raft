using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Raft.Logs;
using Raft.Messages;
using Raft.States;

namespace Raft.Transports
{
    public class LidgrenTransport : Transport, IDisposable
    {
        private enum MessageTypes
        {
            VoteRequest,
            VoteReply,
            AppendEntriesReply,
            AppendEntriesRequest,
            AddServerRequest,
            AddServerReply,
            RemoveServerRequest,
            RemoveServerReply
        }

        private NetServer _rpc;
        private Dictionary<IPEndPoint, NetClient> _clients = new Dictionary<IPEndPoint, NetClient>();
        //private NetOutgoingMessage _delayedMessage;

        public override void Start(IPEndPoint _config)
        {
            NetPeerConfiguration config = new NetPeerConfiguration(_config.ToString());
            //config.SetMessageTypeEnabled(NetIncomingMessageType.UnconnectedData, true);
            //config
            //config.ConnectionTimeout = 100;

            config.SendBufferSize = 1024 * 128 * 2;
            config.ReceiveBufferSize = 1024 * 128 * 2;
            config.AutoExpandMTU = true;
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.Port = _config.Port;
            config.LocalAddress = _config.Address;
            //config.AutoFlushSendQueue = false;

            _rpc = new NetServer(config);
            _rpc.Start();
            while (_rpc.Status != NetPeerStatus.Running)
                System.Threading.Thread.Sleep(1);
        }

        public override void Shutdown()
        {
            if (_rpc != null)
            {
                foreach (var client in _clients.Values)
                {
                    client.Shutdown("stopping");
                    while (client.Status != NetPeerStatus.NotRunning)
                        System.Threading.Thread.Sleep(0);
                }

                _rpc.Shutdown(string.Empty);
                while (_rpc.Status != NetPeerStatus.NotRunning)
                    System.Threading.Thread.Sleep(1);
            }

            _rpc = null;
        }

        public override void ResetConnection(Client client)
        {
            NetClient netClient;
            if (_clients.TryGetValue(client.ID, out netClient))
            {
                if(netClient.ConnectionStatus == NetConnectionStatus.Connected)
                    netClient.Disconnect("resetting connection");
            }
        }

        private void SendMessage(Client client, NetOutgoingMessage msg)
        {
            NetClient netClient;
            if (!_clients.TryGetValue(client.ID, out netClient))
            {
                var config = new NetPeerConfiguration(client.ID.ToString());
                //config.AutoFlushSendQueue = false;
                config.SendBufferSize = 1024 * 128 * 2;
                config.ReceiveBufferSize = 1024 * 128 * 2;
                config.AutoExpandMTU = true;
                config.ConnectionTimeout = 100;
                netClient = new NetClient(config);
                netClient.Start();
                _clients.Add(client.ID, netClient);
            }

            if (netClient.ConnectionStatus != NetConnectionStatus.InitiatedConnect &&
                netClient.ConnectionStatus != NetConnectionStatus.Connected)
            {
                netClient.Connect(client.ID);
                Console.WriteLine("Connecting to {0}", client.ID);
            }

            if (netClient.ConnectionStatus == NetConnectionStatus.Connected)
            {
                //_rpc.SendMessage(msg, netClient, NetDeliveryMethod.Unreliable);
                netClient.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
                //_rpc.SendUnconnectedMessage(msg, client.ID);
            }
            //else
            //    _delayedMessage = msg;

            netClient.FlushSendQueue();
        }

        public override void SendMessage(Client client, VoteRequest request)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.VoteRequest);
            msg.Write(request.From);
            msg.Write(request.Term);
            msg.Write(request.LastTerm);
            msg.Write(request.LogLength);
            SendMessage(client, msg);
        }

        public override void SendMessage(Client client, VoteReply reply)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.VoteReply);
            msg.Write(reply.From);
            msg.Write(reply.Term);
            msg.Write(reply.Granted);
            SendMessage(client, msg);
        }

        public override void SendMessage(Client client, AppendEntriesRequest request)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.AppendEntriesRequest);
            msg.Write(request.From);
            msg.Write(request.Term);
            msg.Write(request.PrevTerm);
            msg.Write(request.PrevIndex);
            msg.Write(request.CommitIndex);

            if (request.Entries == null)
                msg.Write(0);
            else
            {
                msg.Write(request.Entries.Length);
                for (var i = 0; i < request.Entries.Length; i++)
                {
                    msg.Write(request.Entries[i].Index.Term);
                    msg.Write((uint)request.Entries[i].Index.Type);
                    msg.Write(request.Entries[i].Index.ChunkOffset);
                    msg.Write(request.Entries[i].Index.ChunkSize);
                    msg.Write(request.Entries[i].Index.Flag1);
                    msg.Write(request.Entries[i].Index.Flag2);
                    msg.Write(request.Entries[i].Index.Flag3);
                    msg.Write(request.Entries[i].Index.Flag4);

                    msg.Write(request.Entries[i].Data.Length);
                    msg.Write(request.Entries[i].Data);
                }
            }
            SendMessage(client, msg);
        }

        public override void SendMessage(Client client, AppendEntriesReply reply)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.AppendEntriesReply);
            msg.Write(reply.From);
            msg.Write(reply.Term);
            msg.Write(reply.MatchIndex);
            msg.Write(reply.Success);
            SendMessage(client, msg);
        }

        public override void SendMessage(Client client, AddServerRequest request)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.AddServerRequest);
            msg.Write(request.From);
            SendMessage(client, msg);
        }

        public override void SendMessage(Client client, AddServerReply reply)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.AddServerReply);
            msg.Write(reply.From);
            msg.Write((uint)reply.Status);
            //TODO leader can't be null
            msg.Write(reply.LeaderHint);
            SendMessage(client, msg);
        }

        public override void SendMessage(Client client, RemoveServerRequest request)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.RemoveServerRequest);
            msg.Write(request.From);
            SendMessage(client, msg);
        }

        public override void SendMessage(Client client, RemoveServerReply reply)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.RemoveServerReply);
            msg.Write(reply.From);
            msg.Write((uint)reply.Status);
            msg.Write(reply.LeaderHint);
            SendMessage(client, msg);
        }

        private void Process(Server server, NetPeer peer)
        {
            NetIncomingMessage msg;
            while ((msg = peer.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.ConnectionApproval:
                        Console.WriteLine("{0}: Approving connection {1}", server.ID, msg.SenderConnection.RemoteEndPoint);
                        msg.SenderConnection.Approve();
                        break;
                    //case NetIncomingMessageType.StatusChanged:
                    //    {
                    //        var status = (NetConnectionStatus)msg.ReadByte();
                    //        if (status == NetConnectionStatus.Connected)
                    //        {
                    //            _rpc.
                    //        }
                    //    }
                    case NetIncomingMessageType.Data:
                        switch ((MessageTypes)msg.ReadByte())
                        {
                            case MessageTypes.VoteRequest:
                                {
                                    var incomingMessage = new VoteRequest();
                                    incomingMessage.From = msg.ReadIPEndPoint();
                                    incomingMessage.Term = msg.ReadUInt32();
                                    incomingMessage.LastTerm = msg.ReadUInt32();
                                    incomingMessage.LogLength = msg.ReadUInt32();

                                    _incomingMessages.Enqueue(incomingMessage);
                                }
                                break;
                            case MessageTypes.VoteReply:
                                {
                                    var incomingMessage = new VoteReply();
                                    incomingMessage.From = msg.ReadIPEndPoint();
                                    incomingMessage.Term = msg.ReadUInt32();
                                    incomingMessage.Granted = msg.ReadBoolean();

                                    _incomingMessages.Enqueue(incomingMessage);
                                }
                                break;
                            case MessageTypes.AppendEntriesRequest:
                                {
                                    var incomingMessage = new AppendEntriesRequest();
                                    incomingMessage.From = msg.ReadIPEndPoint();
                                    incomingMessage.Term = msg.ReadUInt32();
                                    incomingMessage.PrevTerm = msg.ReadUInt32();
                                    incomingMessage.PrevIndex = msg.ReadUInt32();
                                    incomingMessage.CommitIndex = msg.ReadUInt32();

                                    var length = msg.ReadInt32();
                                    if (length > 0)
                                    {
                                        incomingMessage.Entries = new LogEntry[length];
                                        for (var i = 0; i < length; i++)
                                        {
                                            var entry = new LogEntry();
                                            var index = new LogIndex();

                                            index.Term = msg.ReadUInt32();
                                            index.Type = (LogIndexType)msg.ReadUInt32();
                                            index.ChunkOffset = msg.ReadUInt32();
                                            index.ChunkSize = msg.ReadUInt32();
                                            index.Flag1 = msg.ReadUInt32();
                                            index.Flag2 = msg.ReadUInt32();
                                            index.Flag3 = msg.ReadUInt32();
                                            index.Flag4 = msg.ReadUInt32();

                                            entry.Index = index;

                                            var dataLength = msg.ReadInt32();
                                            entry.Data = msg.ReadBytes(dataLength);

                                            incomingMessage.Entries[i] = entry;
                                        }
                                    }

                                    _incomingMessages.Enqueue(incomingMessage);
                                }
                                break;
                            case MessageTypes.AppendEntriesReply:
                                {
                                    var incomingMessage = new AppendEntriesReply();
                                    incomingMessage.From = msg.ReadIPEndPoint();
                                    incomingMessage.Term = msg.ReadUInt32();
                                    incomingMessage.MatchIndex = msg.ReadUInt32();
                                    incomingMessage.Success = msg.ReadBoolean();

                                    _incomingMessages.Enqueue(incomingMessage);
                                }
                                break;
                            case MessageTypes.AddServerRequest:
                                {
                                    var incomingMessage = new AddServerRequest();
                                    incomingMessage.From = msg.ReadIPEndPoint();
                                    _incomingMessages.Enqueue(incomingMessage);
                                }
                                break;
                            case MessageTypes.AddServerReply:
                                {
                                    var incomingMessage = new AddServerReply();
                                    incomingMessage.From = msg.ReadIPEndPoint();
                                    incomingMessage.Status = (AddServerStatus)msg.ReadUInt32();
                                    incomingMessage.LeaderHint = msg.ReadIPEndPoint();
                                    _incomingMessages.Enqueue(incomingMessage);
                                }
                                break;
                            case MessageTypes.RemoveServerRequest:
                                {
                                    var incomingMessage = new RemoveServerRequest();
                                    incomingMessage.From = msg.ReadIPEndPoint();
                                    _incomingMessages.Enqueue(incomingMessage);
                                }
                                break;
                            case MessageTypes.RemoveServerReply:
                                {
                                    var incomingMessage = new RemoveServerReply();
                                    incomingMessage.From = msg.ReadIPEndPoint();
                                    incomingMessage.Status = (RemoveServerStatus)msg.ReadUInt32();
                                    incomingMessage.LeaderHint = msg.ReadIPEndPoint();
                                    _incomingMessages.Enqueue(incomingMessage);
                                }
                                break;
                        }
                        break;
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.ErrorMessage:
                        // print diagnostics message
                        Console.WriteLine(msg.ReadString());
                        break;
                }
                peer.Recycle(msg);
            }
        }

        public override void Process(Server server)
        {
            Process(server, _rpc);
            foreach (var client in _clients.Values)
                Process(server, client);

            base.Process(server);
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
