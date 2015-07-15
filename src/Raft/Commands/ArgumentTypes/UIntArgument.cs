using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Commands.ArgumentTypes
{
    public class UIntArgument : AbstractArgument
    {
        public uint Value;
        public uint MinValue = uint.MinValue;
        public uint MaxValue = uint.MaxValue;

        public UIntArgument(string command, string desc, bool required = false)
            : base(command, desc, required)
        { }

        protected override bool parse(string s)
        {
            if (!uint.TryParse(s, out Value) || Value < MinValue || Value > MaxValue)
            {
                Console.WriteLine("Unsigned integer is not a valid format (expected {0} to {1})", MinValue, MaxValue);
                return false;
            }

            return true;
        }
    }
}
