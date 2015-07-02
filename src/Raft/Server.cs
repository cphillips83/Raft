using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    public enum ServerState
    {
        Follower,
        Candidate,
        Leader,
        Stopped

    }

    public class Settings
    {
        public const int RPC_TIMEOUT = 50000;
        public const int MIN_RPC_LATENCY = 10000;
        public const int MAX_RPC_LATENCY = 15000;
        public const int ELECTION_TIMEOUT = 100000;
        public const int NUM_SERVERS = 5;
        public const int BATCH_SIZE = 1;
    }

    public class Server
    {
        protected Random _random;
        protected int _id;
        protected ServerState _state;
        protected uint _term;
        protected int? _votedFor;
        protected Log _log;
        protected uint _commitIndex;
        protected uint _electionAlarm;

        protected List<Peer> _peers;


        public int Quorum { get { return (_peers.Count / 2) + 1; } }

        public Server(int id)
        {
            _id = id;
            _peers = new List<Peer>();
            _log = new Log();
            _random = new Random((int)id);
        }

        protected void stepDown(Model model, uint term)
        {
            _term = term;
            _state = ServerState.Follower;
            _votedFor = null;
            if (isElectionTimeout(model))
                _electionAlarm = makeElectionAlarm(model);
        }

        protected void startNewElection(Model model)
        {
            if ((_state == ServerState.Follower || _state == ServerState.Candidate) &&
                isElectionTimeout(model))
            {
                _electionAlarm = makeElectionAlarm(model);
                _term++;
                _votedFor = _id;
                _state = ServerState.Candidate;
                foreach (var peer in _peers)
                    peer.Reset();

            }
        }

        protected void sendRequestVote(Model model, Peer peer)
        {
            if (_state == ServerState.Candidate && peer.CheckRpcTimeout(model))
            {
                peer.RpcDue = model.Tick + Settings.RPC_TIMEOUT;
                /* TODO
                    sendRequest(model, {
                      from: server.id,
                      to: peer,
                      type: 'RequestVote',
                      term: server.term,
                      lastLogTerm: logTerm(server.log, server.log.length),
                      lastLogIndex: server.log.length});
                 */
            }
        }

        protected void becomeLeader(Model model)
        {
            if (_state == ServerState.Candidate && _peers.Count(x => x.VotedGranted) >= Quorum)
            {
                _state = ServerState.Leader;
                _electionAlarm = uint.MaxValue;
                foreach (var peer in _peers)
                    peer.LeadershipChanged(_log.Length);

            }
        }

        protected void sendAppendEntries(Model model, Peer peer)
        {
            if (_state == ServerState.Leader &&
                (peer.HeartBeartDue <= model.Tick ||
                 (peer.NextIndex <= _log.Length && peer.RpcDue <= model.Tick)))
            {
                var prevIndex = peer.NextIndex - 1;
                var lastIndex = Math.Min(prevIndex + Settings.BATCH_SIZE, _log.Length);
                if (peer.MatchIndex + 1 < peer.NextIndex)
                    lastIndex = prevIndex;
                /* TODO 
                    sendRequest(model, {
                        from: server.id,
                        to: peer,
                        type: 'AppendEntries',
                        term: server.term,
                        prevIndex: prevIndex,
                        prevTerm: logTerm(server.log, prevIndex),
                        entries: server.log.slice(prevIndex, lastIndex),
                        commitIndex: Math.min(server.commitIndex, lastIndex)});
                 */
                peer.RpcDue = model.Tick + Settings.RPC_TIMEOUT;
                peer.HeartBeartDue = model.Tick + (Settings.ELECTION_TIMEOUT / 2);
            }
        }

        protected void advanceCommitIndex(Model model)
        {

            var matchIndexes = new uint[_peers.Count + 1];
            matchIndexes[matchIndexes.Length - 1] = _log.Length;
            for (var i = 0; i < _peers.Count; i++)
                matchIndexes[i] = _peers[i].MatchIndex;

            Array.Sort(matchIndexes);

            var n = matchIndexes[_peers.Count / 2];
            if (_state == ServerState.Leader && _log.GetTerm(n) == _term)
                _commitIndex = Math.Max(_commitIndex, n);
        }

        protected void handleRequestVote(Model model, VoteRequest request)
        {
            if (_term < request.Term)
                stepDown(model, request.Term);

            var ourLastLogTerm = _log.LastLogterm;
            var canVote = _term == request.Term && (_votedFor == null || _votedFor == request.From);
            var logTermFurther = request.LastLogTerm > ourLastLogTerm;
            var logIndexLonger = request.LastLogTerm == ourLastLogTerm && request.LastLogIndex >= _log.Length;
            var granted = canVote && (logTermFurther || logIndexLonger);

            var peer = _peers.First(x => x.ID == request.From);
            peer.SendReply(new VoteRequestReply() { From = _id, Term = _term, Granted = granted });
        }

        protected void handleRequestVoteReply(Model model, VoteRequestReply reply)
        {
            if (_term < reply.Term)
                stepDown(model, reply.Term);

            if (_state == ServerState.Candidate && _term == reply.Term)
            {
                var peer = _peers.First(x => x.ID == reply.From);
                peer.RpcDue = uint.MaxValue;
                peer.VotedGranted = reply.Granted;
            }
        }

        protected void handleAppendEntriesRequest(Model model, AppendEntriesRequest request)
        {
            if (_term < request.Term)
                stepDown(model, request.Term);

            var peer = _peers.First(x => x.ID == request.From);
            var success = false;
            uint matchIndex = 0;

            if (_term == request.Term)
            {
                _state = ServerState.Follower;
                _electionAlarm = makeElectionAlarm(model);

                if (request.PrevLogIndex == 0 ||
                    (request.PrevIndex <= _log.Length && _log.GetTerm(request.PrevIndex) == request.PrevTerm))
                {
                    success = true;

                    var index = request.PrevIndex;
                    for (var i = 0; request.Entries != null && i < request.Entries.Length; i++)
                    {
                        index++;
                        if (_log.GetTerm(index) != request.Entries[i].Term)
                        {
                            while (_log.Length > index - 1)
                                _log.Pop();

                            _log.Push(request.Entries[i]);
                        }
                    }
                    matchIndex = index;
                    _commitIndex = Math.Max(_commitIndex, request.CommitIndex);
                }
            }

            peer.SendReply(new AppendEntriesReply() { From = _id, Term = _term, MatchIndex = matchIndex, Success = success });
        }

        protected void handleAppendEntriesReply(Model model, AppendEntriesReply reply)
        {
            if (_term < reply.Term)
                stepDown(model, reply.Term);

            if (_state == ServerState.Leader && _term == reply.Term)
            {
                var peer = _peers.First(x => x.ID == reply.From);
                if (reply.Success)
                {
                    peer.MatchIndex = Math.Max(peer.MatchIndex, reply.MatchIndex);
                    peer.NextIndex = reply.MatchIndex + 1;
                }
                else
                {
                    peer.NextIndex = Math.Max(1, peer.NextIndex - 1);
                }
                peer.RpcDue = 0;
            }
        }

        protected void handleMessage(Model model, object message)
        {
            if (_state == ServerState.Stopped)
                return;

            if (message is VoteRequest)
                handleRequestVote(model, (VoteRequest)message);
            else if (message is VoteRequestReply)
                handleRequestVoteReply(model, (VoteRequestReply)message);
            else if (message is AppendEntriesRequest)
                handleAppendEntriesRequest(model, (AppendEntriesRequest)message);
            else if (message is AppendEntriesReply)
                handleAppendEntriesReply(model, (AppendEntriesReply)message);
            else
                throw new Exception("Unhandled message");
        }

        protected void update(Model model)
        {

        }

        protected bool isElectionTimeout(Model model)
        {
            return _electionAlarm <= model.Tick;
        }

        protected uint makeElectionAlarm(Model model)
        {
            return model.Tick + (uint)_random.Next(Settings.ELECTION_TIMEOUT, Settings.ELECTION_TIMEOUT * 2);
        }
    }

    //public class TestServer : Server
    //{
    //    protected override int makeElectionAlarm(int tick)
    //    {
    //        return base.makeElectionAlarm(tick);
    //    }
    //}
}
