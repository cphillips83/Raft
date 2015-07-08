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
        [OperationContractAttribute(Action = "SampleMethod", ReplyAction = "ReplySampleMethod")]
        string SampleMethod(string msg);

        [OperationContractAttribute(Action = "VoteRequest", ReplyAction = "ReplyVoteRequest")]
        VoteReply VoteRequest(VoteRequest request);

        //[OperationContractAttribute(AsyncPattern = true)]
        //IAsyncResult BeginSampleMethod(string msg, AsyncCallback callback, object asyncState);

        ////Note: There is no OperationContractAttribute for the end method.
        //string EndSampleMethod(IAsyncResult result);

        //[OperationContractAttribute(AsyncPattern = true, Action="ServiceAsync")]
        //IAsyncResult BeginServiceAsyncMethod(string msg, AsyncCallback callback, object asyncState);

        //// Note: There is no OperationContractAttribute for the end method.
        //string EndServiceAsyncMethod(IAsyncResult result);

        //[OperationContract(AsyncPattern=true)]
        //IAsyncResult BeginRequestVote(VoteRequest request);

        //VoteReply EndRequestVote(IAsyncResult result);

        //[OperationContract(AsyncPattern = true)]
        //IAsyncResult BeginAppendEntries(AppendEntriesRequest request);

        //AppendEntriesReply EndAppendEntries(IAsyncResult result);
    }

    [ServiceContract()]
    public interface INodeProxyAsync : INodeProxy
    {
        [OperationContractAttribute(AsyncPattern = true, Action = "SampleMethod", ReplyAction = "ReplySampleMethod")]
        IAsyncResult BeginSampleMethod(string msg, AsyncCallback callback, object asyncState);

        //Note: There is no OperationContractAttribute for the end method.
        string EndSampleMethod(IAsyncResult result);

        [OperationContractAttribute(AsyncPattern = true, Action = "VoteRequest", ReplyAction = "ReplyVoteRequest")]
        IAsyncResult BeginVoteRequest(VoteRequest request, AsyncCallback callback, object asyncState);

        VoteReply EndVoteRequest(IAsyncResult r);
    }

    public class Node
    {
        private int _id;

        protected Node(int id)
        {
            _id = id;
        }

        public int ID { get { return _id; } }
    }

    public class Client : Node, IDisposable
    {
        public const int RPC_TIMEOUT = 50;

        private static ChannelFactory<INodeProxyAsync> g_channelFactory;
        private static BasicHttpBinding g_httpBinding;

        static Client()
        {
            g_httpBinding = new BasicHttpBinding();
            g_channelFactory = new ChannelFactory<INodeProxyAsync>(g_httpBinding);
        }

        private Server _server;
        private INodeProxyAsync _nodeProxy;
        private EndpointAddress _endPoint;
        private IAsyncResult _currentRPC;
        private long _nextHeartBeat;
        private bool _voteGranted;
        private uint _matchIndex;
        private uint _nextIndex;
        private Queue<object> _outgoingMessages = new Queue<object>();
        private Queue<object> _incomingMessages = new Queue<object>();

        public long NextHeartBeat { get { return _nextHeartBeat; } }
        public bool WaitingForResponse { get { return _currentRPC != null; } }
        public uint MatchIndex { get { return _matchIndex; } }
        public uint NextIndex { get { return _nextIndex; } }
        public bool VoteGranted { get { return _voteGranted; } }

        protected Client(Server server, int id, EndpointAddress endPoint)
            : base(id)
        {
            _server = server;
            _endPoint = endPoint;
            _nodeProxy = g_channelFactory.CreateChannel(endPoint);

            var channel = (IClientChannel)_nodeProxy;
            channel.OperationTimeout = TimeSpan.FromMilliseconds(RPC_TIMEOUT);
        }

        public void SendRequestVote()
        {
            _outgoingMessages.Enqueue(new VoteRequest() { From = 0, LastTerm = 0, LogLength = 0, Term = 0 });
        }

        public void Update()
        {
            if (_currentRPC != null && _currentRPC.IsCompleted)
            {
                try
                {
                    var message = _currentRPC.AsyncState;
                    if (message is VoteRequest)
                        _incomingMessages.Enqueue(_nodeProxy.EndVoteRequest(_currentRPC));
                }
                catch
                {
                    //TODO: Track client unavailable 
                }
            }

            if (_currentRPC == null && _outgoingMessages.Count > 0)
            {
                var message = _outgoingMessages.Dequeue();
                if (message is VoteRequest)
                    _currentRPC = _nodeProxy.BeginVoteRequest((VoteRequest)message, null, message);
            }

            while (_incomingMessages.Count > 0)
            {
                var message = _incomingMessages.Peek();
                if (!_server.ProcessMessage(message))
                    return;

                _incomingMessages.Dequeue();
            }
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
        public virtual bool VoteReply(VoteReply reply) { return true; }
    }

    public class Follower : State
    {

    }

    public class InitializeState : State
    {
        public override void Enter()
        {
            //load persisted state
        }

        public override void Update()
        {
            _server.InitializePersistedStore();
            _server.ChangeState(new Follower());
        }
    }

    public class FollowerState : State
    {
        private long _heatbeatTimeout = long.MaxValue;

        public override void Enter()
        {
            resetHeartbeat();
        }

        public override void Update()
        {
            if (_heatbeatTimeout > _server.TimeInMS)
                _server.ChangeState(new CandidateState());
            else
            {
                resetHeartbeat();
                _server.PersistedStore.UpdateState(_server.PersistedStore.Term + 1, _server.ID);

                //only request from peers that are allowed to vote
                foreach (var client in _server.Voters)
                    client.Reset();

                Console.WriteLine("{0}: Starting new election for term {1}", _server.ID, _server.PersistedStore.Term);
            }
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

    }

    public class LeaderState : State
    {

    }

    public class Server : Node
    {
        private Random _random;
        private Stopwatch _timer;
        private string _dataDir;
        private PersistedStore _persistedStore;
        private List<Client> _clients = new List<Client>();
        private State _currentState;

        public long TimeInMS { get { return (long)_timer.ElapsedMilliseconds; } }

        public PersistedStore PersistedStore { get { return _persistedStore; } }

        public Random Random { get { return _random; } }

        public IEnumerable<Client> Voters
        {
            get
            {
                foreach (var client in _clients)
                    yield return client;
            }
        }

        public Server(int id, string dataDir)
            : base(id)
        {
            _random = new Random((int)DateTime.UtcNow.Ticks ^ id);
            _dataDir = dataDir;
            _timer = Stopwatch.StartNew();
        }

        public void InitializePersistedStore()
        {
            if (_persistedStore == null)
                _persistedStore = new PersistedStore(_dataDir);

        }

        public void ChangeState(State newState)
        {
            if (_currentState != null)
                _currentState.Exit();

            _currentState = newState;
            _currentState.Enter();
        }

        public bool ProcessMessage(object message)
        {
            if (message is VoteRequest)
                return _currentState.VoteRequest((VoteRequest)message);
            else if (message is VoteReply)
                return _currentState.VoteReply((VoteReply)message);
            //TODO: Unhandled message, we want to return true to remove it from the queue
            return true;
        }

        public void Update()
        {

        }
        //public int LastTerm { get { return 0; } }
        //public int LastLog { get { return 0; } }
    }

    public class Cluster
    {

    }




    public class MyService : INodeProxy
    {
        private Random rand = new Random();
        public string SampleMethod(string msg)
        {
            Console.WriteLine("Called synchronous sample method with \"{0}\"", msg);
            //System.Threading.Thread.Sleep(rand.Next(0, 1000));
            return "The sychronous service greets you: " + msg;
        }

        public VoteReply VoteRequest(VoteRequest request)
        {
            return new VoteReply() { From = 1, Granted = false, Term = 0 };
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
            //new TypedServiceReference
            var endpoint = new Uri("http://localhost:7741/CategoryServiceHost.svc");
            var host = new ServiceHost(typeof(MyService), endpoint);

            var binding = new BasicHttpBinding();

            host.AddServiceEndpoint(typeof(INodeProxy), binding, endpoint);

            host.Description.Behaviors.Add(new ServiceMetadataBehavior()
            {
                HttpGetEnabled = true,
                MetadataExporter = { PolicyVersion = PolicyVersion.Policy15 },
            });

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

            //create client proxy from factory
            var pClient = ClientFactory.CreateClient(typeof(INodeProxy));
            var channel = (IClientChannel)pClient;
            channel.OperationTimeout = TimeSpan.FromMilliseconds(500);
            {
                //Console.WriteLine(pClient.SampleMethod("simple"));
            }
            {
                for (var i = 0; i < 10; i++)
                {
                    try
                    {
                        var r = pClient.BeginSampleMethod("sample", null, null);
                        while (!r.IsCompleted)
                        {
                            //Console.WriteLine(r.IsCompleted);
                            System.Threading.Thread.Sleep(10);
                        }
                        Console.WriteLine(pClient.EndSampleMethod(r));
                        Console.WriteLine(r.IsCompleted);
                    }
                    catch
                    {
                        Console.WriteLine("Failed");
                    }
                }
            }
            {
                var r = pClient.BeginVoteRequest(new VoteRequest() { From = 2, LastTerm = 1, Term = 1, LogLength = 1 }, null, 1);
                while (!r.IsCompleted)
                {
                    Console.WriteLine(r.IsCompleted);
                    System.Threading.Thread.Sleep(0);
                }
                Console.WriteLine(pClient.EndVoteRequest(r));
                Console.WriteLine(r.AsyncState);
            }
            //{
            //    var r = pClient.BeginServiceAsyncMethod("test", null, null);
            //    Console.WriteLine(pClient.EndServiceAsyncMethod(r));
            //}
            Console.Read();
            //Console.WriteLine(pClient.DoSomething());
            //((IClientChannel)pClient).RemoteAddress
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
