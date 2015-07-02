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

        public Peer()
        {
            Reset();
        }

        public uint ID { get; set; }
        public bool VotedGranted { get; set; }
        public uint MatchIndex { get; set; }
        public uint NextIndex { get; set; }
        public uint RpcDue { get; set; }
        public uint HeartBeartDue { get; set; }

        public void Reset()
        {
            VotedGranted = false;
            MatchIndex = 0;
            NextIndex = 1;
            RpcDue = 0;
            HeartBeartDue = 0;
        }

        public bool CheckRpcTimeout(Model model)
        {
            return RpcDue < model.Tick;
        }

        public void LeadershipChanged(uint logLength)
        {
            NextIndex = logLength;
            RpcDue = uint.MaxValue;
            HeartBeartDue = 0;
        }

        public void SendReply(VoteRequestReply reply)
        {

        }

        public void SendReply(AppendEntriesReply reply)
        {

        }

        //public void SendRequestVote(VoteRequest request)
        //{
            
        //}

        //public void HandleRequestVoteReply(VoteRequestReply reply)
        //{

        //}
    }
}
