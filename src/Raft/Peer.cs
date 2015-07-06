using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    public class Peer
    {
        //persistent
        private IPAddress _ipaddress;
        private int _port;
        private PeerState _state;

        //votaile + copy from cluster changes
        public bool VoteGranted { get; set; }
        public uint MatchIndex { get; set; }
        public uint NextIndex { get; set; }
        public long RpcDue { get; set; }
        public long HeartBeartDue { get; set; }

        public Peer(IPAddress ipaddress, int port, PeerState state)
        {
            _ipaddress = ipaddress;
            _port = port;
            _state = state;
        }

        public Peer(int id)
            : this(id, false)
        {

        }

        public Peer(int id, bool voteGranted)
        {
            State = PeerState.Follower;
            Reset();
            ID = id;
            VoteGranted = voteGranted;
        }

        public int ID { get; set; }
        public PeerState State { get { return _state; } set { _state = value; } }

        public void Reset()
        {
            VoteGranted = false;
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

        public void CopyTo(Peer peer)
        {
            peer.RpcDue = this.RpcDue;
            peer.MatchIndex = this.MatchIndex;
            peer.NextIndex = this.NextIndex;
            peer.HeartBeartDue = this.HeartBeartDue;
        }
    }
}
