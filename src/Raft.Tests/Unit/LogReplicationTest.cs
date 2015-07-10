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
    public class LogReplicationTest
    {
        [TestMethod]
        public void LogReplicated()
        {
            using (var s1 = Helper.CreateServer())
            using (var s2 = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                s1.Initialize(new MemoryLog(), transport, s2.ID);
                s2.Initialize(new MemoryLog(), transport, s1.ID);

                s1.PersistedStore.Term = 1;
                s2.PersistedStore.Term = 1;

                s1.ChangeState(new CandidateState(s1)); // will push s1 to term 2

                s2.Advance();
                s1.Advance();

                s1.PersistedStore.Create(s1, new byte[] { 5 });
                s1.Advance();
                s2.Advance();

                LogIndex logIndex;
                var index = s2.PersistedStore.GetLastIndex(out logIndex);

                //log replication check
                Assert.AreNotEqual(0, index);
                Assert.AreEqual(2, logIndex.Term);
                Assert.AreEqual(LogIndexType.RawData, logIndex.Type);
                Assert.AreEqual(0u, logIndex.Offset);
                Assert.AreEqual(1u, logIndex.Size);

                var data = s2.PersistedStore.GetData(logIndex);
                Assert.AreEqual(1, data.Length);
                Assert.AreEqual((byte)5, data[0]);
               
            }
        }

        [TestMethod]
        public void LogCommitIndex()
        {
            using (var s1 = Helper.CreateServer())
            using (var s2 = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                s1.Initialize(new MemoryLog(), transport, s2.ID);
                s2.Initialize(new MemoryLog(), transport, s1.ID);

                s1.PersistedStore.Term = 1;
                s2.PersistedStore.Term = 1;

                s1.ChangeState(new CandidateState(s1)); // will push s1 to term 2

                s2.Advance();
                s1.Advance();

                s1.PersistedStore.Create(s1, new byte[] { 5 });
                s1.Advance();
                s2.Advance();

                //log commit index check
                s1.Advance(50);
                s2.Advance();
                Assert.AreEqual(1u, s1.CommitIndex);
                Assert.AreEqual(1u, s2.CommitIndex);
            }
        }

    }
}
