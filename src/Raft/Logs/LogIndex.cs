using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Logs
{
    //public enum LogIndexType2 : byte
    //{
    //    NOOP = 0,
    //    DataChunk = 1,
    //    DataBlob = 2,
    //    AddServer = 3,
    //    RemoveServer = 4

    //}

    //[StructLayout(LayoutKind.Explicit, Size = 32, Pack = 0)]
    //public struct LogIndex2
    //{
    //    [FieldOffset(0)]
    //    public uint Term;

    //    [FieldOffset(4)]
    //    public LogIndexType2 Type;

    //    public ushort ChunkIndex, ChunkSize;
    //}

    [StructLayout(LayoutKind.Explicit, Size = 32, Pack = 0)]
    public struct LogIndex
    {
        public const int LOG_RECORD_SIZE = 32;

        [FieldOffset(0)]
        public uint Term;

        [FieldOffset(4)]
        public LogIndexType Type;

        [FieldOffset(8)]
        public uint ChunkOffset;

        [FieldOffset(12)]
        public uint ChunkSize;

        [FieldOffset(16)]
        public uint Flag1;

        [FieldOffset(20)]
        public uint Flag2;

        [FieldOffset(24)]
        public uint Flag3;

        [FieldOffset(28)]
        public uint Flag4;

        public override string ToString()
        {
            return string.Format("{{ Term: {0}, Type: {1}, Offset: {2}, Size : {3}, Flag1 : {4}, Flag2 : {5}, Flag3 : {6}, Flag4 : {7} }}", Term, Type, ChunkOffset, ChunkSize, Flag1, Flag2, Flag3, Flag4);
        }
    }

}
