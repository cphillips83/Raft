using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    public class Peer
    {
        //protected bool _voteGranted = false;
        //protected int _matchIndex = 0;
        //protected int _nextIndex = 1;
        //protected int _rpdDue = 0;
        //protected int _heartBeatDue = 0;

        public Peer(int id, bool votedGranted)
        {
            Reset();
            ID = id;
            VotedGranted = votedGranted;
        }

        public int ID { get; set; }
        public bool VotedGranted { get; set; }
        public int MatchIndex { get; set; }
        public int NextIndex { get; set; }
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

        public bool CheckRpcTimeout(IModel model)
        {
            return RpcDue < model.Tick;
        }

        public void LeadershipChanged(int logLength)
        {
            NextIndex = logLength;
            RpcDue = int.MaxValue;
            HeartBeartDue = 0;
        }

    }
}
