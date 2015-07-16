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
        DataBlob = 0,
        DataChunk = 1,
        NOOP = 500,
        AddServer = 1000,
        RemoveServer = 1001,
        StateMachine = 3000,
    }
}
