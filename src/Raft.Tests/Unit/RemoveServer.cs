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
    public class RemoveServer
    {
#if DEBUG
        static RemoveServer()
        {
            if (System.Diagnostics.Debugger.IsAttached)
                Console.SetOut(new DebugWriter());


        }
#endif
        [TestMethod]
        public void RemoveServer_OK()
        {
            using (var s1 = Helper.CreateServer())
            using (var s2 = Helper.CreateServer())
            {
                var transport = new MemoryTransport();

                s1.Initialize(new MemoryLog(), transport, s2.ID);
                s2.Initialize(new MemoryLog(), transport, s1.ID);

                s1.ChangeState(new LeaderState(s1));

                s1.Advance();
                s2.Advance();

                s2.ChangeState(new LeaveState(s2));
                var count = 200;
                while (count-- > 0)
                {
                    s1.Advance();
                    s2.Advance();
                }

                Assert.AreEqual(0, s1.Clients.Count());
                Assert.AreEqual(typeof(StoppedState), s2.CurrentState.GetType());
            }
        }
    }
}
