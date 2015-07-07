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
        Operational,
        Adding,
        Removing,
        //Failing
    }

    public enum LogIndexType : uint
    {
        RawData = 0,
        StateMachine = 1,
    }
}
