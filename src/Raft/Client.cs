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
        private IPEndPoint _id;
        //private Configuration _config;
        //private IPEndPoint _endPoint;
        private Server _server;
        private long _nextHeartBeat;
        private bool _voteGranted;
        private uint _matchIndex;
        private uint _nextIndex;
        private long _rpcDue;
        private object _lastMessage;

        public IPEndPoint ID { get { return _id; } }
        //public Configuration Config { get { return _config; } }
        //public IPEndPoint EndPoint { get { return _endPoint; } }
        public long NextHeartBeat { get { return _nextHeartBeat; } set { _nextHeartBeat = value; } }
        public uint MatchIndex { get { return _matchIndex; } set { _matchIndex = value; } }
        public uint NextIndex { get { return _nextIndex; } set { _nextIndex = value; } }
        public bool VoteGranted { get { return _voteGranted; } set { _voteGranted = value; } }
        public long RpcDue { get { return _rpcDue; } set { _rpcDue = long.MaxValue; } }
        public bool ReadyToSend { get { return _rpcDue <= _server.Tick; } }
        public bool WaitingForResponse { get { return _rpcDue > 0 && _rpcDue < uint.MaxValue && _rpcDue > _server.Tick; } }
        public IPEndPoint AgentIP { get; set; }

        public Client(Server server, IPEndPoint id)
        {
            //_config = config;
            //_id = _config.ID;
            _id = id; //new IPEndPoint(config.IP, config.Port);
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

            _lastMessage = message;
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
            _lastMessage = message;
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
            //if (entries != null && entries.Length > 0)
            //    Console.WriteLine("{0}: {4} - Send AppendEnties[{1}-{2}] to {3}", _server.ID, prevIndex, lastIndex, ID, _server.Tick);
            //else
            //    Console.WriteLine("{0}: Send heart beat to {1}", _server.ID, _id);

            _rpcDue = _server.Tick + _server.PersistedStore.RPC_TIMEOUT;
            _nextHeartBeat = _server.Tick + (_server.PersistedStore.ELECTION_TIMEOUT / 4);

            var message = new AppendEntriesRequest()
            {
                From = _server.ID,
                Term = _persistedState.Term,
                PrevIndex = prevIndex,
                PrevTerm = _persistedState.GetTerm(prevIndex),
                AgentIP = _server.AgentIP,
                Entries = entries,
                CommitIndex = Math.Min(_server.CommitIndex, lastIndex)
            };

            _lastMessage = message;
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

            _lastMessage = message;
            _server.Transport.SendMessage(this, message);
        }

        public void SendEntryRequest(uint index)
        {
            var _persistedState = _server.PersistedStore;
            var message = new EntryRequest()
            {
                From = _server.ID,
                Index = index
            };

            _lastMessage = message;
            _server.Transport.SendMessage(this, message);
        }

        public void SendEntryRequestReply(uint index)
        {
            var _persistedState = _server.PersistedStore;
            var entry = _persistedState.GetEntry(index);

            var message = new EntryRequestReply()
            {
                From = _server.ID,
                Entry = entry
            };

            _lastMessage = message;
            _server.Transport.SendMessage(this, message);
        }

        public void SendAddServerRequest()
        {
            Console.WriteLine("{0}: Sending add server request to {1}", _server.Name, this.ID);
            var message = new AddServerRequest()
            {
                From = _server.ID,
                //EndPoint = new IPEndPoint(_server.Config.IP, _server.Config.Port)
            };
            _lastMessage = message;
            _server.Transport.SendMessage(this, message);
            _rpcDue = _server.Tick + _server.PersistedStore.RPC_TIMEOUT;
        }

        public void SendAddServerReply(AddServerStatus status, IPEndPoint leaderHint)
        {
            Console.WriteLine("{0}: Sending add server reply to {1} with status {2}", _server.Name, ID, status);
            var message = new AddServerReply()
            {
                From = _server.ID,
                Status = status,
                LeaderHint = leaderHint,
            };
            _lastMessage = message;
            _server.Transport.SendMessage(this, message);
        }

        public void SendRemoveServerRequest()
        {
            Console.WriteLine("{0}: Sending remove server request to {1}", _server.Name, this.ID);
            var message = new RemoveServerRequest()
            {
                From = _server.ID,
                //EndPoint = new IPEndPoint(_server.Config.IP, _server.Config.Port)
            };
            _lastMessage = message;
            _server.Transport.SendMessage(this, message);
            _rpcDue = _server.Tick + _server.PersistedStore.RPC_TIMEOUT;
        }

        public void SendRemoveServerReply(RemoveServerStatus status, IPEndPoint leaderHint)
        {
            Console.WriteLine("{0}: Sending remove server reply to {1} with status {2}", _server.Name, ID, status);
            var message = new RemoveServerReply()
            {
                From = _server.ID,
                Status = status,
                LeaderHint = leaderHint,
            };
            _lastMessage = message;
            _server.Transport.SendMessage(this, message);
        }

        public void Update()
        {
            if (_rpcDue > 0 && _rpcDue <= _server.Tick)
            {
                Console.WriteLine("{0}: dropped rpc for {1} at {2} ", _server.Name, _id, _server.Tick);
                _server.Transport.ResetConnection(this);
                _rpcDue = 0;
            }
        }

        public void ResetKnownLogs()
        {

            NextHeartBeat = 0;
            NextIndex = _server.PersistedStore.Length + 1;
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
