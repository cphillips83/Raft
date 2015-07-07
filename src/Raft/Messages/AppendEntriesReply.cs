using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Messages
{
    [DataContract]
    public struct AppendEntriesReply
    {
        [DataMember]
        public int From;
        [DataMember]
        public int Term;
        [DataMember]
        public uint MatchIndex;
        [DataMember]
        public bool Success;
    }
}
