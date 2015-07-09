using System;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raft.Logs;
using Raft.States;
using Raft.Transports;

namespace Raft.Tests
{
    public static class Helper
    {
        public static int id;

        public static Server CreateServer()
        {
            var sid = ++id;
            var port = sid + 7000;

            return new Server(new Configuration(sid, IPAddress.Loopback, port));
        }
    }

    [TestClass]
    public class Test
    {
        //[TestMethod]
        //public void AppendLogEntries()
        //{
        //    using (var server = Helper.CreateServer())
        //    {

        //    }
        //}

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

                s1.Advance();

                s2.Advance();

                s1.Advance();
                //System.Threading.Thread.Sleep(10);

                Assert.AreEqual(s2.PersistedStore.VotedFor, s1.ID);
                Assert.AreEqual(s1.Clients.First().VoteGranted, true);
            }

        }
    }

}
