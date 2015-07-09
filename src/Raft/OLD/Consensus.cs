using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft
{
    public interface IConsensus
    {
        long Tick { get; }
        void SendRequest(Peer peer, VoteRequest request);
        void SendRequest(Peer peer, AppendEntriesRequest request);
        void SendReply(Peer peer, VoteReply reply);
        void SendReply(Peer peer, AppendEntriesReply reply);
    }
}
