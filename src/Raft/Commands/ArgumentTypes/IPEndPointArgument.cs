using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Commands.ArgumentTypes
{
    public class IPEndPointArg : AbstractArgument
    {
        public IPEndPoint Value;

        public IPEndPointArg(string command, string desc, bool required = false)
            : base(command, desc, required)
        { }

        protected override bool parse(string s)
        {
            IPAddress ip;
            int port;
            var args = s.Split(':');
            if (args.Length != 2 || !IPAddress.TryParse(args[0], out ip))
            {
                Console.WriteLine("{0} is not a valid ip format (expected XXX.XXX.XXX.XXX:PORT)", Command);
                return false;
            }

            if (!int.TryParse(args[1], out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                Console.WriteLine("{0} has an invalid port range (expected {1}-{2})", IPEndPoint.MinPort, IPEndPoint.MaxPort);
                return false;
            }

            Value = new IPEndPoint(ip, port);
            return true;
        }

        public static IPEndPointArg CreateID()
        {
            return new IPEndPointArg("ip=", "End point (IP:PORT) for the server, can not be changed and is used as the ID", true);
        }

        public static IPEndPointArg CreateLeaderID()
        {
            return new IPEndPointArg("leader=", "Leader end point (IP:PORT) for the server to join", true);
        }
    }

}
