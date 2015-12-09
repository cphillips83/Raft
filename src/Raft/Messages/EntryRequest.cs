using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Logs;

namespace Raft.Messages
{
    public struct EntryRequest
    {
        public IPEndPoint From;
        public uint Index;
    }

    public struct EntryRequestReply
    {
        public IPEndPoint From;
        public LogEntry? Entry;
    }
}
