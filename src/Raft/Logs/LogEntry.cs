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

        public static bool AreEqual(LogEntry left, LogEntry right)
        {
            if (LogIndex.AreEqual(left.Index, right.Index))
            {
                if (left.Data == null && right.Data == null)
                    return true;

                if (left.Data != null && right.Data == null)
                    return false;

                if (left.Data == null && right.Data != null)
                    return false;

                if (left.Data.Length != right.Data.Length)
                    return false;

                for (var i = 0; i < left.Data.Length; i++)
                    if (left.Data[i] != right.Data[i])
                        return false;

                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return string.Format("{{ LogEntry: {0}, Data: {{ Length: {1}, CRC32: {2} }} }}", Index, Data.Length, Crc32.ComputeChecksum(Data));
        }
    }
}
