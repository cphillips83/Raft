using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Raft.Transports;

namespace Raft.Messages
{
    public struct VoteRequest : IMessage
    {
        public IPEndPoint From;
        public uint Term;
        public uint LastTerm;
        public uint LogLength;

        public void Write(BinaryWriter msg)
        {
            msg.Write((byte)MessageTypes.VoteRequest);
            msg.Write(From);
            msg.Write(Term);
            msg.Write(LastTerm);
            msg.Write(LogLength);
        }

        public void Read(BinaryReader msg)
        {
            From = msg.ReadIPEndPoint();
            Term = msg.ReadUInt32();
            LastTerm = msg.ReadUInt32();
            LogLength = msg.ReadUInt32();
        }
    }

    public struct VoteReply : IMessage
    {
        public IPEndPoint From;
        public uint Term;
        public bool Granted;

        public void Write(BinaryWriter msg)
        {
            msg.Write((byte)MessageTypes.VoteReply);
            msg.Write(From);
            msg.Write(Term);
            msg.Write(Granted);
        }

        public void Read(BinaryReader msg)
        {
            From = msg.ReadIPEndPoint();
            Term = msg.ReadUInt32();
            Granted = msg.ReadBoolean();
        }

        public override string ToString()
        {
            return string.Format("{{ From: {0}, Term: {1}, Granted: {2}", From, Term, Granted);
        }
    }
}
