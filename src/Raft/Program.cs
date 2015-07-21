using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using ManyConsole;
using Raft.Logs;
using Raft.Messages;
using Raft.States;
using Raft.Transports;

namespace Raft
{
    /*
     * File System
     *  WCF + Streaming Mode
     *  WCF server should write the file to HDD
     *  Server should reserve space and index for file
     *  
     * 
     */


    static class Program
    {
        public static void Write(this BinaryWriter bw, IPEndPoint ip)
        {
            if (ip == null)
                bw.Write((byte)0);
            else
            {
                var data = ip.Address.GetAddressBytes();
                bw.Write((byte)data.Length);
                bw.Write(data);
                bw.Write(ip.Port);
            }
        }

        public static IPEndPoint ReadIPEndPoint(this BinaryReader br)
        {
            var len = br.ReadByte();
            if (len == 0)
                return null;

            var bytes = br.ReadBytes(len);
            var port = br.ReadInt32();
            return new IPEndPoint(new IPAddress(bytes), port);
        }


        public static void ForAll<T>(this IEnumerable<T> array, Action<T> action)
        {
            foreach (var item in array)
                action(item);
        }

        //static int Main(string[] args)
        //{
        //    //var sw = Stopwatch.StartNew();
        //    //using (var fs = new FileStream("C:\\delete.txt", FileMode.Create))
        //    //{
        //    //    var data = new byte[1024 * 128];

        //    //    for (var i = 0; i < 1024;i++)
        //    //        fs.Write(data, 0, data.Length);

        //    //}
        //    //sw.Stop();

        //    //Console.WriteLine(sw.Elapsed);
        //    //Console.Read();

        //    try
        //    {
        //        var commands = new List<ConsoleCommand>();

        //        //need data directory
        //        commands.Add(new Commands.CreateCommand());
        //        commands.Add(new Commands.FollowCommand());
        //        commands.Add(new Commands.AgentCommand());
        //        commands.Add(new Commands.JoinCommand());
        //        commands.Add(new Commands.LeaveCommand());
        //        commands.Add(new Commands.IndexCommand());
        //        commands.Add(new Commands.UploadCommand());
        //        commands.Add(new Commands.DownloadCommand());

        //        // then run them.
        //        return ConsoleCommandDispatcher.DispatchCommand(commands as IEnumerable<ConsoleCommand>, args, Console.Out, true);
        //    }
        //    finally
        //    {
        //        Console.ResetColor();
        //        //Console.Read();
        //    }
        //}


        static void DoTest(int count)
        {
            var transport = new MemoryTransport(10, 15, 0);
            var master = new Server(new IPEndPoint(IPAddress.Loopback, 7001));

            master.Initialize(new MemoryLog(), transport);
            master.ChangeState(new LeaderState(master));
            master.PersistedStore.AddServer(master, master.ID);
            master.Advance();

            var servers = new List<Server>();
            servers.Add(master);


            while (count-- > 0)
            {
                var client = new Server(new IPEndPoint(IPAddress.Loopback, count + 7002));
                client.Initialize(new MemoryLog(), transport);
                client.ChangeState(new JoinState(client, new Client(client, master.ID)));
                servers.Add(client);
            }

            // a disruptive server
            //transport.SetPacketDropRate(servers[servers.Count - 1].ID, 0.2f);

            while (true)
            {
                foreach (var c in servers)
                    c.Advance();
                System.Threading.Thread.Sleep(10);
            }

        }

        //static void Main(string[] args)
        //{
        //    DoTest(3);
        //    Console.Read();

        //    var s1 = new Server(new IPEndPoint(IPAddress.Loopback, 7741));
        //    var s2 = new Server(new IPEndPoint(IPAddress.Loopback, 7742));

        //    var transport = new MemoryTransport();

        //    s1.Initialize(new MemoryLog(), transport, false, s2.ID);
        //    s2.Initialize(new MemoryLog(), transport, false, s1.ID);

        //    while (true)
        //    {
        //        s1.Advance();
        //        s2.Advance();
        //        System.Threading.Thread.Sleep(0);

        //        var leader = s1.CurrentState is LeaderState ? s1 : s2;

        //        if (leader.CurrentState is LeaderState && (leader.Tick % 1000) == 0)
        //        {
        //            //Console.WriteLine("create");
        //            leader.PersistedStore.Create(leader, new byte[] { (byte)leader.ID.GetHashCode() });
        //            System.Threading.Thread.Sleep(5);
        //        }
        //        System.Threading.Thread.Sleep(1);

        //        //if (Console.KeyAvailable)
        //        //{
        //        //    var key = Console.ReadKey().KeyChar;
        //        //    switch (key)
        //        //    {
        //        //        case 'a':
        //        //    }
        //        //}
        //    }


        //    Console.Read();
        //}

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
