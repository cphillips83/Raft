using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Messages
{
    public struct AppendEntriesReply
    {
        public int From;
        public int Term;
        public uint MatchIndex;
        public bool Success;
    }
}
