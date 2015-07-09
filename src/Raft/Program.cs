using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using Raft.Logs;
using Raft.Messages;
using Raft.States;
using Raft.Transports;

namespace Raft
{
    //Haystack tech talk http://www.downvids.net/haystack-tech-talk-4-28-2009--375887.html

    /* Design Ideas
     *  Modeling after haystack
     *  
     * Data Store
     *  Each store will have several physical volumes
     *  Each physical volume can be in multiple logical volumes
     *  Logical volumes can span across data centers
     *  
     * 
     */


    class Program
    {
        static void Main(string[] args)
        {
            var dataDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "server");
            if (System.IO.Directory.Exists(dataDir))
                System.IO.Directory.Delete(dataDir, true);


            var s1 = new Server(new Configuration(1, IPAddress.Loopback, 7741));
            var s2 = new Server(new Configuration(2, IPAddress.Loopback, 7742));

            var transport = new MemoryTransport();

            s1.Initialize(new MemoryLog(), transport, s2.Config);
            s2.Initialize(new MemoryLog(), transport, s1.Config);

            while (true)
            {
                s1.Advance();
                s2.Advance();
                System.Threading.Thread.Sleep(0);

                var leader = s1.CurrentState is LeaderState ? s1 : s2;

                if (leader.CurrentState is LeaderState && (leader.Tick % 1000) == 0)
                {
                    //Console.WriteLine("create");
                    leader.PersistedStore.Create(new byte[] { (byte)leader.ID });
                    System.Threading.Thread.Sleep(5);
                }
                System.Threading.Thread.Sleep(1);

                //if (Console.KeyAvailable)
                //{
                //    var key = Console.ReadKey().KeyChar;
                //    switch (key)
                //    {
                //        case 'a':
                //    }
                //}
            }


            Console.Read();
        }

        //static void TestConsole()
        //{
        //    var running = true;
        //    var model = SimulationModel.SetupFreshScenario();
        //    while (running)
        //    {
        //        if (Console.KeyAvailable)
        //        {
        //            var key = Console.ReadKey();
        //            switch (key.KeyChar)
        //            {
        //                case 'x': running = false; break;
        //                case 'k':
        //                    {
        //                        var leader = model.GetLeader();
        //                        if (leader != null)
        //                            leader.Stop(model);
        //                    }
        //                    break;
        //                case 'u':
        //                    model.ResumeAllStopped();
        //                    break;
        //                case 'r': model.ClientRequest(); break;
        //                case 'a':
        //                    {
        //                        //add server to cluster
        //                        var leader = model.GetLeader();
        //                        if (leader != null)
        //                            model.JoinServer(leader);
        //                    }
        //                    break;
        //            }
        //        }
        //        model.Advance();
        //        System.Threading.Thread.Sleep(1);
        //    }
        //}

    }

}
