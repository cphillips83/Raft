using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ManyConsole;
using Raft.Commands.ArgumentTypes;
using Raft.Logs;
using Raft.States;
using Raft.Transports;

namespace Raft.Commands
{
    public class InitCommand : BaseCommand
    {
        private IPEndPointArg _ip = IPEndPointArg.CreateID();
        private StringArgument _dataDir = new StringArgument("data=", "Data directory storage", true);

        private int _initialSize, _maxSize;

        protected override void buildCommands()
        {
            this.IsCommand("init", "Initializes a new cluster with this being the first server in the cluster");

            _commands.Add(_ip);
            _commands.Add(_dataDir);
            ////this.HasOption("","",new NDesk.Options.OptionAction<TKey,TValue>())
            //this.HasOption<int>("initialIndexSize", "", null);
            //this.HasOption<int>("initialDataSize", "initial size of the data file", s => _initialSize = s);
            //this.HasOption<int>("maxDataSize", "max storage size of the data file", s => _maxSize = s);
        }

        protected override int Process(string[] remainingArguments)
        {
            try
            {
                if (System.IO.Directory.Exists(_dataDir.Value))
                {
                    if (System.IO.Directory.GetFiles(_dataDir.Value).Any())
                    {
                        Console.WriteLine("Data directory was not empty");
                        return 1;
                    }
                }
                else
                {
                    System.IO.Directory.CreateDirectory(_dataDir.Value);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("There was an issue with the data directory '{0}'", ex.Message);
                return 1;
            }

            using (var server = new Server(_ip.Value))
            {
                server.Initialize(new FileLog(_dataDir.Value), Transport.NULL);
                server.ChangeState(new LeaderState(server));

                server.Advance();
            }

            Console.WriteLine("Initialized '{0}'", _dataDir.Value);

            return 0;
        }
    }
}
