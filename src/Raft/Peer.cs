using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    public class Peer
    {
        public Peer(int id)
            : this(id, false)
        {

        }

        public Peer(int id, bool votedGranted)
        {
            Reset();
            ID = id;
            VotedGranted = votedGranted;
        }

        public int ID { get; set; }
        public bool VotedGranted { get; set; }
        public uint MatchIndex { get; set; }
        public uint NextIndex { get; set; }
        public long RpcDue { get; set; }
        public long HeartBeartDue { get; set; }

        public void Reset()
        {
            VotedGranted = false;
            MatchIndex = 0;
            NextIndex = 1;
            RpcDue = 0;
            HeartBeartDue = 0;
        }

        public bool CheckRpcTimeout(IConsensus model)
        {
            return RpcDue < model.Tick;
        }

        public void LeadershipChanged(uint logLength)
        {
            NextIndex = logLength;
            RpcDue = int.MaxValue;
            HeartBeartDue = 0;
        }
    }
}
