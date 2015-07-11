using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManyConsole;

namespace Raft.Commands.ArgumentTypes
{
    public interface IArgument
    {
        bool Required { get; }
        bool Supplied { get; }
        bool Valid { get; }
        void Register(ConsoleCommand command);
    }

}
