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
using Raft.Messages;
using Raft.Settings;
using Raft.States;

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

            var s1 = new Server(new TestNodeSettings(1, 7741));
            var s2 = new Server(new TestNodeSettings(2, 7742));

            var c1 = new Client(s2, 1, new IPEndPoint(IPAddress.Loopback, 7741));
            var c2 = new Client(s1, 2, new IPEndPoint(IPAddress.Loopback, 7742));

            s1._clients.Add(c2);
            s2._clients.Add(c1);

            c1.Initialize();
            c2.Initialize();

            s1.Initialize();
            s2.Initialize();

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

        static void TestConsole()
        {
            var running = true;
            var model = SimulationModel.SetupFreshScenario();
            while (running)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey();
                    switch (key.KeyChar)
                    {
                        case 'x': running = false; break;
                        case 'k':
                            {
                                var leader = model.GetLeader();
                                if (leader != null)
                                    leader.Stop(model);
                            }
                            break;
                        case 'u':
                            model.ResumeAllStopped();
                            break;
                        case 'r': model.ClientRequest(); break;
                        case 'a':
                            {
                                //add server to cluster
                                var leader = model.GetLeader();
                                if (leader != null)
                                    model.JoinServer(leader);
                            }
                            break;
                    }
                }
                model.Advance();
                System.Threading.Thread.Sleep(1);
            }
        }

    }

}
