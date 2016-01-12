using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raft.Logs;
using Raft.States;
using Raft.Transports;
using T = Raft.Tests.MemoryTransportImpl;

namespace Raft.Tests.Unit
{
    //[TestClass]
    //public class MemoryBasicNodeTest : BasicNodeTest<MemoryTransportImpl>
    //{

    //}

    //[TestClass]
    //public class UdpBasicNodeTest : BasicNodeTest<UdpTransportImpl>
    //{

    //}

    [TestClass]
    public  class BasicNodeTest//<T>
        //where T : TransportImpl, new()
    {
#if DEBUG
        static BasicNodeTest()
        {
            if (System.Diagnostics.Debugger.IsAttached)
                Console.SetOut(new DebugWriter());
        }
#endif

        [TestMethod]
        public void IsFollower()
        {
            using (var mock = new T())
            using (var server = mock.CreateServer())
            {
                server.Initialize();
                server.Advance(1);

                Assert.AreEqual(typeof(FollowerState), server.CurrentState.GetType());
            }
        }

        [TestMethod]
        public void IsCandidate()
        {
            using (var mock = new T())
            using (var server = mock.CreateServer())
            {
                server.Initialize();
                var ticks = server.PersistedStore.ELECTION_TIMEOUT * 2;
                while (ticks-- > 0 && server.CurrentState is FollowerState)
                    server.Advance();

                Assert.AreEqual(typeof(CandidateState), server.CurrentState.GetType());
            }
        }

        [TestMethod]
        public void IsLeader()
        {
            using (var mock = new T())
            using (var server = mock.CreateServer())
            {
                server.Initialize();

                var count = server.PersistedStore.ELECTION_TIMEOUT * 2;
                while (count-- > 0)
                {
                    server.Advance();
                }

                Assert.AreEqual(typeof(LeaderState), server.CurrentState.GetType());
            }
        }

        [TestMethod]
        public void NodeGrantsVote()
        {
            using (var mock = new T())
            using (var s1 = mock.CreateServer())
            using (var s2 = mock.CreateServer())
            {

                s1.Initialize(s2.ID);
                s2.Initialize(s1.ID);

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
            using (var mock = new T())
            using (var s1 = mock.CreateServer())
            using (var s2 = mock.CreateServer())
            {

                s1.Initialize(s2.ID);
                s2.Initialize(s1.ID);

                s1.PersistedStore.Term = 1;
                s2.PersistedStore.Term = 1;

                s1.PersistedStore.CreateData(s1, new byte[] { 0 });
                s1.PersistedStore.Term = 2;
                s1.PersistedStore.CreateData(s1, new byte[] { 1 });

                s2.PersistedStore.CreateData(s2, new byte[] { 0 });
                s2.PersistedStore.CreateData(s2, new byte[] { 1 });
                s2.PersistedStore.CreateData(s2, new byte[] { 2 });
                s2.PersistedStore.CreateData(s2, new byte[] { 3 });

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
            using (var mock = new T())
            using (var s1 = mock.CreateServer())
            using (var s2 = mock.CreateServer())
            {

                s1.Initialize(s2.ID);
                s2.Initialize(s1.ID);

                s1.PersistedStore.Term = 1;
                s2.PersistedStore.Term = 1;

                s1.PersistedStore.CreateData(s1, new byte[] { 0 });
                s1.PersistedStore.CreateData(s1, new byte[] { 1 });

                s2.PersistedStore.CreateData(s2, new byte[] { 0 });
                s2.PersistedStore.CreateData(s2, new byte[] { 1 });
                s2.PersistedStore.CreateData(s2, new byte[] { 2 });
                s2.PersistedStore.CreateData(s2, new byte[] { 3 });

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
            using (var mock = new T())
            using (var s1 = mock.CreateServer())
            using (var s2 = mock.CreateServer())
            {

                s1.Initialize(s2.ID);
                s2.Initialize(s1.ID);

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
            using (var mock = new T())
            using (var s1 = mock.CreateServer())
            using (var s2 = mock.CreateServer())
            {

                s1.Initialize(s2.ID);
                s2.Initialize(s1.ID);

                s1.PersistedStore.Term = 1;
                s2.PersistedStore.Term = 1;

                s1.PersistedStore.CreateData(s1, new byte[] { 0 });
                s1.PersistedStore.CreateData(s1, new byte[] { 1 });

                s2.PersistedStore.CreateData(s2, new byte[] { 0 });
                s2.PersistedStore.CreateData(s2, new byte[] { 1 });

                s1.ChangeState(new CandidateState(s1)); // will push s1 to term 2

                s2.Advance();
                s1.Advance();

                Assert.AreEqual(s1.ID, s2.PersistedStore.VotedFor);
                Assert.AreEqual(true, s1.Clients.First().VoteGranted);
            }
        }

        [TestMethod]
        public void TestChunkedLogs()
        {
            using (var mock = new T())
            using (var s1 = mock.CreateServer())
            using (var s2 = mock.CreateServer())
            {

                s1.Initialize(s2.ID);
                s2.Initialize(s1.ID);

                s1.ChangeState(new LeaderState(s1));

                //establish leader
                s1.Advance();
                s2.Advance();

                //create entry
                var data = new byte[1024 * 1024];
                for (var i = 0; i < data.Length; i++)
                    data[i] = (byte)i;

                s1.PersistedStore.CreateData(s1, data);

                var count = 200;
                while (count-- > 0)
                {
                    s1.Advance();
                    s2.Advance();
                }

                var addition = (data.Length % Log.MAX_LOG_ENTRY_SIZE) == 0 ? 0u : 1u;
                Assert.AreEqual((uint)(data.Length / Log.MAX_LOG_ENTRY_SIZE) + addition, s1.CommitIndex);
                Assert.AreEqual((uint)(data.Length / Log.MAX_LOG_ENTRY_SIZE) + addition, s2.CommitIndex);

                Assert.AreEqual(0u, s1.PersistedStore[s1.PersistedStore.Length].Flag3);
                Assert.AreEqual((uint)data.Length, s1.PersistedStore[s1.PersistedStore.Length].Flag4);
                Assert.AreEqual(0u, s2.PersistedStore[s2.PersistedStore.Length].Flag3);
                Assert.AreEqual((uint)data.Length, s2.PersistedStore[s2.PersistedStore.Length].Flag4);

                for (var i = 0; i < (uint)(data.Length / Log.MAX_LOG_ENTRY_SIZE) - 1; i++)
                {
                    Assert.AreEqual((uint)(i * Log.MAX_LOG_ENTRY_SIZE), s1.PersistedStore[(uint)i + 1].ChunkOffset);
                    Assert.AreEqual((uint)Log.MAX_LOG_ENTRY_SIZE, s1.PersistedStore[(uint)i + 1].ChunkSize);
                    Assert.AreEqual((uint)(i * Log.MAX_LOG_ENTRY_SIZE), s2.PersistedStore[(uint)i + 1].ChunkOffset);
                    Assert.AreEqual((uint)Log.MAX_LOG_ENTRY_SIZE, s2.PersistedStore[(uint)i + 1].ChunkSize);
                    Assert.AreEqual(0u, s1.PersistedStore[(uint)i + 1].Flag3);
                    Assert.AreEqual(0u, s2.PersistedStore[(uint)i + 1].Flag3);
                    Assert.AreEqual((uint)data.Length, s1.PersistedStore[(uint)i + 1].Flag4);
                    Assert.AreEqual((uint)data.Length, s2.PersistedStore[(uint)i + 1].Flag4);
                }

                var logIndex = s2.PersistedStore[s2.PersistedStore.Length];
                Assert.AreEqual(1u, logIndex.Term);

                var storedData = new byte[logIndex.Flag4];
                Assert.AreEqual(data.Length, storedData.Length);

                using (var stream = s2.PersistedStore.GetDataStream())
                {
                    stream.Seek(logIndex.Flag3, System.IO.SeekOrigin.Begin);
                    stream.Read(storedData, 0, storedData.Length);
                }

                for (var i = 0; i < data.Length; i++)
                {
                    Assert.AreEqual(data[i], storedData[i]);
                }

            }
        }

        [TestMethod]
        public void TestVerify()
        {
            using (var mock = new T())
            using (var s1 = mock.CreateServer())
            using (var s2 = mock.CreateServer())
            {

                s1.Initialize(s2.ID);
                s2.Initialize(s1.ID);

                s1.PersistedStore.Term = 1;
                s2.PersistedStore.Term = 1;

                s1.PersistedStore.CreateData(s1, new byte[] { 0 });
                s1.PersistedStore.CreateData(s1, new byte[] { 1 });

                s2.PersistedStore.CreateData(s2, new byte[] { 0 });
                s2.PersistedStore.CreateData(s2, new byte[] { 1 });

                s1.ChangeState(new LeaderState(s1));
                s1.Advance();

                var verifyState = new VerifyState(0, new Client(s2, s1.ID), s2);
                s2.ChangeState(verifyState);
                s2.Advance();

                var index = 50;
                while (index-- > 0)
                {
                    s1.Advance();
                    s2.Advance();
                }

                Assert.AreEqual(true, verifyState.IsVerified);
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
