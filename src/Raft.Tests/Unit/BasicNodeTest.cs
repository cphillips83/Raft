using System;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raft.Logs;
using Raft.States;
using Raft.Transports;

namespace Raft.Tests.Unit
{
    [TestClass]
    public class BasicNodeTest
    {
        [TestMethod]
        public void IsFollower()
        {
            using (var server = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                server.Initialize(new MemoryLog(), transport);
                server.Advance(1);

                Assert.AreEqual(typeof(FollowerState), server.CurrentState.GetType());
            }
        }

        [TestMethod]
        public void IsCandidate()
        {
            using (var server = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                server.Initialize(new MemoryLog(), transport);
                var ticks = server.PersistedStore.ELECTION_TIMEOUT;
                while (ticks-- > 0 && server.CurrentState is FollowerState)
                    server.Advance();

                Assert.AreEqual(typeof(CandidateState), server.CurrentState.GetType());
            }
        }

        [TestMethod]
        public void IsLeader()
        {
            using (var server = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                server.Initialize(new MemoryLog(), transport);
                server.Advance(server.PersistedStore.ELECTION_TIMEOUT);
                server.Advance(server.PersistedStore.ELECTION_TIMEOUT);

                Assert.AreEqual(typeof(LeaderState), server.CurrentState.GetType());
            }
        }

        [TestMethod]
        public void NodeGrantsVote()
        {
            using (var s1 = Helper.CreateServer())
            using (var s2 = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                s1.Initialize(new MemoryLog(), transport, s2.Config);
                s2.Initialize(new MemoryLog(), transport, s1.Config);

                s1.ChangeState(new CandidateState(s1));

                s2.Advance();

                s1.Advance();

                Assert.AreEqual(s1.ID, s2.PersistedStore.VotedFor);
                Assert.AreEqual(true, s1.Clients.First().VoteGranted);
            }
        }

        [TestMethod]
        public void NodeGrantsVoteWithLongerLogOlderTerm()
        {
            using (var s1 = Helper.CreateServer())
            using (var s2 = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                s1.Initialize(new MemoryLog(), transport, s2.Config);
                s2.Initialize(new MemoryLog(), transport, s1.Config);

                s1.PersistedStore.Term = 1;
                s2.PersistedStore.Term = 1;

                s1.PersistedStore.Create(new byte[] { 0 });
                s1.PersistedStore.Term = 2;
                s1.PersistedStore.Create(new byte[] { 1 });

                s2.PersistedStore.Create(new byte[] { 0 });
                s2.PersistedStore.Create(new byte[] { 1 });
                s2.PersistedStore.Create(new byte[] { 2 });
                s2.PersistedStore.Create(new byte[] { 3 });

                s1.ChangeState(new CandidateState(s1)); // will push s1 to term 2

                s2.Advance();
                s1.Advance();

                Assert.AreEqual(s1.ID, s2.PersistedStore.VotedFor);
                Assert.AreEqual(true, s1.Clients.First().VoteGranted);
            }
        }


        [TestMethod]
        public void NodeDoesntGrantVoteWithSameTermLongerLog()
        {
            using (var s1 = Helper.CreateServer())
            using (var s2 = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                s1.Initialize(new MemoryLog(), transport, s2.Config);
                s2.Initialize(new MemoryLog(), transport, s1.Config);

                s1.PersistedStore.Term = 1;
                s2.PersistedStore.Term = 1;

                s1.PersistedStore.Create(new byte[] { 0 });
                s1.PersistedStore.Create(new byte[] { 1 });

                s2.PersistedStore.Create(new byte[] { 0 });
                s2.PersistedStore.Create(new byte[] { 1 });
                s2.PersistedStore.Create(new byte[] { 2 });
                s2.PersistedStore.Create(new byte[] { 3 });

                s1.ChangeState(new CandidateState(s1)); // will push s1 to term 2

                s2.Advance();
                s1.Advance();

                Assert.AreEqual(null, s2.PersistedStore.VotedFor);
                Assert.AreEqual(false, s1.Clients.First().VoteGranted);
            }
        }

        [TestMethod]
        public void NodeDoesntGrantVoteWithNewerTerm()
        {
            using (var s1 = Helper.CreateServer())
            using (var s2 = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                s1.Initialize(new MemoryLog(), transport, s2.Config);
                s2.Initialize(new MemoryLog(), transport, s1.Config);

                s1.PersistedStore.Term = 1;
                s2.PersistedStore.Term = 3;

                s1.ChangeState(new CandidateState(s1)); // will push s1 to term 2

                s2.Advance();
                s1.Advance();

                Assert.AreEqual(null, s2.PersistedStore.VotedFor);
                Assert.AreEqual(false, s1.Clients.First().VoteGranted);
            }
        }

        [TestMethod]
        public void NodeGrantsVoteWithSameLog()
        {
            using (var s1 = Helper.CreateServer())
            using (var s2 = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                s1.Initialize(new MemoryLog(), transport, s2.Config);
                s2.Initialize(new MemoryLog(), transport, s1.Config);

                s1.PersistedStore.Term = 1;
                s2.PersistedStore.Term = 1;

                s1.PersistedStore.Create(new byte[] { 0 });
                s1.PersistedStore.Create(new byte[] { 1 });

                s2.PersistedStore.Create(new byte[] { 0 });
                s2.PersistedStore.Create(new byte[] { 1 });

                s1.ChangeState(new CandidateState(s1)); // will push s1 to term 2

                s2.Advance();
                s1.Advance();

                Assert.AreEqual(s1.ID, s2.PersistedStore.VotedFor);
                Assert.AreEqual(true, s1.Clients.First().VoteGranted);
            }
        }

        [TestMethod]
        public void MajorityWorks()
        {
            TestMajority(2, 2);
            TestMajority(3, 2);
            TestMajority(4, 3);
            TestMajority(5, 3);
        }

        private void TestMajority(int count, int majority)
        {
            var servers = Helper.CreateServers(count);

            for (var i = 0; i < servers.Length; i++)
                Assert.AreEqual(majority, servers[i].Majority);

            Helper.CleanupServers(servers);
        }
    }

}
