using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Messages
{
    /* 
     * AddServer RPC
     *  Arguments:
     *      From        ID of new server to add
     *      EndPoint    IP endpoint of this machine (ip + port)
     *
     *  Results:
     *      From        Server ID
     *      Status      OK if server was added successfully
     *      LeaderHint  IP endpoint of the leader if they weren't
     *      
     *  Receiver implementation:
     *      1.  Reply AddServerStatus.NotLeader if not leader
     *      2.  Catch up new server for fixed number of rounds.
     *          Reply AddServerStatus.Timeout if new server does not make
     *          progress for an election timeout of if the last round
     *          takes long than the election timeout
     *      3.  Wait until previous configuration in log is committed
     *      4.  Append new configuration entry to log (old configuration plus
     *          newServer), commit it using majority of new configuration.
     *      5.  Reply AddServerStatus.Ok
     */

    public enum AddServerStatus : uint
    {
        NotLeader,
        TimedOut,
        Ok,        
    }

    public struct AddServerRequest
    {
        public string From;
        public IPEndPoint EndPoint;
    }

    public struct AddServerReply
    {
        public string From;
        public AddServerStatus Status;
        public IPEndPoint LeaderHint;
    }
}
