using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ManyConsole;

namespace Raft.Commands
{

    public abstract class Argument
    {
        public string Command, Description;
        public bool Required = false;
        public bool Supplied;
        public bool Parsed;

        public Argument(string command, string desc, bool required = false)
        {
            Command = command;
            Description = desc;
            Required = required;
        }

        public void Parse(string s)
        {
            Supplied = true;
            Parsed = parse(s);
        }

        protected abstract bool parse(string s);

        public void Register(ConsoleCommand cmd)
        {
            if (Required)
                cmd.HasRequiredOption(Command, Description, Parse);
            else
                cmd.HasOption(Command, Description, Parse);
        }
    }



    public class IPArgument : Argument
    {
        public IPAddress Value;
        public IPArgument(string command, string desc, bool required = false)
            : base(command, desc, required)
        { }

        protected override bool parse(string s)
        {
            return IPAddress.TryParse(s, out Value);
        }
    }

    public abstract class IDCommand : ConsoleCommand
    {
        protected bool parseSuccess = true;

        protected IPArgument _ip = new IPArgument("ip=", "IP address to listen on, also identifies the server (CAN'T CHANGE)", true);
        protected int _port;
        protected string _dataDir;

        protected List<Argument> _commands = new List<Argument>();

        protected IDCommand()
        {
            _commands.Add(_ip);

            this.HasRequiredOption<int>("port=", "Port to listen on, forms unique ID combined with IP", s => _port = s);
            this.HasRequiredOption("dataDir=", "Folder to store raft data", s => _dataDir = s);

            buildCommands();

            foreach (var cmd in _commands)
                cmd.Register(this);

        }

        protected abstract void buildCommands();

        public override int Run(string[] remainingArguments)
        {
            if (!_ip.Parsed)
                Console.WriteLine("failed to parse ip");

            if (_port < 1 || _port > 65536)
            {
                Console.WriteLine("port is expected to be between 1 and 65536");
                return 1;
            }

            return Process(remainingArguments);
        }

        protected abstract int Process(string[] remainingArguments);

        //protected void parseIP(string s)
    }

    public class Init : IDCommand
    {
        private int _initialSize, _maxSize;

        protected override void buildCommands()
        {
            this.SkipsCommandSummaryBeforeRunning();

            this.IsCommand("init", "Initializes a new cluster with this being the first server in the cluster");

            this.HasOption<int>("initialSize", "initial size of the data file", s => _initialSize = s);
            this.HasOption<int>("maxSize", "max storage size of the data file", s => _maxSize = s);
        }

        protected override int Process(string[] remainingArguments)
        {

            Console.WriteLine(_ip.Value);


            return 0;
        }
    }
}
