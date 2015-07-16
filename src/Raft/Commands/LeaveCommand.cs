using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class LeaveCommand : BaseCommand
    {
        private IPEndPointArg _ip = IPEndPointArg.CreateID();
        private StringArgument _dataDir = new StringArgument("data=", "Data directory storage", true);

        protected override void buildCommands()
        {
            this.IsCommand("leave", "Removes themselves from the cluster");

            _commands.Add(_ip);
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


            Console.WriteLine("Leaving cluster {0}", _ip.Value);
            using (var server = new Server(_ip.Value))
            {
                server.Initialize(new FileLog(_dataDir.Value, true), new UdpTransport());
                server.ChangeState(new LeaveState(server));

                var timer = Stopwatch.StartNew();
                var lastTick = 0L;
                while (!Console.KeyAvailable)
                {
                    var currentTick = timer.ElapsedMilliseconds;
                    while (lastTick < currentTick )
                    {
                        server.Advance();
                        lastTick++;
                    }
                    System.Threading.Thread.Sleep(1);

                    if(server.CurrentState is StoppedState)
                        break;
                }
            }


            return 0;
        }
    }
}
