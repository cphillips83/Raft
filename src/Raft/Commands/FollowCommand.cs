using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raft.Commands.ArgumentTypes;
using Raft.Logs;
using Raft.States;
using Raft.Transports;

namespace Raft.Commands
{
    public class FollowCommand : BaseCommand
    {
        private IPEndPointArg _ip = IPEndPointArg.CreateID();
        private StringArgument _dataDir = new StringArgument("data=", "Data directory storage", true);

        private int _initialSize, _maxSize;

        protected override void buildCommands()
        {
            this.IsCommand("follow", "Take part in the server cluster as an initial follower");

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

            using (var server = new Server(_ip.Value))
            {
                server.Initialize(new FileLog(_dataDir.Value), new LidgrenTransport());

                Console.WriteLine("Running on {0}, press any key to quit...", server.ID);
                
                while (!Console.KeyAvailable)
                {
                    server.Advance();
                    System.Threading.Thread.Sleep(0);
                }
            }


            return 0;
        }
    }

}
