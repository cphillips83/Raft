//using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

//namespace Raft.Tests
//{
//    public static class Helper
//    {
//        public static int id;

//        public static Server CreateServer()
//        {
//            var sid = ++id;
//            var dataDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "server\\" + sid);
//            if (System.IO.Directory.Exists(dataDir))
//                System.IO.Directory.Delete(dataDir, true);

//            return new Server(sid, dataDir);
//        }
//    }

//    [TestClass]
//    public class Test
//    {
//        [TestMethod]
//        public void AppendLogEntries()
//        {
//            using (var server = Helper.CreateServer())
//            {

//            }
//        }

//        [TestMethod]
//        public void StartsAsFollower()
//        {
//            using (var server = Helper.CreateServer())
//            {
//                server.Initialize();

//                //System.Threading.Thread.Sleep(200);
//                //server.Update();

//                Assert.IsTrue(server.CurrentState is FollowerState);
//            }
//        }

//        [TestMethod]
//        public void ConvertsToCandidate()
//        {
//            using (var server = Helper.CreateServer())
//            {
//                server.Initialize();

//                System.Threading.Thread.Sleep(200);
//                server.Update();

//                Assert.IsTrue(server.CurrentState is CandidateState);
//            }
//        }

//        [TestMethod]
//        public void BecomesLeader()
//        {
//            using (var server = Helper.CreateServer())
//            {
//                server.Initialize();

//                System.Threading.Thread.Sleep(200);
//                server.Update();
//                System.Threading.Thread.Sleep(200);
//                server.Update();

//                Assert.IsTrue(server.CurrentState is LeaderState);
//            }
//        }
//    }

//}
