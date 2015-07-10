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
        //private string _id;
        private int _port;
        private IPAddress _ip;
        private IPEndPoint _endPoint;

        //public string ID { get { return _id; } }

        public IPAddress IP { get { return _ip; } }

        public int Port { get { return _port; } }

        //public IPEndPoint EndPoint { get { return _endPoint; } }

        public Configuration(/*string serverID,*/ IPAddress ip, int port)
        {
            //_id = serverID;
            _ip = ip;
            _port = port;
            _endPoint = new IPEndPoint(ip, port);
        }
    }
}
