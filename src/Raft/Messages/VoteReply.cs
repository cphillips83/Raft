using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Messages
{
    [DataContract]
    public struct VoteReply
    {
        [DataMember]
        public int From;
        [DataMember]
        public int Term;
        [DataMember]
        public bool Granted;

        public override string ToString()
        {
            return string.Format("{{ From: {0}, Term: {1}, Granted: {2}", From, Term, Granted);
        }
    }
}
