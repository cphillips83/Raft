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
        public string From;
        public int Term;
        public int LastTerm;
        public uint LogLength;
    }
}
