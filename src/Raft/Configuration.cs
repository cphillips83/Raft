using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    public class Configuration 
    {
        private int _id, _port;
        private IPAddress _ip;

        public int ID { get { return _id; } }

        public IPAddress IP { get { return _ip; } }

        public int Port { get { return _port; } }

        public Configuration(int serverID, IPAddress ip, int port)
        {
            _id = serverID;
            _ip = ip;
            _port = port;
        }
    }
}
