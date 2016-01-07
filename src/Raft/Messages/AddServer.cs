using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Transports;

namespace Raft.Messages
{
    /* 
     * AddServer RPC
     *  Arguments:
     *      From        ID of new server to add
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
     *          takes longer than the election timeout
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

    public struct AddServerRequest : IMessage
    {
        public IPEndPoint From;

        public void Read(BinaryReader msg)
        {
            From = msg.ReadIPEndPoint();
        }

        public void Write(BinaryWriter msg)
        {
            msg.Write((byte)MessageTypes.AddServerRequest);
            msg.Write(From);
        }
    }

    public struct AddServerReply : IMessage
    {
        public IPEndPoint From;
        public AddServerStatus Status;
        public IPEndPoint LeaderHint;

        public void Read(BinaryReader msg)
        {
            From = msg.ReadIPEndPoint();
            Status = (AddServerStatus)msg.ReadUInt32();
            LeaderHint = msg.ReadIPEndPoint();
        }

        public void Write(BinaryWriter msg)
        {
            msg.Write((byte)MessageTypes.AddServerReply);
            msg.Write(From);
            msg.Write((uint)Status);
            msg.Write(LeaderHint);
        }
    }
}
