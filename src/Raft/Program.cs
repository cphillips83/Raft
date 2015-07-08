using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Raft.Messages;

namespace Raft
{
    //Haystack tech talk http://www.downvids.net/haystack-tech-talk-4-28-2009--375887.html

    /* Design Ideas
     *  Modeling after haystack
     *  
     * Data Store
     *  Each store will have several physical volumes
     *  Each physical volume can be in multiple logical volumes
     *  Logical volumes can span across data centers
     *  
     * 
     */

    ////An AsyncResult that completes as soon as it is instantiated.
    //internal class CompletedAsyncResult : AsyncResult
    //{
    //    public CompletedAsyncResult(AsyncCallback callback, object state)
    //        : base(callback, state)
    //    {
    //        Complete(true);
    //    }

    //    public static void End(IAsyncResult result)
    //    {
    //        AsyncResult.End<CompletedAsyncResult>(result);
    //    }
    //}

    //internal class CompletedAsyncResult<T> : AsyncResult
    //{
    //    private T _data;

    //    public CompletedAsyncResult(T data, AsyncCallback callback, object state)
    //        : base(callback, state)
    //    {
    //        _data = data;
    //        Complete(true);
    //    }

    //    public static T End(IAsyncResult result)
    //    {
    //        CompletedAsyncResult<T> completedResult = AsyncResult.End<CompletedAsyncResult<T>>(result);
    //        return completedResult._data;
    //    }
    //}

    //internal class CompletedAsyncResult<TResult, TParameter> : AsyncResult
    //{
    //    private TResult _resultData;
    //    private TParameter _parameter;

    //    public CompletedAsyncResult(TResult resultData, TParameter parameter, AsyncCallback callback, object state)
    //        : base(callback, state)
    //    {
    //        _resultData = resultData;
    //        _parameter = parameter;
    //        Complete(true);
    //    }

    //    public static TResult End(IAsyncResult result, out TParameter parameter)
    //    {
    //        CompletedAsyncResult<TResult, TParameter> completedResult = AsyncResult.End<CompletedAsyncResult<TResult, TParameter>>(result);
    //        parameter = completedResult._parameter;
    //        return completedResult._resultData;
    //    }
    //}
    [ServiceContract()]
    public interface INodeProxy
    {
        [OperationContractAttribute(IsOneWay= true, AsyncPattern = true, Action = "VoteRequest", ReplyAction = "ReplyVoteRequest")]
        void VoteRequest(VoteRequest request);

        [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "VoteReply", ReplyAction = "ReplyVoteReply")]
        void VoteReply(VoteReply reply);

