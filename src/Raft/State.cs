using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    /*
     * Persistent server state
     *  State is not thread safe
     *  Setting Term or VotedFor causes a write to HDD before returning
     *  FileStream will flush on dispose/close, need to wrap it for safety (don't want stale data out)
     *  
     */

    public class State : IDisposable
    {
        //can not change once in production
        public const int SUPER_BLOCK_SIZE = 8192;
        public const int LOG_DEFAULT_ARRAY_SIZE = 65536;
        public const int LOG_RECORD_SIZE = 16;
        //public const int MAX_LOG_DATA_READS = 16;

        // current term of the cluster
        private int _currentTerm;

        // who we last voted for
        private int? _votedFor;

        private string _dataDir;
        private string _indexFilePath;
        private string _dataFilePath;

        // log index
        private LogIndex[] _logIndices;
        private uint _logLength;
        private uint _logDataPosition;

        private BinaryWriter _logIndexWriter;
        private FileStream _logDataFile;

        public int Term
        {
            get { return _currentTerm; }
            set
            {
                _currentTerm = value;
                saveSuperBlock();
            }
        }

        public int? VotedFor
        {
            get { return _votedFor; }
            set
            {
                _votedFor = value;
                saveSuperBlock();
            }
        }

        public string DataDirectory { get { return _dataDir; } }
        public string IndexFile { get { return _indexFilePath; } }
        public string DataFile { get { return _dataFilePath; } }

        public State(string dataDir)
        {
            _dataDir = dataDir;
            _indexFilePath = System.IO.Path.Combine(dataDir, "index");
            _dataFilePath = System.IO.Path.Combine(dataDir, "data");
        }

        private void readState(BinaryReader br)
        {
            System.Diagnostics.Debug.Assert(br.BaseStream.Length >= SUPER_BLOCK_SIZE);
            System.Diagnostics.Debug.Assert(((br.BaseStream.Length - SUPER_BLOCK_SIZE)) % LOG_RECORD_SIZE == 0);

            //read term and last vote
            _currentTerm = br.ReadInt32();
            _votedFor = br.ReadBoolean() ? (int?)br.ReadInt32() : null;

            //seek to end of superblock for data
            br.BaseStream.Seek(SUPER_BLOCK_SIZE, SeekOrigin.Begin);

            //get record count
            var indices = (uint)((br.BaseStream.Length - SUPER_BLOCK_SIZE) / LOG_RECORD_SIZE);
            ensureLogIndices(indices);

            //read records in
            for (var i = 0; i < indices; i++)
            {
                _logIndices[i].Term = br.ReadInt32();
                _logIndices[i].Flags = br.ReadUInt32();
                _logIndices[i].Offset = br.ReadUInt32();
                _logIndices[i].Size = br.ReadUInt32();
            }

            //update log index
            _logLength = indices;
        }

        private void createSuperBlock()
        {
            //write empty state so that base stream position is at the end of our data
            saveSuperBlock();

            //pad remaining data with 0s
            _logIndexWriter.Write(new byte[SUPER_BLOCK_SIZE - _logIndexWriter.BaseStream.Position]);
            _logIndexWriter.Flush();

            //init default log entry size
            _logIndices = new LogIndex[LOG_DEFAULT_ARRAY_SIZE];

            // set log position
            _logDataPosition = 0;
        }

        private bool saveSuperBlock()
        {
            // move to start of super block
            _logIndexWriter.Seek(0, SeekOrigin.Begin);

            // write current term
            _logIndexWriter.Write(_currentTerm);

            // did we vote?
            _logIndexWriter.Write(_votedFor.HasValue);

            // who did we vote for
            _logIndexWriter.Write(_votedFor.HasValue ? _votedFor.Value : -1);

            // ensure its on the HDD
            _logIndexWriter.Flush();

            return true;
        }

        private void ensureLogIndices(uint size)
        {
            // do we need to increase?
            if (_logIndices.Length < size)
            {
                // calculate next size
                var newSize = _logIndices.Length * 3 / 2;

                // are we still too small?
                while (newSize < size)
                    newSize = newSize * 3 / 2;

                // resize array
                Array.Resize(ref _logIndices, newSize);
            }
        }

        public void Initialize()
        {
            if (!System.IO.Directory.Exists(_dataDir))
                System.IO.Directory.CreateDirectory(_dataDir);

            var stateExists = System.IO.File.Exists(_indexFilePath);
            if (stateExists)
            {
                using (var br = new BinaryReader(File.Open(_indexFilePath, FileMode.Open, FileAccess.Read, FileShare.None)))
                    readState(br);
            }

            _logIndexWriter = new BinaryWriter(System.IO.File.Open(_indexFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
            if (!stateExists)
                createSuperBlock();

            _logDataFile = File.Open(_dataFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        //array of byte arrays, for batching purposes (limiting the writer lock transitions)
        public void Push(byte[][] data)
        {
            // we must first write the data to the dat file
            // in case of crash in between log data and log entry
            // this will orphan the data and on startup will reclaim the space


            // make sure we have enough capacity
            ensureLogIndices(_logLength + (uint)data.Length);
            var newLogIndices = new LogIndex[data.Length];

            for (var i = 0; i < data.Length; i++)
            {
                newLogIndices[i] = new LogIndex()
                {
                    Term = _currentTerm,
                    Flags = 0,
                    Offset = _logDataPosition,
                    Size = (uint)data.Length
                };

                //write to log data file
                _logDataFile.Write(data[i], 0, data[i].Length);

                //update log entries
                _logIndices[_logLength] = newLogIndices[i];

                //inc log index
                _logLength++;
                _logDataPosition += newLogIndices[i].Size;
            }

            //flush data
            _logDataFile.Flush();

            //stream length is in UNSIGN but seek is SIGN?
            _logIndexWriter.Seek((int)(SUPER_BLOCK_SIZE + _logLength * LOG_RECORD_SIZE), SeekOrigin.Begin);

            for (var i = 0; i < data.Length; i++)
            {
                //write data
                _logIndexWriter.Write(newLogIndices[i].Term);
                _logIndexWriter.Write(newLogIndices[i].Flags);
                _logIndexWriter.Write(newLogIndices[i].Offset);
                _logIndexWriter.Write(newLogIndices[i].Size);
            }

            _logIndexWriter.Flush();
        }

        public void Pop()
        {
            System.Diagnostics.Debug.Assert(_logLength > 0);

            _logLength--;

            if (_logLength > 0)
                _logDataPosition = _logIndices[_logLength].Offset;
            else
                _logDataPosition = 0;
        }

        public bool GetIndex(int key, out LogIndex index)
        {
            if (key < 1 || key > _logLength)
            {
                index = new LogIndex() { Flags = 0, Offset = 0, Size = 0, Term = 0 };
                return false;
            }

            index = _logIndices[key - 1];
            return true;
        }

        public bool GetLastIndex(out LogIndex index)
        {
            if (_logLength == 0)
            {
                index = new LogIndex() { Flags = 0, Offset = 0, Size = 0, Term = 0 };
                return false;
            }

            index = _logIndices[_logLength - 1];
            return true;
        }

        public byte[] GetData(int key)
        {
            if (key < 1 || key > _logLength)
                return null;

            var index = _logIndices[key - 1];
            var data = new byte[index.Size];

            _logDataFile.Seek(index.Offset, SeekOrigin.Begin);
            _logDataFile.Read(data, 0, data.Length);

            return data;
        }

        public void Dispose()
        {
            // Dispose could be called from a crash and could be called
            // at an unexpected time, not safe to save data here            
            //if (_logIndexWriter != null)
            //    _logIndexWriter.Dispose();


            // GC finalizer shouldn't call dipose(true)/close of these, so stale data
            // shouldn't be copied which is what we really want
            // then again since we write log data first and index data second it might not matter
            _logDataFile = null;
            _logIndexWriter = null;
        }
    }

}
