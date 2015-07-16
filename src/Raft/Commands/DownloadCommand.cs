using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ManyConsole;
using Raft.API;
using Raft.Commands.ArgumentTypes;
using Raft.Logs;
using Raft.States;
using Raft.Transports;

namespace Raft.Commands
{
    public class DownloadCommand : BaseCommand
    {
        private IPEndPointArg _agentip = IPEndPointArg.CreateAgentIP();
        private UIntArgument _index = new UIntArgument("index=", "Index of the file to download", true);
        private StringArgument _file = new StringArgument("file=", "File to store the data", true);

        protected override void buildCommands()
        {
            this.IsCommand("download", "Downloads a file from the cluster");

            //_commands.Add(_ip);
            _commands.Add(_agentip);
            _commands.Add(_index);
            _commands.Add(_file);
        }

        protected override int Process(string[] remainingArguments)
        {
            try
            {
                if(System.IO.File.Exists(_file.Value))
                {
                    Console.WriteLine("File '{0}' already exists", _file.Value);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was an issue with the file '{0}'", ex.Message);
                return 1;
            }


            Console.WriteLine("Downloading file from cluster");
            var proxy = Agent.CreateClient<IDataService>(_agentip.Value);
            using (var fs = new FileStream(_file.Value, FileMode.CreateNew, FileAccess.Write))
            {
                using (var remoteStream = proxy.DownloadFile(new FileIndex() { Index = _index.Value }))
                {
                    int count = 0;
                    var data = new byte[65536];
                    while ((count = remoteStream.Stream.Read(data, 0, data.Length)) > 0)
                    {
                        fs.Write(data, 0, count);
                    }
                }
                Console.WriteLine("Wrote {0} bytes to file '{1}'", fs.Length, _file.Value);
            }


            return 0;
        }
    }
}
