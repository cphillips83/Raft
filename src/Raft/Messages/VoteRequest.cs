using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Messages
{
    public struct VoteRequest
    {
        public int From;
        public int Term;
        public int LastTerm;
        public uint LogLength;
    }
}
