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
    public class Client : IDisposable
    {

        //private static ChannelFactory<INodeProxyAsync> g_channelFactory;
        //private static BasicHttpBinding g_httpBinding;

        //static Client()
        //{
        //    g_httpBinding = new BasicHttpBinding();
        //    g_httpBinding.AllowCookies = false;
        //    //g_httpBinding.SendTimeout = TimeSpan.FromMilliseconds(RPC_TIMEOUT);

        //    var custom = new CustomBinding(g_httpBinding);
        //    custom.Elements.Find<HttpTransportBindingElement>().KeepAliveEnabled = false;

        //    g_channelFactory = new ChannelFactory<INodeProxyAsync>(custom);
        //}

        private int _id;
        private Configuration _config;
        private IPEndPoint _endPoint;
        private Server _server;
        //private INodeProxyAsync _nodeProxy;
        //private EndpointAddress _endPoint;
        //private IAsyncResult _currentRPC;
        private long _nextHeartBeat;
        private bool _voteGranted;
        private uint _matchIndex;
        private uint _nextIndex;
        private long _rpcDue;
        private MSG _currentmsg;
        //private Queue<IAsyncResult> _outgoingMessages = new Queue<IAsyncResult>();
        //private Queue<object> _incomingMessages = new Queue<object>();
        //private Stack<IAsyncResult> _discardRPC = new Stack<IAsyncResult>();

        public int ID { get { return _id; } }
        public long NextHeartBeat { get { return _nextHeartBeat; } set { _nextHeartBeat = value; } }
        //public bool WaitingForResponse { get { return _currentRPC != null; } }
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

        public void Initialize()
        {
            //_nodeProxy = g_channelFactory.CreateChannel(_endPoint);

            //var channel = (IClientChannel)_nodeProxy;
            //channel.AllowOutputBatching = false;
            //channel.OperationTimeout = TimeSpan.FromMilliseconds(RPC_TIMEOUT);
            //channel.Open();
        }

        private struct MSG
        {
            public long start, end, finished, id;
            public override string ToString()
            {
                return string.Format("{{ id: {0}, start: {1}, end: {2}, finished: {3} }}", id, start, end, finished);
            }
        }

        private long messageId = 0;
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

            var netMsg = _server.IO.CreateMessage();
            VoteRequest.Write(message, netMsg);
            _server.IO.SendUnconnectedMessage(netMsg, _endPoint);
            //var m = new MSG() { start = _server.RawTimeInMS, end = 0, finished = 0, id = messageId++ };
            //_currentmsg = m;
            ////    _nodeProxy.VoteRequest(message);
            //try
            //{
            //    _nodeProxy.BeginVoteRequest(message, new AsyncCallback(ar =>
            //           {
            //               _rpcDue = 0;
            //               m.end = _server.RawTimeInMS;
            //               try
            //               {

            //                   //Console.WriteLine("success");
            //                   _nodeProxy.EndVoteRequest(ar);
            //                   m.finished = _server.RawTimeInMS;
            //                   //Console.WriteLine(m);
            //                   Console.WriteLine("{0}: SUCCESS - {1}", _server.ID, m);
            //                   //Console.WriteLine("{0}: completed in {1}", _server.ID, (long)ar.AsyncState);
            //               }
            //               catch (Exception ex)
            //               {
            //                   m.finished = _server.RawTimeInMS;
            //                   //wcf acts very odd with one way sending and timeouts
            //                   //sometimes the exception is thrown here and other times its 
            //                   //thrown at Begin
            //                   Console.WriteLine("{0}: END - {1}", _server.ID, m);
            //               }
            //           }
            //       )
            //   , null);
            //}
            //catch (Exception ex)
            //{
            //    m.finished = _server.RawTimeInMS;
            //    Console.WriteLine("{0}: BEGIN - {1}", _server.ID, m);
            //    //Console.WriteLine("{0}: failed in {1} : {2}", _server.ID, mid, ex.Message);

            //}
            ////Console.WriteLine("{0}: sent end: {1}", _server.ID, _server.RawTimeInMS);
            ////}
            ////catch (Exception ex)
            ////{
            ////    //wcf acts very odd with one way sending and timeouts
            ////    //sometimes the exception is thrown here and other times its 
            ////    //thrown at End
            ////    Console.WriteLine("failed begin ex: {0}", ex.Message);

            ////}
            //// ar = _nodeProxy.BeginVoteReply(message, null, message);
            ////Console.WriteLine(_outgoingMessages.Count);
            ////_outgoingMessages.Enqueue(ar);
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
            var netMsg = _server.IO.CreateMessage();
            VoteReply.Write(message, netMsg);
            _server.IO.SendUnconnectedMessage(netMsg, _endPoint);
            //_nodeProxy.BeginVoteReply(message, new AsyncCallback(ar =>
            //        {
            //            try
            //            {
            //                _nodeProxy.EndVoteReply(ar);
            //            }
            //            catch
            //            {
            //                Console.WriteLine("failed");
            //            }
            //        }
            //    )
            //, null);
            // ar = _nodeProxy.BeginVoteReply(message, null, message);
            //_outgoingMessages.Enqueue(ar);
            //_rpcDue = _server.TimeInMS + RPC_TIMEOUT;
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
            //    Console.WriteLine("{0}: Send AppendEnties[{1}-{2}] to {3}", _server.ID, prevIndex, lastIndex, ID);

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

            var netMsg = _server.IO.CreateMessage();
            AppendEntriesRequest.Write(message, netMsg);
            _server.IO.SendUnconnectedMessage(netMsg, _endPoint);

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

            var netMsg = _server.IO.CreateMessage();
            AppendEntriesReply.Write(message, netMsg);
            _server.IO.SendUnconnectedMessage(netMsg, _endPoint);

            //_rpcDue = _server.Tick + RPC_TIMEOUT;
        }

        public void Update()
        {
            if (_rpcDue > 0 && _rpcDue <= _server.Tick)
            {
                Console.WriteLine("{0}: dropped at {1} - {2}", _server.ID, _server.Tick, _currentmsg);
                _rpcDue = 0;
            }
            //while (_outgoingMessages.Count > 0)
            //{
            //    var ar = _outgoingMessages.Peek();
            //    if (!ar.IsCompleted)
            //        break;

            //    SendComplete(ar);
            //    _outgoingMessages.Dequeue();
            //}
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
            //if (_nodeProxy != null)
            //{
            //    var channel = (IClientChannel)_nodeProxy;
            //    //channel.OperationTimeout = TimeSpan.FromMilliseconds(15);
            //    channel.Close();
            //}

            //_nodeProxy = null;
        }

        //private void SendComplete(IAsyncResult ar)
        //{
        //    try
        //    {
        //        var message = ar.AsyncState;
        //        if (message is VoteRequest)
        //            _nodeProxy.EndVoteRequest(ar);
        //        else if (message is VoteReply)
        //            _nodeProxy.EndVoteReply(ar);

        //        //throw unhandled message?
        //    }
        //    catch
        //    {
        //        //TODO: Track client unavailable 
        //    }
        //}

    }

}
