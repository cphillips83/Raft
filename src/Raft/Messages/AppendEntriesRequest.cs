using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace Raft.Messages
{
    public struct AppendEntriesRequest
    {
        public int From;
        public int Term;
        public int PrevTerm;
        public uint PrevIndex;
        public uint CommitIndex;
        public LogEntry[] Entries;

        public static AppendEntriesRequest Read(NetIncomingMessage msg)
        {
            var request = new AppendEntriesRequest();
            request.From = msg.ReadInt32();
            request.Term = msg.ReadInt32();
            request.PrevTerm = msg.ReadInt32();
            request.PrevIndex = msg.ReadUInt32();
            request.CommitIndex = msg.ReadUInt32();
            
            var length = msg.ReadInt32();
            if (length > 0)
            {
                request.Entries = new LogEntry[length];
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

                    request.Entries[i] = entry;
                }
            }
            return request;
        }

        public static void Write(AppendEntriesRequest request, NetOutgoingMessage msg)
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
                    msg.Write(request.Entries[i].Index.Offset);
                    msg.Write(request.Entries[i].Index.Size);

                    msg.Write(request.Entries[i].Data.Length);
                    msg.Write(request.Entries[i].Data);
                }
            }
        }
    }
}
