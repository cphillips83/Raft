using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    //public class IModel
    //{
    //    void Start();
    //    void Stop();
    //    void Restart();
    //    void Pause();
    //}
    public interface IModel
    {
        long Tick { get; }
        void SendRequest(Peer peer, VoteRequest request);
        void SendRequest(Peer peer, AppendEntriesRequest request);
        void SendReply(Peer peer, VoteRequestReply reply);
        void SendReply(Peer peer, AppendEntriesReply reply);
    }

    public struct SimulationMessage
    {
        public int To;
        public int From;
        public long SendTick;
        public long RecvTick;
        public object Message;
    }

    public class SimulationModel : IModel
    {
        private const float PACKET_LOSS = 0.0f;
        private const float TIME_SCALE = 10f;
        private Stopwatch _timer = Stopwatch.StartNew();
        private double _counter = 0.0;
        private double _lastTime = 0.0;
        private long _tick;
        private Random _random = new Random();
        private List<SimulationServer> _servers;
        private List<SimulationMessage> _messages = new List<SimulationMessage>();

        public long Tick { get { return _tick; } }

        public SimulationModel()
        {
            _tick = 0;
        }

        public void Advance()
        {
            Advance(1);
        }

        private long lastUpdate = 0;
        public void Advance(int steps)
        {
            while (steps-- > 0)
            {
                //time sampling
                var newTime = _timer.Elapsed.TotalSeconds;
                var delta = newTime - _lastTime;
                _lastTime = newTime;

                _counter += delta / TIME_SCALE;
                _tick = (long)(_counter * 1000);
                if (lastUpdate != _tick)
                {
                    lastUpdate = _tick;
                    //Console.WriteLine(_tick);
                }
                //tick sampling
                //_tick++;

                foreach (var server in _servers)
                    server.Update(this);

                for (var i = 0; i < _messages.Count; i++)
                {
                    if (_messages[i].RecvTick <= _tick)
                    {
                        var server = _servers.First(x => x.ID == _messages[i].To);
                        server.HandleMessage(this, _messages[i].Message);
                        _messages.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        public void ClientRequest()
        {
            foreach (var server in _servers)
                server.ClientRequest(this);
        }

        private void SendMessage(Peer peer, object message)
        {
            if ((float)_random.NextDouble() < PACKET_LOSS)
            {
                //Console.WriteLine("** dropped packet to: {0}, type: {1}", peer.ID, message.GetType().Name);
                return;
            }

            var sm = new SimulationMessage()
            {
                To = peer.ID,
                From = 0,
                SendTick = _tick,
                RecvTick = _tick + _random.Next(Settings.MIN_RPC_LATENCY, Settings.MAX_RPC_LATENCY),
                Message = message
            };
            _messages.Add(sm);
            //Console.WriteLine("** MESSAGE:{0}, type: {1}, send: {2}, recv: {3}", peer.ID, message.GetType().Name, sm.SendTick, sm.RecvTick);
        }

        public void SendRequest(Peer peer, VoteRequest request)
        {
            SendMessage(peer, request);
        }

        public void SendRequest(Peer peer, AppendEntriesRequest request)
        {
            SendMessage(peer, request);
        }

        public void SendReply(Peer peer, VoteRequestReply reply)
        {
            SendMessage(peer, reply);
        }

        public void SendReply(Peer peer, AppendEntriesReply reply)
        {
            SendMessage(peer, reply);
        }

        public void DropMessage(int server)
        {
            for (var i = 0; i < _messages.Count; i++)
            {
                if (_messages[i].To == server)
                {
                    _messages.RemoveAt(i);
                    break;
                }
            }
        }

        public void ResumeAllStopped()
        {
            foreach (var server in _servers)
                if (server.State == ServerState.Stopped)
                    server.Resume(this);

        }

        public void ResumeAll()
        {
            foreach (var server in _servers)
                server.Resume(this);
        }

        public void SpreadTimers()
        {
            var timers = new List<long>(_servers.Count);
            foreach (var server in _servers)
                if (server.ElectionAlarm > _tick && server.ElectionAlarm < int.MaxValue)
                    timers.Add(server.ElectionAlarm);

            timers.Sort();
            if (timers.Count > 1 && timers[1] - timers[0] < Settings.MAX_RPC_LATENCY)
            {
                if (timers[0] > _tick + Settings.MAX_RPC_LATENCY)
                {
                    foreach (var server in _servers)
                    {
                        if (server.ElectionAlarm == timers[0])
                        {
                            server.ElectionAlarm -= Settings.MAX_RPC_LATENCY;
                            Console.WriteLine("Adjusted S{0} timeout forward", server.ID);
                        }
                    }
                }
                else
                {
                    foreach (var server in _servers)
                    {
                        if (server.ElectionAlarm > timers[0] &&
                            server.ElectionAlarm < timers[0] + Settings.MAX_RPC_LATENCY)
                        {
                            server.ElectionAlarm += Settings.MAX_RPC_LATENCY;
                            Console.WriteLine("Adjusted S{0} timeout backward", server.ID);
                        }
                    }
                }
            }
        }

        public void AlignTimers()
        {
            SpreadTimers();

            var timers = new List<long>(_servers.Count);
            foreach (var server in _servers)
                if (server.ElectionAlarm > _tick && server.ElectionAlarm < int.MaxValue)
                    timers.Add(server.ElectionAlarm);

            if (timers.Count > 1)
            {
                timers.Sort();
                foreach (var server in _servers)
                {
                    if (server.ElectionAlarm == timers[1])
                    {
                        server.ElectionAlarm = timers[0];
                        Console.WriteLine("Adjusted S{0} timeout forward", server.ID);
                    }
                }
            }
        }

        public SimulationServer GetLeader()
        {
            return _servers.FirstOrDefault(x => x.State == ServerState.Leader);
        }

        private static int[] GetPeers(int server, int serverCount)
        {
            var peerIndex = 0;
            var peers = new int[serverCount - 1];
            for (var i = 0; i < serverCount; i++)
                if (i != server - 1)
                    peers[peerIndex++] = i + 1;

            return peers;
        }

        public static SimulationModel SetupFreshScenario()
        {
            var model = new SimulationModel();
            var peers = new int[Settings.NUM_SERVERS];

            model._servers = new List<SimulationServer>(Settings.NUM_SERVERS);
            for (var i = 0; i < Settings.NUM_SERVERS; i++)
                model._servers.Add(new SimulationServer(i + 1, GetPeers(i + 1, Settings.NUM_SERVERS)));

            for (var i = 0; i < Settings.NUM_SERVERS; i++)
                model._servers[i].Restart(model);

            return model;
        }

        public static SimulationModel SetupLogReplicationScenario()
        {
            var model = new SimulationModel();

            model._servers = new List<SimulationServer>(Settings.NUM_SERVERS);
            for (var i = 0; i < Settings.NUM_SERVERS; i++)
                model._servers.Add(new SimulationServer(i + 1, GetPeers(i + 1, Settings.NUM_SERVERS)));

            for (var i = 1; i < Settings.NUM_SERVERS; i++)
                model._servers[i].Restart(model);

            model._servers[0].Timeout(model);

            for (var i = 1; i < Settings.NUM_SERVERS; i++)
                model._servers[i].Term = 2;

            for (var i = 1; i < Settings.NUM_SERVERS; i++)
                model._servers[i].VotedFor = 1;

            for (var i = 0; i < Settings.NUM_SERVERS; i++)
                model._servers[0].Peers[i].VotedGranted = true;

            for (var i = 2; i < Settings.NUM_SERVERS; i++)
                model._servers[i].Stop(model);

            model._servers[0].BecomeLeader(model);
            model._servers[0].ClientRequest(model);
            model._servers[0].ClientRequest(model);
            model._servers[0].ClientRequest(model);

            return model;
        }
    }
}
