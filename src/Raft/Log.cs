using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    public class LogEntry
    {
        public uint Index;
        public uint Term;

    }
    public class Log
    {
        private uint _length;

        public uint Length { get { return _length; } }

        public void Pop()
        {

        }

        public void Push(LogEntry entry)
        {

        }

        public uint GetTerm(uint index)
        {
            return 0; //TODO get term for log entry
        }

        public uint LastLogterm { get { return GetTerm(_length); } }
    }
}
