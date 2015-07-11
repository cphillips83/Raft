using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Commands.ArgumentTypes
{
    public class IPArgument : AbstractArgument
    {
        public IPAddress Value;
        public IPArgument(string command, string desc, bool required = false)
            : base(command, desc, required)
        { }

        protected override bool parse(string s)
        {
            if (!IPAddress.TryParse(s, out Value))
            {
                Console.WriteLine("IPAddress is in an invalid format (expect X.X.X.X).");
                return false;
            }

            return true;
        }
    }
}
