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


    public static class Helper
    {
        public static int id;

        public static Server CreateServer()
        {
            var sid = ++id;
            var port = sid + 7000;

            return new Server(new IPEndPoint(IPAddress.Loopback, port));
        }

    }

    class Program
    {
        static void DoTest(int count)
        {
            var transport = new MemoryTransport();
            var master = new Server(new IPEndPoint(IPAddress.Loopback, 7001));

            master.Initialize(new MemoryLog(), transport);
            master.ChangeState(new LeaderState(master));
            master.Advance();

            var servers = new List<Server>();
            servers.Add(master);

            while (count-- > 0)
            {
                var client = new Server(new IPEndPoint(IPAddress.Loopback, count + 7002));
                client.Initialize(new MemoryLog(), transport, true);
                client.ChangeState(new JoinState(client, new Client(client, master.ID)));
                servers.Add(client);
            }

            while (true)
            {
                foreach (var c in servers)
                    c.Advance();
                System.Threading.Thread.Sleep(0);
            }

        }

        static void Main(string[] args)
        {
            DoTest(3);
            Console.Read();

            var s1 = new Server(new IPEndPoint(IPAddress.Loopback, 7741));
            var s2 = new Server(new IPEndPoint(IPAddress.Loopback, 7742));

            var transport = new MemoryTransport();

            s1.Initialize(new MemoryLog(), transport, false, s2.ID);
            s2.Initialize(new MemoryLog(), transport, false, s1.ID);

            while (true)
            {
                s1.Advance();
                s2.Advance();
                System.Threading.Thread.Sleep(0);

                var leader = s1.CurrentState is LeaderState ? s1 : s2;

                if (leader.CurrentState is LeaderState && (leader.Tick % 1000) == 0)
                {
                    //Console.WriteLine("create");
                    leader.PersistedStore.Create(leader, new byte[] { (byte)leader.ID.GetHashCode() });
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
