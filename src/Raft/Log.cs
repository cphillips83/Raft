using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    public class LogEntry
    {
        //public int Index;
        public int Term;
        public int Value;
    }

    public class Log
    {
        private List<LogEntry> _entries = new List<LogEntry>();

        public int Length { get { return _entries.Count; } }

        public Log()
        {
        }

        public void Pop()
        {
            _entries.RemoveAt(_entries.Count - 1);
        }

        public void Push(LogEntry entry)
        {
            _entries.Add(entry);
        }

        public int GetTerm(int index)
        {
            if (index < 1 || index > _entries.Count)
                return 0;

            return _entries[index - 1].Term;
        }

        public LogEntry[] GetEntries(int start, int end)
        {
            if (start < 0 || end < 1 || start == end)
                return new LogEntry[0];
            

            var entries = new LogEntry[end - start];
            for (var i = start; i < end; i++)
                entries[i - start] = _entries[i];

            return entries;
        }

        public int LastLogterm { get { return GetTerm(Length); } }
    }
}
