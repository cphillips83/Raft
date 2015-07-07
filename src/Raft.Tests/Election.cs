using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Raft.Tests
{
    public static class Global
    {
        public static int id;
    }

    [TestClass]
    public class Election2
    {
        
        [TestMethod]
        public void Join_Single_Server()
        {
            using (var model = new SimulationModel())
            {
                var s1 = new SimulationServer(++Global.id, deleteData: true);
                model.AddServer(s1);

                s1.Restart(model);
                model.Advance(Server.ELECTION_TIMEOUT * 10);

                Assert.AreEqual(ServerState.Leader, s1.State, "Server failed to become leader");
            }
        }
    }

    [TestClass]
    public class Election
    {
        [TestMethod]
        public void Single_Server_Election()
        {
            using (var model = new SimulationModel())
            {
                var s1 = new SimulationServer(++Global.id, deleteData: true);
                model.AddServer(s1);

                s1.Restart(model);
                model.Advance(Server.ELECTION_TIMEOUT * 10);

                Assert.AreEqual(ServerState.Leader, s1.State, "Server failed to become leader");
            }
        }

        [TestMethod]
        public void Single_Server_Election2()
        {
            using (var model = new SimulationModel())
            {
                var s1 = new SimulationServer(++Global.id, deleteData: true);
                model.AddServer(s1);

                s1.Restart(model);
                model.Advance(Server.ELECTION_TIMEOUT * 10);

                Assert.AreEqual(ServerState.Leader, s1.State, "Server failed to become leader");
            }
        }
    }
}
