using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Raft.Logs;
using Raft.Transports;

namespace Raft.Messages
{
    public struct AppendEntriesRequest : IMessage
    {
        public IPEndPoint From;
        public IPEndPoint AgentIP;
        public uint Term;
        public uint PrevTerm;
        public uint PrevIndex;
        public uint CommitIndex;

        public LogEntry[] Entries;

        public void Write(BinaryWriter msg)
        {
            msg.Write((byte)MessageTypes.AppendEntriesRequest);
            msg.Write(From);
            msg.Write(AgentIP);
            msg.Write(Term);
            msg.Write(PrevTerm);
            msg.Write(PrevIndex);
            msg.Write(CommitIndex);

            if (Entries == null)
                msg.Write(0);
            else
            {
                msg.Write(Entries.Length);
                for (var i = 0; i < Entries.Length; i++)
                {
                    msg.Write(Entries[i].Index.Term);
                    msg.Write((uint)Entries[i].Index.Type);
                    msg.Write(Entries[i].Index.ChunkOffset);
                    msg.Write(Entries[i].Index.ChunkSize);
                    msg.Write(Entries[i].Index.Flag1);
                    msg.Write(Entries[i].Index.Flag2);
                    msg.Write(Entries[i].Index.Flag3);
                    msg.Write(Entries[i].Index.Flag4);

                    msg.Write(Entries[i].Data.Length);
                    msg.Write(Entries[i].Data);
                }
            }
        }

        public void Read(BinaryReader msg)
        {
            From = msg.ReadIPEndPoint();
            AgentIP = msg.ReadIPEndPoint();
            Term = msg.ReadUInt32();
            PrevTerm = msg.ReadUInt32();
            PrevIndex = msg.ReadUInt32();
            CommitIndex = msg.ReadUInt32();

            var length = msg.ReadInt32();
            if (length > 0)
            {
                Entries = new LogEntry[length];
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

                    Entries[i] = entry;
                }
            }
        }
    }

    public struct AppendEntriesReply : IMessage
    {
        public IPEndPoint From;
        public uint Term;
        public uint MatchIndex;
        public bool Success;

        public void Write(BinaryWriter msg)
        {
            msg.Write((byte)MessageTypes.AppendEntriesReply);
            msg.Write(From);
            msg.Write(Term);
            msg.Write(MatchIndex);
            msg.Write(Success);
        }

        public void Read(BinaryReader msg)
        {
            From = msg.ReadIPEndPoint();
            Term = msg.ReadUInt32();
            MatchIndex = msg.ReadUInt32();
            Success = msg.ReadBoolean();
        }
    }
}
