using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace Raft.Messages
{
    [DataContract]
    public struct VoteReply
    {
        [DataMember]
        public int From;
        [DataMember]
        public int Term;
        [DataMember]
        public bool Granted;

        public override string ToString()
        {
            return string.Format("{{ From: {0}, Term: {1}, Granted: {2}", From, Term, Granted);
        }

        public static VoteReply Read(NetIncomingMessage msg)
        {
            var request = new VoteReply();
            request.From = msg.ReadInt32();
            request.Term = msg.ReadInt32();
            request.Granted = msg.ReadBoolean();
            return request;
        }

        public static void Write(VoteReply request, NetOutgoingMessage msg)
        {
            msg.Write((byte)MessageTypes.VoteReply);
            msg.Write(request.From);
            msg.Write(request.Term);
            msg.Write(request.Granted);
        }
    }
}
