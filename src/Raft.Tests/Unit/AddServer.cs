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
        [TestMethod]
        public void ReplyNotLeaderIfNotLeader()
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
                    From = s2.ID,
                    EndPoint = new IPEndPoint(s2.Config.IP, s2.Config.Port)
                };

                s2.Transport.SendMessage(new Client(s2, s1.Config), request);
                s1.Advance();
                s2.Advance();

                //s2.Transport.SendMessage(new Client())

                Assert.AreEqual(typeof(AddServerReply), testState.LastMessage.GetType());
                Assert.AreEqual(AddServerStatus.NotLeader, ((AddServerReply)testState.LastMessage).Status);
            }
        }
    }
}
