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

        private byte[] _spad = new byte[65535];

        private void SendMessage(Client client, byte[] data, int length)
        {
            _rpc.Send(data, length, client.ID);
        }

        public override void SendMessage(Client client, VoteRequest request)
        {
            using (var msg = new BinaryWriter(new MemoryStream(_spad, true)))
            {
                msg.Write((byte)MessageTypes.VoteRequest);
                msg.Write(request.From);
                msg.Write(request.Term);
                msg.Write(request.LastTerm);
                msg.Write(request.LogLength);
                msg.Flush();

                SendMessage(client, _spad, (int)msg.BaseStream.Position);
            }
        }

        public override void SendMessage(Client client, VoteReply reply)
        {
            using (var msg = new BinaryWriter(new MemoryStream(_spad, true)))
            {
                msg.Write((byte)MessageTypes.VoteReply);
                msg.Write(reply.From);
                msg.Write(reply.Term);
                msg.Write(reply.Granted);
                msg.Flush();

                SendMessage(client, _spad, (int)msg.BaseStream.Position);
            }
        }

        public override void SendMessage(Client client, AppendEntriesRequest request)
        {
            using (var msg = new BinaryWriter(new MemoryStream(_spad, true)))
            {
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

                        msg.Write(request.Entries[i].Data.Length);
                        msg.Write(request.Entries[i].Data);
                    }
                }
                msg.Flush();

                SendMessage(client, _spad, (int)msg.BaseStream.Position);
            }
        }

        public override void SendMessage(Client client, AppendEntriesReply reply)
        {
            using (var msg = new BinaryWriter(new MemoryStream(_spad, true)))
            {
                msg.Write((byte)MessageTypes.AppendEntriesReply);
                msg.Write(reply.From);
                msg.Write(reply.Term);
                msg.Write(reply.MatchIndex);
                msg.Write(reply.Success);
                msg.Flush();

                SendMessage(client, _spad, (int)msg.BaseStream.Position);
            }
        }

        public override void SendMessage(Client client, AddServerRequest request)
        {
            using (var msg = new BinaryWriter(new MemoryStream(_spad, true)))
            {
                msg.Write((byte)MessageTypes.AddServerRequest);
                msg.Write(request.From);
                msg.Flush();

                SendMessage(client, _spad, (int)msg.BaseStream.Position);
            }
        }

        public override void SendMessage(Client client, AddServerReply reply)
        {
            using (var msg = new BinaryWriter(new MemoryStream(_spad, true)))
            {
                msg.Write((byte)MessageTypes.AddServerReply);
                msg.Write(reply.From);
                msg.Write((uint)reply.Status);
                msg.Write(reply.LeaderHint);
                msg.Flush();

                SendMessage(client, _spad, (int)msg.BaseStream.Position);
            }
        }

        public override void SendMessage(Client client, RemoveServerRequest request)
        {
            using (var msg = new BinaryWriter(new MemoryStream(_spad, true)))
            {
                msg.Write((byte)MessageTypes.RemoveServerRequest);
                msg.Write(request.From);
                msg.Flush();

                SendMessage(client, _spad, (int)msg.BaseStream.Position);
            }
        }

        public override void SendMessage(Client client, RemoveServerReply reply)
        {
            using (var msg = new BinaryWriter(new MemoryStream(_spad, true)))
            {
                msg.Write((byte)MessageTypes.RemoveServerReply);
                msg.Write(reply.From);
                msg.Write((uint)reply.Status);
                msg.Write(reply.LeaderHint);

                SendMessage(client, _spad, (int)msg.BaseStream.Position);
            }
        }

        public override void Start(IPEndPoint ip)
        {
            _rpc = new UdpClient(ip.Port);
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
            if (_rpc.Available > 0)
            {
                var remoteIP = new IPEndPoint(IPAddress.Any, server.ID.Port);
                var data = _rpc.Receive(ref remoteIP);
                using (var msg = new BinaryReader(new MemoryStream(data)))
                {

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
                }
            }
            base.Process(server);
        }
    }
}
