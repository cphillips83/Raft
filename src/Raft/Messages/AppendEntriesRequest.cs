using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Messages
{
    [DataContract]
    public struct AppendEntriesRequest
    {
        [DataMember]
        public int From;
        [DataMember]
        public int Term;
        [DataMember]
        public int PrevTerm;
        [DataMember]
        public uint PrevIndex;
        [DataMember]
        public uint CommitIndex;
        [DataMember]
        public LogEntry[] Entries;
    }
}
