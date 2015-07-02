using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    public interface IMessage{

    }

    public struct VoteRequest
    {
        public int From;
        public int Term;
        public int LastLogTerm;
        public int LastLogIndex;
    }

    public struct VoteRequestReply
    {
        public int From;
        public int Term;
        public bool Granted;
    }

    public struct AppendEntriesRequest
    {
        public int From;
        public int Term;
        public int CommitIndex;
        public int PrevIndex;
        public int PrevTerm;
        public int PrevLogIndex;
        public LogEntry[] Entries;

    }

    public struct AppendEntriesReply
    {
        public int From;
        public int Term;
        public int MatchIndex;
        public bool Success;
    }
}
