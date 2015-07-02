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
        public uint Term;
        public uint LastLogTerm;
        public uint LastLogIndex;
    }

    public struct VoteRequestReply
    {
        public int From;
        public uint Term;
        public bool Granted;
    }

    public struct AppendEntriesRequest
    {
        public int From;
        public uint Term;
        public uint CommitIndex;
        public uint PrevIndex;
        public uint PrevTerm;
        public uint PrevLogIndex;
        public LogEntry[] Entries;

    }

    public struct AppendEntriesReply
    {
        public int From;
        public uint Term;
        public uint MatchIndex;
        public bool Success;
    }
}
