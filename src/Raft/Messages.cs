using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    public struct VoteRequest
    {
        public int From;
        public int Term;
        public int LastTerm;
        public uint LogLength;
    }

    public struct VoteRequestReply
    {
        public int From;
        public int Term;
        public bool Granted;
    }

    //public struct StatusRequest
    //{
    //    public int From;
    //    public int Term;
    //}

    //public struct StatusReply
    //{
    //    public int From;
    //    public int Term;
    //    public uint CommitIndex;
    //}

    public struct AppendEntriesRequest
    {
        public int From;
        public int Term;
        public int PrevTerm;
        public uint PrevIndex;
        public uint CommitIndex;
        public LogEntry[] Entries;
    }


    public struct AppendEntriesReply
    {
        public int From;
        public int Term;
        public uint MatchIndex;
        public bool Success;
    }
}
