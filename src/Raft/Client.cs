using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Logs;
using Raft.Messages;

namespace Raft
{
    public class Client 
    {
        private string _id;
        private Configuration _config;
        private IPEndPoint _endPoint;
        private Server _server;
        private long _nextHeartBeat;
        private bool _voteGranted;
        private uint _matchIndex;
        private uint _nextIndex;
        private long _rpcDue;

        public string ID { get { return _id; } }
        public Configuration Config { get { return _config; } }
        public IPEndPoint EndPoint { get { return _endPoint; } }
        public long NextHeartBeat { get { return _nextHeartBeat; } set { _nextHeartBeat = value; } }
        public uint MatchIndex { get { return _matchIndex; } set { _matchIndex = value; } }
        public uint NextIndex { get { return _nextIndex; } set { _nextIndex = value; } }
        public bool VoteGranted { get { return _voteGranted; } set { _voteGranted = value; } }
        public long RpcDue { get { return _rpcDue; } set { _rpcDue = long.MaxValue; } }
        public bool ReadyToSend { get { return _rpcDue <= _server.Tick; } }

        public Client(Server server, Configuration config)
        {
            _config = config;
            _id = _config.ID;
            _endPoint = new IPEndPoint(config.IP, config.Port);
            _server = server;
        }

        public void SendVoteRequest()
        {
            LogIndex lastIndex;
            var lastLogIndex = _server.PersistedStore.GetLastIndex(out lastIndex);

            var message = new VoteRequest()
            {
                From = _server.ID,
                Term = _server.PersistedStore.Term,
                LastTerm = lastIndex.Term,
                LogLength = _server.PersistedStore.Length
            };

            _server.Transport.SendMessage(this, message);
            _rpcDue = _server.Tick + _server.PersistedStore.RPC_TIMEOUT;
        }

        public void SendVoteReply(bool granted)
        {
            var message = new VoteReply()
            {
                From = _server.ID,
                Term = _server.PersistedStore.Term,
                Granted = granted
            };
            _server.Transport.SendMessage(this, message);
        }

        public void SendAppendEntriesRequest()
        {
            var _persistedState = _server.PersistedStore;
            //Console.WriteLine("Send heart beat");
            var prevIndex = _nextIndex - 1;
            var lastIndex = Math.Min(prevIndex + 1, _persistedState.Length);
            if (_matchIndex + 1 < _nextIndex)
                lastIndex = prevIndex;

            var entries = _persistedState.GetEntries(prevIndex, lastIndex);
            if (entries != null && entries.Length > 0)
                Console.WriteLine("{0}: Send AppendEnties[{1}-{2}] to {3}", _server.ID, prevIndex, lastIndex, ID);

            _rpcDue = _server.Tick + _server.PersistedStore.RPC_TIMEOUT;
            _nextHeartBeat = _server.Tick + (_server.PersistedStore.ELECTION_TIMEOUT / 2);

            var message = new AppendEntriesRequest()
            {
                From = _server.ID,
                Term = _persistedState.Term,
                PrevIndex = prevIndex,
                PrevTerm = _persistedState.GetTerm(prevIndex),
                Entries = entries,
                CommitIndex = Math.Min(_server.CommitIndex, lastIndex)
            };

            _server.Transport.SendMessage(this, message);
            _rpcDue = _server.Tick + _server.PersistedStore.RPC_TIMEOUT;
        }

        public void SendAppendEntriesReply(uint matchIndex, bool success)
        {
            var _persistedState = _server.PersistedStore;
            var message = new AppendEntriesReply()
            {
                From = _server.ID,
                Term = _persistedState.Term,
                MatchIndex = matchIndex,
                Success = success
            };

            _server.Transport.SendMessage(this, message);
        }

        public void Update()
        {
            if (_rpcDue > 0 && _rpcDue <= _server.Tick)
            {
                Console.WriteLine("{0}: dropped rpc at {1} ", _server.ID, _server.Tick);
                _rpcDue = 0;
            }
        }

        public void Reset()
        {
            _voteGranted = false;
            _matchIndex = 0;
            _nextIndex = 0;
            _nextHeartBeat = 0;
            _rpcDue = 0;
        }

    }

}
