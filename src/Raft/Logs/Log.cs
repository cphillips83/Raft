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

        public int RPC_TIMEOUT = 50;
        public int ELECTION_TIMEOUT = 100;

        //can not change once in production
        public const int SUPER_BLOCK_SIZE = 1024;
        public const int LOG_DEFAULT_ARRAY_SIZE = 65536;
        public const int LOG_RECORD_SIZE = 16;
        //public const int MAX_LOG_DATA_READS = 16;

        // current term of the cluster
        private int _currentTerm = 1;
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

        public int Term
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
                return index.Offset + index.Size;
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
            System.Diagnostics.Debug.Assert(((br.BaseStream.Length - SUPER_BLOCK_SIZE)) % LOG_RECORD_SIZE == 0);

            // read term and last vote
            _currentTerm = br.ReadInt32();

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
            var indices = (uint)((br.BaseStream.Length - SUPER_BLOCK_SIZE) / LOG_RECORD_SIZE);
            ensureLogIndices(indices);

            //read records in
            for (var i = 0; i < indices; i++)
            {
                _logIndices[i].Term = br.ReadInt32();
                _logIndices[i].Type = (LogIndexType)br.ReadUInt32();
                _logIndices[i].Offset = br.ReadUInt32();
                _logIndices[i].Size = br.ReadUInt32();
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

        public void UpdateState(int term, IPEndPoint votedFor)
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
                    Offset = DataPosition,
                    Size = (uint)data.Length,
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
                    Offset = DataPosition,
                    Size = (uint)data.Length,
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

        public LogEntry Create(Server server, byte[] data)
        {
            var entry = new LogEntry()
            {
                Index = new LogIndex()
                {
                    Term = _currentTerm,
                    Offset = DataPosition,
                    Size = (uint)data.Length,
                    Type = 0
                },
                Data = data
            };

            if (server != null)
                Console.WriteLine("{0}: Created {1}", server.ID, entry.Index);

            Push(server, entry);
            return entry;
        }

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
            _logIndexWriter.Seek((int)(SUPER_BLOCK_SIZE + _logLength * LOG_RECORD_SIZE), SeekOrigin.Begin);

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
            _logIndexWriter.Write(data.Index.Offset);
            _logIndexWriter.Write(data.Index.Size);

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

        public LogIndex this[int index]
        {
            get
            {
                if (index < 0 || index >= _logLength)
                    return new LogIndex() { Type = 0, Offset = 0, Size = 0, Term = 0 };

                return _logIndices[index];
            }
        }

        public bool GetIndex(uint key, out LogIndex index)
        {
            if (key < 1 || key > _logLength)
            {
                index = new LogIndex() { Type = 0, Offset = 0, Size = 0, Term = 0 };
                return false;
            }

            index = _logIndices[key - 1];
            return true;
        }

        public int GetTerm(uint key)
        {
            if (key < 1 || key > _logLength)
                return 0;

            return _logIndices[key - 1].Term;
        }

        public int GetLastTerm()
        {
            return GetTerm(_logLength);
        }

        public uint GetLastIndex()
        {
            if (_logLength == 0)
                return 0;

            return _logLength - 1;
        }

        public uint GetLastIndex(out LogIndex index)
        {
            if (_logLength == 0)
            {
                index = new LogIndex() { Type = 0, Offset = 0, Size = 0, Term = 0 };
                return 0;
            }

            index = _logIndices[_logLength - 1];
            return _logLength - 1;
        }

        public byte[] GetData(LogIndex index)
        {
            var data = new byte[index.Size];

            using (var fr = OpenDataFileReader())
            {
                fr.Seek(index.Offset, SeekOrigin.Begin);
                fr.Read(data, 0, data.Length);
            }
            return data;
        }

        public LogEntry? GetEntry(uint key)
        {
            if (key < 1 || key > _logLength)
                return null;

            var index = _logIndices[key - 1];
            var data = new byte[index.Size];

            using (var fr = OpenDataFileReader())
            {
                fr.Seek(index.Offset, SeekOrigin.Begin);
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

        public bool LogIsBetter(uint logLength, int term)
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


    }
}
