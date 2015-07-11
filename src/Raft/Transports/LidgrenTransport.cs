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
    public class LidgrenTransport : Transport 
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

        private NetPeer _rpc;

        public override void Start(IPEndPoint _config)
        {
            NetPeerConfiguration config = new NetPeerConfiguration(_config.Address.ToString() + ":" + _config.Port);
            config.SetMessageTypeEnabled(NetIncomingMessageType.UnconnectedData, true);
            config.Port = _config.Port;

            _rpc = new NetPeer(config);
            _rpc.Start();
            while (_rpc.Status != NetPeerStatus.Running)
                System.Threading.Thread.Sleep(1);
        }

        public override void Shutdown()
        {
            if (_rpc != null)
            {
                _rpc.Shutdown(string.Empty);
                while (_rpc.Status != NetPeerStatus.NotRunning)
                    System.Threading.Thread.Sleep(1);
            }

            _rpc = null;
        }

        public override void SendMessage(Client client, VoteRequest request)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.VoteRequest);
            msg.Write(request.From);
            msg.Write(request.Term);
            msg.Write(request.LastTerm);
            msg.Write(request.LogLength);
            _rpc.SendUnconnectedMessage(msg, client.ID);
            _rpc.FlushSendQueue();
        }

        public override void SendMessage(Client client, VoteReply reply)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.VoteReply);
            msg.Write(reply.From);
            msg.Write(reply.Term);
            msg.Write(reply.Granted);
            _rpc.SendUnconnectedMessage(msg, client.ID);
            _rpc.FlushSendQueue();
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
                    msg.Write(request.Entries[i].Index.Offset);
                    msg.Write(request.Entries[i].Index.Size);

                    msg.Write(request.Entries[i].Data.Length);
                    msg.Write(request.Entries[i].Data);
                }
            }
            _rpc.SendUnconnectedMessage(msg, client.ID);
            _rpc.FlushSendQueue();
        }

        public override void SendMessage(Client client, AppendEntriesReply reply)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.AppendEntriesReply);
            msg.Write(reply.From);
            msg.Write(reply.Term);
            msg.Write(reply.MatchIndex);
            msg.Write(reply.Success);
            _rpc.SendUnconnectedMessage(msg, client.ID);
            _rpc.FlushSendQueue();
        }

        public override void SendMessage(Client client, AddServerRequest request)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.AddServerRequest);
            msg.Write(request.From);
            _rpc.SendUnconnectedMessage(msg, client.ID);
            _rpc.FlushSendQueue();
        }

        public override void SendMessage(Client client, AddServerReply reply)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.AddServerReply);
            msg.Write(reply.From);
            msg.Write((uint)reply.Status);
            msg.Write(reply.LeaderHint);
            _rpc.SendUnconnectedMessage(msg, client.ID);
            _rpc.FlushSendQueue();
        }

        public override void SendMessage(Client client, RemoveServerRequest request)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.RemoveServerRequest);
            msg.Write(request.From);
            _rpc.SendUnconnectedMessage(msg, client.ID);
            _rpc.FlushSendQueue();
        }

        public override void SendMessage(Client client, RemoveServerReply reply)
        {
            var msg = _rpc.CreateMessage();
            msg.Write((byte)MessageTypes.RemoveServerReply);
            msg.Write(reply.From);
            msg.Write((uint)reply.Status);
            msg.Write(reply.LeaderHint);
            _rpc.SendUnconnectedMessage(msg, client.ID);
            _rpc.FlushSendQueue();
        }

        public override void Process(Server server)
        {
            NetIncomingMessage msg;
            while ((msg = _rpc.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.UnconnectedData:
                        switch ((MessageTypes)msg.ReadByte())
                        {
                            case MessageTypes.VoteRequest:
                                {
                                    var incomingMessage = new VoteRequest();
                                    incomingMessage.From = msg.ReadIPEndPoint();
                                    incomingMessage.Term = msg.ReadInt32();
                                    incomingMessage.LastTerm = msg.ReadInt32();
                                    incomingMessage.LogLength = msg.ReadUInt32();

                                    _incomingMessages.Enqueue(incomingMessage);
                                }
                                break;
                            case MessageTypes.VoteReply:
                                {
                                    var incomingMessage = new VoteReply();
                                    incomingMessage.From = msg.ReadIPEndPoint();
                                    incomingMessage.Term = msg.ReadInt32();
                                    incomingMessage.Granted = msg.ReadBoolean();

                                    _incomingMessages.Enqueue(incomingMessage);
                                }
                                break;
                            case MessageTypes.AppendEntriesRequest:
                                {
                                    var incomingMessage = new AppendEntriesRequest();
                                    incomingMessage.From = msg.ReadIPEndPoint();
                                    incomingMessage.Term = msg.ReadInt32();
                                    incomingMessage.PrevTerm = msg.ReadInt32();
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

                                            index.Term = msg.ReadInt32();
                                            index.Type = (LogIndexType)msg.ReadUInt32();
                                            index.Offset = msg.ReadUInt32();
                                            index.Size = msg.ReadUInt32();

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
                                    incomingMessage.Term = msg.ReadInt32();
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
            }

            base.Process(server); 
        }
    }
}
