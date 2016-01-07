using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Logs;
using Raft.Transports;

namespace Raft.Messages
{
    public struct EntryRequest : IMessage
    {
        public IPEndPoint From;
        public uint Index;

        public void Write(BinaryWriter msg) {
            msg.Write((byte)MessageTypes.EntryRequest);
            msg.Write(From);
            msg.Write(Index);
        }

        public void Read(BinaryReader msg)
        {
            From = msg.ReadIPEndPoint();
            Index = msg.ReadUInt32();
        }
    }

    public struct EntryRequestReply : IMessage
    {
        public IPEndPoint From;
        public LogEntry? Entry;

        public void Write(BinaryWriter msg)
        {
            msg.Write((byte)MessageTypes.EntryRequestReply);
            msg.Write(From);

            msg.Write(Entry.HasValue);
            if (Entry.HasValue)
            {
                msg.Write(Entry.Value.Index.Term);
                msg.Write((uint)Entry.Value.Index.Type);
                msg.Write(Entry.Value.Index.ChunkOffset);
                msg.Write(Entry.Value.Index.ChunkSize);
                msg.Write(Entry.Value.Index.Flag1);
                msg.Write(Entry.Value.Index.Flag2);
                msg.Write(Entry.Value.Index.Flag3);
                msg.Write(Entry.Value.Index.Flag4);

                msg.Write(Entry.Value.Data.Length);
                msg.Write(Entry.Value.Data);
            }
        }

        public void Read(BinaryReader msg)
        {
            From = msg.ReadIPEndPoint();

            if (msg.ReadBoolean())
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

                Entry = entry;
            }
            else
            {
                Entry = null;
            }
        }
    }
}
