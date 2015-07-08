using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Messages
{
    public enum MessageTypes
    {
        VoteRequest,
        VoteReply,
        AppendEntriesReply,
        AppendEntriesRequest
    }
}
