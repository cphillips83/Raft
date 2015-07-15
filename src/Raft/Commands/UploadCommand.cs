using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    public class UploadCommand : BaseCommand
    {
        private IPEndPointArg _agentip = IPEndPointArg.CreateAgentIP();
        private StringArgument _file = new StringArgument("file=", "File to upload", true);

        protected override void buildCommands()
        {
            this.IsCommand("upload", "Uploads a file to the cluster");

            //_commands.Add(_ip);
            _commands.Add(_agentip);
            _commands.Add(_file);
        }

        protected override int Process(string[] remainingArguments)
        {
            try
            {
                if(!System.IO.File.Exists(_file.Value))
                {
                    Console.WriteLine("Could not find file '{0}' to upload", _file.Value);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was an issue with the file '{0}'", ex.Message);
                return 1;
            }


            Console.WriteLine("Uploading file to cluster");
            var index = 0u;

            var timer = Stopwatch.StartNew();
            var count = 0;
            for (var i = 0; i < 100; i++)
                System.Threading.ThreadPool.QueueUserWorkItem(x =>
                {
                    var proxy = ClientFactory.CreateClient<IDataService>(_agentip.Value);

                    using (var fs = new FileStream(_file.Value, FileMode.Open, FileAccess.Read))
                        index = proxy.UploadFile(fs);

                    Console.WriteLine("Uploaded, index: {0}", index);
                    ++count;
                });

            while (count < 100)
                System.Threading.Thread.Sleep(0);

            timer.Stop();
            Console.WriteLine("Took: {0}", timer.Elapsed);

            return 0;
        }
    }
}
