﻿using System;
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
        Stopped
    }
}