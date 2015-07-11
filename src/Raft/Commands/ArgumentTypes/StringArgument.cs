using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Commands.ArgumentTypes
{
    public class StringArgument : AbstractArgument
    {
        public string Value;

        public StringArgument(string command, string desc, bool required = false)
            : base(command, desc, required)
        { }

        protected override bool parse(string s)
        {
            Value = s;
            return true;
        }
    }
}
