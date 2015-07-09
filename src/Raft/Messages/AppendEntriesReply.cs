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
        public string From;
        public int Term;
        public uint MatchIndex;
        public bool Success;
    }
}
