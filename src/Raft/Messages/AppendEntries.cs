﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Raft.Logs;

namespace Raft.Messages
{
    public struct AppendEntriesRequest
    {
        public IPEndPoint From;
        public int Term;
        public int PrevTerm;
        public uint PrevIndex;
        public uint CommitIndex;
        public LogEntry[] Entries;
    }

    public struct AppendEntriesReply
    {
        public IPEndPoint From;
        public int Term;
        public uint MatchIndex;
        public bool Success;
    }
}