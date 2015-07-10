using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Logs;
using Raft.Transports;

namespace Raft.Tests
{
#if DEBUG
    public class DebugWriter : TextWriter
    {
        //save static reference to stdOut
        static TextWriter stdOut = Console.Out;

        static DebugWriter()
        {
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("");
        }

        public override void WriteLine(string value)
        {
            System.Diagnostics.Debug.WriteLine(value);
            stdOut.WriteLine(value);
            base.WriteLine(value);
        }

        public override void Write(string value)
        {
            System.Diagnostics.Debug.Write(value);
            stdOut.Write(value);
            base.Write(value);
        }

        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }
    }
#endif

    public static class Helper
    {
        public static int id;

        public static Server CreateServer()
        {
            var sid = ++id;
            var port = sid + 7000;

            return new Server(new IPEndPoint(IPAddress.Loopback, port));
        }


        public static Server[] CreateServers(int count)
        {
            var servers = new Server[count];
            var transport = new MemoryTransport();
            for (var i = 0; i < servers.Length; i++)
                servers[i] = Helper.CreateServer();

            for (var i = 0; i < servers.Length; i++)
                servers[i].Initialize(new MemoryLog(), transport,
                        servers.Where(x => !x.ID.Equals(servers[i].ID))
                               .Select(x => x.ID).ToArray()
                    );

            return servers;
        }

        public static void CleanupServers(Server[] servers)
        {
            for (var i = 0; i < servers.Length; i++)
            {
                servers[i].Dispose();
                servers[i] = null;
            }
        }
    }
}
