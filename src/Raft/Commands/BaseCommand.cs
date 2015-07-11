using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManyConsole;
using Raft.Commands.ArgumentTypes;

namespace Raft.Commands
{
    public abstract class BaseCommand : ConsoleCommand
    {
        protected List<IArgument> _commands = new List<IArgument>();

        protected BaseCommand()
        {
            this.SkipsCommandSummaryBeforeRunning();

            buildCommands();

            foreach (var cmd in _commands)
                cmd.Register(this);
        }

        protected abstract void buildCommands();

        public override int Run(string[] remainingArguments)
        {
            foreach (var cmd in _commands)
                if ((cmd.Required && !cmd.Valid) || (cmd.Supplied && !cmd.Valid))
                    return 1;
          
            return Process(remainingArguments);
        }

        protected abstract int Process(string[] remainingArguments);
    }
}
