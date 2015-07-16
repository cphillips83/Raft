using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Commands.ArgumentTypes;
using Raft.Logs;
using Raft.States;
using Raft.Transports;

namespace Raft.Commands
{
    public class IndexCommand : BaseCommand
    {
        private UIntArgument _index = new UIntArgument("-i=", "Index to dump", false);
        private StringArgument _dataDir = new StringArgument("data=", "Data directory storage", true);


        protected override void buildCommands()
        {
            this.IsCommand("index", "Dumps a given index or defaults to last");

            _commands.Add(_index);
            _commands.Add(_dataDir);
        }

        protected override int Process(string[] remainingArguments)
        {
            try
            {
                if (!System.IO.Directory.Exists(_dataDir.Value))
                {
                    Console.WriteLine("Data directory not found, aborting");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was an issue with the data directory '{0}'", ex.Message);
                return 1;
            }

            using (var log = new FileLog(_dataDir.Value))
            {
                log.Initialize();

                if (!_index.Supplied)
                    _index.Value = log.Length;

                var index = log[_index.Value];
                Console.WriteLine(index);
            }

            return 0;
        }
    }

}
