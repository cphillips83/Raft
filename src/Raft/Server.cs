using System;
using System.Collections.Generic;
using System.IO;
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
        public const int RPC_TIMEOUT = 50;
        public const int MIN_RPC_LATENCY = 10;
        public const int MAX_RPC_LATENCY = 15;
        public const int ELECTION_TIMEOUT = 100;
        public const int NUM_SERVERS = 5;
        public const int BATCH_SIZE = 2;
    }


    public class Server
    {
        protected State _persistedState;
        protected Random _random;
        protected int _id;
        protected ServerState _state;
        //protected Log _log;
        protected uint _commitIndex;
        protected long _electionAlarm;
        protected string _dataDir;

        protected List<Peer> _peers;

        public int Quorum { get { return ((_peers.Count + 1) / 2) + 1; } }

        public Server(int id, string dataDir)
        {
            _id = id;
            _dataDir = dataDir;

            _peers = new List<Peer>();
            //_log = new Log();
            _random = new Random(id ^ (int)DateTime.Now.Ticks);
            _state = ServerState.Stopped;
        }

        protected void stepDown(IModel model, int term)
        {
            _state = ServerState.Follower;
            _persistedState.UpdateState(term, null);
            if (isElectionTimeout(model))
                _electionAlarm = makeElectionAlarm(model);
        }

        protected void startNewElection(IModel model)
        {
            if ((_state == ServerState.Follower || _state == ServerState.Candidate) &&
                isElectionTimeout(model))
            {
                _electionAlarm = makeElectionAlarm(model);
                _persistedState.UpdateState(_persistedState.Term + 1, _id);
                _state = ServerState.Candidate;
                foreach (var peer in _peers)
                    peer.Reset();

                Console.WriteLine("{0}: Starting new election for term {1}", _id, _persistedState.Term);
            }
        }

        protected void sendRequestVote(IModel model, Peer peer)
        {
            if (_state == ServerState.Candidate && peer.CheckRpcTimeout(model))
            {
                //Console.WriteLine("{0}: Requesting vote from {1}", _id, peer.ID);

                LogIndex lastIndex;
                var lastLogIndex = _persistedState.GetLastIndex(out lastIndex);

                peer.RpcDue = model.Tick + Settings.RPC_TIMEOUT;
                model.SendRequest(peer, new VoteRequest()
                {
                    From = _id,
                    Term = _persistedState.Term,
                    LastTerm = lastIndex.Term,
                    LogLength = lastLogIndex
                });
            }
        }

        //protected void sendStatusRequest(IModel model, Peer peer)
        //{
        //    // since we are designed around not knowning what the data is
        //    // it could be large, we don't want to slowly send index after 
        //    // index to a node thats been down for a long time

        //    System.Diagnostics.Debug.Assert(peer.RpcDue == int.MaxValue);

        //    if (_state == ServerState.Leader)
        //    {
        //        peer.RpcDue = model.Tick + Settings.RPC_TIMEOUT;
        //        peer.HeartBeartDue = model.Tick + (Settings.ELECTION_TIMEOUT / 2);
        //        model.SendRequest(peer, new StatusRequest()
        //        {
        //            From = _id,
        //            Term = _persistedState.Term
        //        });
        //    }
        //}

        protected void becomeLeader(IModel model)
        {
            if (_state == ServerState.Candidate)
            {
                var voteCount = _peers.Count(x => x.VotedGranted) + 1;
                if (voteCount >= Quorum)
                {
                    Console.WriteLine("{0}: I am now leader of term {2} with {1} votes", _id, voteCount, _persistedState.Term);
                    _state = ServerState.Leader;
                    _electionAlarm = int.MaxValue;
                    foreach (var peer in _peers)
                    {
                        peer.LeadershipChanged(_persistedState.Length + 1);
                        //sendStatusRequest(model, peer);
                    }
                }
            }
        }

        protected void sendAppendEntries(IModel model, Peer peer)
        {
            if (_state == ServerState.Leader &&
                (peer.HeartBeartDue <= model.Tick ||
                 (peer.NextIndex <= _persistedState.Length && peer.RpcDue <= model.Tick)))
            {
                var prevIndex = peer.NextIndex - 1;
                var lastIndex = Math.Min(prevIndex + Settings.BATCH_SIZE, _persistedState.Length);
                if (peer.MatchIndex + 1 < peer.NextIndex)
                    lastIndex = prevIndex;

                var entries = _persistedState.Get(prevIndex, lastIndex);
                if (entries != null && entries.Length > 0)
                    Console.WriteLine("{0}: Send AppendEnties[{1}-{2}] to {3}", _id, prevIndex, lastIndex, peer.ID);

                peer.RpcDue = model.Tick + Settings.RPC_TIMEOUT;
                peer.HeartBeartDue = model.Tick + (Settings.ELECTION_TIMEOUT / 2);
                model.SendRequest(peer, new AppendEntriesRequest()
                {
                    From = _id,
                    Term = _persistedState.Term,
                    PrevIndex = prevIndex,
                    PrevTerm = _persistedState.GetTerm(prevIndex),
                    Entries = entries,
                    CommitIndex = Math.Min(_commitIndex, lastIndex)
                });
            }
        }

        protected void advanceCommitIndex(IModel model)
        {
            var matchIndexes = new uint[_peers.Count + 1];
            matchIndexes[matchIndexes.Length - 1] = _persistedState.Length;
            for (var i = 0; i < _peers.Count; i++)
                matchIndexes[i] = _peers[i].MatchIndex;

            Array.Sort(matchIndexes);

            var n = matchIndexes[_peers.Count / 2];
            if (_state == ServerState.Leader && _persistedState.GetTerm(n) == _persistedState.Term)
            {
                var newCommitIndex = Math.Max(_commitIndex, n);
                if (newCommitIndex != _commitIndex)
                {
                    Console.WriteLine("{0}: Advancing commit index from {1} to {2}", _id, _commitIndex, newCommitIndex);
                    _commitIndex = newCommitIndex;
                }
            }
        }

        //protected void handleRequestStatus(IModel model, StatusRequest request)
        //{
        //    if (_persistedState.Term < request.Term)
        //        stepDown(model, request.Term);

        //    if (_state == ServerState.Follower && _persistedState.Term == request.Term)
        //    {
        //        var peer = _peers.First(x => x.ID == request.From);
        //        _electionAlarm = makeElectionAlarm(model);
        //        model.SendReply(peer, new StatusReply()
        //        {
        //            From = _id, 
        //            CommitIndex = _commitIndex,
        //            Term = _persistedState.Term
        //        });

        //        Console.WriteLine("{0}: Sent status {1} to {2}", _id, _commitIndex, peer.ID);
        //    }
        //}

        //protected void handleStatusReply(IModel model, StatusReply reply)
        //{
        //    if (_persistedState.Term < reply.Term)
        //        stepDown(model, reply.Term);

        //    if (_state == ServerState.Leader && _persistedState.Term == reply.Term)
        //    {
        //        var peer = _peers.First(x => x.ID == reply.From);
        //        peer.RpcDue = int.MaxValue;
        //        peer.MatchIndex = reply.CommitIndex;

        //        //Console.WriteLine("{0}: Peer {1} voted {2}", _id, peer.ID, peer.VotedGranted);
        //    }
        //}

        protected void handleRequestVote(IModel model, VoteRequest request)
        {
            if (_persistedState.Term < request.Term)
                stepDown(model, request.Term);

            var peer = _peers.First(x => x.ID == request.From);
            var ourLastLogTerm = _persistedState.GetLastTerm();
            var termCheck = _persistedState.Term == request.Term;
            var canVote = _persistedState.VotedFor == null || _persistedState.VotedFor == request.From;
            var logTermFurther = request.LastTerm > ourLastLogTerm;
            var logIndexLonger = request.LastTerm == ourLastLogTerm && request.LogLength >= _persistedState.Length;
            var granted = termCheck && canVote && (logTermFurther || logIndexLonger);

            if (!termCheck)
                Console.WriteLine("{0}: Can not vote for {1} because term {2}, expected {3}", _id, peer.ID, request.Term, _persistedState.Term);

            if (!canVote)
                Console.WriteLine("{0}: Can not vote for {1} because I already voted for {2}", _id, peer.ID, _persistedState.VotedFor);

            if (!(logTermFurther || logIndexLonger))
                Console.WriteLine("{0}: Can not vote for {1} because my log is more update to date", _id, peer.ID);

            if (granted)
            {
                Console.WriteLine("{0}: Voted for {1}", _id, peer.ID);
                _persistedState.VotedFor = peer.ID;
                _electionAlarm = makeElectionAlarm(model);
            }

            model.SendReply(peer, new VoteRequestReply() { From = _id, Term = _persistedState.Term, Granted = granted });

        }

        protected void handleRequestVoteReply(IModel model, VoteRequestReply reply)
        {
            if (_persistedState.Term < reply.Term)
                stepDown(model, reply.Term);

            if (_state == ServerState.Candidate && _persistedState.Term == reply.Term)
            {
                var peer = _peers.First(x => x.ID == reply.From);
                peer.RpcDue = int.MaxValue;
                peer.VotedGranted = reply.Granted;

                //Console.WriteLine("{0}: Peer {1} voted {2}", _id, peer.ID, peer.VotedGranted);
            }
        }

        protected void handleAppendEntriesRequest(IModel model, AppendEntriesRequest request)
        {
            if (_persistedState.Term < request.Term)
                stepDown(model, request.Term);

            var peer = _peers.First(x => x.ID == request.From);
            var success = false;
            var matchIndex = 0u;

            if (_persistedState.Term == request.Term)
            {
                _state = ServerState.Follower;
                _electionAlarm = makeElectionAlarm(model);

                if (request.PrevIndex == 0 ||
                    (request.PrevIndex <= _persistedState.Length && _persistedState.GetTerm(request.PrevIndex) == request.PrevTerm))
                {
                    success = true;

                    var index = request.PrevIndex;
                    for (var i = 0; request.Entries != null && i < request.Entries.Length; i++)
                    {
                        index++;
                        if (_persistedState.GetTerm(index) != request.Entries[i].Index.Term)
                        {
                            while (_persistedState.Length > index - 1)
                            {
                                Console.WriteLine("{0}: Rolling back log {1}", _id, _persistedState.Length - 1);
                                _persistedState.Pop();
                            }

                            //Console.WriteLine("{0}: Writing log value {1}", _id, request.Entries[i].Offset);
                            _persistedState.Push(request.Entries[i]);
                        }
                    }

                    matchIndex = index;

                    var newCommitIndex = Math.Max(_commitIndex, request.CommitIndex);
                    if (newCommitIndex != _commitIndex)
                    {
                        Console.WriteLine("{0}: Advancing commit index from {1} to {2}", _id, _commitIndex, newCommitIndex);
                        _commitIndex = newCommitIndex;
                    }
                }
            }

            model.SendReply(peer, new AppendEntriesReply() { From = _id, Term = _persistedState.Term, MatchIndex = matchIndex, Success = success });
        }

        protected void handleAppendEntriesReply(IModel model, AppendEntriesReply reply)
        {
            if (_persistedState.Term < reply.Term)
                stepDown(model, reply.Term);

            if (_state == ServerState.Leader && _persistedState.Term == reply.Term)
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

        protected void handleMessage(IModel model, object message)
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
            //else if (message is StatusRequest)
            //    handleRequestStatus(model, (StatusRequest)message);
            //else if (message is StatusReply)
            //    handleStatusReply(model, (StatusReply)message);
            else
                throw new Exception("Unhandled message");
        }

        public void Update(IModel model)
        {
            if (_persistedState == null)
            {
                _persistedState = new State(_dataDir);
                _persistedState.Initialize();
            }

            startNewElection(model);
            becomeLeader(model);
            advanceCommitIndex(model);
            foreach (var peer in _peers)
            {
                sendRequestVote(model, peer);
                sendAppendEntries(model, peer);
            }
        }

        protected bool isElectionTimeout(IModel model)
        {
            return _electionAlarm <= model.Tick;
        }

        protected long makeElectionAlarm(IModel model)
        {
            return model.Tick + _random.Next(Settings.ELECTION_TIMEOUT, Settings.ELECTION_TIMEOUT * 2);
        }

    }

    public class SimulationServer : Server
    {
        public int ID { get { return _id; } }
        public int Term { get { return _persistedState.Term; } set { _persistedState.Term = value; } }
        public int? VotedFor { get { return _persistedState.VotedFor; } set { _persistedState.VotedFor = value; } }
        public long ElectionAlarm { get { return _electionAlarm; } set { _electionAlarm = value; } }
        public ServerState State { get { return _state; } }
        public List<Peer> Peers { get { return _peers; } }

        public SimulationServer(int id, int[] peers)
            : base(id, "D:\\server\\" + id)
        {
            for (var i = 0; i < peers.Length; i++)
                _peers.Add(new Peer(peers[i], false));
        }

        public void BecomeLeader(IModel model)
        {
            becomeLeader(model);
        }

        public void HandleMessage(IModel model, object message)
        {
            handleMessage(model, message);
        }

        public void Stop(IModel model)
        {
            _state = ServerState.Stopped;
            _electionAlarm = 0;
        }

        public void Resume(IModel model)
        {
            _state = ServerState.Follower;
            _commitIndex = 0;
            _electionAlarm = makeElectionAlarm(model);
        }

        public void Restart(IModel model)
        {
            Stop(model);
            Resume(model);
        }

        public void Timeout(IModel model)
        {
            _state = ServerState.Follower;
            _electionAlarm = 0;
            startNewElection(model);
        }

        public void ClientRequest(IModel model)
        {
            if (_state == ServerState.Leader)
                _persistedState.Create(new byte[] { (byte)_id });
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
