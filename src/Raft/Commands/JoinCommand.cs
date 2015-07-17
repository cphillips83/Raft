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
    public class JoinCommand : BaseCommand
    {
        private IPEndPointArg _ip = IPEndPointArg.CreateID();
        private IPEndPointArg _leader = IPEndPointArg.CreateLeaderID();
        private StringArgument _dataDir = new StringArgument("data=", "Data directory storage", true);


        protected override void buildCommands()
        {
            this.IsCommand("join", "Initializes a new cluster with this being the first server in the cluster");

            _commands.Add(_ip);
            _commands.Add(_leader);
            _commands.Add(_dataDir);
        }

        protected override int Process(string[] remainingArguments)
        {
            try
            {
                if (!System.IO.Directory.Exists(_dataDir.Value))
                    System.IO.Directory.CreateDirectory(_dataDir.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was an issue with the data directory '{0}'", ex.Message);
                return 1;
            }


            Console.WriteLine("Trying to join {0}", _leader.Value);
            using (var server = new Server(_ip.Value))
            {
                server.Initialize(new FileLog(_dataDir.Value, true), new UdpTransport());
                server.ChangeState(new JoinState(server, new Client(server, _leader.Value)));

                var timer = Stopwatch.StartNew();
                var lastTick = 0L;
                while (!Console.KeyAvailable && server.CurrentState is JoinState)
                {
                    var currentTick = timer.ElapsedMilliseconds;
                    while (lastTick < currentTick)
                    {
                        server.Advance();
                        lastTick++;
                    }
                    System.Threading.Thread.Sleep(1);
                }
            }


            return 0;
        }
    }
}
