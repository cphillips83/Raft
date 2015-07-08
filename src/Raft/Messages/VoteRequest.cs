using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace Raft.Messages
{
    public struct VoteRequest
    {
        public int From;
        public int Term;
        public int LastTerm;
        public uint LogLength;

        public static VoteRequest Read(NetIncomingMessage msg)
        {
            var request = new VoteRequest();
            request.From = msg.ReadInt32();
            request.Term = msg.ReadInt32();
            request.LastTerm = msg.ReadInt32();
            request.LogLength = msg.ReadUInt32();
            return request;
        }

        public static void Write(VoteRequest request, NetOutgoingMessage msg)
        {
            msg.Write((byte)MessageTypes.VoteRequest);
            msg.Write(request.From);
            msg.Write(request.Term);
            msg.Write(request.LastTerm);
            msg.Write(request.LogLength);
        }
    }
}
