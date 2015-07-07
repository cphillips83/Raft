using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Messages
{
    [DataContract]
    public struct VoteRequest
    {
        [DataMember]
        public int From;
        [DataMember]
        public int Term;
        [DataMember]
        public int LastTerm;
        [DataMember]
        public uint LogLength;
    }
}
