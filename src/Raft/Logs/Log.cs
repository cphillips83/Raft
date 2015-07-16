using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Logs
{
    public abstract class Log : IDisposable
    {

        public int RPC_TIMEOUT = 50;
        public int ELECTION_TIMEOUT = 100;

        //can not change once in production
        public const int SUPER_BLOCK_SIZE = 1024;
        public const int LOG_DEFAULT_ARRAY_SIZE = 65536;
        //public const int MAX_LOG_ENTRY_SIZE = 1024 * 128; //128kb
        //public const int MAX_LOG_ENTRY_SIZE = 65535 // MAX UDP PACKET SIZE?
        //                                        - 20 //overhead
        //                                        - 8  //udp header
        //                                        - 25 //From, term, PrevTerm, PrevIndex, CommitIndex, DataLength
        //                                        - 16; //log index
        
        public const int MAX_LOG_ENTRY_SIZE = 65400; //135 buffer for udp 

        //public const int MAX_LOG_DATA_READS = 16;

        private byte[] _writeSpad = new byte[MAX_LOG_ENTRY_SIZE];

        // current term of the cluster
        private uint _currentTerm = 1;
        //private uint _maxDataFileSize = 0;
        private uint _lastAppliedIndex;
        private bool _configLocked;

        // who we last voted for
        private IPEndPoint _votedFor;

        private Stream _indexStream, _logDataFile;
        private BinaryWriter _logIndexWriter;

        // log index
        private LogIndex[] _logIndices;
        private uint _logLength;

        //private List<Peer> _peers;
        private List<IPEndPoint> _clients = new List<IPEndPoint>();

        public IEnumerable<IPEndPoint> Clients { get { return _clients; } }

        public uint LastAppliedIndex { get { return _lastAppliedIndex; } }

        public uint Term
        {
            get { return _currentTerm; }
            set
            {
                _currentTerm = value;
                _votedFor = null;
                saveSuperBlock();
            }
        }

        public IPEndPoint VotedFor
        {
            get { return _votedFor; }
            set
            {
                _votedFor = value;
                saveSuperBlock();
            }
        }

        public bool ConfigLocked
        {
            get
            {
                return _configLocked;
            }
            set
            {
                System.Diagnostics.Debug.Assert(!_configLocked);
                _configLocked = value;
                saveSuperBlock();
            }
        }
        public uint Length { get { return _logLength; } }

        public uint DataPosition
        {
            get
            {
                if (_logLength == 0)
                    return 0;

                var index = _logIndices[_logLength - 1];
                return index.ChunkOffset + index.ChunkSize;
            }
        }

        //public uint MaxDataFileSize { get { return _maxDataFileSize; } }

        protected Log()
        {
        }

        protected abstract Stream OpenIndexFileWriter();

        protected abstract Stream OpenDataFileWriter();

        protected abstract Stream OpenDataFileReader();

        protected abstract Stream OpenIndexFileReader();

        private void readState(BinaryReader br)
        {
            System.Diagnostics.Debug.Assert(br.BaseStream.Length >= SUPER_BLOCK_SIZE);
            System.Diagnostics.Debug.Assert(((br.BaseStream.Length - SUPER_BLOCK_SIZE)) % LogIndex.LOG_RECORD_SIZE == 0);

            // read term and last vote
            _currentTerm = br.ReadUInt32();

            // read config lock
            _configLocked = br.ReadBoolean();

            var voteForLen = br.ReadInt32();
            if (voteForLen > 0)
            {
                var ipData = br.ReadBytes(voteForLen);
                var port = br.ReadInt32();
                _votedFor = new IPEndPoint(new IPAddress(ipData), port);
            }
            else
                _votedFor = null;

            _lastAppliedIndex = br.ReadUInt32();

            // peers
            var peerCount = br.ReadInt32();
            for (var i = 0; i < peerCount; i++)
            {
                //var id = br.ReadString();
                var addrBytesLen = br.ReadInt32();
                var addrBytes = br.ReadBytes(addrBytesLen);
                var port = br.ReadInt32();

                _clients.Add(new IPEndPoint(new System.Net.IPAddress(addrBytes), port));
            }

            //seek to end of superblock for data
            br.BaseStream.Seek(SUPER_BLOCK_SIZE, SeekOrigin.Begin);

            //get record count
            var indices = (uint)((br.BaseStream.Length - SUPER_BLOCK_SIZE) / LogIndex.LOG_RECORD_SIZE);
            ensureLogIndices(indices);

            //read records in
            for (var i = 0; i < indices; i++)
            {
                _logIndices[i].Term = br.ReadUInt32();
                _logIndices[i].Type = (LogIndexType)br.ReadUInt32();
                _logIndices[i].ChunkOffset = br.ReadUInt32();
                _logIndices[i].ChunkSize = br.ReadUInt32();
                _logIndices[i].Flag1 = br.ReadUInt32();
                _logIndices[i].Flag2 = br.ReadUInt32();
                _logIndices[i].Flag3 = br.ReadUInt32();
                _logIndices[i].Flag4 = br.ReadUInt32();
            }

            //update log index
            _logLength = indices;
        }

        private void createSuperBlock()
        {
            Console.WriteLine("creating superblock");
            //write empty state so that base stream position is at the end of our data
            saveSuperBlock();

            //pad remaining data with 0s
            _logIndexWriter.Write(new byte[SUPER_BLOCK_SIZE - _logIndexWriter.BaseStream.Position]);
            _logIndexWriter.Flush();

            //init default log entry size
            _logIndices = new LogIndex[LOG_DEFAULT_ARRAY_SIZE];

        }

        private bool saveSuperBlock()
        {
            // move to start of super block
            _logIndexWriter.Seek(0, SeekOrigin.Begin);

            // write current term
            _logIndexWriter.Write(_currentTerm);

            // write config lock
            _logIndexWriter.Write(_configLocked);

            // did we vote?
            //_logIndexWriter.Write(_votedFor != null);

            if (_votedFor == null)
                _logIndexWriter.Write(0);
            else
            {
                // who did we vote for
                var voteData = _votedFor.Address.GetAddressBytes();
                _logIndexWriter.Write(voteData.Length);
                _logIndexWriter.Write(voteData);
                _logIndexWriter.Write(_votedFor.Port);

            }

            // last applied index
            _logIndexWriter.Write(_lastAppliedIndex);

            // peers
            _logIndexWriter.Write(_clients.Count);
            for (var i = 0; i < _clients.Count; i++)
            {
                //_logIndexWriter.Write(_clients[i].ID);

                var addrBytes = _clients[i].Address.GetAddressBytes();
                _logIndexWriter.Write(addrBytes.Length);
                _logIndexWriter.Write(addrBytes);
                _logIndexWriter.Write(_clients[i].Port);
            }

            // ensure its on the HDD
            _logIndexWriter.Flush();

            return true;
        }

        private void ensureLogIndices(uint size)
        {
            // we don't want to increase the size yet
            // of a system is readonly it would create wasted memory
            if (_logIndices == null)
                _logIndices = new LogIndex[size];

            // do we need to increase?
            if (_logIndices.Length < size)
            {
                // calculate next size
                var newSize = Math.Max(_logIndices.Length * 3 / 2, LOG_DEFAULT_ARRAY_SIZE);

                // are we still too small?
                while (newSize < size)
                    newSize = newSize * 3 / 2;

                // resize array
                Array.Resize(ref _logIndices, newSize);
            }
        }

        public void Initialize()
        {
            _indexStream = OpenIndexFileWriter();
            _logDataFile = OpenDataFileWriter();

            if (_indexStream.Length > 0)
                using (var br = new BinaryReader(OpenIndexFileReader()))
                    readState(br);

            _logIndexWriter = new BinaryWriter(_indexStream);
            if (_indexStream.Length == 0)
                createSuperBlock();

        }

        public void UpdateState(uint term, IPEndPoint votedFor)
        {
            _currentTerm = term;
            _votedFor = votedFor;
            saveSuperBlock();
        }

        public LogEntry AddServer(Server server, IPEndPoint id)
        {
            var data = id.Address.GetAddressBytes();
            Array.Resize(ref data, data.Length + 4);

            data[data.Length - 4] = (byte)(id.Port >> 24);
            data[data.Length - 3] = (byte)(id.Port >> 16);
            data[data.Length - 2] = (byte)(id.Port >> 8);
            data[data.Length - 1] = (byte)(id.Port);

            var entry = new LogEntry()
            {
                Index = new LogIndex()
                {
                    Term = _currentTerm,
                    ChunkOffset = DataPosition,
                    ChunkSize = (uint)data.Length,
                    Type = LogIndexType.AddServer
                },
                Data = data
            };

            Push(server, entry);
            return entry;
        }

        public LogEntry RemoveServer(Server server, IPEndPoint id)
        {
            var data = id.Address.GetAddressBytes();
            Array.Resize(ref data, data.Length + 4);

            data[data.Length - 4] = (byte)(id.Port >> 24);
            data[data.Length - 3] = (byte)(id.Port >> 16);
            data[data.Length - 2] = (byte)(id.Port >> 8);
            data[data.Length - 1] = (byte)(id.Port);

            var entry = new LogEntry()
            {
                Index = new LogIndex()
                {
                    Term = _currentTerm,
                    ChunkOffset = DataPosition,
                    ChunkSize = (uint)data.Length,
                    Type = LogIndexType.RemoveServer
                },
                Data = data
            };

            Push(server, entry);
            return entry;
        }

        public IPEndPoint GetIPEndPoint(byte[] data)
        {
            var port = (data[data.Length - 4] << 24) +
                       (data[data.Length - 3] << 16) +
                       (data[data.Length - 2] << 8) +
                       (data[data.Length - 1]);

            Array.Resize(ref data, data.Length - 4);
            var ip = new IPAddress(data);

            return new IPEndPoint(ip, port);
        }
        public uint CreateNoop(Server server)
        {
            var entry = new LogEntry()
            {
                Index = new LogIndex()
                {
                    Term = _currentTerm,
                    ChunkOffset = DataPosition,
                    ChunkSize = 0,
                    Type = LogIndexType.NOOP
                },
                Data = new byte[0]
            };

            if (server != null)
                Console.WriteLine("{0}: Created  NOOP", server.ID);

            Push(server, entry);
            return _logLength;
        }

        public uint CreateData(Server server, byte[] data)
        {
            //this function breaks the entries in to MAX_LOG_ENTRY_SIZE as
            //DataChunk types but preserves the first byte so that when
            //it creates the DataBlob entry to spans the entire chunk range
            //this helps AppendEntries send data in chunks instead of blobs
            //once the LogEntry return from here is committed, the DataBlob is 
            //safe and linear in entries via DataChunks

            var start = DataPosition;
            var remaining = data.Length;
            
            while (remaining > MAX_LOG_ENTRY_SIZE)
            {
                Array.Copy(data, data.Length - remaining, _writeSpad, 0, MAX_LOG_ENTRY_SIZE);
                var chunkEntry = new LogEntry()
                {
                    Index = new LogIndex()
                    {
                        Term = _currentTerm,
                        ChunkOffset = DataPosition,
                        ChunkSize = MAX_LOG_ENTRY_SIZE,
                        Type = LogIndexType.DataChunk,
                        Flag3 = start,
                        Flag4 = (uint)data.Length
                    },
                    Data = _writeSpad
                };

                if (server != null)
                    Console.WriteLine("{0}: Creating chunk {1}", server.ID, chunkEntry.Index);

                Push(server, chunkEntry);
                remaining -= MAX_LOG_ENTRY_SIZE;
            }

            Array.Copy(data, data.Length - remaining, _writeSpad, 0, remaining);

            var entry = new LogEntry()
            {
                Index = new LogIndex()
                {
                    Term = _currentTerm,
                    ChunkOffset = DataPosition,
                    ChunkSize = (uint)remaining,
                    Type = LogIndexType.DataBlob,
                    Flag3 = start,
                    Flag4 = (uint)data.Length
                },
                Data = _writeSpad
            };

            if (server != null)
                Console.WriteLine("{0}: Created {1}", server.ID, entry.Index);

            Push(server, entry);
            return _logLength;
        }

        //public bool WriteChunk(Server server, byte[] )

        public bool Push(Server server, LogEntry data)
        {
            //we couldn't take this entry because we are still waiting for another
            //to finish
            if (_configLocked)
            {
                return false;
            }

            // we must first write the data to the dat file
            // in case of crash in between log data and log entry
            // this will orphan the data and on startup will reclaim the space

            // stream length is in UNSIGN but seek is SIGN?
            // seek before we commit the data so we are at the right position
            _logIndexWriter.Seek((int)(SUPER_BLOCK_SIZE + _logLength * LogIndex.LOG_RECORD_SIZE), SeekOrigin.Begin);

            // make sure we have enough capacity
            ensureLogIndices(_logLength + 1);

            //write to log data file
            _logDataFile.Seek(DataPosition, SeekOrigin.Begin);
            _logDataFile.Write(data.Data, 0, data.Data.Length);

            //update log entries
            _logIndices[_logLength] = data.Index;

            //inc log index
            _logLength++;
            //_logDataPosition += data.Index.Size;

            //flush data
            _logDataFile.Flush();

            //write data
            _logIndexWriter.Write(data.Index.Term);
            _logIndexWriter.Write((uint)data.Index.Type);
            _logIndexWriter.Write(data.Index.ChunkOffset);
            _logIndexWriter.Write(data.Index.ChunkSize);
            _logIndexWriter.Write(data.Index.Flag1);
            _logIndexWriter.Write(data.Index.Flag2);
            _logIndexWriter.Write(data.Index.Flag3);
            _logIndexWriter.Write(data.Index.Flag4);

            _logIndexWriter.Flush();

            //add server before commit
            if (data.Index.Type == LogIndexType.AddServer)
            {
                var id = GetIPEndPoint(data.Data);
                if (!server.ID.Equals(id))
                    server.AddClientFromLog(id);

                //System.Diagnostics.Debug.Assert(_configLocked == false);
                Console.WriteLine("{0}: Adding server {1} and locking config", server.ID, id);
                _configLocked = true;
                saveSuperBlock();
            }
            else if (data.Index.Type == LogIndexType.RemoveServer)
            {
                var id = GetIPEndPoint(data.Data);
                if (!server.ID.Equals(id))
                    server.RemoveClientFromLog(id);

                //System.Diagnostics.Debug.Assert(_configLocked == false);
                Console.WriteLine("{0}: Removing server {1} and locking config", server.ID, id);
                _configLocked = true;
                saveSuperBlock();
            }

            return true;
        }

        public void ApplyIndex(Server server, uint index)
        {
            Console.WriteLine("{0}: Applying commit index {1}", server.ID, index);
            if (index != _lastAppliedIndex + 1)
                throw new Exception();

            var applyIndex = _logIndices[_lastAppliedIndex];
            if (applyIndex.Type == LogIndexType.AddServer)
            {
                System.Diagnostics.Debug.Assert(_configLocked);

                var endPointData = GetData(applyIndex);
                var id = GetIPEndPoint(endPointData);

                Console.WriteLine("{0}: Committing add server {1} and unlocking config", server.ID, id);
                System.Diagnostics.Debug.Assert(_clients.Count(x => x.Equals(id)) == 0);

                if (!server.ID.Equals(id))
                    _clients.Add(id);

                _configLocked = false;
                server.CurrentState.CommittedAddServer(id);
            }
            else if (applyIndex.Type == LogIndexType.RemoveServer)
            {
                System.Diagnostics.Debug.Assert(_configLocked);

                var endPointData = GetData(applyIndex);
                var id = GetIPEndPoint(endPointData);

                Console.WriteLine("{0}: Committing remove server {1} and unlocking config", server.ID, id);
                System.Diagnostics.Debug.Assert(_clients.Count(x => x.Equals(id)) == 1);

                if (!server.ID.Equals(id))
                {
                    for (var i = 0; i < _clients.Count; i++)
                    {
                        if (_clients[i].Equals(id))
                            _clients.RemoveAt(i--);
                    }
                }

                _configLocked = false;
                server.CurrentState.CommittedRemoveServer(id);
            }


            _lastAppliedIndex++;
            saveSuperBlock();
        }

        public void Pop(Server server)
        {
            System.Diagnostics.Debug.Assert(_logLength > 0);


            var lastIndex = _logIndices[--_logLength];

            //roll back add
            if (lastIndex.Type == LogIndexType.AddServer)
            {
                System.Diagnostics.Debug.Assert(_configLocked);
                var id = GetIPEndPoint(GetData(lastIndex));
                _configLocked = false;
                if (!server.ID.Equals(id))
                    server.RemoveClientFromLog(id);
                Console.WriteLine("{0}x: Rolling back add server {1} and unlocking config", server.ID, id);
                saveSuperBlock();
            }
            else if (lastIndex.Type == LogIndexType.RemoveServer)
            {
                System.Diagnostics.Debug.Assert(_configLocked);
                var id = GetIPEndPoint(GetData(lastIndex));
                _configLocked = false;
                if (!server.ID.Equals(id))
                    server.AddClientFromLog(id);
                Console.WriteLine("{0}x: Rolling back remove server {1} and unlocking config", server.ID, id);
                saveSuperBlock();
            }

            _logLength--;
        }

        public void UpdateClients(IEnumerable<IPEndPoint> clients)
        {
            _clients.Clear();
            foreach (var client in clients)
                _clients.Add(client);

            saveSuperBlock();
        }

        public LogIndex this[uint index]
        {
            get
            {
                if (index < 1 || index > _logLength)
                    return new LogIndex() { Type = 0, ChunkOffset = 0, ChunkSize = 0, Term = 0 };

                return _logIndices[index - 1];
            }
        }

        public bool GetIndex(uint key, out LogIndex index)
        {
            if (key < 1 || key > _logLength)
            {
                index = new LogIndex() { Type = 0, ChunkOffset = 0, ChunkSize = 0, Term = 0 };
                return false;
            }

            index = _logIndices[key - 1];
            return true;
        }

        public uint GetTerm(uint key)
        {
            if (key < 1 || key > _logLength)
                return 0;

            return _logIndices[key - 1].Term;
        }

        public uint GetLastTerm()
        {
            return GetTerm(_logLength);
        }

        public uint GetLastIndex()
        {
            if (_logLength == 0)
                return 0;

            return _logLength;
        }

        public uint GetLastIndex(out LogIndex index)
        {
            if (_logLength == 0)
            {
                index = new LogIndex() { Type = 0, ChunkOffset = 0, ChunkSize = 0, Term = 0 };
                return 0;
            }

            index = _logIndices[_logLength - 1];
            return _logLength;
        }

        public byte[] GetData(LogIndex index)
        {
            var data = new byte[index.ChunkSize];

            using (var fr = OpenDataFileReader())
            {
                fr.Seek(index.ChunkOffset, SeekOrigin.Begin);
                fr.Read(data, 0, data.Length);
            }
            return data;
        }

        public Stream GetDataStream()
        {
            return OpenDataFileReader();
        }

        public LogEntry? GetEntry(uint key)
        {
            if (key < 1 || key > _logLength)
                return null;

            var index = _logIndices[key - 1];
            var data = new byte[index.ChunkSize];

            using (var fr = OpenDataFileReader())
            {
                fr.Seek(index.ChunkOffset, SeekOrigin.Begin);
                fr.Read(data, 0, data.Length);
            }

            return new LogEntry() { Index = index, Data = data };
        }

        public LogEntry[] GetEntries(uint start, uint end)
        {
            if (start < 0 || end < 1 || start == end)
                return null;

            var entries = new LogEntry[end - start];
            for (var i = start; i < end; i++)
            {
                var entry = GetEntry(i + 1);
                System.Diagnostics.Debug.Assert(entry.HasValue);
                entries[i - start] = entry.Value;
            }

            return entries;
        }

        public bool LogIsBetter(uint logLength, uint term)
        {
            var ourLastLogTerm = GetLastTerm();
            var logTermFurther = term > ourLastLogTerm;
            var logIndexLonger = term == ourLastLogTerm && logLength >= Length;

            return !(logTermFurther || logIndexLonger);
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
            _logDataFile.Dispose();
            _logDataFile = null;

            _logIndexWriter.Dispose();
            _logIndexWriter = null;
        }

        public string ToString(IPEndPoint ip)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Config {0}\n", ip);
            sb.AppendFormat("  Rpc Timeout: {0}\n", RPC_TIMEOUT);
            sb.AppendFormat("  Election Timeout: {0}\n", ELECTION_TIMEOUT);
            sb.AppendFormat("  Allocated Super Block: {0}\n", SUPER_BLOCK_SIZE);
            sb.AppendLine();

            sb.AppendFormat("  Current Term: {0}\n", _currentTerm);
            sb.AppendFormat("  Last Applied Index: {0}\n", _lastAppliedIndex);
            sb.AppendFormat("  Config Locked: {0}\n", _configLocked);
            sb.AppendFormat("  Voted For: {0}\n", _votedFor);
            sb.AppendLine();

            sb.AppendFormat("  Log Indices: {0}\n", _logIndices.Length);
            sb.AppendFormat("  Log Data Size: {0}\n", DataPosition);
            sb.AppendLine();

            sb.AppendFormat("  Nodes\n");
            sb.AppendFormat("    {0}\n", ip);
            foreach (var client in _clients)
                sb.AppendFormat("    {0}\n", client);

            return sb.ToString();
        }
    }
}
