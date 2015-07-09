using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Logs
{
    public struct LogEntry
    {
        public LogIndex Index;
        public byte[] Data;
    }
}