        [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "AppendEntriesRequest", ReplyAction = "ReplyAppendEntriesRequest")]
        void AppendEntriesRequest(AppendEntriesRequest request);

        [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "AppendEntriesReply", ReplyAction = "ReplyAppendEntriesReply")]
        void AppendEntriesReply(AppendEntriesReply reply);
    }

    [ServiceContract()]
    public interface INodeProxyAsync : INodeProxy
    {
        [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "VoteRequest", ReplyAction = "ReplyVoteRequest")]
        IAsyncResult BeginVoteRequest(VoteRequest request, AsyncCallback callback, object asyncState);

        void EndVoteRequest(IAsyncResult r);

        [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "VoteReply", ReplyAction = "ReplyVoteReply")]
        IAsyncResult BeginVoteReply(VoteReply reply, AsyncCallback callback, object asyncState);

        void EndVoteReply(IAsyncResult r);

        [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "AppendEntriesRequest", ReplyAction = "ReplyAppendEntriesRequest")]
        IAsyncResult BeginAppendEntriesRequest(AppendEntriesRequest request, AsyncCallback callback, object asyncState);

        void EndAppendEntries(IAsyncResult r);

        [OperationContractAttribute(IsOneWay = true, AsyncPattern = true, Action = "AppendEntriesReply", ReplyAction = "ReplyAppendEntriesReply")]
        IAsyncResult BeginAppendEntriesReply(AppendEntriesRequest request, AsyncCallback callback, object asyncState);

        void EndAppendEntriesReply(IAsyncResult r);
    }

    public class Client : IDisposable
    {
        public const int RPC_TIMEOUT = 50;

        private static ChannelFactory<INodeProxyAsync> g_channelFactory;
        private static BasicHttpBinding g_httpBinding;

        static Client()
        {
            g_httpBinding = new BasicHttpBinding();
            g_channelFactory = new ChannelFactory<INodeProxyAsync>(g_httpBinding);
        }

        private int _id;
        private Server _server;
        private INodeProxyAsync _nodeProxy;
        private EndpointAddress _endPoint;
        //private IAsyncResult _currentRPC;
        private long _nextHeartBeat;
        private bool _voteGranted;
        private uint _matchIndex;
        private uint _nextIndex;
        private long _rpcDue;
        private Queue<IAsyncResult> _outgoingMessages = new Queue<IAsyncResult>();
        //private Queue<object> _incomingMessages = new Queue<object>();
        //private Stack<IAsyncResult> _discardRPC = new Stack<IAsyncResult>();

        public int ID { get { return _id; } }
        public long NextHeartBeat { get { return _nextHeartBeat; } set { _nextHeartBeat = value; } }
        //public bool WaitingForResponse { get { return _currentRPC != null; } }
        public uint MatchIndex { get { return _matchIndex; } }
        public uint NextIndex { get { return _nextIndex; } set { _nextIndex = value; } }
        public bool VoteGranted { get { return _voteGranted; } }
        public long RpcDue { get { return _rpcDue; } set { _rpcDue = long.MaxValue; } }
        public bool ReadyToSend { get { return _rpcDue <= _server.TimeInMS; } }

        protected Client(Server server, int id, EndpointAddress endPoint)
        {
            _id = id;
            _server = server;
            _endPoint = endPoint;
            _nodeProxy = g_channelFactory.CreateChannel(endPoint);

            var channel = (IClientChannel)_nodeProxy;
            channel.OperationTimeout = TimeSpan.FromMilliseconds(RPC_TIMEOUT);
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
                LogLength = lastLogIndex
            };

            var ar = _nodeProxy.BeginVoteRequest(message, null, message);
            _outgoingMessages.Enqueue(ar);
            _rpcDue = _server.TimeInMS + RPC_TIMEOUT;
        }

        public void SendVoteReply(bool granted)
        {
            var message = new VoteReply()
            {
                From = _server.ID,
                Term = _server.PersistedStore.Term,
                Granted = granted
            };

            var ar = _nodeProxy.BeginVoteReply(message, null, message);
            _outgoingMessages.Enqueue(ar);
            _rpcDue = _server.TimeInMS + RPC_TIMEOUT;
        }

        public void Update()
        {
            while (_outgoingMessages.Count > 0)
            {
                var ar = _outgoingMessages.Peek();
                if (!ar.IsCompleted)
                    break;

                SendComplete(ar);
                _outgoingMessages.Dequeue();
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

        public void Dispose()
        {
            if (_nodeProxy != null)
            {
                var channel = (IClientChannel)_nodeProxy;
                channel.OperationTimeout = TimeSpan.FromMilliseconds(15);
                channel.Close();
            }

            _nodeProxy = null;
        }

        private void SendComplete(IAsyncResult ar)
        {
            try
            {
                var message = ar.AsyncState;
                if (message is VoteRequest)
                    _nodeProxy.EndVoteRequest(ar);
                else if (message is VoteReply)
                    _nodeProxy.EndVoteReply(ar);

                //throw unhandled message?
            }
            catch
            {
                //TODO: Track client unavailable 
            }
        }
       
    }

    public abstract class State
    {
        protected Server _server;
        protected State(Server server)
        {
            _server = server;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void Exit() { }
        public virtual bool VoteRequest(VoteRequest request) { return true; }
        //public virtual bool AppendEntries(AppendEntriesRequest request) { }
        public virtual bool VoteReply(VoteReply reply) { return true; }
    }

    public class StoppedState : State
    {
        public StoppedState(Server _server) : base(_server) { }
    }

    //public class InitializeState : State
    //{
    //    public InitializeState(Server server) : base(server) { }

    //    public override void Enter()
    //    {
    //        //load persisted state
    //    }

    //    public override void Update()
    //    {
    //        _server.InitializePersistedStore();
    //        _server.ChangeState(new FollowerState(_server));
    //    }
    //}

    public class FollowerState : State
    {
        private long _heatbeatTimeout = long.MaxValue;

        public FollowerState(Server server) : base(server) { }

        public override void Enter()
        {
            resetHeartbeat();
        }

        public override void Update()
        {
            if (_server.TimeInMS > _heatbeatTimeout)
                _server.ChangeState(new CandidateState(_server));

        }

        private void resetHeartbeat()
        {
            var timeout = _server.PersistedStore.ELECTION_TIMEOUT;
            var randomTimeout = _server.Random.Next(timeout, timeout + timeout) / 2;
            _heatbeatTimeout = _server.TimeInMS + randomTimeout;
        }
    }

    public class CandidateState : State
    {
        private long _electionTimeout = long.MaxValue;

        public CandidateState(Server server) : base(server) { }

        public override void Enter()
        {
            var timeout = _server.PersistedStore.ELECTION_TIMEOUT;
            var randomTimeout = _server.Random.Next(timeout, timeout + timeout) / 2;
            _electionTimeout = _server.TimeInMS + randomTimeout;

            _server.PersistedStore.UpdateState(_server.PersistedStore.Term + 1, _server.ID);

            Console.WriteLine("{0}: Starting new election for term {1}", _server.ID, _server.PersistedStore.Term);

            //only request from peers that are allowed to vote
            foreach (var client in _server.Voters)
                client.Reset();

            foreach (var client in _server.Voters)
                if (client.ReadyToSend)
                    client.SendVoteRequest();
        }

        public override void Update()
        {
            if (_electionTimeout > _server.TimeInMS)
                _server.ChangeState(new CandidateState(_server));
            else
            {
                var votes = _server.Voters.Count(x => x.VoteGranted) + 1;
                var votesNeeded = ((_server.Voters.Count() + 1) / 2) + 1;
                if (votes >= votesNeeded)
                    _server.ChangeState(new LeaderState(_server));
            }
        }

    }

    public class LeaderState : State
    {
        public LeaderState(Server server) : base(server) { }

        public override void Enter()
        {
            var votes = _server.Voters.Count(x => x.VoteGranted) + 1;
            Console.WriteLine("{0}: I am now leader of term {2} with {1} votes", _server.ID, votes, _server.PersistedStore.Term);
            foreach (var client in _server.Clients)
            {
                client.NextIndex = _server.PersistedStore.Length + 1;
                client.NextHeartBeat = 0;
            }
        }

        public override void Update()
        {
            foreach (var client in _server.Clients)
            {
                if(client.NextHeartBeat <= _server.TimeInMS ||
                    (client.NextIndex <= _server.PersistedStore.Length && client.ReadyToSend))
                {
                    //Console.WriteLine("Send heart beat")
                //var prevIndex = peer.NextIndex - 1;
                //var lastIndex = Math.Min(prevIndex + BATCH_SIZE, _persistedState.Length);
                //if (peer.MatchIndex + 1 < peer.NextIndex)
                //    lastIndex = prevIndex;

                //var entries = _persistedState.GetEntries(prevIndex, lastIndex);
                //if (entries != null && entries.Length > 0)
                //    Console.WriteLine("{0}: Send AppendEnties[{1}-{2}] to {3}", _id, prevIndex, lastIndex, peer.ID);

                //peer.RpcDue = model.Tick + RPC_TIMEOUT;
                //peer.HeartBeartDue = model.Tick + (ELECTION_TIMEOUT / 2);
                //model.SendRequest(peer, new AppendEntriesRequest()
                //{
                //    From = _id,
                //    Term = _persistedState.Term,
                //    PrevIndex = prevIndex,
                //    PrevTerm = _persistedState.GetTerm(prevIndex),
                //    Entries = entries,
                //    CommitIndex = Math.Min(_commitIndex, lastIndex)
                //});
                }
            }
        }
    }

    public class Server : INodeProxy, IDisposable
    {
        private static object _syncLock = new object();

        private int _id;
        private Random _random;
        private Stopwatch _timer;
        private string _dataDir;
        private PersistedStore _persistedStore;
        private List<Client> _clients = new List<Client>();
        private State _currentState;
        private long _tick = 0;

        public int ID { get { return _id; } }

        public long TimeInMS { get { return _tick; } }

        public PersistedStore PersistedStore { get { return _persistedStore; } }

        public Random Random { get { return _random; } }

        public State CurrentState { get { return _currentState; } }

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

        public Server(int id, string dataDir)
        {
            _id = id;
            _random = new Random((int)DateTime.UtcNow.Ticks ^ id);
            _dataDir = dataDir;
            _currentState = new StoppedState(this);
        }

        public void Initialize()
        {
            if (_persistedStore == null)
            {
                _persistedStore = new PersistedStore(_dataDir);
                _persistedStore.Initialize();

                _timer = Stopwatch.StartNew();

                ChangeState(new FollowerState(this));
            }
        }

        public void ChangeState(State newState)
        {
            if (_currentState != null)
                _currentState.Exit();

            _currentState = newState;
            _currentState.Enter();
        }

        public void ProcessMessage(object message)
        {
            if (message is VoteReply)
                _currentState.VoteReply((VoteReply)message);
        }

        public void Update()
        {
                _tick = (long)_timer.ElapsedMilliseconds;

                _currentState.Update();

                foreach (var client in Clients)
                    client.Update();
        }

        public void Dispose()
        {

        }

        public void VoteRequest(VoteRequest request)
        {
            throw new NotImplementedException();
        }

        public void VoteReply(VoteReply reply)
        {
            throw new NotImplementedException();
        }

        public void AppendEntriesRequest(AppendEntriesRequest request)
        {
            throw new NotImplementedException();
        }

        public void AppendEntriesReply(AppendEntriesReply reply)
        {
            throw new NotImplementedException();
        }
    }


    //Factory class for client proxy
    public abstract class ClientFactory
    {
        public static INodeProxyAsync CreateClient(Type targetType)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            //Get the address of the service from configuration or some other mechanism - Not shown here
            EndpointAddress address = new EndpointAddress("http://localhost:7741/CategoryServiceHost.svc");

            var factory3 = new ChannelFactory<INodeProxyAsync>(binding, address);
            return factory3.CreateChannel();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var dataDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "server\\1");
            if (System.IO.Directory.Exists(dataDir))
                System.IO.Directory.Delete(dataDir, true);

            var server = new Server(1, dataDir);
            server.Initialize();

            System.Threading.Thread.Sleep(200);
            server.Update();
            System.Threading.Thread.Sleep(200);
            server.Update();

            Console.Read();
            ////new TypedServiceReference
            //var endpoint = new Uri("http://localhost:7741/CategoryServiceHost.svc");
            //var host = new ServiceHost(typeof(MyService), endpoint);

            //var binding = new BasicHttpBinding();

            //host.AddServiceEndpoint(typeof(INodeProxy), binding, endpoint);

            //host.Description.Behaviors.Add(new ServiceMetadataBehavior()
            //{
            //    HttpGetEnabled = true,
            //    MetadataExporter = { PolicyVersion = PolicyVersion.Policy15 },
            //});

            //host.Open();
            //while (true)
            //{
            //    System.Threading.Thread.Sleep(0);
            //}
            Test2();
            //TestConsole();
        }
        public static void Test2()
        {

            ////create client proxy from factory
            //var pClient = ClientFactory.CreateClient(typeof(INodeProxy));
            //var channel = (IClientChannel)pClient;
            //channel.OperationTimeout = TimeSpan.FromMilliseconds(500);
            //{
            //    //Console.WriteLine(pClient.SampleMethod("simple"));
            //}
            //{
            //    for (var i = 0; i < 10; i++)
            //    {
            //        try
            //        {
            //            var r = pClient.BeginSampleMethod("sample", null, null);
            //            while (!r.IsCompleted)
            //            {
            //                //Console.WriteLine(r.IsCompleted);
            //                System.Threading.Thread.Sleep(10);
            //            }
            //            Console.WriteLine(pClient.EndSampleMethod(r));
            //            Console.WriteLine(r.IsCompleted);
            //        }
            //        catch
            //        {
            //            Console.WriteLine("Failed");
            //        }
            //    }
            //}
            //{
            //    var r = pClient.BeginVoteRequest(new VoteRequest() { From = 2, LastTerm = 1, Term = 1, LogLength = 1 }, null, 1);
            //    while (!r.IsCompleted)
            //    {
            //        Console.WriteLine(r.IsCompleted);
            //        System.Threading.Thread.Sleep(0);
            //    }
            //    Console.WriteLine(pClient.EndVoteRequest(r));
            //    Console.WriteLine(r.AsyncState);
            //}
            ////{
            ////    var r = pClient.BeginServiceAsyncMethod("test", null, null);
            ////    Console.WriteLine(pClient.EndServiceAsyncMethod(r));
            ////}
            //Console.Read();
            ////Console.WriteLine(pClient.DoSomething());
            ////((IClientChannel)pClient).RemoteAddress
        }


        static void TestConsole()
        {
            var running = true;
            var model = SimulationModel.SetupFreshScenario();
            while (running)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey();
                    switch (key.KeyChar)
                    {
                        case 'x': running = false; break;
                        case 'k':
                            {
                                var leader = model.GetLeader();
                                if (leader != null)
                                    leader.Stop(model);
                            }
                            break;
                        case 'u':
                            model.ResumeAllStopped();
                            break;
                        case 'r': model.ClientRequest(); break;
                        case 'a':
                            {
                                //add server to cluster
                                var leader = model.GetLeader();
                                if (leader != null)
                                    model.JoinServer(leader);
                            }
                            break;
                    }
                }
                model.Advance();
                System.Threading.Thread.Sleep(1);
            }
        }

    }

}
