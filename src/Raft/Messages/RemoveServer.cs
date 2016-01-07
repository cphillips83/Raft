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
     * RemoveServer RPC
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
     *      2.  Wait until previous configuration in log is committed
     *      3.  Append new configuration entry to log (old configuration without
     *          oldServer), commit it using majority of new configuration.
     *      4.  Reply AddServerStatus.Ok and if this server was removed, step down
     */

    public enum RemoveServerStatus : uint
    {
        NotLeader,
        Ok
    }

    public struct RemoveServerRequest : IMessage
    {
        public IPEndPoint From;

        public void Write(BinaryWriter msg)
        {
            msg.Write((byte)MessageTypes.RemoveServerRequest);
            msg.Write(From);
        }

        public void Read(BinaryReader msg)
        {
            From = msg.ReadIPEndPoint();
        }
    }

    public struct RemoveServerReply : IMessage
    {
        public IPEndPoint From;
        public RemoveServerStatus Status;
        public IPEndPoint LeaderHint;

        public void Write(BinaryWriter msg)
        {
            msg.Write((byte)MessageTypes.RemoveServerReply);
            msg.Write(From);
            msg.Write((uint)Status);
            msg.Write(LeaderHint);
        }

        public void Read(BinaryReader msg)
        {
            From = msg.ReadIPEndPoint();
            Status = (RemoveServerStatus)msg.ReadUInt32();
            LeaderHint = msg.ReadIPEndPoint();
        }
    }
}
