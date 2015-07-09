using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    /* 
     * Only stores the last update, consumer must pull the log data and determine what to do with it
     * 
     * Untested
     * 
     */

    public class StateMachine : IDisposable
    {

        private uint _lastCommitApplied;
        private string _dataDir;
        private string _stateMachineFile;
        private BinaryWriter _stateMachineWriter;
        private Dictionary<string, uint> _states = new Dictionary<string, uint>();

        public uint LastCommitApplied { get { return _lastCommitApplied; } }

        public StateMachine(string dataDir)
        {
            _dataDir = dataDir;
            _stateMachineFile = System.IO.Path.Combine(dataDir, "statemachine");
        }

        public void Initialize()
        {
            var stateExists = System.IO.File.Exists(_stateMachineFile);
            if (!System.IO.Directory.Exists(_dataDir))
                System.IO.Directory.CreateDirectory(_dataDir);
            else if (stateExists)
            {
                using (var br = new BinaryReader(File.Open(_stateMachineFile, FileMode.Open, FileAccess.Read, FileShare.None)))
                    readStateMachine(br);
            }
            
            _stateMachineWriter = new BinaryWriter(System.IO.File.Open(_stateMachineFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
            
            if (!stateExists)
                writeStateMachine();
        }

        private void readStateMachine(BinaryReader br)
        {
            br.BaseStream.Seek(0, SeekOrigin.Begin);

            _lastCommitApplied = br.ReadUInt32();

            var states = br.ReadInt32();

            //read records in
            for (var i = 0; i < states; i++)
            {
                var state = br.ReadString();
                var index = br.ReadUInt32();

                _states[state] = index;
            }
        }

        private void writeStateMachine()
        {
            _stateMachineWriter.Seek(0, SeekOrigin.Begin);
            _stateMachineWriter.Write(_lastCommitApplied);
            _stateMachineWriter.Write(_states.Count);

            foreach (var kvp in _states)
            {
                _stateMachineWriter.Write(kvp.Key);
                _stateMachineWriter.Write(kvp.Value);
            }

            _stateMachineWriter.Flush();
        }

        public void Apply(string name, uint key)
        {
            System.Diagnostics.Debug.Assert((_lastCommitApplied + 1 == key));
            _states[name] = key;
        }

        public void TakeSnapshot()
        {
            writeStateMachine();
        }

        public uint GetIndex(string name)
        {
            uint result;
            if (!_states.TryGetValue(name, out result))
                return 0;

            return result;
        }

        public void Dispose()
        {
            _stateMachineWriter = null;
        }
    }
}
