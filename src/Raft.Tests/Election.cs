using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Raft.Tests
{
    public static class Helper
    {
        public static int id;

        public static Server CreateServer()
        {
            var sid = ++id;
            var dataDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "server\\" + sid);
            if (System.IO.Directory.Exists(dataDir))
                System.IO.Directory.Delete(dataDir, true);

            return new Server(sid, dataDir);

        }
    }

    [TestClass]
    public class Test
    {
        [TestMethod]
        public void AppendLogEntries()
        {
            using (var server = Helper.CreateServer())
            {

            }
        }

        [TestMethod]
        public void StartsAsFollower()
        {
            using (var server = Helper.CreateServer())
            {
                server.Initialize();

                //System.Threading.Thread.Sleep(200);
                //server.Update();

                Assert.IsTrue(server.CurrentState is FollowerState);
            }
        }

        [TestMethod]
        public void ConvertsToCandidate()
        {
            using (var server = Helper.CreateServer())
            {
                server.Initialize();

                System.Threading.Thread.Sleep(200);
                server.Update();

                Assert.IsTrue(server.CurrentState is CandidateState);
            }
        }

        [TestMethod]
        public void BecomesLeader()
        {
            using (var server = Helper.CreateServer())
            {
                server.Initialize();

                System.Threading.Thread.Sleep(200);
                server.Update();
                System.Threading.Thread.Sleep(200);
                server.Update();

                Assert.IsTrue(server.CurrentState is LeaderState);
            }
        }
    }

    //[TestClass]
    //public class Election2
    //{

    //    [TestMethod]
    //    public void Join_Single_Server()
    //    {
    //        using (var model = new SimulationModel())
    //        {
    //            var s1 = new SimulationServer(++Global.id, deleteData: true);
    //            model.AddServer(s1);

    //            s1.Restart(model);
    //            model.Advance(ServerOld.ELECTION_TIMEOUT * 10);

    //            Assert.AreEqual(ServerState.Leader, s1.State, "Server failed to become leader");
    //        }
    //    }
    //}

    //[TestClass]
    //public class Election
    //{
    //    [TestMethod]
    //    public void Single_Server_Election()
    //    {
    //        using (var model = new SimulationModel())
    //        {
    //            var s1 = new SimulationServer(++Global.id, deleteData: true);
    //            model.AddServer(s1);

    //            s1.Restart(model);
    //            model.Advance(ServerOld.ELECTION_TIMEOUT * 10);

    //            Assert.AreEqual(ServerState.Leader, s1.State, "Server failed to become leader");
    //        }
    //    }

    //    [TestMethod]
    //    public void Single_Server_Election2()
    //    {
    //        using (var model = new SimulationModel())
    //        {
    //            var s1 = new SimulationServer(++Global.id, deleteData: true);
    //            model.AddServer(s1);

    //            s1.Restart(model);
    //            model.Advance(ServerOld.ELECTION_TIMEOUT * 10);

    //            Assert.AreEqual(ServerState.Leader, s1.State, "Server failed to become leader");
    //        }
    //    }
    //}
}
