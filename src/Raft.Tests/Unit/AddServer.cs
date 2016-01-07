using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raft.Logs;
using Raft.Messages;
using Raft.States;
using Raft.Transports;

namespace Raft.Tests.Unit
{
    [TestClass]
    public class MemoryAddServer : AddServer<MemoryTransportImpl> { }

    [TestClass]
    public class UdpAddServer : AddServer<UdpTransportImpl> { }

    [TestClass]
    public class TcpAddServer : AddServer<TcpTransportImpl> { }

    [TestClass]
    public abstract class AddServer<T>
        where T : TransportImpl, new()
    {
#if DEBUG
        static AddServer()
        {
            if (System.Diagnostics.Debugger.IsAttached)
                Console.SetOut(new DebugWriter());
        }
#endif

        [TestMethod]
        public void AddServer_ReplyNotLeaderIfNotLeader()
        {
            using (var mock = new T())
            using (var s1 = mock.CreateServer())
            using (var s2 = mock.CreateServer())
            {

                s1.Initialize();
                s2.Initialize();
                //s1.ChangeState(new CandidateState(s1)); // will push s1 to term 2

                //s1.Advance();

                var testState = new TestState(s2);
                s2.ChangeState(testState);

                var request = new AddServerRequest()
                {
                    From = s2.ID
                };

                s2.Transport.SendMessage(new Client(s2, s1.ID), request);
                s1.Advance();
                s2.Advance();

                //s2.Transport.SendMessage(new Client())

                Assert.AreEqual(typeof(AddServerReply), testState.LastMessage.GetType());
                Assert.AreEqual(AddServerStatus.NotLeader, ((AddServerReply)testState.LastMessage).Status);
            }
        }

        [TestMethod]
        public void AddServer_ReplicateToLog()
        {
            using (var mock = new T())
            using (var s1 = mock.CreateServer())
            using (var s2 = mock.CreateServer())
            {

                s1.Initialize();
                s2.Initialize();

                s1.ChangeState(new LeaderState(s1)); // will push s1 to term 2
                s1.PersistedStore.AddServer(s1, s1.ID);

                // applies its own entry and advances commit
                s1.Advance();

                // this sends out an add request
                s2.ChangeState(new JoinState(s2, new Client(s2, s1.ID)));

                var count = 200;
                while (count-- > 0)
                {
                    s1.Advance();
                    s2.Advance();
                }

                Assert.AreEqual(2, s1.Majority);
                Assert.AreEqual(2, s2.Majority);
                Assert.IsTrue(s1.ID.Equals(s2.GetClient(s1.ID).ID));
                Assert.IsTrue(s2.ID.Equals(s1.GetClient(s2.ID).ID));

                Assert.IsTrue(s1.PersistedStore.Clients.Any(x => x.Equals(s2.ID)));
                Assert.IsTrue(s2.PersistedStore.Clients.Any(x => x.Equals(s1.ID)));
            }
        }

        [TestMethod]
        public void AddServer_ReplicateToLogWithRollback()
        {
            using (var mock = new T())
            using (var s1 = mock.CreateServer())
            using (var s2 = mock.CreateServer())
            {

                s1.Initialize();
                s2.Initialize();

                s1.ChangeState(new LeaderState(s1)); // will push s1 to term 2
                s1.PersistedStore.AddServer(s1, s1.ID);

                // applies its own entry and advances commit
                s1.Advance();
                s1.PersistedStore.CreateData(s1, new byte[] { 1 });

                // this sends out an add request
                s2.ChangeState(new JoinState(s2, new Client(s2, s1.ID)));

                var count = 50;
                while (count-- > 0)
                {
                    s1.Advance();
                    s2.Advance();
                }

                s1.PersistedStore.Term++;
                s1.PersistedStore.CreateData(s1, new byte[] { 2 });
                //s1.PersistedStore.CreateData(s1, new byte[65400 * 3]);
                s2.PersistedStore.CreateData(s1, new byte[65400 * 3]);

                count = 200;
                while (count-- > 0)
                {
                    s1.Advance();
                    s2.Advance();
                }

                Console.WriteLine("");
                Console.WriteLine("");
                Log.DumpLog(s1.PersistedStore);
                Console.WriteLine("");
                Console.WriteLine("");
                Log.DumpLog(s2.PersistedStore);

                Assert.AreEqual(2, s1.Majority);
                Assert.AreEqual(2, s2.Majority);
                Assert.IsTrue(s1.ID.Equals(s2.GetClient(s1.ID).ID));
                Assert.IsTrue(s2.ID.Equals(s1.GetClient(s2.ID).ID));

                Assert.IsTrue(s1.PersistedStore.Clients.Any(x => x.Equals(s2.ID)));
                Assert.IsTrue(s2.PersistedStore.Clients.Any(x => x.Equals(s1.ID)));

                Assert.IsTrue(Log.AreEqual(s1.PersistedStore, s2.PersistedStore));
            }
        }

        //[TestMethod]
        //public void AddServer_ReplicateToLogUdp()
        //{
        //    using (var s1 = mock.CreateServer())
        //    using (var s2 = mock.CreateServer())
        //    {
        //        s1.Initialize();
        //        s2.Initialize();

        //        s1.ChangeState(new LeaderState(s1)); // will push s1 to term 2
        //        s1.PersistedStore.AddServer(s1, s1.ID);

        //        // applies its own entry and advances commit
        //        s1.Advance();

        //        // this sends out an add request
        //        s2.ChangeState(new JoinState(s2, new Client(s2, s1.ID)));

        //        var timer = Stopwatch.StartNew();
        //        var lastTick = 0L;
        //        while (lastTick < 1000)
        //        {
        //            var currentTick = timer.ElapsedMilliseconds;
        //            if (lastTick != currentTick)
        //            {
        //                s1.Advance();
        //                s2.Advance();
        //                lastTick = currentTick;
        //            }

        //            System.Threading.Thread.Sleep(1);
        //        }

        //        Assert.AreEqual(2, s1.Majority);
        //        Assert.AreEqual(2, s2.Majority);
        //        Assert.IsTrue(s1.ID.Equals(s2.GetClient(s1.ID).ID));
        //        Assert.IsTrue(s2.ID.Equals(s1.GetClient(s2.ID).ID));
        //        Assert.IsTrue(s1.PersistedStore.Clients.Any(x => x.Equals(s2.ID)));
        //        Assert.IsTrue(s2.PersistedStore.Clients.Any(x => x.Equals(s1.ID)));
        //    }
        //}

        [TestMethod]
        public void AddServer_Timesout()
        {
            using (var mock = new T())
            using (var s1 = mock.CreateServer())
            using (var s2 = mock.CreateServer())
            {

                s1.Initialize();
                s2.Initialize();

                s1.ChangeState(new LeaderState(s1)); // will push s1 to term 2
                s1.PersistedStore.AddServer(s1, s1.ID);

                // applies its own entry and advances commit
                s1.Advance();

                // this sends out an add request
                s2.ChangeState(new JoinState(s2, new Client(s2, s1.ID)));

                // reads add request and sends its self as the first entry
                s1.Advance();

                var testState = new TestState(s2);
                s2.ChangeState(testState);

                s1.Advance(s1.PersistedStore.ELECTION_TIMEOUT);

                s2.Advance();

                Assert.AreEqual(typeof(AddServerReply), testState.LastMessage.GetType());
                Assert.AreEqual(AddServerStatus.TimedOut, ((AddServerReply)testState.LastMessage).Status);
            }
        }

        [TestMethod]
        public void AddServer_StillGrantsVote()
        {
            using (var mock = new T())
            using (var s1 = mock.CreateServer())
            using (var s2 = mock.CreateServer())
            {

                s1.Initialize();
                s2.Initialize();

                s1.ChangeState(new LeaderState(s1)); // will push s1 to term 2
                s1.PersistedStore.AddServer(s1, s1.ID);

                // applies its own entry and advances commit
                s1.Advance();

                // this sends out an add request
                s2.ChangeState(new JoinState(s2, new Client(s2, s1.ID)));

                // these are needed because the first append entries will fail
                // and s2 will return where its nextIndex is
                s1.Advance();
                s2.Advance();

                // reads add request and sends its self as the first entry
                s1.Advance();

                // s2 now has s1 as an added entry and has applied the index
                s2.Advance();

                // s1 sees that s2 is up to date and adds log entry for s2 and locks config
                s1.Advance();

                s2.Advance();
                s1.Advance();

                s1.ChangeState(new CandidateState(s1));

                //var count = 50;
                //while (count-- > 0)
                {
                    s2.Advance();
                    s1.Advance();
                }

                Assert.AreEqual(s1.ID, s2.PersistedStore.VotedFor);
                Assert.AreEqual(true, s1.Clients.First().VoteGranted);
            }
        }

    }
}
