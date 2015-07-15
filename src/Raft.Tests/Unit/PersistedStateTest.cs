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
    public class PersistedStateTest
    {
        [TestMethod]
        public void TermChangeResetsVoteFor()
        {
            var test = new MemoryLog();
            test.Initialize();

            test.Term = 1;
            test.VotedFor = new IPEndPoint(IPAddress.Loopback, 123);

            test.Term = 2;
            Assert.AreEqual(2u, test.Term);
            Assert.AreEqual(null, test.VotedFor);
        }

        [TestMethod]
        public void CanAppendLog()
        {
            var test = new MemoryLog();
            test.Initialize();

            test.Term = 1;
            test.Create(null, new[] { (byte)5 });

            test.Term = 2;
            test.Create(null, new[] { (byte)6 });

            Assert.AreEqual(2u, test.Length);
            Assert.AreEqual(1u, test[1].Term);
            Assert.AreEqual(2u, test[2].Term);
        }

        [TestMethod]
        public void LastPersistedInfoWorks()
        {
            var test = new MemoryLog();
            test.Initialize();

            test.Term = 1;
            test.Create(null, new[] { (byte)5 });

            Assert.AreEqual(1u, test.GetLastTerm());
            Assert.AreEqual(1u, test.GetLastIndex());
        }

        [TestMethod]
        public void LogIsBetter()
        {
            var test = new MemoryLog();
            test.Initialize();
            test.Term = 1;

            Assert.AreEqual(false, test.LogIsBetter(0, 1));

            test.Create(null, new[] { (byte)5 });

            Assert.AreEqual(true, test.LogIsBetter(0, 1));
            Assert.AreEqual(false, test.LogIsBetter(1, 1));
        }

        [TestMethod]
        public void AppendEntriesDataValid()
        {
            var test = new MemoryLog();
            test.Initialize();

            test.Term = 1;
            test.Create(null, new[] { (byte)5 });

            var index = test[1];
            var data = test.GetData(index);
            Assert.AreEqual((byte)5, data[0]);
        }
    }
}
