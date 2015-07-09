using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Raft.Logs;
using Raft.Messages;
using Raft.States;
using Raft.Transports;

namespace Raft
{
    //[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Server : /*INodeProxy,*/ IDisposable
    {
        private static object _syncLock = new object();

        //private NetPeer _rpc;

        private int _id;
        private uint _commitIndex = 0;
        private Random _random;
        //private Stopwatch _timer;
        private Configuration _config;
        private Log _persistedStore;
        private ITransport _transport;
        private List<Client> _clients = new List<Client>();
        private AbstractState _currentState;
        private long _tick = 0;

        public int ID { get { return _id; } }
        public uint CommitIndex { get { return _commitIndex; } set { _commitIndex = value; } }

        public long Tick { get { return _tick; } }
        //public long RawTimeInMS { get { return _timer.ElapsedMilliseconds; } }
        public Configuration Config { get { return _config; } }

        public ITransport Transport { get { return _transport; } }
        //public NetPeer IO { get { return _rpc; } }

        public Log PersistedStore { get { return _persistedStore; } }

        public Random Random { get { return _random; } }

        public AbstractState CurrentState { get { return _currentState; } }

        public int Majority { get { return ((_clients.Count() + 1) / 2) + 1; } }

        public IEnumerable<Client> Voters
        {
            get
            {
                foreach (var client in _clients)
                    yield return client;
            }
        }

        public IEnumerable<Client> Clients
        {
            get
            {
                foreach (var client in _clients)
                    yield return client;
            }
        }

        public Server(Configuration config)
        {
            _config = config;
            _id = config.ID;
            _random = new Random((int)DateTime.UtcNow.Ticks ^ _id);
            //_dataDir = dataDir;
            _currentState = new StoppedState(this);
        }

        public Client GetClient(int id)
        {
            return _clients.First(x => x.ID == id);
        }

        public void Initialize(Log log, ITransport transport, params Configuration[] clients)
        {
            if (_persistedStore == null)
            {
                _persistedStore = log;
                _persistedStore.Initialize();

                if (clients != null && clients.Length > 0)
                    _persistedStore.UpdateClients(clients);

                foreach (var client in _persistedStore.Clients)
                    _clients.Add(new Client(this, client));

                //_timer = Stopwatch.StartNew();

                ChangeState(new FollowerState(this));

                //var binding = new BasicHttpBinding();
                //var custom = new CustomBinding(binding);
                //custom.Elements.Find<HttpTransportBindingElement>().KeepAliveEnabled = false;

                //var host = new ServiceHost(this, endpoint);

                //host.AddServiceEndpoint(typeof(INodeProxy), custom, endpoint);

                //host.Description.Behaviors.Add(new ServiceMetadataBehavior()
                //{
                //    HttpGetEnabled = true,
                //    MetadataExporter = { PolicyVersion = PolicyVersion.Policy15 },
                //});

                //host.Open();
                _transport = transport;
                transport.Start(_config);

            }

        }

        public void ChangeState(AbstractState newState)
        {
            if (_currentState != null)
                _currentState.Exit();

            _currentState = newState;
            _currentState.Enter();
        }

        public void AdvanceCommits()
        {
            var _persistedStore = PersistedStore;

            var matchIndexes = new uint[_clients.Count + 1];
            matchIndexes[matchIndexes.Length - 1] = _persistedStore.Length;
            for (var i = 0; i < _clients.Count; i++)
                matchIndexes[i] = _clients[i].MatchIndex;

            Array.Sort(matchIndexes);

            var n = matchIndexes[_clients.Count / 2];
            if (_persistedStore.GetTerm(n) == _persistedStore.Term)
                CommitIndex2(Math.Max(CommitIndex, n));
        }

        public void Process()
        {
            _transport.Process(this);

            foreach (var client in Clients)
                client.Update();

            _currentState.Update();
        }

        public void Advance()
        {
            _tick++;
            Process();
        }

        public void Advance(long ticks)
        {
            while (ticks-- > 0)
                Advance();
        }

        public void AdvanceTo(long tick)
        {
            while (_tick < tick)
                Advance();
        }

        public void Dispose()
        {
            if (_transport != null)
                _transport.Shutdown();

            if (_persistedStore != null)
                _persistedStore.Dispose();

            _clients.Clear();

            _transport = null;
            _persistedStore = null;
            _currentState = null;
        }

        public bool CommitIndex2(uint newCommitIndex)
        {
            if (newCommitIndex != _commitIndex)
            {
                Console.WriteLine("{0}: Advancing commit index from {1} to {2}", _id, _commitIndex, newCommitIndex);
                _commitIndex = newCommitIndex;
                //for (var i = _commitIndex; i < newCommitIndex; i++)
                //{
                //    if (i == _stateMachine.LastCommitApplied)
                //    {
                //        LogIndex index;
                //        if (!_persistedState.GetIndex(i + 1, out index))
                //            return false;

                //        if (index.Type == LogIndexType.StateMachine)
                //        {
                //            using (var br = new BinaryReader(new MemoryStream(_persistedState.GetData(index))))
                //            {
                //                var name = br.ReadString();
                //                _stateMachine.Apply(name, i + 1);
                //            }
                //        }
                //        _commitIndex++;
                //    }
                //    // make commit index 
                //    _commitIndex++;
                //}
                return true;
            }

            return false;
        }
    }

}
