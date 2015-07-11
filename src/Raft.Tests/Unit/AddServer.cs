using System;
using System.Collections.Generic;
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
    public class AddServer
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
            using (var s1 = Helper.CreateServer())
            using (var s2 = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                s1.Initialize(new MemoryLog(), transport);
                s2.Initialize(new MemoryLog(), transport);
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
            using (var s1 = Helper.CreateServer())
            using (var s2 = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                s1.Initialize(new MemoryLog(), transport);
                s2.Initialize(new MemoryLog(), transport, true);

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

                // heart beat went out before the new log entry, needs to respond to it
                s2.Advance();

                // tells s2 about the new log entry
                s1.Advance(50);

                // s2 sees that is now part of the majority, needs to commit log
                // so that s1 can apply it
                s2.Advance();

                // s1 sees its commited on majority (2)
                s1.Advance(50);

                // s2 sees that s1 has committed its add entry
                // s2 switches to follower and is now part of the cluster
                s2.Advance();

                Assert.AreEqual(2, s1.Majority);
                Assert.AreEqual(2, s2.Majority);
                Assert.IsTrue(s1.ID.Equals(s2.GetClient(s1.ID).ID));
                Assert.IsTrue(s2.ID.Equals(s1.GetClient(s2.ID).ID));
                Assert.IsTrue(s1.ID.Equals(s2.PersistedStore.Clients.First()));
                Assert.IsTrue(s2.ID.Equals(s1.PersistedStore.Clients.First()));
            }
        }

        [TestMethod]
        public void AddServer_Timesout()
        {
            using (var s1 = Helper.CreateServer())
            using (var s2 = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                s1.Initialize(new MemoryLog(), transport);
                s2.Initialize(new MemoryLog(), transport, true);

                s1.ChangeState(new LeaderState(s1)); // will push s1 to term 2

                // applies its own entry and advances commit
                s1.Advance();

                // this sends out an add request
                s2.ChangeState(new JoinState(s2, new Client(s2, s1.ID)));

                // reads add request and sends its self as the first entry
                s1.Advance();

                var testState = new TestState(s2);
                s2.ChangeState(testState);

                s1.Advance(100);

                s2.Advance();

                Assert.AreEqual(typeof(AddServerReply), testState.LastMessage.GetType());
                Assert.AreEqual(AddServerStatus.TimedOut, ((AddServerReply)testState.LastMessage).Status);
            }
        }

        [TestMethod]
        public void AddServer_StillGrantsVote()
        {
            using (var s1 = Helper.CreateServer())
            using (var s2 = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                s1.Initialize(new MemoryLog(), transport);
                s2.Initialize(new MemoryLog(), transport, true);

                s1.ChangeState(new LeaderState(s1)); // will push s1 to term 2

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

                s1.ChangeState(new CandidateState(s1));

                s2.Advance();
                s1.Advance();

                Assert.AreEqual(s1.ID, s2.PersistedStore.VotedFor);
                Assert.AreEqual(true, s1.Clients.First().VoteGranted);
            }
        }

    }
}
