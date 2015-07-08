using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace Raft.Messages
{
    public struct AppendEntriesReply
    {
        public int From;
        public int Term;
        public uint MatchIndex;
        public bool Success;

        public static AppendEntriesReply Read(NetIncomingMessage msg)
        {
            var request = new AppendEntriesReply();
            request.From = msg.ReadInt32();
            request.Term = msg.ReadInt32();
            request.MatchIndex = msg.ReadUInt32();
            request.Success = msg.ReadBoolean();

            return request;
        }

        public static void Write(AppendEntriesReply request, NetOutgoingMessage msg)
        {
            msg.Write((byte)MessageTypes.AppendEntriesReply);
            msg.Write(request.From);
            msg.Write(request.Term);
            msg.Write(request.MatchIndex);
            msg.Write(request.Success);
        }
    }
}
