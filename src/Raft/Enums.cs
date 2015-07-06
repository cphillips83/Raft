using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    public enum ServerState
    {
        Follower,
        Candidate,
        Leader,
        Stopped,
        Adding,
        Removing
    }

    public enum PeerState
    {
        Follower,
        Adding,
        Removing
    }

    public enum LogIndexType : uint
    {
        RawData = 0,
        StateMachine = 1,
    }
}
