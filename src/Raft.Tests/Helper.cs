using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Tests
{
    public static class Helper
    {
        public static int id;

        public static Server CreateServer()
        {
            var sid = ++id;
            var port = sid + 7000;

            return new Server(new Configuration(sid, IPAddress.Loopback, port));
        }
    }
}
