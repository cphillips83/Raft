using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Raft.Logs;
using Raft.Transports;

namespace Raft.Tests
{
    public static class Helper
    {
        public static int id;

        public static Server CreateServer()
        {
            var sid = ++id;
            var port = sid + 7000;

            return new Server(new Configuration(sid.ToString(), IPAddress.Loopback, port));
        }


        public static Server[] CreateServers(int count)
        {
            var servers = new Server[count];
            var transport = new MemoryTransport();
            for (var i = 0; i < servers.Length; i++)
                servers[i] = Helper.CreateServer();

            for (var i = 0; i < servers.Length; i++)
                servers[i].Initialize(new MemoryLog(), transport,
                        servers.Where(x => x.ID != servers[i].ID)
                               .Select(x => x.Config).ToArray()
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
