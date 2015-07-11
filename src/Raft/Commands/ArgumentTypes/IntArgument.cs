using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft.Commands.ArgumentTypes
{
    public class IntArgument : AbstractArgument
    {
        public int Value;
        public int MinValue = int.MinValue;
        public int MaxValue = int.MaxValue;

        public IntArgument(string command, string desc, bool required = false)
            : base(command, desc, required)
        { }

        protected override bool parse(string s)
        {
            if (!int.TryParse(s, out Value) || Value < MinValue || Value > MaxValue)
            {
                Console.WriteLine("Integer is not a valid format (expected {0} to {1})", MinValue, MaxValue);
                return false;
            }

            return true;
        }
    }
}
