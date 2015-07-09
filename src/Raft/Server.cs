using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Raft.Messages;
using Raft.Settings;
using Raft.States;

namespace Raft
{
    //[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Server : /*INodeProxy,*/ IDisposable
    {
        private static object _syncLock = new object();

        private NetPeer _rpc;
        private int _id;
        private uint _commitIndex = 0;
        private Random _random;
        //private Stopwatch _timer;
        private string _dataDir;
        private INodeSettings _nodeSettings;
        private PersistedStore _persistedStore;
        public List<Client> _clients = new List<Client>();
        private AbstractState _currentState;
        private long _tick = 0;

        public int ID { get { return _id; } }
        public uint CommitIndex { get { return _commitIndex; } set { _commitIndex = value; } }

        public long Tick { get { return _tick; } }
        //public long RawTimeInMS { get { return _timer.ElapsedMilliseconds; } }
        public INodeSettings Settings { get { return _nodeSettings; } }

        public NetPeer IO { get { return _rpc; } }

        public PersistedStore PersistedStore { get { return _persistedStore; } }

        public Random Random { get { return _random; } }

        public AbstractState CurrentState { get { return _currentState; } }

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

        public Server(INodeSettings nodeSettings)
        {
            _nodeSettings = nodeSettings;
            _id = nodeSettings.ID;
            _random = new Random((int)DateTime.UtcNow.Ticks ^ _id);
            //_dataDir = dataDir;
            _currentState = new StoppedState(this);
        }

        public void Initialize()
        {
            if (_persistedStore == null)
            {
                _persistedStore = new PersistedStore(this);
                _persistedStore.Initialize();

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

                NetPeerConfiguration config = new NetPeerConfiguration("masterserver");
                config.SetMessageTypeEnabled(NetIncomingMessageType.UnconnectedData, true);
                config.Port = _nodeSettings.Port;

                _rpc = new NetPeer(config);
                _rpc.Start();
            }

        }

        public void ChangeState(AbstractState newState)
        {
            if (_currentState != null)
                _currentState.Exit();

            _currentState = newState;
            _currentState.Enter();
        }

        public void Process()
        {
            processNetworkIO();
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
            if (_rpc != null)
                _rpc.Shutdown(string.Empty);

            _rpc = null;
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

        private void processNetworkIO()
        {
            NetIncomingMessage msg;
            while ((msg = _rpc.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.UnconnectedData:
                        switch ((MessageTypes)msg.ReadByte())
                        {
                            case MessageTypes.VoteRequest:
                                {
                                    var voteRequest = Messages.VoteRequest.Read(msg);
                                    var client = _clients.FirstOrDefault(x => x.ID == voteRequest.From);

                                    if (!_currentState.VoteRequest(client, voteRequest))
                                        _currentState.VoteRequest(client, voteRequest);
                                }
                                break;
                            case MessageTypes.VoteReply:
                                {
                                    var voteReply = Messages.VoteReply.Read(msg);
                                    var client = _clients.FirstOrDefault(x => x.ID == voteReply.From);

                                    if (!_currentState.VoteReply(client, voteReply))
                                        _currentState.VoteReply(client, voteReply);
                                }
                                break;
                            case MessageTypes.AppendEntriesRequest:
                                {
                                    var appendEntriesRequest = Messages.AppendEntriesRequest.Read(msg);
                                    var client = _clients.FirstOrDefault(x => x.ID == appendEntriesRequest.From);

                                    if (!_currentState.AppendEntriesRequest(client, appendEntriesRequest))
                                        _currentState.AppendEntriesRequest(client, appendEntriesRequest);
                                }
                                break;
                            case MessageTypes.AppendEntriesReply:
                                {
                                    var appendEntriesReply = Messages.AppendEntriesReply.Read(msg);
                                    var client = _clients.FirstOrDefault(x => x.ID == appendEntriesReply.From);

                                    if (!_currentState.AppendEntriesReply(client, appendEntriesReply))
                                        _currentState.AppendEntriesReply(client, appendEntriesReply);
                                }
                                break;
                        }
                        break;
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.ErrorMessage:
                        // print diagnostics message
                        Console.WriteLine(msg.ReadString());
                        break;
                }
            }
        }
    }

}
