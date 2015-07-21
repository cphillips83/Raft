﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Messages
{
    public struct VoteRequest
    {
        public IPEndPoint From;
        public uint Term;
        public uint LastTerm;
        public uint LogLength;
    }

    public struct VoteReply
    {
        public IPEndPoint From;
        public uint Term;
        public bool Granted;

        public override string ToString()
        {
            return string.Format("{{ From: {0}, Term: {1}, Granted: {2}", From, Term, Granted);
        }
    }
}
