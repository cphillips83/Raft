using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    [StructLayout(LayoutKind.Explicit, Size = 16, Pack = 0)]
    public struct LogIndex
    {
        [FieldOffset(0)]
        public int Term;

        [FieldOffset(4)]
        public uint Flags;

        [FieldOffset(8)]
        public uint Offset;

        [FieldOffset(12)]
        public uint Size;
    }

    public struct LogEntry
    {
        public int Term;        //4
        //public ulong Key;       //12
        //public uint Flags;      //16
        //public uint Offset;     //20
        //public uint Size;       //24
        //public ulong Padding;   //32
        public int Offset;
    }

    public struct LogEntry2
    {
        public uint Header;         //8
        //public int Index;
        public int Term;            //4
        public ulong ID;            //8
        public uint Cookie;         //16
        public uint AlternateKey;   //20
        public uint Flags;          //24
        public uint Offset;         //28
        public uint Size;           //32
        public byte[] Data;
        public int DataCheckSum;        //36

        public uint Footer;         //40

        //public ulong ID;
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
                return null; 
            
            var entries = new LogEntry[end - start];
            for (var i = start; i < end; i++)
                entries[i - start] = _entries[i];

            return entries;
        }

        public int LastLogterm { get { return GetTerm(Length); } }
    }
}
