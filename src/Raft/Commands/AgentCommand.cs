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
    public class AgentCommand : BaseCommand
    {
        private IPEndPointArg _ip = IPEndPointArg.CreateID();
        private IPEndPointArg _agentip = IPEndPointArg.CreateAgentIP();
        private StringArgument _dataDir = new StringArgument("data=", "Data directory storage", true);


        protected override void buildCommands()
        {
            this.IsCommand("agent", "Starts the service as a follower and loads the agent");

            _commands.Add(_ip);
            _commands.Add(_agentip);
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

            using (var server = new Server(_ip.Value))
            {
                server.Initialize(new FileLog(_dataDir.Value), new UdpTransport());

                Console.WriteLine("Running on {0}, press any key to quit...", server.ID);
                
                var agent = new Agent(server);
                agent.Run(_agentip.Value);
            }


            return 0;
        }
    }

}
